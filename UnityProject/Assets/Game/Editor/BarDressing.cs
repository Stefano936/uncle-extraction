using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UncleExtraction.EditorTools
{
    /// Direccion visual de Bar_LastRound (pulido, sesion 10): noche de cierre en un bar de mala muerte.
    /// - Paleta: interior calido (tungsteno 2700 K, madera oscura, verde botella), exterior frio (luna azulada, sodio de la
    ///   calle), acentos de neon rosa/magenta. Las luces guian a lo importante: barra y tio (focos), carrito, salida (cartel
    ///   SALIDA verde), camioneta (farol y luces traseras).
    /// - Todo lo que agrega es VISUAL: sin colisiones, fuera de las rutas de juego y sin parecerse a objetos agarrables.
    ///   Colliders, masas, sockets, puertas, rampa y zona de extraccion no cambian.
    /// Se llama desde PhysicsLabBuilder solo para el escenario principal; los laboratorios quedan como estaban.
    public static class BarDressing
    {
        const string GenTex = "Assets/Game/Art/Materials/Textures/Generated";
        const string MatDir = "Assets/Game/Art/Materials/Bar";

        static Transform root;

        public static void Apply(Transform env, float hx, float backZ, float doorZ, float doorX)
        {
            Directory.CreateDirectory(GenTex); Directory.CreateDirectory(MatDir);
            root = new GameObject("Dressing").transform; root.SetParent(env, false);
            Atmosphere();
            Ground(env, hx, backZ, doorZ);
            Architecture(hx, backZ, doorZ, doorX);
            Facade(hx, doorZ, doorX);
            InteriorLights();
            BarIdentity(hx, backZ, doorZ);
            Street(doorZ);
            Van(env);
            PostFx();
            Debug.Log("[BarDressing] noche, techo, terminaciones, luces, cartel, calle, camioneta y posproceso aplicados");
        }

        // ---------- materiales
        static Material Lit(string name, Color c, float smooth = 0.25f, float metal = 0f)
        {
            string p = $"{MatDir}/{name}.mat";
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
            m.shader = sh; m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(m); return m;
        }
        static Material Glow(string name, Color c, float intensity)
        {
            var m = Lit(name, c * 0.6f, 0.4f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m); return m;
        }
        static Material Textured(string name, Texture2D tex, Color tint, Vector2 tiling, float smooth)
        {
            var m = Lit(name, tint, smooth);
            m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tiling);
            EditorUtility.SetDirty(m); return m;
        }

        /// Textura procedural guardada como PNG (asfalto, hormigon con juntas): ruido de valor y juntas en el borde.
        static Texture2D Noise(string file, Color a, Color b, float scale, int joints, int seed)
        {
            string path = $"{GenTex}/{file}.png";
            if (!File.Exists(path))
            {
                const int N = 256; var t = new Texture2D(N, N, TextureFormat.RGB24, true);
                var rnd = new System.Random(seed); float ox = rnd.Next(1000), oy = rnd.Next(1000);
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float n = 0.55f * Mathf.PerlinNoise(ox + u * scale, oy + v * scale) + 0.3f * Mathf.PerlinNoise(ox + u * scale * 4f, oy + v * scale * 4f)
                              + 0.15f * (float)rnd.NextDouble();
                    var c = Color.Lerp(a, b, n);
                    if (joints > 0 && (x < joints || y < joints)) c *= 0.55f; // junta entre losetas
                    t.SetPixel(x, y, c);
                }
                // repetible: los bordes del ruido se suavizan mezclando con el lado opuesto
                t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.wrapMode = TextureWrapMode.Repeat; ti.filterMode = FilterMode.Trilinear; ti.anisoLevel = 4; ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------- primitivas visuales (sin colision)
        static GameObject V(PrimitiveType p, string n, Transform parent, Vector3 pos, Vector3 scale, Material m, Quaternion? rot = null)
        {
            var g = GameObject.CreatePrimitive(p); g.name = n; g.transform.SetParent(parent ?? root, true);
            g.transform.position = pos; g.transform.rotation = rot ?? Quaternion.identity; g.transform.localScale = scale;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m;
            if (m && m.IsKeywordEnabled("_EMISSION")) r.shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }
        static GameObject B(string n, Vector3 pos, Vector3 size, Material m, Transform parent = null) => V(PrimitiveType.Cube, n, parent, pos, size, m);
        static Transform Group(string n) { var g = new GameObject(n).transform; g.SetParent(root, false); return g; }

        static Light L(string n, LightType type, Vector3 pos, Color c, float intensity, float range, Transform parent, bool shadows = false, Vector3? look = null, float angle = 90f)
        {
            var go = new GameObject(n); go.transform.SetParent(parent, false); go.transform.position = pos;
            if (look.HasValue) go.transform.rotation = Quaternion.LookRotation(look.Value - pos);
            var l = go.AddComponent<Light>(); l.type = type; l.color = c; l.intensity = intensity; l.range = range;
            if (type == LightType.Spot) { l.spotAngle = angle; l.innerSpotAngle = angle * 0.55f; }
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            if (shadows) { l.shadowStrength = 0.85f; l.shadowBias = 0.04f; l.shadowNormalBias = 0.3f; }
            return l;
        }

        static TextMesh Sign(string n, string text, Vector3 pos, Quaternion rot, Color c, float size, Transform parent)
        {
            var go = new GameObject(n); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(pos, rot);
            var tm = go.AddComponent<TextMesh>(); tm.text = text; tm.color = c; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.fontSize = 96; tm.characterSize = size; tm.fontStyle = FontStyle.Bold;
            var skin = UiSkin.Load();
            if (skin && skin.fontBold) { tm.font = skin.fontBold; go.GetComponent<MeshRenderer>().sharedMaterial = SignMaterial(skin.fontBold); go.AddComponent<WorldTextFont>(); }
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return tm;
        }

        /// Material de texto 3D con prueba de profundidad: el de la fuente se dibuja encima de todo (se veia a traves de muros).
        static Material SignMaterial(Font f)
        {
            string p = $"{MatDir}/Sign_Text.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(f.material); AssetDatabase.CreateAsset(m, p); }
            var sh = Shader.Find("UncleExtraction/WorldText");
            m.shader = sh ? sh : f.material.shader; m.mainTexture = f.material.mainTexture; m.SetFloat("_Intensity", 2.2f);
            EditorUtility.SetDirty(m); return m;
        }

        static readonly Color Tungsten = new Color(1f, 0.72f, 0.42f), Sodium = new Color(1f, 0.62f, 0.28f), Moon = new Color(0.55f, 0.66f, 0.95f);
        static readonly Color NeonPink = new Color(1f, 0.22f, 0.55f), NeonMagenta = new Color(0.85f, 0.25f, 1f), ExitGreen = new Color(0.25f, 1f, 0.45f);

        // ---------- noche
        static void Atmosphere()
        {
            var sun = Object.FindAnyObjectByType<Light>();
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) sun = l;
            if (sun) { sun.color = Moon; sun.intensity = 0.32f; sun.transform.rotation = Quaternion.Euler(38f, 140f, 0f); sun.shadowStrength = 0.75f; }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.13f, 0.15f, 0.25f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.13f, 0.13f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.05f, 0.05f);
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Sky_Night.mat");
            var skySh = Shader.Find("Skybox/Procedural");
            if (!skyMat) { skyMat = new Material(skySh); AssetDatabase.CreateAsset(skyMat, $"{MatDir}/Sky_Night.mat"); }
            skyMat.shader = skySh;
            skyMat.SetFloat("_SunDisk", 0f); skyMat.SetFloat("_AtmosphereThickness", 0.55f);
            skyMat.SetColor("_SkyTint", new Color(0.18f, 0.22f, 0.42f)); skyMat.SetColor("_GroundColor", new Color(0.05f, 0.05f, 0.07f));
            skyMat.SetFloat("_Exposure", 0.22f); EditorUtility.SetDirty(skyMat);
            RenderSettings.skybox = skyMat;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.018f;
            RenderSettings.fogColor = new Color(0.07f, 0.08f, 0.12f);
            RenderSettings.reflectionIntensity = 0.35f;
        }

        // ---------- suelos: baldosas solo adentro; asfalto y vereda de hormigon afuera
        static void Ground(Transform env, float hx, float backZ, float doorZ)
        {
            var asphalt = Textured("Asphalt", Noise("asphalt", new Color(0.09f, 0.09f, 0.1f), new Color(0.2f, 0.2f, 0.21f), 18f, 0, 7), Color.white, new Vector2(15f, 22f), 0.32f);
            var floor = env.Find("Floor"); if (floor) floor.GetComponent<Renderer>().sharedMaterial = asphalt;
            // piso interior con las baldosas del kit (la colision sigue siendo el suelo general a y = 0)
            var tiles = PhysicsLabBuilder.floorM;
            var inner = PhysicsLabBuilder.Box("Floor_Interior", env, new Vector3(0f, -0.005f + 0.004f, (backZ + doorZ) * 0.5f), new Vector3(hx * 2f, 0.01f, backZ - doorZ), tiles);
            PhysicsLabBuilder.NoCollider(inner);
            var concrete = Textured("Concrete_Slabs", Noise("concrete_slabs", new Color(0.38f, 0.37f, 0.36f), new Color(0.55f, 0.54f, 0.52f), 10f, 3, 11), Color.white, new Vector2(14f, 6f), 0.12f);
            var walk = env.Find("Sidewalk"); if (walk) walk.GetComponent<Renderer>().sharedMaterial = concrete;
            // cordon: banda clara al borde de la vereda (a ras: solo visual)
            B("Curb", new Vector3(0f, 0.012f, doorZ - 6.62f), new Vector3(14f, 0.026f, 0.25f), Lit("Curb", new Color(0.62f, 0.6f, 0.56f), 0.1f));
        }

        // ---------- techo, zocalos, friso de madera, molduras y marco de la puerta
        static void Architecture(float hx, float backZ, float doorZ, float doorX)
        {
            var g = Group("Architecture");
            var darkWood = Lit("Wood_Dark", new Color(0.2f, 0.11f, 0.06f), 0.35f);
            var panel = Lit("Wainscot", new Color(0.3f, 0.16f, 0.08f), 0.3f);
            var trim = Lit("Trim", new Color(0.13f, 0.08f, 0.05f), 0.4f);
            var ceiling = Lit("Ceiling", new Color(0.16f, 0.12f, 0.1f), 0.1f);
            float midZ = (backZ + doorZ) * 0.5f, depth = backZ - doorZ, innerFront = doorZ + 0.1f;
            var ceil = B("Ceiling", new Vector3(0f, 3.06f, midZ), new Vector3(hx * 2f + 0.4f, 0.12f, depth + 0.4f), ceiling, g);
            for (float z = doorZ + 2f; z < backZ - 0.5f; z += 3f) B("Beam", new Vector3(0f, 2.9f, z), new Vector3(hx * 2f, 0.2f, 0.24f), darkWood, g);
            // friso (0-1 m), su moldura y el zocalo, por dentro de cada muro; el frente se corta en la puerta
            void Run(string n, Vector3 a, Vector3 b, float nx, float nz)
            {   // de a a b a lo largo de la cara interior; n = normal hacia el salon
                Vector3 mid = (a + b) * 0.5f; float len = Vector3.Distance(a, b);
                bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
                Vector3 S(float th, float h) => alongX ? new Vector3(len, h, th) : new Vector3(th, h, len);
                Vector3 off(float d) => new Vector3(nx * d, 0f, nz * d);
                B(n + "_Panel", mid + off(0.012f) + Vector3.up * 0.52f, S(0.024f, 1.0f), panel, g);
                B(n + "_Rail", mid + off(0.03f) + Vector3.up * 1.03f, S(0.06f, 0.06f), trim, g);
                B(n + "_Base", mid + off(0.035f) + Vector3.up * 0.07f, S(0.07f, 0.14f), trim, g);
                B(n + "_Crown", mid + off(0.03f) + Vector3.up * 2.94f, S(0.06f, 0.08f), trim, g);
            }
            Run("Back", new Vector3(-hx, 0, backZ), new Vector3(hx, 0, backZ), 0, -1);
            Run("Left", new Vector3(-hx, 0, innerFront), new Vector3(-hx, 0, backZ), 1, 0);
            Run("Right", new Vector3(hx, 0, innerFront), new Vector3(hx, 0, backZ), -1, 0);
            Run("FrontL", new Vector3(-hx, 0, innerFront), new Vector3(doorX - 0.9f, 0, innerFront), 0, 1);
            Run("FrontR", new Vector3(doorX + 0.9f, 0, innerFront), new Vector3(hx, 0, innerFront), 0, 1);
            // marco de la puerta: jambas y dintel que asoman 5 cm a cada lado del muro (fuera del vano de 1,5 m)
            foreach (var s in new[] { -1f, 1f }) B("Door_Jamb", new Vector3(doorX + s * 0.81f, 1.12f, doorZ), new Vector3(0.12f, 2.24f, 0.3f), trim, g);
            B("Door_Head", new Vector3(doorX, 2.29f, doorZ), new Vector3(1.74f, 0.12f, 0.3f), trim, g);
            // umbral metalico (a ras)
            B("Door_Sill", new Vector3(doorX, 0.006f, doorZ), new Vector3(1.5f, 0.012f, 0.24f), Lit("Sill", new Color(0.45f, 0.42f, 0.38f), 0.6f, 0.8f), g);
        }

        // ---------- fachada: zocalo, cornisa, cartel de neon, aplique y edificios vecinos
        static void Facade(float hx, float doorZ, float doorX)
        {
            var g = Group("Facade");
            float outer = doorZ - 0.1f;
            var plinth = Lit("Facade_Plinth", new Color(0.16f, 0.14f, 0.13f), 0.15f);
            var cornice = Lit("Facade_Cornice", new Color(0.22f, 0.17f, 0.14f), 0.2f);
            B("Plinth_L", new Vector3((-hx + doorX - 0.85f) * 0.5f, 0.25f, outer - 0.02f), new Vector3(hx + doorX - 0.85f, 0.5f, 0.05f), plinth, g);
            B("Plinth_R", new Vector3((hx + doorX + 0.85f) * 0.5f, 0.25f, outer - 0.02f), new Vector3(hx - doorX - 0.85f, 0.5f, 0.05f), plinth, g);
            B("Cornice", new Vector3(0f, 3.05f, outer - 0.06f), new Vector3(hx * 2f + 0.5f, 0.2f, 0.16f), cornice, g);
            // cartel: tabla oscura, marco de tubo de neon y nombre
            var board = Lit("Sign_Board", new Color(0.05f, 0.04f, 0.05f), 0.3f);
            var neon = Glow("Neon_Pink", NeonPink, 6f);
            B("Sign_Board", new Vector3(doorX, 2.68f, outer - 0.06f), new Vector3(2.9f, 0.56f, 0.06f), board, g);
            B("Neon_Top", new Vector3(doorX, 2.93f, outer - 0.1f), new Vector3(2.8f, 0.035f, 0.035f), neon, g);
            B("Neon_Bottom", new Vector3(doorX, 2.43f, outer - 0.1f), new Vector3(2.8f, 0.035f, 0.035f), neon, g);
            Sign("Sign_Name", "THE LAST ROUND", new Vector3(doorX, 2.69f, outer - 0.12f), Quaternion.identity, new Color(1f, 0.55f, 0.75f), 0.03f, g);
            L("Sign_Glow", LightType.Point, new Vector3(doorX, 2.6f, outer - 0.8f), NeonPink, 2.2f, 4.5f, g);
            // apliques a los lados de la puerta
            var sconce = Glow("Sconce", Tungsten, 4f);
            foreach (var s in new[] { -1f, 1f })
            {
                B("Sconce", new Vector3(doorX + s * 1.25f, 2.05f, outer - 0.08f), new Vector3(0.14f, 0.22f, 0.12f), sconce, g);
                L("Sconce_Light", LightType.Point, new Vector3(doorX + s * 1.25f, 2.0f, outer - 0.45f), Tungsten, 1.4f, 4.5f, g);
            }
            // vecinos pegados al bar (cierran la vista desde la vereda; fuera del area jugable)
            var brick = Lit("Neighbour_Brick", new Color(0.24f, 0.13f, 0.1f), 0.1f);
            var stucco = Lit("Neighbour_Stucco", new Color(0.2f, 0.21f, 0.23f), 0.1f);
            B("Neighbour_E", new Vector3(hx + 4.4f, 2.7f, 4f), new Vector3(8.4f, 5.4f, 12.6f), brick, g);
            B("Neighbour_W", new Vector3(-hx - 4.4f, 3.3f, 4f), new Vector3(8.4f, 6.6f, 12.6f), stucco, g);
            var win = Glow("Window_Warm", new Color(1f, 0.75f, 0.45f), 1.6f);
            var winCold = Glow("Window_Cold", new Color(0.5f, 0.7f, 1f), 1.2f);
            var dark = Lit("Window_Dark", new Color(0.04f, 0.05f, 0.07f), 0.8f);
            var rnd = new System.Random(3);
            Material W() { int k = rnd.Next(10); return k < 2 ? win : k < 3 ? winCold : dark; }
            for (int i = 0; i < 3; i++) B("Win_E", new Vector3(hx + 1.6f + i * 2.4f, 3.9f, -2.12f), new Vector3(1.1f, 1.2f, 0.04f), W(), g);
            for (int i = 0; i < 3; i++) B("Win_W", new Vector3(-hx - 1.6f - i * 2.4f, 4.4f, -2.12f), new Vector3(1.1f, 1.3f, 0.04f), W(), g);
            // cuadra de enfrente
            for (int i = 0; i < 5; i++)
            {
                float x = -14f + i * 7f, h = 6f + (i * 37 % 5);
                B("Block", new Vector3(x, h * 0.5f, -21.2f), new Vector3(6.8f, h, 2f), i % 2 == 0 ? brick : stucco, g);
                for (int fl = 1; fl < (int)(h / 2.6f); fl++) for (int c = 0; c < 3; c++)
                        B("Win", new Vector3(x - 2f + c * 2f, fl * 2.6f + 0.4f, -20.18f), new Vector3(1f, 1.2f, 0.04f), W(), g);
            }
        }

        // ---------- luces del interior: focos de la barra (con sombra), colgantes de mesa, relleno y karaoke
        static void InteriorLights()
        {
            var g = Group("Interior_Lights");
            var shade = Lit("Lamp_Shade", new Color(0.08f, 0.22f, 0.15f), 0.6f, 0.3f);
            var cord = Lit("Lamp_Cord", new Color(0.03f, 0.03f, 0.03f), 0.2f);
            var bulb = Glow("Bulb", Tungsten, 5f);
            void Pendant(string n, Vector3 at, float intensity, float range, bool spot, bool shadows)
            {
                V(PrimitiveType.Cylinder, n + "_Cord", g, new Vector3(at.x, (3f + at.y + 0.15f) * 0.5f, at.z), new Vector3(0.012f, (3f - at.y - 0.15f) * 0.5f, 0.012f), cord);
                V(PrimitiveType.Cylinder, n + "_Shade", g, at + Vector3.up * 0.06f, new Vector3(0.42f, 0.07f, 0.42f), shade);
                V(PrimitiveType.Sphere, n + "_Bulb", g, at - Vector3.up * 0.04f, new Vector3(0.11f, 0.11f, 0.11f), bulb);
                if (spot) L(n + "_Light", LightType.Spot, at - Vector3.up * 0.1f, Tungsten, intensity, range, g, shadows, at + Vector3.down * 3f, 118f);
                else L(n + "_Light", LightType.Point, at - Vector3.up * 0.15f, Tungsten, intensity, range, g, shadows);
            }
            // barra y tio: los dos focos con sombra (lectura del cuerpo y de las poses de ataque)
            // sobre el mostrador (fuera de la linea de vista entre el salon y el cartel de la pared)
            Pendant("Bar_A", new Vector3(3.4f, 2.2f, 8.6f), 9f, 7f, true, true);
            Pendant("Bar_B", new Vector3(5.6f, 2.2f, 8.6f), 9f, 7f, true, false);
            foreach (var p in new[] { new Vector3(-4.2f, 2.0f, 6.2f), new Vector3(-4.5f, 2.0f, 2.6f), new Vector3(-6.8f, 2.0f, 4.4f), new Vector3(7.6f, 2.0f, 4.2f) })
                Pendant("Table", p, 2.4f, 4.2f, false, false);
            // salon y recorrido: relleno calido sin sombra (para que nada importante quede en negro)
            L("Fill_Hall", LightType.Point, new Vector3(1.5f, 1.9f, 4.5f), new Color(1f, 0.8f, 0.6f), 1.8f, 9f, g);
            L("Fill_Door", LightType.Point, new Vector3(0.3f, 1.9f, 0.4f), new Color(1f, 0.82f, 0.62f), 1.4f, 6f, g);
            L("Fill_Cart", LightType.Point, new Vector3(5.4f, 1.9f, 2.4f), new Color(1f, 0.8f, 0.6f), 1.4f, 5f, g);
            // karaoke: cenital magenta sobre la tarima
            L("Karaoke_Spot", LightType.Spot, new Vector3(-7f, 2.85f, 6.6f), NeonMagenta, 6f, 6f, g, false, new Vector3(-7.4f, 0.2f, 8.6f), 70f);
        }

        // ---------- identidad: estante de botellas, carteles de neon, cuadros, diana, cartel de salida
        static void BarIdentity(float hx, float backZ, float doorZ)
        {
            var g = Group("Bar_Identity");
            var darkWood = Lit("Wood_Dark", new Color(0.2f, 0.11f, 0.06f), 0.35f);
            float wall = backZ - 0.03f;
            B("BackBar_Panel", new Vector3(4.5f, 1.75f, wall - 0.01f), new Vector3(4.2f, 1.3f, 0.02f), Lit("BackBar_Mirror", new Color(0.12f, 0.14f, 0.15f), 0.92f, 0.6f), g);
            var glassCols = new[] { new Color(0.55f, 0.32f, 0.08f), new Color(0.12f, 0.35f, 0.15f), new Color(0.75f, 0.78f, 0.72f), new Color(0.45f, 0.08f, 0.1f) };
            var glassMats = new Material[glassCols.Length];
            for (int i = 0; i < glassCols.Length; i++) { glassMats[i] = Glow("Bottle_" + i, glassCols[i], 0.35f); glassMats[i].SetFloat("_Smoothness", 0.9f); }
            foreach (var y in new[] { 1.3f, 1.8f })
            {
                B("Shelf", new Vector3(4.5f, y, wall - 0.14f), new Vector3(4.0f, 0.04f, 0.26f), darkWood, g);
                for (int i = 0; i < 11; i++)
                {
                    float x = 2.75f + i * 0.35f + (i % 3) * 0.02f, h = 0.2f + (i * 7 % 4) * 0.025f;
                    var m = glassMats[(i + (y > 1.5f ? 2 : 0)) % glassMats.Length];
                    V(PrimitiveType.Cylinder, "Bottle", g, new Vector3(x, y + 0.02f + h * 0.5f, wall - 0.14f), new Vector3(0.075f, h * 0.5f, 0.075f), m);
                    V(PrimitiveType.Cylinder, "Neck", g, new Vector3(x, y + 0.02f + h + 0.05f, wall - 0.14f), new Vector3(0.028f, 0.05f, 0.028f), m);
                }
            }
            // neon sobre el estante y sobre la tarima
            Sign("Neon_Beer", "CERVEZA HELADA", new Vector3(4.5f, 2.58f, wall - 0.03f), Quaternion.identity, new Color(1f, 0.78f, 0.35f), 0.025f, g);
            L("Neon_Beer_Glow", LightType.Point, new Vector3(4.5f, 2.45f, wall - 0.6f), new Color(1f, 0.7f, 0.3f), 1.2f, 3f, g);
            Sign("Neon_Karaoke", "KARAOKE", new Vector3(-hx + 0.03f, 2.35f, 8.4f), Quaternion.Euler(0, -90, 0), new Color(0.95f, 0.45f, 1f), 0.03f, g);
            L("Neon_Karaoke_Glow", LightType.Point, new Vector3(-hx + 0.6f, 2.3f, 8.4f), NeonMagenta, 1.4f, 3.5f, g);
            // cuadros en el muro izquierdo y diana en el derecho (lejos de los recorridos; sin colision)
            var frame = Lit("Frame", new Color(0.1f, 0.06f, 0.03f), 0.5f);
            var posters = new[] { Lit("Poster_Gold", new Color(0.75f, 0.55f, 0.18f), 0.2f), Lit("Poster_Red", new Color(0.55f, 0.12f, 0.1f), 0.2f), Lit("Poster_Teal", new Color(0.12f, 0.38f, 0.36f), 0.2f) };
            float[] pz = { 1.4f, 3.6f, 5.6f };
            for (int i = 0; i < pz.Length; i++)
            {
                B("Frame", new Vector3(-hx + 0.03f, 1.85f, pz[i]), new Vector3(0.04f, 0.8f, 0.6f), frame, g);
                B("Poster", new Vector3(-hx + 0.055f, 1.85f, pz[i]), new Vector3(0.01f, 0.68f, 0.48f), posters[i], g);
            }
            var cork = Lit("Dart_Board", new Color(0.12f, 0.1f, 0.08f), 0.2f);
            var ring = Lit("Dart_Ring", new Color(0.6f, 0.12f, 0.1f), 0.3f);
            var rot = Quaternion.Euler(0, 0, 90);
            V(PrimitiveType.Cylinder, "Dart_Board", g, new Vector3(hx - 0.03f, 1.75f, 7.2f), new Vector3(0.46f, 0.02f, 0.46f), cork, rot);
            V(PrimitiveType.Cylinder, "Dart_Ring", g, new Vector3(hx - 0.05f, 1.75f, 7.2f), new Vector3(0.3f, 0.01f, 0.3f), ring, rot);
            V(PrimitiveType.Cylinder, "Dart_Bull", g, new Vector3(hx - 0.065f, 1.75f, 7.2f), new Vector3(0.07f, 0.01f, 0.07f), Lit("Dart_Bull", new Color(0.15f, 0.45f, 0.2f), 0.3f), rot);
            // cartel de SALIDA sobre la puerta (guia hacia la camioneta)
            var exitBox = Glow("Exit_Sign", ExitGreen, 2.2f);
            B("Exit_Sign", new Vector3(0f, 2.55f, doorZ + 0.14f), new Vector3(0.62f, 0.2f, 0.06f), exitBox, g);
            Sign("Exit_Text", "SALIDA", new Vector3(0f, 2.55f, doorZ + 0.18f), Quaternion.Euler(0, 180, 0), new Color(0.05f, 0.15f, 0.07f), 0.016f, g);
            L("Exit_Glow", LightType.Point, new Vector3(0f, 2.45f, doorZ + 0.5f), ExitGreen, 0.8f, 2.5f, g);
        }

        // ---------- calle: farol de sodio, marcas viales, contenedor y cajones (fuera del area jugable)
        static void Street(float doorZ)
        {
            var g = Group("Street");
            var pole = Lit("Lamp_Pole", new Color(0.12f, 0.13f, 0.14f), 0.5f, 0.7f);
            var head = Glow("Lamp_Head", Sodium, 5f);
            var p = new Vector3(4.8f, 0f, doorZ - 6.3f);
            V(PrimitiveType.Cylinder, "Lamp_Pole", g, p + Vector3.up * 2.2f, new Vector3(0.12f, 2.2f, 0.12f), pole);
            B("Lamp_Arm", p + new Vector3(-0.7f, 4.35f, 0f), new Vector3(1.5f, 0.07f, 0.07f), pole, g);
            B("Lamp_Head", p + new Vector3(-1.35f, 4.27f, 0f), new Vector3(0.5f, 0.1f, 0.24f), head, g);
            L("Street_Light", LightType.Spot, p + new Vector3(-1.35f, 4.2f, 0f), Sodium, 22f, 12f, g, true, p + new Vector3(-2.6f, 0f, -1.5f), 105f);
            var paint = Lit("Road_Paint", new Color(0.85f, 0.72f, 0.25f), 0.3f);
            for (float x = -14f; x < 15f; x += 3f) B("Lane_Dash", new Vector3(x, 0.006f, -16.8f), new Vector3(1.6f, 0.012f, 0.14f), paint, g);
            var bin = Lit("Bin", new Color(0.12f, 0.25f, 0.16f), 0.35f, 0.4f);
            B("Dumpster", new Vector3(-10.3f, 0.65f, -4.2f), new Vector3(1.8f, 1.3f, 1.1f), bin, g);
            B("Dumpster_Lid", new Vector3(-10.3f, 1.33f, -4.2f), new Vector3(1.85f, 0.06f, 1.15f), Lit("Bin_Lid", new Color(0.05f, 0.08f, 0.06f), 0.3f), g);
            var crate = Lit("Crate", new Color(0.62f, 0.12f, 0.1f), 0.25f);
            B("Crate", new Vector3(-9.6f, 0.17f, -3.0f), new Vector3(0.5f, 0.34f, 0.36f), crate, g);
            B("Crate", new Vector3(-9.55f, 0.51f, -3.05f), new Vector3(0.5f, 0.34f, 0.36f), crate, g);
        }

        // ---------- camioneta: luces, paragolpes, ventanillas, rampa y zona con cinta
        static void Van(Transform env)
        {
            var van = env.Find("Passenger_Van"); if (!van) return;
            const float rearZ = -9.25f, frontZ = -14.65f, halfW = 1.25f, cabZ = -12.4f, floorY = 0.30f, roofY = 2.3f;
            // El kit trae un bloque de prueba (placas planas, sin techo visible): se reemplaza por una carroceria propia que
            // sigue la caja funcional. Colisiones, piso, rampa, zona y hojas de las puertas quedan como estaban.
            foreach (var r in van.GetComponentsInChildren<Renderer>(true))
                if (r.transform.IsChildOf(van) && (r.name.StartsWith("Van_Side") || r.name == "Van_Roof" || r.name == "Van_Front" || r.name == "Van_Cab_Partition" || r.name == "Van_Rear_Lintel"
                    || r.GetComponentInParent<Transform>().name.StartsWith("Kit_") || (r.transform.parent && (r.transform.parent.name.EndsWith("_Fit") || r.transform.parent.name.StartsWith("Kit_")))))
                    r.enabled = false;
            var body = Lit("Van_Body", new Color(0.78f, 0.7f, 0.55f), 0.45f, 0.1f);       // crema gastada
            var inner = Lit("Van_Inner", new Color(0.3f, 0.27f, 0.23f), 0.15f);
            var stripe = Lit("Van_Stripe", new Color(0.72f, 0.28f, 0.12f), 0.4f);
            var black = Lit("Van_Black", new Color(0.05f, 0.05f, 0.05f), 0.3f);
            var tire = Lit("Van_Tire", new Color(0.04f, 0.04f, 0.045f), 0.15f);
            var hub = Lit("Van_Hub", new Color(0.55f, 0.56f, 0.58f), 0.7f, 0.9f);
            var g = new GameObject("Van_Body").transform; g.SetParent(van, false);
            float cargoMid = (rearZ + cabZ) * 0.5f, len = rearZ - frontZ, mid = (rearZ + frontZ) * 0.5f;
            foreach (var s in new[] { -1f, 1f })
            {
                // laterales: cara exterior crema y cara interior oscura; zocalo negro y franja oxido
                B("Van_SideOuter", new Vector3(s * (halfW - 0.02f), (floorY + roofY) * 0.5f + 0.02f, mid), new Vector3(0.06f, roofY - floorY + 0.06f, len), body, g);
                B("Van_SideInner", new Vector3(s * (halfW - 0.075f), (floorY + roofY) * 0.5f, cargoMid), new Vector3(0.03f, roofY - floorY - 0.02f, rearZ - cabZ - 0.04f), inner, g);
                B("Van_Rocker", new Vector3(s * (halfW + 0.005f), floorY + 0.06f, mid), new Vector3(0.05f, 0.24f, len - 0.1f), black, g);
                B("Van_Stripe", new Vector3(s * (halfW + 0.012f), 1.18f, mid), new Vector3(0.02f, 0.14f, len - 0.2f), stripe, g);
                // ruedas con taza y guardabarros oscuros
                foreach (var wz in new[] { rearZ - 1.0f, frontZ + 1.1f })
                {
                    V(PrimitiveType.Cylinder, "Van_Tire", g, new Vector3(s * (halfW - 0.04f), 0.36f, wz), new Vector3(0.72f, 0.12f, 0.72f), tire, Quaternion.Euler(0, 0, 90));
                    V(PrimitiveType.Cylinder, "Van_Hub", g, new Vector3(s * (halfW + 0.03f), 0.36f, wz), new Vector3(0.36f, 0.02f, 0.36f), hub, Quaternion.Euler(0, 0, 90));
                    B("Van_Arch", new Vector3(s * (halfW + 0.01f), 0.78f, wz), new Vector3(0.05f, 0.12f, 0.95f), black, g);
                }
            }
            B("Van_RoofShell", new Vector3(0f, roofY + 0.04f, mid), new Vector3(halfW * 2f + 0.02f, 0.1f, len), body, g);
            B("Van_RoofInner", new Vector3(0f, roofY - 0.03f, cargoMid), new Vector3(halfW * 2f - 0.15f, 0.03f, rearZ - cabZ), inner, g);
            B("Van_RoofRack", new Vector3(0f, roofY + 0.14f, cargoMid), new Vector3(1.6f, 0.05f, 2.2f), black, g);
            B("Van_Partition", new Vector3(0f, (floorY + roofY) * 0.5f, cabZ), new Vector3(halfW * 2f - 0.1f, roofY - floorY, 0.08f), inner, g);
            B("Van_RearHeader", new Vector3(0f, roofY - 0.08f, rearZ + 0.04f), new Vector3(halfW * 2f, 0.18f, 0.1f), body, g);
            foreach (var s in new[] { -1f, 1f }) B("Van_RearPillar", new Vector3(s * (halfW - 0.06f), (floorY + roofY) * 0.5f, rearZ + 0.04f), new Vector3(0.12f, roofY - floorY, 0.1f), body, g);
            // frente: capot bajo, parabrisas inclinado oscuro, paragolpes y parrilla
            B("Van_Nose", new Vector3(0f, 0.75f, frontZ + 0.05f), new Vector3(halfW * 2f, 0.9f, 0.1f), body, g);
            var ws = V(PrimitiveType.Cube, "Van_Windshield", g, new Vector3(0f, 1.72f, frontZ + 0.32f), new Vector3(halfW * 2f - 0.14f, 1.05f, 0.05f),
                Lit("Van_Glass", new Color(0.05f, 0.07f, 0.09f), 0.92f, 0.4f), Quaternion.Euler(-28f, 0f, 0f));
            B("Van_Grille", new Vector3(0f, 0.62f, frontZ - 0.01f), new Vector3(1.1f, 0.28f, 0.03f), black, g);
            B("Van_FrontBumper", new Vector3(0f, 0.3f, frontZ - 0.05f), new Vector3(halfW * 2f + 0.06f, 0.18f, 0.14f), black, g);
            var tail = Glow("Van_Tail", new Color(1f, 0.08f, 0.05f), 3.5f);
            var head = Glow("Van_Head", new Color(1f, 0.95f, 0.8f), 3f);
            var bumper = Lit("Van_Bumper", new Color(0.08f, 0.08f, 0.09f), 0.35f, 0.3f);
            var glass = Lit("Van_Glass", new Color(0.05f, 0.07f, 0.09f), 0.92f, 0.4f);
            foreach (var s in new[] { -1f, 1f })
            {
                B("Van_TailLight", new Vector3(s * (halfW + 0.01f), 1.05f, rearZ - 0.08f), new Vector3(0.06f, 0.34f, 0.12f), tail, van);
                B("Van_Bumper", new Vector3(s * (halfW - 0.18f), 0.2f, rearZ + 0.02f), new Vector3(0.36f, 0.16f, 0.16f), bumper, van);
                B("Van_HeadLight", new Vector3(s * 0.82f, 0.95f, frontZ - 0.02f), new Vector3(0.36f, 0.16f, 0.05f), head, van);
                B("Van_SideWindow", new Vector3(s * (halfW + 0.012f), 1.6f, (cabZ + frontZ) * 0.5f), new Vector3(0.02f, 0.6f, 1.6f), glass, van);
                B("Van_CargoWindow", new Vector3(s * (halfW + 0.012f), 1.7f, cabZ + 1.2f), new Vector3(0.02f, 0.45f, 1.4f), glass, van);
            }
            B("Van_DomeLamp", new Vector3(0f, roofY - 0.06f, (rearZ + cabZ) * 0.5f), new Vector3(0.5f, 0.04f, 0.18f), Glow("Van_Dome", new Color(1f, 0.85f, 0.6f), 3f), van);
            L("Van_DomeLight", LightType.Point, new Vector3(0f, roofY - 0.25f, (rearZ + cabZ) * 0.5f), new Color(1f, 0.82f, 0.6f), 1.6f, 3.6f, van);
            L("Van_TailGlow", LightType.Point, new Vector3(0f, 1.0f, rearZ + 0.5f), new Color(1f, 0.15f, 0.1f), 0.8f, 3f, van);
            // zona de extraccion: piso de goma oscuro con cinta amarilla (sigue mostrando donde va el tio)
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r.name == "Van_Floor") r.sharedMaterial = Lit("Van_CargoFloor", new Color(0.14f, 0.13f, 0.12f), 0.2f);
                else if (r.name == "Zone_Marker")
                {
                    r.sharedMaterial = Lit("Zone_Rubber", new Color(0.1f, 0.1f, 0.11f), 0.25f);
                    var b = r.bounds; var tape = Glow("Zone_Tape", new Color(1f, 0.78f, 0.12f), 0.6f);
                    float y = b.max.y + 0.004f;
                    B("Zone_Tape", new Vector3(b.center.x, y, b.min.z + 0.03f), new Vector3(b.size.x, 0.006f, 0.06f), tape, r.transform.parent);
                    B("Zone_Tape", new Vector3(b.center.x, y, b.max.z - 0.03f), new Vector3(b.size.x, 0.006f, 0.06f), tape, r.transform.parent);
                    B("Zone_Tape", new Vector3(b.min.x + 0.03f, y, b.center.z), new Vector3(0.06f, 0.006f, b.size.z), tape, r.transform.parent);
                    B("Zone_Tape", new Vector3(b.max.x - 0.03f, y, b.center.z), new Vector3(0.06f, 0.006f, b.size.z), tape, r.transform.parent);
                }
                else if (r.transform.parent && r.transform.parent.name.StartsWith("Van_Door_")) r.sharedMaterial = body; // hojas color carroceria
                else if (r.name == "Van_Ramp")
                {
                    r.sharedMaterial = Lit("Ramp_Steel", new Color(0.32f, 0.33f, 0.35f), 0.55f, 0.85f);
                    var yellow = Lit("Ramp_Edge", new Color(0.9f, 0.7f, 0.12f), 0.3f);
                    foreach (var s in new[] { -1f, 1f })
                    {
                        var e = B("Ramp_Edge", Vector3.zero, Vector3.one, yellow, r.transform);
                        e.transform.localPosition = new Vector3(s * 0.47f, 0.55f, 0f); e.transform.localRotation = Quaternion.identity;
                        e.transform.localScale = new Vector3(0.05f, 0.2f, 1f);
                    }
                }
        }

        // ---------- posproceso: tonemapping, bloom contenido, contraste y vineta suave
        static void PostFx()
        {
            const string path = "Assets/Game/Settings/PostFX_Bar.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
            T Get<T>() where T : VolumeComponent { if (!profile.TryGet<T>(out var c)) { c = profile.Add<T>(true); AssetDatabase.AddObjectToAsset(c, profile); } c.active = true; return c; }
            var tm = Get<Tonemapping>(); tm.mode.Override(TonemappingMode.ACES);
            var bloom = Get<Bloom>(); bloom.threshold.Override(1.05f); bloom.intensity.Override(0.55f); bloom.scatter.Override(0.6f);
            var ca = Get<ColorAdjustments>(); ca.postExposure.Override(0.7f); ca.contrast.Override(14f); ca.saturation.Override(6f);
            var vig = Get<Vignette>(); vig.intensity.Override(0.24f); vig.smoothness.Override(0.45f);
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
            var go = new GameObject("PostFX"); go.transform.SetParent(root, false);
            var vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = profile; vol.priority = 1;
            go.AddComponent<ScenePostFx>(); // posproceso en la camara local solo en esta escena
        }
    }
}
