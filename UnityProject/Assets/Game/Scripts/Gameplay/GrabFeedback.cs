using UnityEngine;

namespace UncleExtraction
{
    /// Lenguaje de interaccion del jugador local (UI_SPEC 4; GDD 04): sobre el punto apuntado, un anillo con su estado
    /// (valido / ocupado / fuera de alcance) y su nombre; desde cada mano, una linea de tension que se engrosa y parpadea
    /// cerca de soltarse; con esfuerzo, las manos vibran. La forma y el texto cambian con el estado, no solo el color.
    /// Solo presentacion y solo en la instancia del jugador local; lee el estado replicado (PlayerHands).
    public class GrabFeedback : MonoBehaviour
    {
        public enum AimState { None, Valid, Busy, OutOfReach }
        public PlayerHands hands; public Material material;
        public AimState State { get; private set; }
        public GrabSocket Aimed { get; private set; }

        Transform ring; Mesh ringMesh; Material ringMat; TextMesh label; LineRenderer[] lines = new LineRenderer[2];
        Transform warn; TextMesh warnLabel; Material warnMat; Mesh warnMesh;
        /// Texto del bloqueo marcado en el mundo ("" = nada que marcar). Para pruebas.
        public string BlockShown { get; private set; } = "";
        PlayerControls controls;

        void Awake()
        {
            if (!hands) hands = GetComponent<PlayerHands>(); controls = GetComponent<PlayerControls>();
            ring = new GameObject("Grab_Aim_Ring").transform; ring.SetParent(null, false);
            ringMesh = new Mesh { name = "GrabRing" };
            var mf = ring.gameObject.AddComponent<MeshFilter>(); mf.sharedMesh = ringMesh;
            var mr = ring.gameObject.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material) { ringMat = new Material(material); mr.sharedMaterial = ringMat; }
            var lgo = new GameObject("Grab_Aim_Label"); lgo.transform.SetParent(ring, false);
            label = lgo.AddComponent<TextMesh>(); label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center;
            label.characterSize = 0.02f; label.fontSize = 64;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font) { label.font = font; lgo.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            for (int h = 0; h < 2; h++)
            {
                var go = new GameObject(h == 0 ? "Tension_L" : "Tension_R"); go.transform.SetParent(null, false);
                var lr = go.AddComponent<LineRenderer>(); lr.positionCount = 2; lr.useWorldSpace = true; lr.numCapVertices = 2;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false;
                if (material) lr.sharedMaterial = new Material(material);
                lines[h] = lr; go.SetActive(false);
            }
            ring.gameObject.SetActive(false);
            // aviso de bloqueo de la camioneta: rombo sobre lo que impide cerrar, con el motivo
            warn = new GameObject("Block_Warning").transform; warn.SetParent(null, false);
            warnMesh = new Mesh { name = "Diamond" };
            warnMesh.SetVertices(new System.Collections.Generic.List<Vector3> { new Vector3(0, 0.12f), new Vector3(0.09f, 0), new Vector3(0, -0.12f), new Vector3(-0.09f, 0), new Vector3(0, 0.07f), new Vector3(0.05f, 0), new Vector3(0, -0.07f), new Vector3(-0.05f, 0) });
            warnMesh.SetTriangles(new[] { 0, 1, 5, 0, 5, 4, 1, 2, 6, 1, 6, 5, 2, 3, 7, 2, 7, 6, 3, 0, 4, 3, 4, 7 }, 0); warnMesh.RecalculateBounds();
            warn.gameObject.AddComponent<MeshFilter>().sharedMesh = warnMesh;
            var wr = warn.gameObject.AddComponent<MeshRenderer>(); wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material) { warnMat = new Material(material); wr.sharedMaterial = warnMat; }
            var wl = new GameObject("Block_Label"); wl.transform.SetParent(warn, false); wl.transform.localPosition = new Vector3(0, 0.16f, 0);
            warnLabel = wl.AddComponent<TextMesh>(); warnLabel.anchor = TextAnchor.LowerCenter; warnLabel.alignment = TextAlignment.Center; warnLabel.characterSize = 0.022f; warnLabel.fontSize = 64;
            var f2 = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (f2) { warnLabel.font = f2; wl.GetComponent<MeshRenderer>().sharedMaterial = f2.material; }
            warn.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (ring) Destroy(ring.gameObject); if (warn) Destroy(warn.gameObject);
            foreach (var l in lines) if (l) Destroy(l.gameObject);
        }

        bool IsLocalPlayer => controls ? controls.IsLocal && (PlayerControls.Local == controls) : true;

        static void Ring(Mesh m, float r, float w, bool dashed)
        {
            const int N = 32; var v = new System.Collections.Generic.List<Vector3>(); var tri = new System.Collections.Generic.List<int>();
            for (int i = 0; i < N; i++)
            {
                if (dashed && i % 2 == 1) continue; // fuera de alcance: anillo punteado
                float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N; int b = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * (r - w)); v.Add(new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * r);
                v.Add(new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * (r - w)); v.Add(new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * r);
                tri.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 });
            }
            m.Clear(); m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateBounds();
        }

        public static string SocketName(GrabSocket s) => s == null ? "" : Loc.Has("grab.socket." + s.socketId) ? Loc.T("grab.socket." + s.socketId) : s.label;

        void LateUpdate()
        {
            bool local = IsLocalPlayer && hands && hands.cam && hands.cam.isActiveAndEnabled;
            var motor = hands ? hands.GetComponent<PlayerMotor>() : null;
            bool canAct = local && motor && motor.ControlsEnabled && !GameFlowUI.BlockingInput;
            // estado del punto apuntado
            Aimed = null; State = AimState.None;
            if (canAct)
            {
                var c = hands.Candidate;
                if (c && !(hands.HeldSocketId(0) == c.socketId && hands.HeldSocketId(1) == c.socketId))
                { Aimed = c; State = PlayerHands.OccupancyOf(c) >= c.capacity && hands.HeldSocketId(0) != c.socketId && hands.HeldSocketId(1) != c.socketId ? AimState.Busy : AimState.Valid; }
                else if (hands.AimedOutOfReach) { Aimed = hands.AimedOutOfReach; State = AimState.OutOfReach; }
            }
            bool both = hands && hands.HeldSocketId(0) != "" && hands.HeldSocketId(1) != "";
            ring.gameObject.SetActive(Aimed && !both);
            if (ring.gameObject.activeSelf)
            {
                var cam = hands.cam.transform;
                ring.position = Aimed.Position; ring.rotation = Quaternion.LookRotation(Aimed.Position - cam.position, cam.up);
                float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 10f);
                Ring(ringMesh, State == AimState.Valid ? 0.09f * pulse : 0.08f, State == AimState.Busy ? 0.03f : 0.018f, State == AimState.OutOfReach);
                Color col = State == AimState.Valid ? new Color(0.95f, 0.91f, 0.81f, 0.95f) : State == AimState.Busy ? new Color(0.94f, 0.47f, 0.38f, 0.95f) : new Color(0.72f, 0.76f, 0.75f, 0.6f);
                if (ringMat) ringMat.color = col;
                string n = SocketName(Aimed);
                label.text = State == AimState.Valid ? n : State == AimState.Busy ? Loc.T("grab.aim_busy", ("socket", n)) : Loc.T("grab.aim_far", ("socket", n));
                label.color = col; label.transform.localPosition = new Vector3(0f, 0.11f, 0f);
            }
            // que bloquea el cierre de la camioneta (con el tio adentro y el jugador a menos de 7 m de la abertura)
            BlockShown = "";
            var van = VanDoors.Instance; var zone = ExtractionZone.Instance;
            if (local && van && zone && !zone.Secured && zone.AllPointsInside && Vector3.Distance(hands.transform.position, van.closePoint) < 7f)
            {
                string why = van.BlockReason();
                if (why != "" && van.BlockPoint.HasValue && van.CurrentPhase != VanDoors.Phase.Closing) BlockShown = why;
                warn.gameObject.SetActive(BlockShown != "");
                if (BlockShown != "")
                {
                    var cam = hands.cam.transform;
                    warn.position = van.BlockPoint.Value + Vector3.up * 0.25f; warn.rotation = Quaternion.LookRotation(warn.position - cam.position, cam.up);
                    float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 8f); warn.localScale = Vector3.one * pulse;
                    var col = new Color(0.91f, 0.66f, 0.29f, 0.95f); if (warnMat) warnMat.color = col;
                    warnLabel.text = Loc.Coded(BlockShown); warnLabel.color = col;
                }
            }
            else warn.gameObject.SetActive(false);

            // tension de cada mano
            for (int h = 0; h < 2; h++)
            {
                var lr = lines[h]; var s = local && hands ? hands.HeldSocket(h) : null;
                lr.gameObject.SetActive(s);
                if (!s) continue;
                float t = hands.Tension01(h);
                Vector3 from = hands.HandVisual(h) ? hands.HandVisual(h).position : hands.HandTarget(h);
                Vector3 jitter = hands.EffortActive ? Random.insideUnitSphere * 0.006f : Vector3.zero;
                lr.SetPosition(0, from + jitter); lr.SetPosition(1, s.Position);
                float blink = t > 0.75f ? (Mathf.Sin(Time.time * 30f) > 0f ? 1f : 0.35f) : 1f;
                lr.widthMultiplier = Mathf.Lerp(0.008f, 0.03f, t);
                Color c = Color.Lerp(new Color(0.24f, 0.65f, 0.63f, 0.7f), new Color(0.94f, 0.47f, 0.38f, 0.95f), t);
                c.a *= blink; lr.startColor = lr.endColor = c;
                if (lr.sharedMaterial) lr.sharedMaterial.color = c;
            }
        }
    }
}
