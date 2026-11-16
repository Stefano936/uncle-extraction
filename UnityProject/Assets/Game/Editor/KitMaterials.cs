using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Kit de materiales (preproduction/uncle_extraction_kit_materiales, data/texture_import.json): cuatro Base Color RGB de
    /// 1254 px, sin normal ni PBR completo. Copia las texturas al proyecto con la importacion propuesta, crea materiales
    /// URP/Lit y los aplica a las cajas de piso, pared y madera del escenario con mallas cuyas UV estan en metros divididos
    /// por la repeticion fisica de cada textura (Tiling 1,1, igual que las variantes FBX del kit). Solo cambia lo visual:
    /// colliders, masas y sockets quedan como estaban.
    public static class KitMaterials
    {
        const string Src = "../preproduction/uncle_extraction_kit_materiales/textures";
        const string TexDir = "Assets/Game/Art/Materials/Textures";
        const string MatDir = "Assets/Game/Art/Materials";
        const string MeshDir = "Assets/Game/Art/GeneratedMeshes";

        public class Recipe { public string id, file; public TextureWrapMode wrap; public float smoothness, tileM; }
        // Valores de texture_import.json (smoothness_proposed, wrap_proposed, physical_tile_m_encoded_in_uv0)
        public static readonly Recipe Wood = new Recipe { id = "MAT_Wood_Boards", file = "01_wood_basecolor.png", wrap = TextureWrapMode.Mirror, smoothness = 0.22f, tileM = 1.0f };
        public static readonly Recipe Floor = new Recipe { id = "MAT_Floor_Tiles", file = "02_floor_basecolor.png", wrap = TextureWrapMode.Repeat, smoothness = 0.18f, tileM = 2.0f };
        public static readonly Recipe Wall = new Recipe { id = "MAT_Wall_Taupe", file = "03_wall_basecolor.png", wrap = TextureWrapMode.Mirror, smoothness = 0.05f, tileM = 2.0f };
        public static readonly Recipe Fabric = new Recipe { id = "MAT_Shirt_Fabric", file = "04_fabric_basecolor.png", wrap = TextureWrapMode.Mirror, smoothness = 0.10f, tileM = 0.5f };

        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        /// Textura importada y material URP/Lit de una receta (null si falta la fuente).
        public static Material Get(Recipe r)
        {
            if (cache.TryGetValue(r.id, out var cached) && cached) return cached;
            Directory.CreateDirectory(TexDir); Directory.CreateDirectory(MatDir);
            string dst = $"{TexDir}/{r.file}", src = Path.Combine(Src, r.file);
            if (!File.Exists(dst))
            {
                if (!File.Exists(src)) { Debug.LogWarning($"[KitMaterials] falta {src}"); return null; }
                File.Copy(src, dst); AssetDatabase.ImportAsset(dst);
            }
            var ti = (TextureImporter)AssetImporter.GetAtPath(dst);
            bool dirty = false;
            void Set<T>(T cur, T want, System.Action<T> apply) { if (!EqualityComparer<T>.Default.Equals(cur, want)) { apply(want); dirty = true; } }
            Set(ti.sRGBTexture, true, v => ti.sRGBTexture = v);
            Set(ti.mipmapEnabled, true, v => ti.mipmapEnabled = v);
            Set(ti.filterMode, FilterMode.Trilinear, v => ti.filterMode = v);
            Set(ti.maxTextureSize, 2048, v => ti.maxTextureSize = v);
            Set(ti.wrapMode, r.wrap, v => ti.wrapMode = v);
            Set(ti.npotScale, TextureImporterNPOTScale.ToNearest, v => ti.npotScale = v);
            Set(ti.isReadable, false, v => ti.isReadable = v);
            Set(ti.alphaSource, TextureImporterAlphaSource.None, v => ti.alphaSource = v);
            if (dirty) ti.SaveAndReimport();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);

            string mp = $"{MatDir}/{r.id}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (!m) { m = new Material(lit); AssetDatabase.CreateAsset(m, mp); }
            if (m.shader != lit) m.shader = lit;
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", r.smoothness); m.SetFloat("_Metallic", 0f);
            m.mainTextureScale = Vector2.one; // la escala fisica va en las UV de la malla
            EditorUtility.SetDirty(m);
            Debug.Log($"[KitMaterials] {r.id}: {tex.width}x{tex.height} importada (fuente 1254 px), wrap {r.wrap}, smoothness {r.smoothness}, repeticion {r.tileM} m");
            cache[r.id] = m;
            return m;
        }

        /// Reemplaza el material de las cajas que usan los materiales lisos de piso, pared y madera.
        public static int ApplyToLevel(Transform root, Material plainFloor, Material plainWall, Material plainWood)
        {
            cache.Clear();
            var map = new Dictionary<Material, Recipe> { { plainFloor, Floor }, { plainWall, Wall }, { plainWood, Wood } };
            int n = 0;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.enabled || !map.TryGetValue(r.sharedMaterial, out var recipe)) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh || mf.sharedMesh.name != "Cube") continue; // solo cajas primitivas
                var mat = Get(recipe); if (!mat) continue;
                mf.sharedMesh = MetricBox(r.transform.lossyScale, recipe.tileM);
                r.sharedMaterial = mat;
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[KitMaterials] {n} superficies con materiales del kit");
            return n;
        }

        /// Cubo unitario (como el primitivo) con UV = metros reales / repeticion, segun la escala del objeto.
        static Mesh MetricBox(Vector3 scale, float tile)
        {
            Vector3 s = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            string name = $"Box_{s.x:0.###}x{s.y:0.###}x{s.z:0.###}_t{tile:0.##}".Replace(',', '.');
            Directory.CreateDirectory(MeshDir);
            string path = $"{MeshDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) return existing;

            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            void Face(Vector3 normal, Vector3 u, Vector3 w, float su, float sw)
            {   // u, w: ejes locales de la cara; su, sw: su tamano en metros
                int b = v.Count; Vector3 c = normal * 0.5f;
                Vector3[] corners = { c - u * 0.5f - w * 0.5f, c + u * 0.5f - w * 0.5f, c + u * 0.5f + w * 0.5f, c - u * 0.5f + w * 0.5f };
                Vector2[] uvs = { new Vector2(0, 0), new Vector2(su / tile, 0), new Vector2(su / tile, sw / tile), new Vector2(0, sw / tile) };
                for (int i = 0; i < 4; i++) { v.Add(corners[i]); n.Add(normal); uv.Add(uvs[i]); }
                // orientacion: u x w = -normal (mano izquierda de Unity): visto desde afuera, u a la derecha y w arriba
                tri.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }
            Face(Vector3.up, Vector3.right, Vector3.forward, s.x, s.z);
            Face(Vector3.down, Vector3.right, Vector3.back, s.x, s.z);
            Face(Vector3.forward, Vector3.left, Vector3.up, s.x, s.y);
            Face(Vector3.back, Vector3.right, Vector3.up, s.x, s.y);
            Face(Vector3.right, Vector3.forward, Vector3.up, s.z, s.y);
            Face(Vector3.left, Vector3.back, Vector3.up, s.z, s.y);
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
