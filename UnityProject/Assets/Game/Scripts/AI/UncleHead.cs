using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UncleExtraction
{
    /// Cabeza de produccion del tio (sesion 20, UE-14). Reemplaza la cabeza del kit (cara fundida, sin expresiones) por una
    /// cabeza generada por codigo con parametros editables (Style): craneo con papada y mofletes, nariz de borracho, orejas,
    /// ojos con iris y parpados que se cierran, cejas pobladas, bigote de morsa, pelo en herradura y boca que se abre.
    /// Metodo reproducible (sin modelos externos ni licencias): mismas formas en cada arranque, sin aleatoriedad.
    ///
    /// Contrato con el juego: SOLO presentacion. No agrega colliders ni cuerpos, no toca joints, masas ni sockets; se cuelga
    /// de la parte fisica de la cabeza y corre igual en todos los pares leyendo el estado replicado del tio. La cabeza del kit
    /// se oculta colapsando su hueso (la piel del cuello que comparte peso queda dentro de la cabeza nueva).
    ///
    /// Expresiones (lectura rapida a distancia de agarre): borracho (parpados a media asta, cejas caidas, parpadeo lento),
    /// resistencia (ojos entrecerrados, cejas fruncidas, boca apretada), anticipacion de golpe (ojos muy abiertos, cejas
    /// arriba, boca abierta: se ve venir), golpe (grito), caido/recuperando (aturdido), distraido (ilusion), asegurado (dormido).
    /// -ueOldFace vuelve a la cara anterior para comparar; -ueHeadVariant Fiesta|Boina elige la silueta.
    public class UncleHead : MonoBehaviour
    {
        public enum Variant { Fiesta, Boina }
        [System.Serializable]
        public class Style
        {
            public Variant variant = Variant.Fiesta;
            public Vector3 skull = new Vector3(1.0f, 1.08f, 1.0f);   // proporcion del craneo (sobre el tamaño de la cabeza del kit)
            public float jowls = 0.22f, chin = 0.18f, brow = 0.08f, cheeks = 0.12f;
            public float noseSize = 0.30f, eyeSize = 0.15f, mustache = 1.0f;
            public Color skin = new Color(0.86f, 0.58f, 0.45f), nose = new Color(0.86f, 0.36f, 0.32f), hair = new Color(0.16f, 0.11f, 0.08f);
            public Color hairGrey = new Color(0.55f, 0.52f, 0.5f), iris = new Color(0.33f, 0.22f, 0.12f), mouthIn = new Color(0.32f, 0.07f, 0.08f);
            public Color cap = new Color(0.22f, 0.26f, 0.3f);
        }
        public Style style = new Style();

        public static bool UseNewHead => !QaArgs.Has("-ueOldFace");
        /// Ajuste a la cabeza del kit (coordenadas del tio, frente +z), medido por ArticulatedUncleBuilder.
        public bool fitted; public Vector3 fitCenter; public float fitHalfW;
        public void Fit(SkinnedMeshRenderer smr, Transform uncle, float faceSign) { fitted = Measure(smr, uncle, faceSign, out fitCenter, out fitHalfW); }

        /// Centro y medio ancho de los vertices con peso del hueso de la cabeza (>= 0,6), en coordenadas del tio.
        public static bool Measure(SkinnedMeshRenderer smr, Transform uncle, float fs, out Vector3 center, out float halfW)
        {
            center = Vector3.zero; halfW = 0f;
            if (!smr || !smr.sharedMesh || !(smr.sharedMesh.isReadable || Application.isEditor)) return false;
            var bones = smr.bones; int hb = -1;
            for (int i = 0; i < bones.Length; i++) if (bones[i] && bones[i].name.ToLowerInvariant().StartsWith("head") && !bones[i].name.Contains("_end")) { hb = i; break; }
            if (hb < 0) return false;
            var m = smr.sharedMesh; var v = m.vertices; var bw = m.boneWeights; var bp = m.bindposes;
            if (v.Length == 0 || bw.Length != v.Length) return false;
            var mtx = bones[hb].localToWorldMatrix * bp[hb];
            Vector3 mn = Vector3.one * 1e9f, mx = -Vector3.one * 1e9f; int n = 0;
            for (int i = 0; i < v.Length; i++)
            {
                var w = bw[i]; float wt = w.boneIndex0 == hb ? w.weight0 : w.boneIndex1 == hb ? w.weight1 : w.boneIndex2 == hb ? w.weight2 : w.boneIndex3 == hb ? w.weight3 : 0f;
                if (wt < 0.6f) continue;
                Vector3 u = uncle.InverseTransformPoint(mtx.MultiplyPoint3x4(v[i])); u.x *= fs; u.z *= fs;
                mn = Vector3.Min(mn, u); mx = Vector3.Max(mx, u); n++;
            }
            if (n < 50) return false;
            center = (mn + mx) * 0.5f; halfW = Mathf.Clamp((mx.x - mn.x) * 0.5f, 0.12f, 0.3f);
            Debug.Log($"[Head] cabeza del kit medida: vertices={n} min={mn} max={mx}");
            return true;
        }
        public bool Built { get; private set; }
        public float Lid01 { get; private set; }       // 0 abierto, 1 cerrado (diagnostico y pruebas)
        public string Mood { get; private set; } = "";

        NpcBrain brain; NpcBody body; Transform anchor, root;
        float R;                                // radio de referencia (mitad del ancho de la cabeza del kit)
        Transform[] eye = new Transform[2], iris = new Transform[2], lidUp = new Transform[2], lidLo = new Transform[2], browT = new Transform[2];
        Transform mouthT, jawT, mustacheT;
        Vector3[] browPos0 = new Vector3[2];
        float lid, lidLow, browUp, browAngle, mouthOpen, mouthWide, blinkAt, blinkT, wobble;
        Vector3 lookDir = Vector3.forward;

        void Awake()
        {
            if (!UseNewHead) { enabled = false; return; }
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-ueHeadVariant" && System.Enum.TryParse(a[i + 1], true, out Variant v)) style.variant = v;
        }

        void Start()
        {
            brain = GetComponent<NpcBrain>(); body = GetComponent<NpcBody>();
            anchor = body && body.head ? body.head.parent : null;
            if (!anchor) { enabled = false; return; }
            Build();
        }

        // ---------------------------------------------------------------- construccion
        void Build()
        {
            float fs = body.FaceSign;
            Vector3 W(Vector3 u) => transform.TransformPoint(new Vector3(u.x * fs, u.y, u.z * fs));       // coordenadas del tio -> mundo
            Quaternion faceRot = transform.rotation * (fs < 0f ? Quaternion.Euler(0, 180f, 0) : Quaternion.identity);

            // medida de la cabeza del kit: la guarda el constructor (en el editor la malla se puede leer); si falta, se mide
            // aca si la malla es legible; si no, valores por defecto
            Vector3 center = fitted ? fitCenter : new Vector3(0f, 1.72f, 0.04f); float halfW = fitted ? fitHalfW : 0.2f; Transform headBone = null;
            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr)
            {
                foreach (var bn in smr.bones) if (bn && bn.name.ToLowerInvariant().StartsWith("head") && !bn.name.Contains("_end")) { headBone = bn; break; }
                if (!fitted && Measure(smr, transform, fs, out var c2, out var h2)) { center = c2; halfW = h2; }
            }
            Debug.Log($"[Head] ajuste a la cabeza del kit: medido={(fitted ? "constructor" : "ejecucion/defecto")} centro={center} medio_ancho={halfW:0.000} hueso={(headBone ? headBone.name : "-")}");
            R = halfW;
            root = new GameObject("UncleHead_Visual").transform;
            root.SetPositionAndRotation(W(center), faceRot);
            // escala del craneo: el frente local es +z; todo se arma en espacio local de 'root' (metros)
            var skinM = Mat("Uncle_Skin", style.skin, 0.35f); var noseM = Tinted(skinM, style.nose, "Uncle_NoseSkin", 0.45f);
            var hairM = Mat("Uncle_Hair", style.hair, 0.25f); var greyM = Tinted(hairM, style.hairGrey, "Uncle_HairGrey", 0.2f);
            var whiteM = Mat("Uncle_EyeWhite", new Color(0.95f, 0.93f, 0.88f), 0.85f); var pupilM = Mat("Uncle_Pupil", new Color(0.02f, 0.02f, 0.02f), 0.95f);
            var irisM = Tinted(whiteM, style.iris, "Uncle_Iris", 0.8f); var mouthM = Tinted(skinM, style.mouthIn, "Uncle_MouthIn", 0.2f);
            var lipM = Tinted(skinM, Color.Lerp(style.skin, style.nose, 0.55f) * 0.9f, "Uncle_Lip", 0.4f);

            Vector3 S = style.skull * R; skullS = S;
            Part("Skull", SculptedSkull(S), skinM, root);
            // cada rasgo se apoya en la superficie REAL del craneo esculpido (mofletes y papada incluidos)
            Vector3 F = Vector3.forward;
            // nariz bulbosa (de borracho) y su punta
            float nose = R * style.noseSize;
            Part("Nose", Ellipsoid(new Vector3(nose * 0.78f, nose * 0.78f, nose * 0.83f), 18, 12, (d) => 1f + 0.15f * Mathf.Max(0f, -d.y)), noseM, root, Surf(0f, -0.08f) + F * nose * 0.27f);
            Part("Nose_Bridge", Ellipsoid(new Vector3(R * 0.11f, R * 0.22f, R * 0.12f), 10, 8), skinM, root, Surf(0f, 0.12f) + F * R * 0.02f);
            // orejas grandes
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1f : 1f;
                var ear = Part("Ear", Ellipsoid(new Vector3(R * 0.11f, R * 0.25f, R * 0.17f), 16, 12, (d) => 1f - 0.35f * Mathf.Max(0f, -d.x * sx)), skinM, root, SurfDir(new Vector3(sx, 0.02f, -0.08f)) - new Vector3(sx * R * 0.035f, 0f, 0f));
                ear.localRotation = Quaternion.Euler(0f, sx * 18f, sx * -8f);
            }
            // ojos: blanco, iris y pupila; parpados superior e inferior (cascaras que giran)
            float er = R * style.eyeSize * 1.4f;
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1f : 1f;
                Vector3 ep = Surf(sx * 0.34f, 0.2f) - F * er * 0.42f;
                eye[s] = new GameObject(s == 0 ? "Eye_L" : "Eye_R").transform; eye[s].SetParent(root, false); eye[s].localPosition = ep;
                Part("Sclera", Ellipsoid(Vector3.one * er, 16, 12), whiteM, eye[s]);
                iris[s] = new GameObject("Iris").transform; iris[s].SetParent(eye[s], false);
                Part("Iris_Disc", Ellipsoid(new Vector3(er * 0.55f, er * 0.55f, er * 0.18f), 14, 8), irisM, iris[s], new Vector3(0, 0, er * 0.9f));
                Part("Pupil", Ellipsoid(new Vector3(er * 0.26f, er * 0.26f, er * 0.1f), 10, 6), pupilM, iris[s], new Vector3(0, 0, er * 1.0f));
                lidUp[s] = Part("Lid_Up", Cap(er * 1.12f, 0f, 95f), skinM, eye[s]);
                lidLo[s] = Part("Lid_Lo", Cap(er * 1.1f, 0f, 95f), skinM, eye[s]); lidLo[s].localRotation = Quaternion.Euler(180f, 0f, 0f);
                // ceja poblada (tubo curvo y afinado)
                var bp = new[] { new Vector3(-sx * er * 1.3f, 0f, 0f), new Vector3(0f, er * 0.35f, er * 0.15f), new Vector3(sx * er * 1.5f, -er * 0.1f, -er * 0.2f) };
                browT[s] = Part("Brow", Tube(Curve(bp, 10), new[] { R * 0.07f, R * 0.1f, R * 0.05f }, 7), hairM, root, Surf(sx * 0.34f, 0.47f) + F * R * 0.05f);
                browPos0[s] = browT[s].localPosition;
            }
            // bigote de morsa: dos mechones que caen a los costados de la boca
            mustacheT = new GameObject("Mustache").transform; mustacheT.SetParent(root, false); mustacheT.localPosition = Surf(0f, -0.3f) + F * R * 0.1f;
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1f : 1f, k = style.mustache * 1.15f;
                var mp = new[] { new Vector3(0f, 0.01f, 0.02f), new Vector3(sx * R * 0.28f * k, -R * 0.02f, 0f), new Vector3(sx * R * 0.48f * k, -R * 0.2f * k, -R * 0.1f), new Vector3(sx * R * 0.52f * k, -R * 0.42f * k, -R * 0.14f) };
                Part("Mustache_Half", Tube(Curve(mp, 14), new[] { R * 0.14f * k, R * 0.17f * k, R * 0.12f * k, R * 0.05f }, 9), hairM, mustacheT);
            }
            // boca (interior oscuro + labio inferior), bajo el bigote; la mandibula baja al abrir
            jawT = new GameObject("Jaw").transform; jawT.SetParent(root, false); jawT.localPosition = Surf(0f, -0.5f) - F * R * 0.04f;
            mouthT = Part("Mouth_In", Ellipsoid(new Vector3(R * 0.22f, R * 0.12f, R * 0.06f), 14, 8), mouthM, jawT, new Vector3(0f, R * 0.04f, R * 0.04f));
            Part("Lip_Lo", Tube(Curve(new[] { new Vector3(-R * 0.2f, 0.01f, 0f), new Vector3(0f, -R * 0.05f, R * 0.04f), new Vector3(R * 0.2f, 0.01f, 0f) }, 10), new[] { R * 0.035f, R * 0.055f, R * 0.035f }, 6), lipM, jawT, new Vector3(0f, -R * 0.06f, R * 0.07f));
            // pelo: herradura canosa alrededor de la nuca y las sienes; arriba, tres mechones peinados (o boina)
            // herradura de pelo canoso: mechones mullidos alrededor de la nuca, de oreja a oreja (se lee como pelo, no como banda)
            const int hairLocks = 13;
            for (int i = 0; i < hairLocks; i++)
            {
                float across = i / (hairLocks - 1f);
                float t = Mathf.Lerp(-100f, 100f, across) * Mathf.Deg2Rad;
                var hd = new Vector3(Mathf.Sin(t), 0.12f + 0.06f * Mathf.Cos(t * 3f), -Mathf.Cos(t));
                float height = Mathf.Lerp(0.20f, 0.27f, 1f - Mathf.Abs(2f * across - 1f));
                var puff = Part("Hair_Puff", Ellipsoid(new Vector3(R * 0.25f, R * height, R * 0.13f), 12, 10, (d) => 1f + 0.12f * Mathf.Sin(d.x * 9f) * Mathf.Sin(d.y * 7f)), greyM, root, SurfDir(hd) - hd.normalized * R * 0.025f);
                puff.localRotation = Quaternion.LookRotation(hd.normalized, Vector3.up);
            }
            if (style.variant == Variant.Boina)
            {
                var cap = Part("Cap", Ellipsoid(new Vector3(S.x * 1.1f, S.y * 0.32f, S.z * 1.18f), 20, 10, (d) => d.y < 0f ? 0.25f : 1f), Tinted(skinM, style.cap, "Uncle_Cap", 0.15f), root, new Vector3(0f, S.y * 0.78f, S.z * 0.08f));
                cap.localRotation = Quaternion.Euler(-8f, 0f, 0f);
                Part("Cap_Visor", Ellipsoid(new Vector3(S.x * 0.75f, R * 0.04f, S.z * 0.45f), 16, 6), cap.GetComponent<Renderer>().sharedMaterial, root, new Vector3(0f, S.y * 0.7f, S.z * 0.85f));
            }
            else
                for (int k = -1; k <= 1; k++)
                {
                    Vector3 H(Vector3 d) => SurfDir(d) + d.normalized * R * 0.06f;
                    var cp = new[] { H(new Vector3(-0.85f, 0.5f, k * 0.25f)), H(new Vector3(0f, 1f, k * 0.25f)), H(new Vector3(0.8f, 0.55f, k * 0.28f)) };
                    Part("Combover", Tube(Curve(cp, 10), new[] { R * 0.05f, R * 0.07f, R * 0.035f }, 6), hairM, root);
                }
            // la cabeza sigue a la parte fisica; la del kit se colapsa dentro de la nueva
            root.SetParent(anchor, true);
            if (headBone) { headBone.localScale = Vector3.one * 0.001f; hiddenBone = headBone; }
            blinkAt = Time.time + 2f;
            Built = true;
            Debug.Log($"[Head] cabeza nueva ({style.variant}) R={R:0.000} piezas={root.GetComponentsInChildren<Renderer>().Length}");
        }
        Transform hiddenBone;

        Transform Part(string n, Mesh mesh, Material m, Transform parent, Vector3 localPos = default)
        {
            var g = new GameObject(n); g.transform.SetParent(parent, false); g.transform.localPosition = localPos;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = n == "Skull" ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return g.transform;
        }

        Material Mat(string prefix, Color fallback, float smooth)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials) if (m && m.name.StartsWith(prefix)) return m;
            var sh = Shader.Find("Universal Render Pipeline/Lit"); var mat = new Material(sh ? sh : Shader.Find("Standard")) { name = prefix, color = fallback };
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth); return mat;
        }
        static Material Tinted(Material src, Color c, string n, float smooth)
        {
            var m = new Material(src.shader) { name = n }; m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            return m;
        }

        // ---------------------------------------------------------------- mallas
        /// Craneo esculpido: elipsoide con papada, mofletes, menton, arco de las cejas y nuca aplanada.
        Vector3 skullS;
        /// Punto de la superficie del craneo al frente, en coordenadas normalizadas (x, y en -1..1).
        Vector3 Surf(float nx, float ny) => SurfDir(new Vector3(nx, ny, Mathf.Sqrt(Mathf.Max(0.05f, 1f - nx * nx - ny * ny))));
        Vector3 SurfDir(Vector3 d) { d.Normalize(); return Vector3.Scale(d, skullS) * SkullShape(d); }
        Mesh SculptedSkull(Vector3 S) => Ellipsoid(S, 40, 30, SkullShape);
        float SkullShape(Vector3 d)
        {
            {
                float f = 1f;
                float front = Mathf.Max(0f, d.z), low = Mathf.Max(0f, -d.y), side = Mathf.Abs(d.x);
                f += style.jowls * low * low * side * 1.6f;                                     // papada a los costados
                f += style.chin * Mathf.Exp(-((d.x * d.x) / 0.05f + (d.y + 0.75f) * (d.y + 0.75f) / 0.05f)) * front; // menton/papada al frente
                f += style.cheeks * Mathf.Exp(-(((side - 0.55f) * (side - 0.55f)) / 0.04f + ((d.y + 0.15f) * (d.y + 0.15f)) / 0.04f)) * front;
                f += style.brow * Mathf.Exp(-((d.y - 0.35f) * (d.y - 0.35f)) / 0.01f) * front * (1f - side * 0.6f); // arco de las cejas
                f -= 0.06f * Mathf.Exp(-(((side - 0.36f) * (side - 0.36f)) / 0.01f + ((d.y - 0.2f) * (d.y - 0.2f)) / 0.012f)) * front; // cuencas
                f -= 0.08f * Mathf.Max(0f, -d.z) * Mathf.Max(0f, -d.z) * Mathf.Max(0f, d.y);      // nuca algo plana arriba
                return f;
            }
        }

        static Mesh Ellipsoid(Vector3 r, int seg, int rings, System.Func<Vector3, float> shape = null)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int j = 0; j <= rings; j++)
            {
                float p = Mathf.PI * j / rings;
                for (int i = 0; i <= seg; i++)
                {
                    float q = 2f * Mathf.PI * i / seg;
                    var d = new Vector3(Mathf.Sin(p) * Mathf.Sin(q), Mathf.Cos(p), Mathf.Sin(p) * Mathf.Cos(q));
                    float k = shape != null ? shape(d) : 1f;
                    v.Add(Vector3.Scale(d, r) * k);
                }
            }
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < seg; i++)
                {
                    int a = j * (seg + 1) + i, b = a + seg + 1;
                    t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1);
                }
            return Finish(v, t, "Ellipsoid");
        }

        /// Casquete esferico (parpado): cubre desde el polo superior hasta 'toDeg' (frente +z). Gira en x para abrir/cerrar.
        static Mesh Cap(float r, float fromDeg, float toDeg)
        {
            var v = new List<Vector3>(); var t = new List<int>(); int seg = 18, rings = 8;
            for (int j = 0; j <= rings; j++)
            {
                float p = Mathf.Deg2Rad * Mathf.Lerp(fromDeg, toDeg, j / (float)rings);
                for (int i = 0; i <= seg; i++)
                {
                    float q = Mathf.Lerp(-100f, 100f, i / (float)seg) * Mathf.Deg2Rad;  // solo la mitad delantera
                    v.Add(new Vector3(Mathf.Sin(p) * Mathf.Sin(q), Mathf.Cos(p), Mathf.Sin(p) * Mathf.Cos(q)) * r);
                }
            }
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < seg; i++)
                {
                    int a = j * (seg + 1) + i, b = a + seg + 1;
                    t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1);
                }
            return Finish(v, t, "Lid");
        }

        /// Curva Catmull-Rom por puntos de control.
        static Vector3[] Curve(Vector3[] c, int perSeg)
        {
            var o = new List<Vector3>();
            for (int s = 0; s < c.Length - 1; s++)
            {
                Vector3 p0 = c[Mathf.Max(0, s - 1)], p1 = c[s], p2 = c[s + 1], p3 = c[Mathf.Min(c.Length - 1, s + 2)];
                for (int i = 0; i < perSeg; i++)
                {
                    float t = i / (float)perSeg, t2 = t * t, t3 = t2 * t;
                    o.Add(0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            o.Add(c[c.Length - 1]); return o.ToArray();
        }

        /// Tubo con radio interpolado a lo largo (cejas, bigote, pelo), extremos cerrados.
        static Mesh Tube(Vector3[] path, float[] radii, int sides)
        {
            var v = new List<Vector3>(); var t = new List<int>(); int n = path.Length;
            Vector3 prevN = Vector3.up;
            for (int i = 0; i < n; i++)
            {
                Vector3 fwd = (path[Mathf.Min(n - 1, i + 1)] - path[Mathf.Max(0, i - 1)]).normalized; if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.right;
                Vector3 nrm = Vector3.Cross(fwd, Vector3.Cross(prevN, fwd)).normalized; if (nrm.sqrMagnitude < 1e-6f) nrm = Vector3.Cross(fwd, Vector3.forward).normalized; prevN = nrm;
                Vector3 bin = Vector3.Cross(fwd, nrm);
                float u = i / (float)(n - 1), rr = SampleRadius(radii, u);
                for (int k = 0; k < sides; k++) { float a = 2f * Mathf.PI * k / sides; v.Add(path[i] + (nrm * Mathf.Cos(a) + bin * Mathf.Sin(a)) * rr); }
            }
            for (int i = 0; i < n - 1; i++)
                for (int k = 0; k < sides; k++)
                {
                    int a = i * sides + k, b = i * sides + (k + 1) % sides, c = a + sides, d = b + sides;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                }
            int s0 = v.Count; v.Add(path[0]); int s1 = v.Count; v.Add(path[n - 1]);
            for (int k = 0; k < sides; k++) { t.Add(s0); t.Add((k + 1) % sides); t.Add(k); t.Add(s1); t.Add((n - 1) * sides + k); t.Add((n - 1) * sides + (k + 1) % sides); }
            return Finish(v, t, "Tube");
        }
        static float SampleRadius(float[] r, float u) { float x = u * (r.Length - 1); int i = Mathf.Min(r.Length - 2, (int)x); return Mathf.Lerp(r[i], r[i + 1], x - i); }

        static Mesh Finish(List<Vector3> v, List<int> t, string n)
        {
            var m = new Mesh { name = "UncleHead_" + n }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // ---------------------------------------------------------------- expresiones
        void LateUpdate()
        {
            if (!Built || !brain) return;
            if (hiddenBone) hiddenBone.localScale = Vector3.one * 0.001f;
            var st = brain.State; float dt = Time.deltaTime;
            // objetivo de cada rasgo segun el estado (0..1)
            float tLid = 0.45f, tLow = 0.1f, tBrowUp = -0.2f, tBrowAng = -10f, tOpen = 0f, tWide = 0f; string mood = "borracho";
            switch (st)
            {
                case NpcState.Protest: case NpcState.Struggle: case NpcState.Anchor:
                    tLid = 0.55f; tLow = 0.4f; tBrowUp = -0.35f; tBrowAng = 22f; tOpen = 0.08f; tWide = 0.8f; mood = "resiste"; break;
                case NpcState.Windup:
                    tLid = 0f; tLow = 0f; tBrowUp = 1f; tBrowAng = 18f; tOpen = 0.55f; tWide = 0.2f; mood = "anticipa"; break;
                case NpcState.Attack:
                    tLid = 0.1f; tLow = 0.2f; tBrowUp = -0.2f; tBrowAng = 28f; tOpen = 1f; tWide = 0.6f; mood = "golpea"; break;
                case NpcState.Recover: case NpcState.ForcedRecovery: case NpcState.Fallen:
                    tLid = 0.6f; tLow = 0.15f; tBrowUp = 0.6f; tBrowAng = -18f; tOpen = 0.35f; tWide = 0f; mood = "aturdido"; break;
                case NpcState.Distracted:
                    tLid = 0.05f; tLow = 0.1f; tBrowUp = 0.8f; tBrowAng = -6f; tOpen = 0.2f; tWide = 0.3f; mood = "ilusion"; break;
                case NpcState.Secured:
                    tLid = 1f; tLow = 0.2f; tBrowUp = 0.1f; tBrowAng = -12f; tOpen = 0.12f; tWide = 0f; mood = "dormido"; break;
                case NpcState.Escape:
                    tLid = 0.25f; tLow = 0.1f; tBrowUp = 0.3f; tBrowAng = 10f; tOpen = 0.1f; mood = "escapa"; break;
            }
            if (brain.BellWindow) { tLid = 0.05f; tBrowUp = 0.9f; tBrowAng = -4f; mood = "campana"; }
            Mood = mood;
            float k = Smooth.Factor(st == NpcState.Windup || st == NpcState.Attack ? 0.06f : 0.18f);
            lid = Mathf.Lerp(lid, tLid, k); lidLow = Mathf.Lerp(lidLow, tLow, k); browUp = Mathf.Lerp(browUp, tBrowUp, k);
            browAngle = Mathf.Lerp(browAngle, tBrowAng, k); mouthOpen = Mathf.Lerp(mouthOpen, tOpen, k); mouthWide = Mathf.Lerp(mouthWide, tWide, k);
            // parpadeo (lento de borracho); no parpadea al anticipar un golpe
            if (Time.time >= blinkAt && st != NpcState.Windup) { blinkT = 0.22f; blinkAt = Time.time + Random.Range(2.2f, 5.5f); }
            float blink = blinkT > 0f ? Mathf.Sin(Mathf.Clamp01(1f - blinkT / 0.22f) * Mathf.PI) : 0f; blinkT -= dt;
            float upper = Mathf.Clamp01(Mathf.Max(lid, blink)); Lid01 = upper;
            wobble += dt * (mood == "aturdido" ? 9f : 1.3f);
            // mirada: al jugador mas cercano (o a lo que lo distrae); aturdido = ojos que giran
            Vector3 target = Vector3.zero; bool has = false; float best = 6f;
            foreach (var pm in PlayerMotor.All) { if (!pm) continue; Vector3 e = pm.transform.position + Vector3.up * 1.6f; float d = Vector3.Distance(e, root.position); if (d < best) { best = d; target = e; has = true; } }
            Vector3 want = has ? root.InverseTransformDirection((target - root.position).normalized) : Vector3.forward;
            if (mood == "aturdido") want = new Vector3(Mathf.Sin(wobble) * 0.5f, Mathf.Cos(wobble) * 0.35f, 1f).normalized;
            want.z = Mathf.Max(0.45f, want.z); want = want.normalized;
            lookDir = Vector3.Slerp(lookDir, want, Smooth.Factor(0.08f));
            for (int s = 0; s < 2; s++)
            {
                if (!eye[s]) continue;
                iris[s].localRotation = Quaternion.FromToRotation(Vector3.forward, Vector3.Slerp(Vector3.forward, lookDir, 0.7f));
                // parpado superior: 0 = abierto (gira hacia arriba), 1 = cerrado (baja hasta el ecuador del ojo)
                lidUp[s].localRotation = Quaternion.Euler(Mathf.Lerp(-80f, -2f, upper), 0f, 0f);
                lidLo[s].localRotation = Quaternion.Euler(180f + Mathf.Lerp(80f, 30f, Mathf.Max(lidLow, upper * 0.6f)), 0f, 0f);
                float sx = s == 0 ? -1f : 1f;
                browT[s].localPosition = browPos0[s] + new Vector3(0f, browUp * R * 0.12f, 0f);
                browT[s].localRotation = Quaternion.Euler(0f, 0f, -sx * browAngle);
            }
            float open = Mathf.Clamp01(mouthOpen + (mood == "golpea" ? 0.1f * Mathf.Sin(Time.time * 30f) : 0f));
            jawT.localRotation = Quaternion.Euler(open * 16f, 0f, 0f);
            mouthT.localScale = new Vector3(1f + 0.35f * mouthWide, 0.35f + 1.9f * open, 1f);
            mustacheT.localRotation = Quaternion.Euler(-open * 6f, 0f, Mathf.Sin(wobble * 0.7f) * (mood == "borracho" ? 2f : 0.5f));
        }
    }
}
