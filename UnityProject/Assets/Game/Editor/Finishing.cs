using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UncleExtraction.EditorTools
{
    /// Segunda pasada de pulido (sesion 11): materiales del tio, gorro de fiesta, carrito propio y relieve de texturas.
    /// Todo visual: rig, huesos, puntos de agarre, colliders, masas y dimensiones funcionales no cambian.
    public static class Finishing
    {
        const string MatDir = "Assets/Game/Art/Materials/Finish";
        const string TexDir = "Assets/Game/Art/Materials/Textures";

        // ---------- materiales
        public static Material Lit(string name, Color c, float smooth, float metal = 0f, Texture2D tex = null, Texture2D normal = null, float bump = 0.5f, Vector2? tiling = null, bool emissionReady = false)
        {
            Directory.CreateDirectory(MatDir);
            string p = $"{MatDir}/{name}.mat";
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
            m.shader = sh; m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            m.SetTexture("_BaseMap", tex); if (tiling.HasValue) m.SetTextureScale("_BaseMap", tiling.Value);
            if (normal) { m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP"); if (tiling.HasValue) m.SetTextureScale("_BumpMap", tiling.Value); }
            else { m.SetTexture("_BumpMap", null); m.DisableKeyword("_NORMALMAP"); }
            // el tio senaliza preparacion con un brillo (bloque de propiedades); el material lo admite con emision negra por defecto
            if (emissionReady) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
            EditorUtility.SetDirty(m); return m;
        }

        /// Mapa de normales a partir del brillo de una textura de color (relieve sutil: juntas, vetas, trama). Se guarda como PNG.
        public static Texture2D NormalFrom(string srcPath, string name, float strength)
        {
            string dst = $"{TexDir}/Generated/{name}_normal.png";
            if (!File.Exists(dst) && File.Exists(srcPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                var src = new Texture2D(2, 2); src.LoadImage(File.ReadAllBytes(srcPath));
                int w = Mathf.Min(src.width, 512), h = Mathf.Min(src.height, 512);
                var rt = RenderTexture.GetTemporary(w, h); Graphics.Blit(src, rt);
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var small = new Texture2D(w, h, TextureFormat.RGB24, false); small.ReadPixels(new Rect(0, 0, w, h), 0, 0); small.Apply();
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                float H(int x, int y) { x = (x + w) % w; y = (y + h) % h; return small.GetPixel(x, y).grayscale; }
                var n = new Texture2D(w, h, TextureFormat.RGB24, false);
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    float dx = (H(x + 1, y) - H(x - 1, y)) * strength, dy = (H(x, y + 1) - H(x, y - 1)) * strength;
                    var v = new Vector3(-dx, -dy, 1f).normalized;
                    n.SetPixel(x, y, new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f));
                }
                n.Apply(); File.WriteAllBytes(dst, n.EncodeToPNG());
                Object.DestroyImmediate(src); Object.DestroyImmediate(small); Object.DestroyImmediate(n);
                AssetDatabase.ImportAsset(dst);
                var ti = (TextureImporter)AssetImporter.GetAtPath(dst);
                ti.textureType = TextureImporterType.NormalMap; ti.wrapMode = TextureWrapMode.Repeat; ti.anisoLevel = 4; ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
        }

        static Texture2D Tex(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{file}");

        /// Relieve para los materiales del kit (madera, baldosas, revoque) y los generados (asfalto, losetas).
        public static void ReliefForLevel()
        {
            void Add(string mat, string tex, float strength, float bump)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(mat); if (!m) return;
                var nrm = NormalFrom($"{TexDir}/{tex}", Path.GetFileNameWithoutExtension(tex), strength); if (!nrm) return;
                m.SetTexture("_BumpMap", nrm); m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP");
                m.SetTextureScale("_BumpMap", m.GetTextureScale("_BaseMap")); EditorUtility.SetDirty(m);
            }
            Add("Assets/Game/Art/Materials/MAT_Wood_Boards.mat", "01_wood_basecolor.png", 6f, 0.6f);
            Add("Assets/Game/Art/Materials/MAT_Floor_Tiles.mat", "02_floor_basecolor.png", 5f, 0.5f);
            Add("Assets/Game/Art/Materials/MAT_Wall_Taupe.mat", "03_wall_basecolor.png", 8f, 0.45f);
            Add("Assets/Game/Art/Materials/Bar/Asphalt.mat", "Generated/asphalt.png", 6f, 0.7f);
            Add("Assets/Game/Art/Materials/Bar/Concrete_Slabs.mat", "Generated/concrete_slabs.png", 7f, 0.6f);
            AssetDatabase.SaveAssets();
        }

        // ---------- tio
        public static void Uncle(GameObject kit, Transform head)
        {
            var fabric = Tex("04_fabric_basecolor.png"); var fabricN = NormalFrom($"{TexDir}/04_fabric_basecolor.png", "04_fabric_basecolor", 5f);
            var map = new System.Collections.Generic.Dictionary<string, Material>
            {
                // musculosa gastada, camisa de fiesta, bermuda de corderoy, piel con brillo de transpiracion, zapatos de cuero
                { "cream",   Lit("Uncle_Undershirt", new Color(0.78f, 0.74f, 0.64f), 0.1f, 0f, fabric, fabricN, 0.35f, new Vector2(3, 3), true) },
                { "wine",    Lit("Uncle_Shirt", new Color(0.7f, 0.12f, 0.15f), 0.12f, 0f, fabric, fabricN, 0.5f, new Vector2(2, 2), true) },
                { "mustard", Lit("Uncle_Shorts", new Color(0.82f, 0.56f, 0.16f), 0.1f, 0f, fabric, fabricN, 0.7f, new Vector2(4, 4), true) },
                { "skin",    Lit("Uncle_Skin", new Color(0.84f, 0.55f, 0.41f), 0.3f, 0f, null, null, 0f, null, true) },
                { "shoe",    Lit("Uncle_Shoe", new Color(0.2f, 0.11f, 0.06f), 0.6f, 0f, null, null, 0f, null, true) },
                { "white",   Lit("Uncle_EyeWhite", new Color(0.95f, 0.92f, 0.82f), 0.85f, 0f, null, null, 0f, null, true) },
                { "pupil",   Lit("Uncle_Pupil", new Color(0.03f, 0.03f, 0.03f), 0.9f, 0f, null, null, 0f, null, true) },
                { "hair",    Lit("Uncle_Hair", new Color(0.12f, 0.08f, 0.06f), 0.3f, 0f, null, null, 0f, null, true) },
            };
            foreach (var r in kit.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) if (mats[i] && map.TryGetValue(mats[i].name, out var m)) mats[i] = m;
                r.sharedMaterials = mats;
            }
            if (!head) return;
            // gorro de fiesta torcido (identidad «fiestero»): cono con franjas y pompon; sin colision, sigue al hueso de la cabeza
            var hat = new GameObject("Party_Hat"); hat.transform.SetParent(head, false);
            hat.transform.localPosition = new Vector3(0.05f, 0.47f, 0.02f); hat.transform.localRotation = Quaternion.Euler(-8f, 0f, -18f);
            var mf = hat.AddComponent<MeshFilter>(); mf.sharedMesh = ConeMesh();
            var mr = hat.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { Lit("Hat_Magenta", new Color(0.85f, 0.15f, 0.55f), 0.35f), Lit("Hat_Gold", new Color(0.95f, 0.75f, 0.2f), 0.6f, 0.6f) };
            var pom = GameObject.CreatePrimitive(PrimitiveType.Sphere); pom.name = "Pompom"; Object.DestroyImmediate(pom.GetComponent<Collider>());
            pom.transform.SetParent(hat.transform, false); pom.transform.localPosition = new Vector3(0, 0.3f, 0); pom.transform.localScale = Vector3.one * 0.07f;
            pom.GetComponent<Renderer>().sharedMaterial = Lit("Hat_Pompom", new Color(0.98f, 0.95f, 0.9f), 0.1f);
            var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder); band.name = "Elastic"; Object.DestroyImmediate(band.GetComponent<Collider>());
            band.transform.SetParent(hat.transform, false); band.transform.localPosition = new Vector3(0, 0.005f, 0); band.transform.localScale = new Vector3(0.235f, 0.006f, 0.235f);
            band.GetComponent<Renderer>().sharedMaterial = Lit("Hat_Gold", new Color(0.95f, 0.75f, 0.2f), 0.6f, 0.6f);
        }

        /// Cono de 0,3 m con dos materiales en franjas alternas (submallas).
        static Mesh ConeMesh()
        {
            const string path = "Assets/Game/Art/Meshes/PartyHat.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing) return existing;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            const int seg = 18; const float r = 0.115f, h = 0.3f;
            var v = new System.Collections.Generic.List<Vector3>(); var t0 = new System.Collections.Generic.List<int>(); var t1 = new System.Collections.Generic.List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                int b = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r)); v.Add(new Vector3(0, h, 0)); v.Add(new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r));
                (i % 3 == 0 ? t1 : t0).AddRange(new[] { b, b + 1, b + 2 });
            }
            var mesh = new Mesh { name = "PartyHat" }; mesh.SetVertices(v); mesh.subMeshCount = 2; mesh.SetTriangles(t0, 0); mesh.SetTriangles(t1, 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        // ---------- carrito propio sobre la geometria funcional (0,90 x 1,40 m, asa en el extremo corto a 1,0 m)
        public static void Cart(Transform ct)
        {
            foreach (var r in ct.GetComponentsInChildren<Renderer>(true)) r.enabled = false; // primitivos funcionales y piezas del kit
            var g = new GameObject("Cart_Visual").transform; g.SetParent(ct, false);
            var paint = Lit("Cart_Paint", new Color(0.95f, 0.42f, 0.1f), 0.55f, 0.2f);        // naranja de seguridad: se lee de lejos
            var steel = Lit("Cart_Steel", new Color(0.62f, 0.63f, 0.66f), 0.6f, 0.85f);
            var rubber = Lit("Cart_Rubber", new Color(0.05f, 0.05f, 0.055f), 0.2f);
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/Materials/MAT_Wood_Boards.mat");
            var deck = wood ? Lit("Cart_Deck", new Color(0.82f, 0.68f, 0.52f), 0.3f, 0f, wood.GetTexture("_BaseMap") as Texture2D, wood.GetTexture("_BumpMap") as Texture2D, 0.5f, new Vector2(1, 0.5f)) : Lit("Cart_Deck", new Color(0.55f, 0.38f, 0.22f), 0.3f);
            GameObject Box(string n, Vector3 p, Vector3 s, Material m) { var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = n; Object.DestroyImmediate(b.GetComponent<Collider>()); b.transform.SetParent(g, false); b.transform.localPosition = p; b.transform.localScale = s; b.GetComponent<Renderer>().sharedMaterial = m; return b; }
            GameObject Cyl(string n, Vector3 p, Vector3 s, Quaternion q, Material m) { var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder); b.name = n; Object.DestroyImmediate(b.GetComponent<Collider>()); b.transform.SetParent(g, false); b.transform.localPosition = p; b.transform.localRotation = q; b.transform.localScale = s; b.GetComponent<Renderer>().sharedMaterial = m; return b; }
            var alongZ = Quaternion.Euler(90, 0, 0); var alongX = Quaternion.Euler(0, 0, 90);
            // chasis pintado y tablas del piso (el piso funcional va de y 0,25 a 0,35)
            Box("Frame_L", new Vector3(-0.43f, 0.29f, 0), new Vector3(0.05f, 0.08f, 1.4f), paint); Box("Frame_R", new Vector3(0.43f, 0.29f, 0), new Vector3(0.05f, 0.08f, 1.4f), paint);
            Box("Frame_F", new Vector3(0, 0.29f, 0.68f), new Vector3(0.9f, 0.08f, 0.05f), paint); Box("Frame_B", new Vector3(0, 0.29f, -0.68f), new Vector3(0.9f, 0.08f, 0.05f), paint);
            for (int i = 0; i < 6; i++) Box("Slat", new Vector3(0, 0.335f, -0.6f + i * 0.24f), new Vector3(0.82f, 0.03f, 0.21f), deck);
            // barandas tubulares a los costados y al frente (alto de las laterales funcionales: 0,55 m)
            foreach (var x in new[] { -0.44f, 0.44f })
            {
                Cyl("Rail_Top", new Vector3(x, 0.54f, 0), new Vector3(0.035f, 0.7f, 0.035f), alongZ, steel);
                foreach (var z in new[] { -0.62f, 0f, 0.62f }) Cyl("Rail_Post", new Vector3(x, 0.445f, z), new Vector3(0.03f, 0.095f, 0.03f), Quaternion.identity, steel);
            }
            Cyl("Rail_Front", new Vector3(0, 0.54f, 0.69f), new Vector3(0.035f, 0.44f, 0.035f), alongX, steel);
            // asa: montantes pintados, barra de acero y empunaduras de goma (en la posicion del socket cart_handle)
            foreach (var x in new[] { -0.35f, 0.35f }) Box("Handle_Upright", new Vector3(x, 0.66f, -0.72f), new Vector3(0.05f, 0.68f, 0.05f), paint);
            Cyl("Handle_Bar", new Vector3(0, 1.0f, -0.74f), new Vector3(0.04f, 0.375f, 0.04f), alongX, steel);
            foreach (var x in new[] { -0.2f, 0.2f }) Cyl("Handle_Grip", new Vector3(x, 1.0f, -0.74f), new Vector3(0.052f, 0.11f, 0.052f), alongX, rubber);
            // ruedas de goma con taza y horquilla
            foreach (var x in new[] { -0.38f, 0.38f }) foreach (var z in new[] { -0.55f, 0.55f })
            {
                Cyl("Wheel", new Vector3(x, 0.12f, z), new Vector3(0.24f, 0.035f, 0.24f), alongX, rubber);
                Cyl("Hub", new Vector3(x + Mathf.Sign(x) * 0.037f, 0.12f, z), new Vector3(0.1f, 0.004f, 0.1f), alongX, steel);
                Box("Fork", new Vector3(x, 0.2f, z), new Vector3(0.09f, 0.12f, 0.05f), steel);
            }
        }
    }
}
