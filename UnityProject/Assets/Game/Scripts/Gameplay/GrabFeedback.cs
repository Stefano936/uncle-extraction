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

        static readonly bool NoHud = QaArgs.Has("-ueNoHud"); // material de tienda
        Transform ring, labelPlate; Material plateMat; Mesh ringMesh; Material ringMat; TextMesh label; LineRenderer[] lines = new LineRenderer[2];
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
            label.characterSize = 0.011f; label.fontSize = 64;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font) { label.font = font; lgo.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            if (NoHud) lgo.GetComponent<MeshRenderer>().enabled = false; // material de tienda
            // sesion 20 (auditoria 54b5246): placa oscura detras del rotulo. En Detallado el texto claro se perdia sobre la ropa
            // iluminada a 720p y texto al 150 %; con contraste local se lee con o sin bloom.
            labelPlate = GameObject.CreatePrimitive(PrimitiveType.Quad).transform; labelPlate.name = "Grab_Aim_Label_Plate";
            var pc = labelPlate.GetComponent<Collider>(); if (pc) { pc.enabled = false; Destroy(pc); }
            labelPlate.SetParent(lgo.transform, false);
            var pr = labelPlate.GetComponent<MeshRenderer>(); pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; pr.receiveShadows = false;
            // mismo sombreador que el texto (ignora la profundidad): con el material del anillo el cuerpo del tio tapaba la placa,
            // porque el punto de agarre esta sobre su superficie. Se dibuja justo antes que el texto.
            var fontMat = lgo.GetComponent<MeshRenderer>().sharedMaterial;
            var sh = fontMat ? fontMat.shader : Shader.Find("GUI/Text Shader");
            if (sh) { plateMat = new Material(sh) { name = "Grab_Label_Plate", mainTexture = Texture2D.whiteTexture, color = new Color(0.04f, 0.045f, 0.05f, 0.8f) }; plateMat.renderQueue = (fontMat ? fontMat.renderQueue : 3000) - 1; pr.sharedMaterial = plateMat; }
            if (NoHud) pr.enabled = false;
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
            warnLabel = wl.AddComponent<TextMesh>(); warnLabel.anchor = TextAnchor.LowerCenter; warnLabel.alignment = TextAlignment.Center; warnLabel.characterSize = 0.014f; warnLabel.fontSize = 64;
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

        /// Placa a la medida del texto (espacio local del rotulo; queda apenas detras, del lado opuesto a la camara).
        void FitPlate()
        {
            if (!labelPlate) return;
            var lr = label.GetComponent<MeshRenderer>();
            bool show = lr.enabled && !string.IsNullOrEmpty(label.text);
            labelPlate.gameObject.SetActive(show); if (!show) return;
            var b = lr.localBounds; float padX = 0.25f * b.size.y + 0.004f, padY = 0.18f * b.size.y + 0.003f;
            labelPlate.localPosition = new Vector3(b.center.x, b.center.y, 0.004f);
            labelPlate.localRotation = Quaternion.identity;
            labelPlate.localScale = new Vector3(b.size.x + padX * 2f, b.size.y + padY * 2f, 1f);
        }
        public bool LabelHasPlate => labelPlate && labelPlate.gameObject.activeSelf;

        public static string SocketName(GrabSocket s) => s == null ? "" : Loc.Has("grab.socket." + s.socketId) ? Loc.T("grab.socket." + s.socketId) : s.label;

        public static AimState ResolveAim(PlayerHands hands, out GrabSocket socket)
        {
            socket = null;
            if (!hands || !hands.cam || !hands.cam.isActiveAndEnabled || !hands.CanGrab
                || !LabInput.Focused || GameFlowUI.BlockingInput) return AimState.None;
            string left = hands.HeldSocketId(0), right = hands.HeldSocketId(1);
            if (left != "" && right != "") return AimState.None;
            socket = hands.Candidate;
            if (socket)
            {
                bool ownGrip = left == socket.socketId || right == socket.socketId;
                return PlayerHands.OccupancyOf(socket) >= socket.capacity && (!ownGrip || GameSettings.Hud == HudProfile.Contextual)
                    ? AimState.Busy : AimState.Valid;
            }
            socket = hands.AimedOutOfReach;
            return socket ? AimState.OutOfReach : AimState.None;
        }

        static string GrabKeys(PlayerHands hands)
        {
            bool left = hands.HeldSocketId(0) == "", right = hands.HeldSocketId(1) == "";
            if (left && right) return InputBindings.Display(Act.GrabLeft) + " / " + InputBindings.Display(Act.GrabRight);
            return InputBindings.Display(left ? Act.GrabLeft : Act.GrabRight);
        }

        public static string ContextInstruction(PlayerHands hands, AimState state, GrabSocket socket)
        {
            if (state == AimState.Valid)
                return Loc.T(GrabIntent.Toggle ? "grab.context_toggle" : "grab.context_hold", ("keys", GrabKeys(hands)));
            if (state == AimState.OutOfReach) return Loc.T("grab.context_far");
            if (state != AimState.Busy) return "";
            string holder = PlayerHands.HolderName(socket, hands);
            return holder != "" ? Loc.T("grab.context_busy_by", ("player", holder)) : Loc.T("grab.context_busy");
        }

        void LateUpdate()
        {
            bool local = IsLocalPlayer && hands && hands.cam && hands.cam.isActiveAndEnabled;
            bool contextual = GameSettings.Hud == HudProfile.Contextual;
            GrabSocket aimed = null;
            State = local ? ResolveAim(hands, out aimed) : AimState.None;
            Aimed = aimed;
            ring.gameObject.SetActive(!contextual && Aimed);
            if (ring.gameObject.activeSelf)
            {
                var cam = hands.cam.transform;
                ring.position = Aimed.Position; ring.rotation = Quaternion.LookRotation(Aimed.Position - cam.position, cam.up);
                float pulse = GameSettings.ReduceMotion ? 1f : 1f + 0.06f * Mathf.Sin(Time.time * 10f);
                Ring(ringMesh, State == AimState.Valid ? 0.09f * pulse : 0.08f, State == AimState.Busy ? 0.03f : 0.018f, State == AimState.OutOfReach);
                Color col = State == AimState.Valid ? new Color(0.95f, 0.91f, 0.81f, 0.95f) : State == AimState.Busy ? new Color(0.94f, 0.47f, 0.38f, 0.95f) : new Color(0.72f, 0.76f, 0.75f, 0.6f);
                if (ringMat) ringMat.color = col;
                string n = SocketName(Aimed);
                // sesion 17: con que boton se agarra (mano libre, segun el dispositivo en uso) y QUIEN lo tiene si esta ocupado
                string keys = State == AimState.Valid ? GrabKeys(hands) : "";
                string holder = State == AimState.Busy ? PlayerHands.HolderName(Aimed, hands) : "";
                label.text = NoHud ? "" : State == AimState.Valid ? Loc.T("grab.aim_valid", ("socket", n), ("key", keys))
                    : State == AimState.Busy ? (holder != "" ? Loc.T("grab.aim_busy_by", ("socket", n), ("player", holder)) : Loc.T("grab.aim_busy", ("socket", n)))
                    : Loc.T("grab.aim_far", ("socket", n));
                label.color = col; label.transform.localPosition = new Vector3(0f, 0.11f, 0f);
                FitPlate();
            }
            // que bloquea el cierre de la camioneta (con el tio adentro y el jugador a menos de 7 m de la abertura)
            BlockShown = "";
            var van = VanDoors.Instance; var zone = ExtractionZone.Instance;
            bool showBlock = !contextual || !NoHud && GameHud.Instance && GameHud.Instance.ShowVanBlockMarker;
            if (local && showBlock && van && zone && !zone.Secured && zone.AllPointsInside && Vector3.Distance(hands.transform.position, van.closePoint) < 7f)
            {
                string why = van.BlockReason();
                if (why != "" && van.BlockPoint.HasValue && van.CurrentPhase != VanDoors.Phase.Closing) BlockShown = why;
                warn.gameObject.SetActive(BlockShown != "");
                if (BlockShown != "")
                {
                    var cam = hands.cam.transform;
                    warn.position = van.BlockPoint.Value + Vector3.up * 0.25f; warn.rotation = Quaternion.LookRotation(warn.position - cam.position, cam.up);
                    float pulse = contextual || GameSettings.ReduceMotion ? 1f : 1f + 0.12f * Mathf.Sin(Time.time * 8f); warn.localScale = Vector3.one * pulse;
                    var col = new Color(0.91f, 0.66f, 0.29f, 0.95f); if (warnMat) warnMat.color = col;
                    warnLabel.text = contextual ? "" : Loc.Coded(BlockShown); warnLabel.color = col;
                }
            }
            else warn.gameObject.SetActive(false);

            // tension de cada mano
            for (int h = 0; h < 2; h++)
            {
                var lr = lines[h]; var s = !contextual && local ? hands.HeldSocket(h) : null;
                lr.gameObject.SetActive(s);
                if (!s) continue;
                float t = hands.Tension01(h);
                Vector3 from = hands.HandVisual(h) ? hands.HandVisual(h).position : hands.HandTarget(h);
                Vector3 jitter = hands.EffortActive && !GameSettings.ReduceMotion ? Random.insideUnitSphere * 0.006f : Vector3.zero;
                lr.SetPosition(0, from + jitter); lr.SetPosition(1, s.Position);
                float blink = t > 0.75f && !GameSettings.ReduceMotion ? (Mathf.Sin(Time.time * 30f) > 0f ? 1f : 0.35f) : 1f;
                lr.widthMultiplier = Mathf.Lerp(0.008f, 0.03f, t);
                Color c = Color.Lerp(new Color(0.24f, 0.65f, 0.63f, 0.7f), new Color(0.94f, 0.47f, 0.38f, 0.95f), t);
                c.a *= blink; lr.startColor = lr.endColor = c;
                if (lr.sharedMaterial) lr.sharedMaterial.color = c;
            }
        }
    }
}
