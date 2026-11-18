using UnityEngine;

namespace UncleExtraction
{
    /// Aviso de ataque legible sin depender del color (GDD 06, accesibilidad): en el piso, el sector que va a barrer el
    /// ataque con su alcance y arco reales (LabBalance) y un relleno que crece con la preparacion; arriba de la cabeza, el
    /// nombre del ataque. La sacudida (afecta a quien lo tiene agarrado) se marca como un anillo alrededor del cuerpo.
    /// Solo presentacion, en todos los pares, con el estado replicado de NpcBrain; no decide impactos.
    public class AttackTelegraph : MonoBehaviour
    {
        public NpcBrain brain; public NpcBody body;
        public Material material;            // URP/Unlit transparente (lo asigna el constructor)
        public float shakeRadius = 1.0f;
        const int Segs = 28;

        MeshFilter outlineMf, fillMf; MeshRenderer outlineMr, fillMr; Mesh outline, fill;
        TextMesh label; Transform root;
        Material outlineMat, fillMat;
        AttackId lastId = (AttackId)(-1); float flashUntil;
        public bool Visible { get; private set; }
        /// Direccion a la que apunta el arco del piso (pruebas: tiene que coincidir con la del impacto).
        public Vector3 AimDirection => root ? root.forward : Vector3.zero;
        public float Fill01 { get; private set; }

        void Awake()
        {
            if (!brain) brain = GetComponent<NpcBrain>(); if (!body) body = GetComponent<NpcBody>();
            root = new GameObject("Attack_Telegraph").transform; root.SetParent(null, false); // sin heredar la inclinacion del cuerpo
            (outlineMf, outlineMr) = Part("Reach", out outline); (fillMf, fillMr) = Part("Fill", out fill);
            if (material) { outlineMat = new Material(material); fillMat = new Material(material); outlineMr.sharedMaterial = outlineMat; fillMr.sharedMaterial = fillMat; }
            var lgo = new GameObject("Attack_Label"); lgo.transform.SetParent(root, false);
            label = lgo.AddComponent<TextMesh>(); label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.characterSize = 0.045f; label.fontSize = 64; label.fontStyle = FontStyle.Bold;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font) { label.font = font; lgo.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            root.gameObject.SetActive(false);
        }

        void OnDestroy() { if (root) Destroy(root.gameObject); }

        (MeshFilter, MeshRenderer) Part(string n, out Mesh m)
        {
            var go = new GameObject(n); go.transform.SetParent(root, false);
            m = new Mesh { name = n }; m.MarkDynamic();
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            return (mf, mr);
        }

        /// Sector plano (o anillo si arc >= 360) entre los radios r0 y r1, centrado en +z local.
        // sesion 17 (perfil): antes se reconstruian las dos mallas con arreglos nuevos en CADA cuadro del aviso. Ahora solo se
        // rehacen cuando cambia la forma (radio/arco, con tolerancia de 1 cm / 0,5 grados) y reutilizan sus arreglos.
        readonly System.Collections.Generic.Dictionary<Mesh, Vector4> shapeOf = new();
        void SectorIfChanged(Mesh m, float r0, float r1, float arcDeg)
        {
            var k = new Vector4(r0, r1, arcDeg, 0f);
            if (shapeOf.TryGetValue(m, out var prev) && Mathf.Abs(prev.x - k.x) < 0.01f && Mathf.Abs(prev.y - k.y) < 0.01f && Mathf.Abs(prev.z - k.z) < 0.5f) return;
            shapeOf[m] = k; Sector(m, r0, r1, arcDeg); Rebuilds++;
        }
        /// Reconstrucciones de malla del aviso (diagnostico y prueba de rendimiento).
        public static int Rebuilds;
        static Vector3[] vbuf; static int[] tbuf;
        static void Sector(Mesh m, float r0, float r1, float arcDeg)
        {
            int n = Segs + 1; var v = vbuf ??= new Vector3[n * 2]; bool firstTri = tbuf == null; var tri = tbuf ??= new int[Segs * 6];
            float half = Mathf.Min(arcDeg, 360f) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Deg2Rad * Mathf.Lerp(-half, half, i / (float)Segs);
                Vector3 d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                v[i * 2] = d * r0; v[i * 2 + 1] = d * r1;
            }
            if (firstTri)
                for (int i = 0; i < Segs; i++)
                {
                    int b = i * 2, t = i * 6;
                    tri[t] = b; tri[t + 1] = b + 1; tri[t + 2] = b + 3; tri[t + 3] = b; tri[t + 4] = b + 3; tri[t + 5] = b + 2;
                }
            bool fresh = m.vertexCount != v.Length;
            m.SetVertices(v); if (fresh) m.SetTriangles(tri, 0); m.RecalculateBounds();
        }

        /// Claves del catalogo del kit de UI (hud.slap = «VA A DAR UN MANOTAZO» / «SLAP INCOMING», etc.).
        public static string LabelKey(AttackId id) => id == AttackId.Slap ? "hud.slap" : id == AttackId.Push ? "hud.push" : id == AttackId.Kick ? "hud.kick" : "hud.shake";
        static string Name(AttackId id) => Loc.T(LabelKey(id));

        void LateUpdate()
        {
            if (!brain || !body) return;
            bool windup = brain.State == NpcState.Windup, attack = brain.State == NpcState.Attack;
            if (attack && lastId != brain.CurrentAttack.id) flashUntil = Time.time + 0.35f;
            Visible = (windup || attack || Time.time < flashUntil) && body.Posture == NpcPosture.Upright;
            root.gameObject.SetActive(Visible);
            if (!Visible) { lastId = (AttackId)(-1); Fill01 = 0f; return; }
            var def = brain.CurrentAttack; lastId = attack ? def.id : lastId;
            bool ring = def.id == AttackId.Shake;
            float range = ring ? shakeRadius : def.range, arc = ring ? 360f : def.arcDeg;
            Fill01 = windup ? Mathf.Clamp01(1f - brain.StateTimeLeft / Mathf.Max(0.05f, def.windup)) : 1f;

            Vector3 basePos = body.Body.position; basePos.y = body.transform.position.y + 0.03f; // raiz al nivel del piso
            // el arco apunta a donde va el golpe (la misma direccion que decide el impacto), no a donde gira el cuerpo
            Vector3 aim = ring ? body.Forward : brain.AttackDirectionShared; aim.y = 0f; if (aim.sqrMagnitude < 1e-4f) aim = body.Forward;
            root.SetPositionAndRotation(basePos, Quaternion.LookRotation(aim, Vector3.up));
            SectorIfChanged(outline, ring ? range - 0.06f : range - 0.05f, range, arc);
            SectorIfChanged(fill, ring ? 0.3f : 0.25f, Mathf.Lerp(ring ? 0.3f : 0.25f, range, Fill01), arc);
            // Color de apoyo (no es la unica senal): el contorno queda tenue y el relleno se intensifica al acercarse el golpe
            Color c = def.id == AttackId.Slap ? new Color(1f, 0.6f, 0.1f) : def.id == AttackId.Push ? new Color(1f, 0.25f, 0.2f) : def.id == AttackId.Kick ? new Color(0.95f, 0.85f, 0.1f) : new Color(0.2f, 0.9f, 1f);
            if (outlineMat) outlineMat.color = new Color(c.r, c.g, c.b, 0.85f);
            if (fillMat) fillMat.color = new Color(c.r, c.g, c.b, attack ? 0.75f : Mathf.Lerp(0.15f, 0.5f, Fill01));

            label.text = Name(def.id); label.color = Color.white;
            var cam = Camera.main;
            label.transform.position = body.ChestPosition + Vector3.up * 0.95f;
            if (cam) label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position, Vector3.up);
            float pulse = attack ? 1.25f : 1f + 0.08f * Mathf.Sin(Time.time * 14f);
            // de cerca (menos de 2,4 m) el aviso del HUD ya lo dice: el rotulo se achica y se oculta en vez de tapar la pantalla
            float d = cam ? Vector3.Distance(cam.transform.position, label.transform.position) : 10f;
            // tamano angular constante (no crece al acercarse) y oculto a menos de 2,8 m, donde el aviso del HUD ya lo dice
            float near = Mathf.InverseLerp(2.8f, 3.4f, d);
            label.gameObject.SetActive(near > 0.01f);
            label.transform.localScale = Vector3.one * pulse * Mathf.Clamp(d * 0.16f, 0.35f, 1f) * near;
        }
    }
}
