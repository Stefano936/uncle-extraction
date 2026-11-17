using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UncleExtraction
{
    public enum MarkKind : byte { Point, Uncle, Cart, Van }

    /// Marcador contextual y etiquetas de compañeros (sesion 13).
    /// - Marcador: el jugador apunta y toca «Marcar» (boton central por defecto); el anfitrion valida (emisor, distancia,
    ///   frecuencia) y lo reparte a todos. Un marcador activo por jugador, dura Lifetime s; sigue al tio o al carrito.
    ///   Muestra quien lo puso (nombre y color del jugador), que es y a que distancia; en el borde de la pantalla si queda fuera.
    /// - Compañeros: nombre sobre cada jugador remoto con su color, y «CAÍDO» cuando necesita ayuda para levantarse.
    public class CoopMarkers : MonoBehaviour
    {
        public const float Lifetime = 6f, MinInterval = 0.75f, MaxRange = 30f;
        public static CoopMarkers Instance { get; private set; }
        static readonly Color[] Palette = { Ui.Hex("#E9A84A"), Ui.Hex("#3CA6A0"), Ui.Hex("#D98BD0"), Ui.Hex("#8FB4F0") };
        public static Color ColorOf(ulong clientId) => Palette[(int)(clientId % (ulong)Palette.Length)];

        class Mark { public ulong owner; public string name; public MarkKind kind; public Vector3 pos; public float until; public RectTransform ui; public Text text; public Image pip; }
        readonly Dictionary<ulong, Mark> marks = new Dictionary<ulong, Mark>();
        readonly Dictionary<PlayerMotor, (RectTransform rt, Text t)> tags = new Dictionary<PlayerMotor, (RectTransform, Text)>();
        RectTransform root;
        public int ActiveCount { get { int n = 0; foreach (var m in marks.Values) if (Time.time < m.until) n++; return n; } }
        public bool TryGet(ulong owner, out MarkKind kind, out Vector3 pos) { kind = MarkKind.Point; pos = default; if (!marks.TryGetValue(owner, out var m) || Time.time >= m.until) return false; kind = m.kind; pos = WorldPos(m); return true; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (root) Destroy(root.root.gameObject); }
        void Start() { var c = Ui.Canvas("CoopMarkers", 8); root = (RectTransform)c.transform; }

        /// Que hay bajo la mira (lo decide quien marca; el anfitrion solo valida).
        public static MarkKind Classify(Collider c)
        {
            if (!c) return MarkKind.Point;
            if (NpcBody.Of(c.attachedRigidbody) || c.GetComponentInParent<NpcBody>()) return MarkKind.Uncle; // el tio articulado tiene partes sueltas
            if (c.GetComponentInParent<TransportCart>()) return MarkKind.Cart;
            if (c.GetComponentInParent<VanDoors>() || (VanDoors.Instance && c.transform.IsChildOf(VanDoors.Instance.transform.root))) return MarkKind.Van;
            return MarkKind.Point;
        }

        /// Ayuda de punteria: si el rayo pasa a menos de AssistRadius de una parte del tio o del carrito, no mas de 1,5 m
        /// detras de lo que toco, marca ese objetivo (el tio suele estar detras de la barra o de otro jugador).
        public const float AssistRadius = 0.6f;
        public static void AimAssist(Ray ray, float hitDist, ref MarkKind kind, ref Vector3 pos)
        {
            float best = AssistRadius; Vector3 at = pos; MarkKind k = kind;
            void Try(Vector3 p, MarkKind what)
            {
                float t = Vector3.Dot(p - ray.origin, ray.direction); if (t < 0f || t > hitDist + 1.5f) return;
                float d = Vector3.Distance(ray.origin + ray.direction * t, p); if (d < best) { best = d; at = p; k = what; }
            }
            var npc = NpcBrain.Instance ? NpcBrain.Instance.GetComponent<NpcBody>() : null;
            if (npc) foreach (var b in npc.AllBodies) if (b) Try(b.worldCenterOfMass, MarkKind.Uncle);
            if (TransportCart.Instance) Try(TransportCart.Instance.transform.position + Vector3.up * 0.6f, MarkKind.Cart);
            kind = k; pos = at;
        }

        /// Regla del anfitrion (probada en EditMode): posicion finita, al alcance del emisor y sin repetir antes de MinInterval.
        public static bool Accept(Vector3 from, Vector3 pos, float lastAt, float now)
        {
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z) || float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z)) return false;
            if ((pos - from).sqrMagnitude > (MaxRange + 2f) * (MaxRange + 2f)) return false;
            return now - lastAt >= MinInterval;
        }

        public void Show(ulong owner, string name, MarkKind kind, Vector3 pos)
        {
            if (!marks.TryGetValue(owner, out var m) || !m.ui)
            {
                m = new Mark { owner = owner };
                m.ui = Ui.Rect("Mark" + owner, root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(320, 90));
                var pr = Ui.Rect("Pip", m.ui, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(24, 24));
                pr.localRotation = Quaternion.Euler(0, 0, 45f);
                Ui.Rect("Edge", pr, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 8)).gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
                m.pip = Ui.Rect("Fill", pr, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>(); m.pip.color = ColorOf(owner); m.pip.raycastTarget = false;
                m.text = Ui.Label(m.ui, "Text", "", Ui.FontHud, ColorOf(owner), TextAnchor.LowerCenter, true);
                m.text.rectTransform.offsetMin = new Vector2(0, 44);
                m.text.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.85f);
                marks[owner] = m;
            }
            m.name = name; m.kind = kind; m.pos = pos; m.until = Time.time + Lifetime; m.ui.gameObject.SetActive(true);
            if (GameAudio.Instance) GameAudio.Instance.Play2D("ping", 0.6f);
            Debug.Log($"[Mark] {name} marca {kind} en {pos}");
        }

        static Vector3 WorldPos(Mark m)
        {
            switch (m.kind)
            {
                case MarkKind.Uncle: { var b = NpcBrain.Instance; if (b) return b.transform.position + Vector3.up * 1.9f; break; }
                case MarkKind.Cart: { var c = TransportCart.Instance; if (c) return c.transform.position + Vector3.up * 1.2f; break; }
            }
            return m.pos + Vector3.up * 0.3f;
        }

        void LateUpdate()
        {
            var cam = Camera.main; if (!root) return;
            var round = RoundDirector.Instance;
            bool live = cam && round && !round.InLobby && round.State == RoundState.Running;
            var me = PlayerControls.Local ? PlayerControls.Local.GetComponent<PlayerMotor>() : null;
            foreach (var m in marks.Values)
            {
                bool on = live && Time.time < m.until; if (m.ui.gameObject.activeSelf != on) m.ui.gameObject.SetActive(on);
                if (!on) continue;
                var w = WorldPos(m);
                float dist = me ? Vector3.Distance(me.transform.position, w) : 0f;
                m.text.text = $"{m.name}: {Loc.T("mark." + m.kind.ToString().ToLowerInvariant())}  {dist:0} m";
                Place(cam, m.ui, w);
                float left = m.until - Time.time; var c = m.pip.color; c.a = left < 1f ? left : 1f; m.pip.color = c;
            }
            // etiquetas de compañeros
            foreach (var pm in PlayerMotor.All)
            {
                if (!pm || pm.Controls == PlayerControls.Local) continue;
                if (!tags.TryGetValue(pm, out var tag) || !tag.rt)
                {
                    var rt = Ui.Rect("Tag", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300, 60));
                    var t = Ui.Label(rt, "Name", "", Ui.FontHud, Color.white, TextAnchor.LowerCenter, true);
                    t.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.85f);
                    tag = (rt, t); tags[pm] = tag;
                }
                var head = pm.transform.position + Vector3.up * 2.05f;
                bool vis = live && cam && Vector3.Dot(cam.transform.forward, head - cam.transform.position) > 0.1f && (head - cam.transform.position).magnitude < 40f;
                if (tag.rt.gameObject.activeSelf != vis) tag.rt.gameObject.SetActive(vis);
                if (!vis) continue;
                bool down = pm.State == PlayerState.Fallen || pm.State == PlayerState.GettingUp;
                tag.t.color = down ? Ui.Danger : ColorOf(pm.OwnerClientId);
                tag.t.text = down ? $"{pm.playerName} · {Loc.T("mark.fallen")}" : pm.playerName;
                var sp = cam.WorldToScreenPoint(head); var cr = root.rect;
                tag.rt.anchoredPosition = new Vector2(sp.x / Screen.width * cr.width - cr.width * 0.5f, sp.y / Screen.height * cr.height - cr.height * 0.5f);
            }
            var gone = new List<PlayerMotor>(); foreach (var k in tags.Keys) if (!k) gone.Add(k);
            foreach (var k in gone) { if (tags[k].rt) Destroy(tags[k].rt.gameObject); tags.Remove(k); }
        }

        /// Sobre el objetivo si se ve; si no, pegado al borde en su direccion.
        void Place(Camera cam, RectTransform rt, Vector3 world)
        {
            var cr = root.rect; var sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0f; if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }
            var p = new Vector2(sp.x / Screen.width * cr.width - cr.width * 0.5f, sp.y / Screen.height * cr.height - cr.height * 0.5f);
            float hx = cr.width * 0.5f - 180f, hy = cr.height * 0.5f - 110f;
            if (behind || Mathf.Abs(p.x) > hx || Mathf.Abs(p.y) > hy)
            {
                if (behind && p.sqrMagnitude < 1f) p = Vector2.down;
                float s = Mathf.Min(hx / Mathf.Max(1e-3f, Mathf.Abs(p.x)), hy / Mathf.Max(1e-3f, Mathf.Abs(p.y)));
                if (behind || s < 1f) p *= Mathf.Min(1f, s);
                if (behind) { p = p.normalized * 10000f; s = Mathf.Min(hx / Mathf.Max(1e-3f, Mathf.Abs(p.x)), hy / Mathf.Max(1e-3f, Mathf.Abs(p.y))); p *= s; }
            }
            rt.anchoredPosition = p;
        }
    }
}
