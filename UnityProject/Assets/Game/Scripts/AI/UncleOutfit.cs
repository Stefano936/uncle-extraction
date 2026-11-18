using UnityEngine;
using UnityEngine.Rendering;

namespace UncleExtraction
{
    /// Detalle de ropa del tio (sesion 15): la malla del kit es de bloques; se suman piezas pequeñas que dan volumen y
    /// costuras sin tocar el rig: cuello de camisa abierto, bolsillo con costura, botones, ruedo, cinturon con hebilla y
    /// vivos en las mangas. Se anclan a la parte que corresponde (pecho, pelvis, brazos) y siguen la fisica. Coordenadas
    /// del generador del kit (sources/generate_kit.py) en el marco de la CARA (+z adelante, y arriba, en metros).
    /// Solo presentacion: sin colision; los puntos de agarre y las zonas de cierre no cambian.
    public class UncleOutfit : MonoBehaviour
    {
        NpcBody body; float fs;
        public int Pieces { get; private set; }

        void Start()
        {
            body = GetComponent<NpcBody>(); if (!body) { enabled = false; return; }
            fs = body.FaceSign;
            Material shirt = Find("Uncle_Shirt"), shorts = Find("Uncle_Shorts"), hair = Find("Uncle_Hair");
            var shirtDark = Tint(shirt, 0.62f, "Shirt_Seam"); var shirtLight = Tint(shirt, 1.25f, "Shirt_Light");
            var leather = Solid("Belt_Leather", new Color(0.22f, 0.12f, 0.06f), 0.45f, 0f);
            var brass = Solid("Belt_Buckle", new Color(0.86f, 0.68f, 0.3f), 0.75f, 0.85f);
            var button = Solid("Shirt_Button", new Color(0.93f, 0.9f, 0.82f), 0.6f, 0f);
            Transform chest = Part("chest"), pelvis = transform, armL = Part("upper_arm.L"), armR = Part("upper_arm.R");
            if (!chest) chest = transform;
            // cuello abierto (dos solapas inclinadas)
            for (int s = -1; s <= 1; s += 2) Piece(chest, "Collar", new Vector3(0.12f * s, 1.455f, 0.15f), new Vector3(0.17f, 0.045f, 0.11f), Quaternion.Euler(-28f, 0, 24f * s), shirt);
            // bolsillo y su costura superior (lado izquierdo del tio)
            Piece(chest, "Pocket", new Vector3(0.3f, 1.31f, 0.238f), new Vector3(0.105f, 0.115f, 0.014f), Quaternion.identity, shirtDark);
            Piece(chest, "Pocket_Stitch", new Vector3(0.3f, 1.365f, 0.247f), new Vector3(0.105f, 0.008f, 0.01f), Quaternion.identity, shirtLight);
            // botones en los bordes de la camisa abierta y ruedo con costura
            foreach (float y in new[] { 1.34f, 1.2f, 1.07f }) Piece(chest, "Button", new Vector3(-0.185f, y, 0.238f), new Vector3(0.028f, 0.028f, 0.012f), Quaternion.identity, button, PrimitiveType.Sphere);
            for (int s = -1; s <= 1; s += 2) Piece(chest, "Hem", new Vector3(0.3f * s, 1.035f, 0.157f), new Vector3(0.235f, 0.014f, 0.168f), Quaternion.identity, shirtDark);
            // cinturon (sobre la cintura del short) y hebilla
            Piece(pelvis, "Belt", new Vector3(0f, 0.955f, 0f), new Vector3(0.655f, 0.055f, 0.42f), Quaternion.identity, leather);
            Piece(pelvis, "Buckle", new Vector3(0f, 0.955f, 0.212f), new Vector3(0.08f, 0.06f, 0.018f), Quaternion.identity, brass);
            // vivo al final de cada manga corta (anillo oscuro)
            if (armL) Piece(armL, "Sleeve_Hem", new Vector3(0.48f, 1.27f, 0f), new Vector3(0.29f, 0.03f, 0.29f), Quaternion.Euler(0, 0, 54f), shirtDark, PrimitiveType.Cylinder);
            if (armR) Piece(armR, "Sleeve_Hem", new Vector3(-0.48f, 1.27f, 0f), new Vector3(0.29f, 0.03f, 0.29f), Quaternion.Euler(0, 0, -54f), shirtDark, PrimitiveType.Cylinder);
        }

        Transform Part(string role) { foreach (var p in body.parts) if (p && p.role == role) return p.transform; return null; }

        /// Posicion/rotacion en el marco de la cara -> local de la parte (medido en reposo, al empezar).
        void Piece(Transform anchor, string name, Vector3 facePos, Vector3 size, Quaternion faceRot, Material m, PrimitiveType type = PrimitiveType.Cube)
        {
            if (!anchor || !m) return;
            Vector3 rootLocal = new Vector3(facePos.x * fs, facePos.y, facePos.z * fs);
            Quaternion rootRot = (fs < 0f ? Quaternion.Euler(0, 180f, 0) : Quaternion.identity) * faceRot;
            var g = GameObject.CreatePrimitive(type); g.name = "Outfit_" + name;
            var c = g.GetComponent<Collider>(); if (c) { c.enabled = false; Destroy(c); }
            g.transform.SetParent(anchor, true);
            g.transform.position = transform.TransformPoint(rootLocal);
            g.transform.rotation = transform.rotation * rootRot;
            Vector3 ls = anchor.lossyScale; g.transform.localScale = new Vector3(size.x / Mathf.Max(1e-4f, ls.x), size.y / Mathf.Max(1e-4f, ls.y), size.z / Mathf.Max(1e-4f, ls.z));
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.On;
            Pieces++;
        }

        Material Find(string prefix) { foreach (var r in GetComponentsInChildren<Renderer>()) foreach (var m in r.sharedMaterials) if (m && m.name.StartsWith(prefix)) return m; return null; }
        static Material Tint(Material src, float k, string n)
        {
            if (!src) return Solid(n, new Color(0.5f * k, 0.1f, 0.1f), 0.2f, 0f);
            // sesion 17: NO se copia el material del proyecto con new Material(src): en el editor esa copia terminaba
            // reescribiendo Uncle_Shirt.mat al correr PlayMode (perdia _EMISSION). Se arma desde el shader y se copian solo
            // las propiedades que importan; el asset no se toca.
            var m = new Material(src.shader) { name = n }; var c = src.color;
            foreach (var p in new[] { "_BaseMap", "_BumpMap" }) if (src.HasProperty(p) && m.HasProperty(p)) { m.SetTexture(p, src.GetTexture(p)); m.SetTextureScale(p, src.GetTextureScale(p)); }
            foreach (var p in new[] { "_Smoothness", "_Metallic", "_BumpScale" }) if (src.HasProperty(p) && m.HasProperty(p)) m.SetFloat(p, src.GetFloat(p));
            if (src.IsKeywordEnabled("_NORMALMAP")) m.EnableKeyword("_NORMALMAP");
            m.color = new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a); return m;
        }
        static Material Solid(string n, Color c, float smooth, float metal)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit"); var m = new Material(sh ? sh : Shader.Find("Standard")) { name = n, color = c };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth); if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal); return m;
        }
    }
}
