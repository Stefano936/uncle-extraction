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
        static void Sector(Mesh m, float r0, float r1, float arcDeg)
        {
            int n = Segs + 1; var v = new Vector3[n * 2]; var tri = new int[Segs * 6];
            float half = Mathf.Min(arcDeg, 360f) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Deg2Rad * Mathf.Lerp(-half, half, i / (float)Segs);
                Vector3 d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                v[i * 2] = d * r0; v[i * 2 + 1] = d * r1;
            }
            for (int i = 0; i < Segs; i++)
            {
                int b = i * 2, t = i * 6;
                tri[t] = b; tri[t + 1] = b + 1; tri[t + 2] = b + 3; tri[t + 3] = b; tri[t + 4] = b + 3; tri[t + 5] = b + 2;
            }
            m.Clear(); m.vertices = v; m.triangles = tri; m.RecalculateBounds();
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
            root.SetPositionAndRotation(basePos, Quaternion.LookRotation(body.Forward, Vector3.up));
            Sector(outline, ring ? range - 0.06f : range - 0.05f, range, arc);
            Sector(fill, ring ? 0.3f : 0.25f, Mathf.Lerp(ring ? 0.3f : 0.25f, range, Fill01), arc);
            // Color de apoyo (no es la unica senal): el contorno queda tenue y el relleno se intensifica al acercarse el golpe
            Color c = def.id == AttackId.Slap ? new Color(1f, 0.6f, 0.1f) : def.id == AttackId.Push ? new Color(1f, 0.25f, 0.2f) : def.id == AttackId.Kick ? new Color(0.95f, 0.85f, 0.1f) : new Color(0.2f, 0.9f, 1f);
            if (outlineMat) outlineMat.color = new Color(c.r, c.g, c.b, 0.85f);
            if (fillMat) fillMat.color = new Color(c.r, c.g, c.b, attack ? 0.75f : Mathf.Lerp(0.15f, 0.5f, Fill01));

            label.text = Name(def.id); label.color = Color.white;
            var cam = Camera.main;
            label.transform.position = body.ChestPosition + Vector3.up * 0.95f;
            if (cam) label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position, Vector3.up);
            float pulse = attack ? 1.25f : 1f + 0.08f * Mathf.Sin(Time.time * 14f);
            label.transform.localScale = Vector3.one * pulse;
        }
    }
}
