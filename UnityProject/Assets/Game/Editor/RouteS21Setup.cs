using System.IO;
using UnityEditor;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Sesion 21 (UE-16): importacion y materiales del entorno de la ruta (Resources/RouteS21, generado por
    /// tools/art/blender/build_route_s21.py). Los FBX traen materiales con nombre (RS_*); aca se crean los de URP con texturas
    /// del proyecto y se asignan por nombre. Las UV del modelo estan en metros (1 unidad = 1 m), asi el mosaico es fisico.
    /// No toca escenas: el montaje es en ejecucion (RouteDressing).
    /// Uso batch: -executeMethod UncleExtraction.EditorTools.RouteS21Setup.Run
    public class RouteS21Setup : AssetPostprocessor
    {
        public const string ModelDir = "Assets/Game/Resources/RouteS21";
        const string MatDir = "Assets/Game/Art/RouteS21/Materials";
        const string TexDir = "Assets/Game/Art/Materials/Textures";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelDir)) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1f; mi.useFileScale = true; mi.animationType = ModelImporterAnimationType.None; mi.importAnimation = false;
            mi.importCameras = false; mi.importLights = false; mi.addCollider = false; mi.isReadable = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            // bordes biselados redondeados y aristas vivas: normales suavizadas hasta 40 grados
            mi.importNormals = ModelImporterNormals.Calculate; mi.normalSmoothingAngle = 40f;
            mi.normalCalculationMode = ModelImporterNormalCalculationMode.AngleWeighted;
            mi.importTangents = ModelImporterTangents.CalculateMikk; mi.meshCompression = ModelImporterMeshCompression.Off;
        }

        public static void Run()
        {
            int code = 0;
            try
            {
                AssetDatabase.Refresh();
                var mats = Materials();
                foreach (var path in Directory.GetFiles(ModelDir, "*.fbx"))
                {
                    var p = path.Replace('\\', '/');
                    var mi = (ModelImporter)AssetImporter.GetAtPath(p);
                    foreach (var kv in mats) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                    mi.SaveAndReimport();
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    var b = new Bounds(); bool first = true; int missing = 0; float outward = 0f;
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    {
                        var mb = r.GetComponent<MeshFilter>().sharedMesh.bounds; mb = TransformBounds(r.transform, mb);
                        if (first) { b = mb; first = false; } else b.Encapsulate(mb);
                        foreach (var m in r.sharedMaterials) if (!m || !m.name.StartsWith("RS_") || AssetDatabase.GetAssetPath(m) == p) missing++;
                    }
                    outward = OutwardShare(p);
                    Debug.Log($"[RouteS21] {Path.GetFileName(p)}: limites min {b.min:F3} max {b.max:F3} materiales sin asignar {missing} caras hacia afuera {outward:P0}");
                    if (missing > 0) code = 2;
                }
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e) { Debug.LogException(e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        /// Comprobacion del sentido de las caras: en cada pieza cerrada la normal de la cara (por su orden de vertices, como la
        /// dibuja Unity) apunta lejos del centro de la pieza. Se mide sobre la malla legible de una copia temporal.
        /// Orientativo (en piezas compuestas parte de las caras mira al centro de todas formas): se compara con el valor esperado.
        static float OutwardShare(string path)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            mi.isReadable = true; mi.SaveAndReimport();
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var v = mesh.vertices; var tri = mesh.triangles; var c = mesh.bounds.center; int ok = 0, n = tri.Length / 3;
            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 a = v[tri[i]], b2 = v[tri[i + 1]], c2 = v[tri[i + 2]];
                var nrm = Vector3.Cross(b2 - a, c2 - a); // Unity: caras frontales en sentido horario => esta normal apunta al frente
                if (Vector3.Dot(nrm, (a + b2 + c2) / 3f - c) > 0f) ok++;
            }
            mi.isReadable = false; mi.SaveAndReimport();
            return n > 0 ? ok / (float)n : 0f;
        }

        static Bounds TransformBounds(Transform t, Bounds b)
        {
            var r = new Bounds(t.TransformPoint(b.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
                r.Encapsulate(t.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return r;
        }

        // ---------- materiales (URP Lit)
        static System.Collections.Generic.Dictionary<string, Material> Materials()
        {
            Directory.CreateDirectory(MatDir);
            var d = new System.Collections.Generic.Dictionary<string, Material>();
            Texture2D T(string p) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{p}");
            var paint = T("Generated/paint_worn.png"); var paintN = T("Generated/paint_worn_normal.png");
            var wood = T("01_wood_basecolor.png"); var woodN = T("Generated/01_wood_basecolor_normal.png");
            var conc = T("Generated/concrete_slabs.png"); var concN = T("Generated/concrete_slabs_normal.png");
            var rub = T("Generated/rubber_grain.png"); var rubN = T("Generated/rubber_grain_normal.png");
            var plaster = T("S19/matte_plaster_color_gloss.png"); var plasterN = T("S19/matte_plaster_normal.png");
            var brick = Brick(); var brickN = Finishing.NormalFrom(AssetDatabase.GetAssetPath(brick), brick.name, 2.4f);
            void M(string n, Color c, float sm, float mt, Texture2D tex = null, Texture2D nrm = null, float bump = 0.5f, float tile = 1f, Color? glow = null, float alpha = 1f)
            {
                string p = $"{MatDir}/{n}.mat"; var sh = Shader.Find("Universal Render Pipeline/Lit");
                var m = AssetDatabase.LoadAssetAtPath<Material>(p); if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
                m.shader = sh; c.a = alpha; m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", sm); m.SetFloat("_Metallic", mt);
                m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", Vector2.one * tile);
                if (nrm) { m.SetTexture("_BumpMap", nrm); m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP"); m.SetTextureScale("_BumpMap", Vector2.one * tile); }
                else { m.SetTexture("_BumpMap", null); m.DisableKeyword("_NORMALMAP"); }
                if (glow.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", glow.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
                else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
                bool clear = alpha < 1f;
                m.SetFloat("_Surface", clear ? 1 : 0); m.SetFloat("_Blend", 0); m.SetFloat("_ZWrite", clear ? 0 : 1);
                m.SetFloat("_SrcBlend", clear ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.One);
                m.SetFloat("_DstBlend", clear ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (float)UnityEngine.Rendering.BlendMode.Zero);
                if (clear) { m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000; m.SetOverrideTag("RenderType", "Transparent"); }
                else { m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = -1; m.SetOverrideTag("RenderType", ""); }
                EditorUtility.SetDirty(m); d[n] = m;
            }
            // carrito
            M("RS_CartPaint", new Color(0.95f, 0.42f, 0.1f), 0.42f, 0.15f, paint, paintN, 0.3f, 2f);
            M("RS_Steel", new Color(0.62f, 0.63f, 0.66f), 0.62f, 0.85f);
            M("RS_Rubber", new Color(0.06f, 0.06f, 0.065f), 0.15f, 0f, rub, rubN, 0.4f, 3f);
            M("RS_Wood", new Color(0.85f, 0.7f, 0.55f), 0.3f, 0f, wood, woodN, 0.5f, 1f);
            // camioneta: crema gastada con franja oxido, interior de terciado
            // pintura lisa: la textura de pintura gastada se leia como vetas y manchas a la escala de la carroceria
            M("RS_VanBody", new Color(0.78f, 0.71f, 0.57f), 0.42f, 0.1f);
            M("RS_VanStripe", new Color(0.68f, 0.27f, 0.12f), 0.4f, 0.05f);
            M("RS_VanInner", new Color(0.42f, 0.39f, 0.35f), 0.2f, 0f, wood, woodN, 0.35f, 0.7f); // gris calido: con la luz del techo el terciado viraba a rojo
            M("RS_Black", new Color(0.045f, 0.045f, 0.05f), 0.3f, 0f);
            M("RS_Chrome", new Color(0.85f, 0.86f, 0.88f), 0.85f, 1f);
            M("RS_Glass", new Color(0.05f, 0.07f, 0.09f), 0.93f, 0.4f);
            M("RS_Tail", new Color(0.5f, 0.05f, 0.03f), 0.7f, 0f, glow: new Color(1f, 0.08f, 0.05f) * 0.9f);
            M("RS_Head", new Color(0.9f, 0.88f, 0.8f), 0.8f, 0f, glow: new Color(1f, 0.95f, 0.8f) * 2.2f);
            M("RS_Amber", new Color(0.8f, 0.45f, 0.08f), 0.7f, 0f, glow: new Color(1f, 0.55f, 0.1f) * 0.45f);
            M("RS_Plate", new Color(0.9f, 0.9f, 0.86f), 0.5f, 0.2f);
            // puerta de salida: chapa verde pintada y vidrio armado (semitransparente: se ve el otro lado)
            M("RS_DoorPaint", new Color(0.13f, 0.3f, 0.24f), 0.4f, 0.25f);
            M("RS_DoorGlass", new Color(0.25f, 0.33f, 0.35f), 0.95f, 0.1f, alpha: 0.38f);
            // cuadra de enfrente
            M("RS_Brick", new Color(0.86f, 0.74f, 0.7f), 0.12f, 0f, brick, brickN, 0.8f, 1f);
            M("RS_Stucco", new Color(0.5f, 0.52f, 0.55f), 0.1f, 0f, plaster, plasterN, 0.6f, 0.5f);
            M("RS_StuccoWarm", new Color(0.62f, 0.5f, 0.38f), 0.1f, 0f, plaster, plasterN, 0.6f, 0.5f);
            M("RS_Concrete", new Color(0.55f, 0.55f, 0.55f), 0.15f, 0f, conc, concN, 0.5f, 0.5f);
            M("RS_Trim", new Color(0.12f, 0.11f, 0.1f), 0.35f, 0.1f);
            M("RS_WinWarm", new Color(0.6f, 0.45f, 0.27f), 0.8f, 0f, glow: new Color(1f, 0.75f, 0.45f) * 1.0f);
            M("RS_WinCold", new Color(0.3f, 0.42f, 0.6f), 0.8f, 0f, glow: new Color(0.5f, 0.7f, 1f) * 0.8f);
            M("RS_WinDark", new Color(0.04f, 0.05f, 0.07f), 0.85f, 0.3f);
            M("RS_Awning", new Color(0.55f, 0.1f, 0.08f), 0.2f, 0f);
            M("RS_Shutter", new Color(0.42f, 0.43f, 0.45f), 0.45f, 0.7f);
            M("RS_ShopGlow", new Color(0.6f, 0.48f, 0.3f), 0.6f, 0f, glow: new Color(1f, 0.8f, 0.5f) * 0.6f);
            M("RS_Sign", new Color(0.08f, 0.3f, 0.25f), 0.4f, 0f, glow: new Color(0.15f, 0.6f, 0.45f) * 0.6f);
            AssetDatabase.SaveAssets();
            return d;
        }

        /// Ladrillo visto: 1 m x 1 m de textura (4 ladrillos de 0,25 m por fila, 13 hiladas), juntas hundidas y tono variable.
        static Texture2D Brick()
        {
            string path = $"{TexDir}/Generated/brick_s21.png";
            if (!File.Exists(path))
            {
                const int N = 512; var t = new Texture2D(N, N, TextureFormat.RGB24, true); var rnd = new System.Random(21);
                int rows = 13; float rowH = N / (float)rows, brickW = N / 4f;
                var tone = new float[rows * 5];
                for (int i = 0; i < tone.Length; i++) tone[i] = 0.78f + 0.3f * (float)rnd.NextDouble();
                for (int y = 0; y < N; y++)
                {
                    int row = (int)(y / rowH); float fy = y - row * rowH; float off = (row % 2) * brickW * 0.5f;
                    for (int x = 0; x < N; x++)
                    {
                        float xx = (x + off) % N; int col = (int)(xx / brickW); float fx = xx - col * brickW;
                        bool mortar = fy < 3.5f || fx < 3.5f;
                        float n = 0.92f + 0.16f * (float)rnd.NextDouble();
                        Color c = mortar ? new Color(0.62f, 0.6f, 0.56f) * n : new Color(0.62f, 0.3f, 0.22f) * tone[row * 5 + col] * n;
                        t.SetPixel(x, y, c);
                    }
                }
                t.Apply(); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, t.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
