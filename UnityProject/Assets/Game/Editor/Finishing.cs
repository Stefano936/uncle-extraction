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
            // sesion 17: el asset NO lleva _EMISSION (URP se la quitaba al de la camisa al dibujarlo en el editor y PlayMode dejaba el
            // proyecto modificado). emissionReady solo deja la emision en negro; NpcPresentation la activa en copias en ejecucion.
            m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black);
            if (emissionReady) m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
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

        /// URP conserva el color en RGB y la suavidad en el alfa del albedo opaco.
        internal static Material S19Surface(string id, bool repeat, float bump, Vector2? tiling = null)
        {
            var color = S19Texture(id + "_color_gloss", false, repeat);
            var normal = S19Texture(id + "_normal", true, repeat);
            var material = Lit("S19_" + id, Color.white, 1f, 0f, color, normal, bump, tiling ?? Vector2.one);
            material.SetFloat("_SmoothnessTextureChannel", 1f);
            material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            // URP sincroniza este alias para herramientas antiguas al guardar materiales existentes.
            material.SetTexture("_MainTex", color);
            material.SetTextureScale("_MainTex", tiling ?? Vector2.one);
            return material;
        }

        static Texture2D S19Texture(string id, bool normal, bool repeat)
        {
            string path = $"{TexDir}/S19/{id}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new System.InvalidOperationException($"Falta la textura de la sesión 19: {path}");
            var wrap = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var kind = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            var alpha = normal ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            if (importer.textureType != kind || importer.wrapMode != wrap || importer.sRGBTexture == normal
                || importer.alphaSource != alpha || importer.alphaIsTransparency || !importer.mipmapEnabled
                || importer.filterMode != FilterMode.Trilinear || importer.anisoLevel != 8
                || importer.npotScale != TextureImporterNPOTScale.None || importer.maxTextureSize != 2048
                || importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.textureType = kind;
                importer.sRGBTexture = !normal;
                importer.alphaSource = alpha;
                importer.alphaIsTransparency = false;
                importer.wrapMode = wrap;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 8;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// Conserva posiciones, normales e índices del modelo y guarda las UV y tangentes en una malla visual.
        internal static void ProjectS19(MeshRenderer renderer, string id, Transform space, Material material, System.Func<Vector3, Vector2> uv)
        {
            const string dir = "Assets/Game/Art/GeneratedMeshes";
            var filter = renderer.GetComponent<MeshFilter>();
            var source = filter.sharedMesh;
            var matrix = space.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var coords = source.vertices.Select(v => uv(matrix.MultiplyPoint3x4(v))).ToArray();
            string path = $"{dir}/S19_{id}.asset";
            var projection = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!projection)
            {
                Directory.CreateDirectory(dir);
                projection = Object.Instantiate(source);
                projection.name = "S19_" + id;
                projection.uv = coords;
                projection.RecalculateTangents();
                AssetDatabase.CreateAsset(projection, path);
            }
            else if (!projection.uv.SequenceEqual(coords))
            {
                projection.uv = coords;
                projection.RecalculateTangents();
                EditorUtility.SetDirty(projection);
            }

            filter.sharedMesh = projection;
            renderer.sharedMaterial = material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }

        internal static void ApplyS19Furniture(Transform env, Material trim)
        {
            Transform counter = env.Find("Kit_bar_counter");
            MeshRenderer Part(Transform root, string name) => root.GetComponentsInChildren<MeshRenderer>()
                .Single(r => r.name == name);

            ProjectS19(Part(counter, "CounterTop"), "CounterTop", env, S19Surface("bar_top", false, 1.3f),
                p => new Vector2((p.x - 2.44f) / 4.12f + (p.z - 8.85f) * 0.018f, (p.z - 8.40f) / 0.9f + (p.y - 1.08f) * 0.5f));
            ProjectS19(Part(counter, "CounterBody"), "CounterFront", env, S19Surface("bar_front", false, 1.0f),
                p => new Vector2((p.x - 2.5f) / 4f + (p.z - 8.85f) * 0.024f, p.y / 0.96f + (p.z - 8.85f) * 0.035f));
            var molding = env.Find("Bar_Trims/Molding").GetComponent<MeshRenderer>();
            ProjectS19(molding, "CounterMolding", molding.transform, trim,
                p => new Vector2(p.x * 2f + 0.5f, p.y + 0.5f + p.z * 0.1f));

            // En la escena ya guardada, el desgaste antiguo era una tira superpuesta de color uniforme.
            var legacyWear = env.Find("Bar_Trims/Elbow_Wear");
            if (legacyWear) Object.DestroyImmediate(legacyWear.gameObject);

            Transform hinge = env.Find("Exit_Door");
            ProjectS19(Part(hinge.Find("Kit_exit_door"), "DoorPanel"), "DoorLeaf", hinge, S19Surface("exit_leaf", false, 1.2f),
                p => new Vector2(p.x / 1.5f + p.z * 0.06f, p.y / 2.2f));
        }

        internal static void ApplyS19Casing(Transform root, string name, Material trim)
        {
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                bool upright = renderer.name == "Jamb" || renderer.name == "Door_Jamb";
                if (!upright && renderer.name != "Header" && renderer.name != "Door_Head") continue;
                ProjectS19(renderer, name + (upright ? "Jamb" : "Header"), renderer.transform, trim,
                    p => upright ? new Vector2(p.y + 0.5f, p.x * 0.80f + p.z * 0.12f + 0.5f)
                                 : new Vector2(p.x * 0.8f + 0.5f, p.y * 0.80f + p.z * 0.12f + 0.5f));
            }
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

        // ---------- sesion 16: texturas propias para que metal, pintura, madera y tela se distingan de cerca ----------
        /// Textura repetible de 256 px generada por una funcion (u, v en 0..1, rnd); se guarda como PNG en Generated/.
        static Texture2D Gen(string name, System.Func<float, float, System.Random, Color> f, int seed)
        {
            string path = $"{TexDir}/Generated/{name}.png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                const int N = 256; var t = new Texture2D(N, N, TextureFormat.RGB24, true); var rnd = new System.Random(seed);
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) t.SetPixel(x, y, f(x / (float)N, y / (float)N, rnd));
                t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path); ti.wrapMode = TextureWrapMode.Repeat; ti.filterMode = FilterMode.Trilinear; ti.anisoLevel = 8; ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static float P(float x, float y) => Mathf.PerlinNoise(x, y);
        /// Acero cepillado: vetas finas en una direccion (se ve el sentido del pulido con la luz rasante).
        public static Texture2D BrushedMetal() => Gen("metal_brushed", (u, v, r) =>
        {
            float g = 0.72f + 0.12f * P(u * 3f, v * 140f) + 0.06f * P(u * 40f, v * 600f) + 0.03f * (float)r.NextDouble();
            return new Color(g, g, g * 1.02f);
        }, 11);
        /// Pintura gastada: color pleno con desconchados que dejan ver el metal y rayones; el color lo pone el material.
        public static Texture2D WornPaint() => Gen("paint_worn", (u, v, r) =>
        {
            float chip = P(u * 9f + 3f, v * 9f + 7f) * 0.75f + P(u * 31f, v * 31f) * 0.25f;
            float scratch = Mathf.Abs(Mathf.Sin((u * 0.8f + v * 0.25f) * 140f + P(u * 4f, v * 4f) * 6f)) > 0.985f ? 1f : 0f;
            float k = 0.9f + 0.1f * P(u * 60f, v * 60f);
            if (chip > 0.72f || scratch > 0f) return new Color(0.45f, 0.45f, 0.47f); // metal a la vista (lo tiñe el color del material)
            return new Color(k, k, k);
        }, 23);
        /// Goma: grano fino irregular.
        public static Texture2D RubberGrain() => Gen("rubber_grain", (u, v, r) => { float g = 0.8f + 0.2f * P(u * 70f, v * 70f) + 0.05f * (float)r.NextDouble(); return new Color(g, g, g); }, 37);
        /// Madera barnizada oscura de molduras (veta larga).
        public static Texture2D DarkGrain() => Gen("wood_dark_grain", (u, v, r) =>
        {
            float ring = Mathf.Sin((v * 14f + P(u * 2f, v * 6f) * 3f) * Mathf.PI * 2f) * 0.5f + 0.5f;
            float g = 0.75f + 0.18f * ring + 0.07f * P(u * 80f, v * 8f);
            return new Color(g, g * 0.92f, g * 0.86f);
        }, 41);

        static Material TexMat(string name, Color c, float smooth, float metal, Texture2D tex, float bump, Vector2 tiling)
        {
            var nrm = tex ? NormalFrom(AssetDatabase.GetAssetPath(tex), tex.name, bump * 6f) : null;
            return Lit(name, c, smooth, metal, tex, nrm, bump, tiling);
        }

        /// Barra: moldura en el canto y apoyapies de bronce con soportes. El desgaste del tablero va en el material.
        /// Sin colision (no compite con la baranda de anclaje, que es la que se agarra).
        public static void BarTrims(Transform env, Vector3 counterCenter, Vector3 counterSize)
        {
            var g = new GameObject("Bar_Trims").transform; g.SetParent(env, false);
            var molding = TexMat("Bar_Molding", new Color(0.24f, 0.12f, 0.06f), 0.62f, 0f, DarkGrain(), 0.35f, new Vector2(6, 1));
            var brass = TexMat("Bar_Brass", new Color(0.78f, 0.58f, 0.26f), 0.72f, 0.9f, BrushedMetal(), 0.15f, new Vector2(1, 8));
            float front = counterCenter.z - counterSize.z * 0.5f, top = counterCenter.y + counterSize.y * 0.5f;
            NoCol(PrimitiveType.Cube, "Molding", g, new Vector3(counterCenter.x, top - 0.02f, front - 0.025f), new Vector3(counterSize.x + 0.04f, 0.05f, 0.06f), Quaternion.identity, molding);
            NoCol(PrimitiveType.Cylinder, "Foot_Rail", g, new Vector3(counterCenter.x, 0.2f, front - 0.17f), new Vector3(0.05f, (counterSize.x - 0.2f) * 0.5f, 0.05f), Quaternion.Euler(0, 0, 90), brass);
            for (int i = 0; i < 5; i++)
            {
                float x = counterCenter.x - (counterSize.x - 0.4f) * 0.5f + i * (counterSize.x - 0.4f) / 4f;
                NoCol(PrimitiveType.Cube, "Rail_Bracket", g, new Vector3(x, 0.2f, front - 0.085f), new Vector3(0.03f, 0.04f, 0.17f), Quaternion.identity, brass);
            }
        }

        /// Puerta de salida: marco de madera alrededor de la abertura (fijo) y, en la hoja, placa de empuje, placa de patada y
        /// bisagras de metal cepillado (se mueven con la hoja). Sin colision.
        public static void DoorTrims(Transform env, Transform leaf, Vector3 openingCenter, float width, float height)
        {
            var frame = TexMat("Door_Frame", new Color(0.3f, 0.17f, 0.09f), 0.45f, 0f, DarkGrain(), 0.35f, new Vector2(1, 4));
            var steel = TexMat("Door_Steel", new Color(0.66f, 0.67f, 0.7f), 0.7f, 0.85f, BrushedMetal(), 0.2f, new Vector2(1, 4));
            var g = new GameObject("Door_Trims").transform; g.SetParent(env, false);
            foreach (var s in new[] { -1f, 1f })
                NoCol(PrimitiveType.Cube, "Jamb", g, openingCenter + new Vector3(s * (width * 0.5f + 0.05f), height * 0.5f, 0f), new Vector3(0.1f, height + 0.1f, 0.36f), Quaternion.identity, frame);
            NoCol(PrimitiveType.Cube, "Header", g, openingCenter + new Vector3(0f, height + 0.05f, 0f), new Vector3(width + 0.3f, 0.12f, 0.36f), Quaternion.identity, frame);
            if (!leaf) return;
            foreach (var z in new[] { -0.045f, 0.045f })
            {
                NoCol(PrimitiveType.Cube, "Push_Plate", leaf, leaf.TransformPoint(new Vector3(1.25f, 1.15f, z)), new Vector3(0.12f, 0.34f, 0.006f), leaf.rotation, steel);
                NoCol(PrimitiveType.Cube, "Kick_Plate", leaf, leaf.TransformPoint(new Vector3(0.75f, 0.12f, z)), new Vector3(1.36f, 0.2f, 0.006f), leaf.rotation, steel);
            }
            foreach (var y in new[] { 0.3f, 1.05f, 1.8f }) NoCol(PrimitiveType.Cylinder, "Hinge", leaf, leaf.TransformPoint(new Vector3(0.02f, y, 0f)), new Vector3(0.05f, 0.07f, 0.05f), leaf.rotation, steel);
        }

        static GameObject NoCol(PrimitiveType t, string n, Transform parent, Vector3 worldPos, Vector3 scale, Quaternion worldRot, Material m)
        {
            var g = GameObject.CreatePrimitive(t); g.name = n; Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, true); g.transform.position = worldPos; g.transform.rotation = worldRot;
            var ls = parent.lossyScale; g.transform.localScale = new Vector3(scale.x / ls.x, scale.y / ls.y, scale.z / ls.z);
            g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }

        public static void Cart(Transform ct)
        {
            foreach (var r in ct.GetComponentsInChildren<Renderer>(true)) r.enabled = false; // primitivos funcionales y piezas del kit
            var g = new GameObject("Cart_Visual").transform; g.SetParent(ct, false);
            // sesion 16: pintura gastada (desconchados y rayones), acero cepillado y goma con grano: se distinguen de cerca
            var paint = TexMat("Cart_Paint", new Color(0.95f, 0.42f, 0.1f), 0.42f, 0.15f, WornPaint(), 0.3f, new Vector2(2, 2));  // naranja de seguridad
            var steel = TexMat("Cart_Steel", new Color(0.66f, 0.67f, 0.7f), 0.68f, 0.88f, BrushedMetal(), 0.2f, new Vector2(1, 3));
            var rubber = TexMat("Cart_Rubber", new Color(0.06f, 0.06f, 0.065f), 0.12f, 0f, RubberGrain(), 0.4f, new Vector2(2, 2));
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
