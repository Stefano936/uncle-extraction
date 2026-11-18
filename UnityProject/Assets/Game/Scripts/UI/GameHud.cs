using UnityEngine;
using UnityEngine.UI;

namespace UncleExtraction
{
    /// HUD del kit de UI (UI_SPEC 4 y 5; data/ui_bindings.csv) conectado al estado real y replicado: manos independientes
    /// (Free / Pending / Holding / Strained / Released / Unavailable, con el punto sostenido y la tension), energia,
    /// compostura (solo al recibir golpes o baja), reloj (oculto sin limite), objetivo, aviso de ataque con icono, texto y
    /// subtitulo descriptivo, interaccion con progreso, avisos breves y caida con recuperacion. Al terminar la ronda se
    /// limpian los avisos de juego. Solo presenta: no decide nada.
    public class GameHud : MonoBehaviour
    {
        public enum HandState { Free, Pending, Holding, Strained, Released, Unavailable }
        public static GameHud Instance { get; private set; }
        public static bool AttackSubtitles = true;

        RoundDirector round; NpcBrain npc; Canvas canvas; RectTransform root;
        class HandUi { public RectTransform box; public Image icon; public Text title, state, socket; public Image tension; public HandState s; public float releasedUntil; public string lastHeld = ""; }
        readonly HandUi[] hand = new HandUi[2];
        Image energy, composure; RectTransform composureBox; float composureShowUntil, lastComposure = 100f;
        Text timer, objective, objectiveDetail, prompt, toast, fallTitle, fallDetail, attackTitle, attackHint, attackSub, effortHint;
        Image promptBar, attackIcon, fallBar; RectTransform attackBox, promptBox, fallBox, toastBox, subBox; Text subtitle;
        public string SubtitleText => subBox && subBox.gameObject.activeSelf ? subtitle.text : "";
        float objectiveUntil;

        public HandState StateOf(int h) => hand[h] != null ? hand[h].s : HandState.Unavailable;
        public string AttackText => attackBox && attackBox.gameObject.activeSelf ? attackTitle.text : "";
        public string PromptText => promptBox && promptBox.gameObject.activeSelf ? prompt.text : "";
        public bool Visible => root && root.gameObject.activeSelf;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; Loc.Changed -= Relabel; if (canvas) Destroy(canvas.gameObject); }

        float builtScale = -1f;
        /// Reconstruye el HUD (tamano de texto cambiado durante la partida: fuentes y filas se calculan al construir).
        public void Rebuild() { Loc.Changed -= Relabel; if (canvas) Destroy(canvas.gameObject); Start(); }

        void Start()
        {
            builtScale = Ui.TextScale;
            round = FindAnyObjectByType<RoundDirector>(); npc = FindAnyObjectByType<NpcBrain>();
            canvas = Ui.Canvas("HUD", 10); root = Ui.Stretch("Root", canvas.transform);
            // manos: abajo a la izquierda y a la derecha
            for (int h = 0; h < 2; h++)
            {
                var a = new Vector2(h == 0 ? 0f : 1f, 0f);
                // filas desde arriba (sin superponerse) que crecen con el texto: titulo, estado, punto/ayuda (2 lineas si el texto
                // es grande); la tension va abajo
                float k = Ui.TextScale, r1 = 12f, h1 = Ui.K(26), r2 = r1 + h1 + 2f, h2 = Ui.K(34), r3 = r2 + h2 + 4f, h3 = Ui.K(30) * (k > 1.01f ? 1.9f : 1f);
                float boxH = r3 + h3 + 42f, boxW = 380f * (1f + (k - 1f) * 0.5f);
                var u = new HandUi { box = Ui.PanelBox(h == 0 ? "LeftHand" : "RightHand", root, a, new Vector2(h == 0 ? Ui.Margin : -Ui.Margin, Ui.Margin), new Vector2(boxW, boxH)) };
                u.icon = Ui.Icon(u.box, "Icon", h == 0 ? "hand_left" : "hand_right", new Vector2(0, 0.5f), new Vector2(46, 8), 64, Ui.Text);
                u.title = Ui.Label(u.box, "Title", "", Ui.FontHud, Ui.Muted, TextAnchor.MiddleLeft, true); Row(u.title, 92 + 10 * (k - 1f) * 2f, r1, h1, 16);
                u.state = Ui.Label(u.box, "State", "", Ui.FontBody, Ui.Text, TextAnchor.MiddleLeft, true); Row(u.state, 92 + 10 * (k - 1f) * 2f, r2, h2, 16);
                u.socket = Ui.Label(u.box, "Socket", "", Ui.FontHud, Ui.Muted, TextAnchor.UpperLeft); Row(u.socket, 92 + 10 * (k - 1f) * 2f, r3, h3, 16);
                u.tension = Ui.Bar(u.box, "Tension", new Vector2(0.5f, 0f), new Vector2(38, 14), new Vector2(272, 16), true, out _);
                hand[h] = u;
            }
            // energia (abajo al centro) y compostura
            float energyH = 84f + Ui.K(20) - 20f;
            var eb = Ui.PanelBox("Energy", root, new Vector2(0.5f, 0f), new Vector2(0, Ui.Margin), new Vector2(420, energyH));
            Ui.Icon(eb, "Icon", "energy", new Vector2(0, 0.5f), new Vector2(36, 0), 44, Ui.Focus);
            // etiqueta a la izquierda y ayuda a la derecha, cada una en su mitad (con texto grande no se pisan)
            var el = Ui.Label(eb, "Label", "", Ui.FontHud, Ui.Muted, TextAnchor.UpperLeft, true); el.name = "EnergyLabel";
            HalfRow(el, 0f, 0.42f, 70f, 6f, 8f, Ui.K(26));
            energy = Ui.Bar(eb, "Bar", new Vector2(0.5f, 0f), new Vector2(26, 14), new Vector2(320, 22), true, out _);
            effortHint = Ui.Label(eb, "EffortHint", "", Ui.FontHud, Ui.Muted, TextAnchor.UpperRight); HalfRow(effortHint, 0.42f, 1f, 6f, 14f, 8f, Ui.K(26));
            composureBox = Ui.PanelBox("Composure", root, new Vector2(0.5f, 0f), new Vector2(0, Ui.Margin + energyH + 8f), new Vector2(420, 56));
            Ui.Icon(composureBox, "Icon", "composure", new Vector2(0, 0.5f), new Vector2(30, 0), 36, Ui.Danger);
            composure = Ui.Bar(composureBox, "Bar", new Vector2(0.5f, 0.5f), new Vector2(26, 0), new Vector2(320, 20), false, out _);
            // reloj y objetivo (arriba)
            var tb = Ui.PanelBox("Timer", root, new Vector2(0.5f, 1f), new Vector2(0, -Ui.Margin * 0.5f), new Vector2(200, 64));
            timer = Ui.Label(tb, "Text", "", Ui.FontTimer, Ui.Text, TextAnchor.MiddleCenter, true);
            var ob = Ui.PanelBox("Objective", root, new Vector2(0f, 1f), new Vector2(Ui.Margin, -Ui.Margin * 0.5f), new Vector2(460 + 160 * (Ui.TextScale - 1f), Ui.K(96)));
            objective = Ui.Label(ob, "Title", "", Ui.FontSection, Ui.Text, TextAnchor.UpperLeft, true); HalfRow(objective, 0f, 1f, 20f, 16f, 10f, Ui.K(44));
            objectiveDetail = Ui.Label(ob, "Detail", "", Ui.FontHud, Ui.Muted, TextAnchor.LowerLeft); Inset(objectiveDetail, 20, 0, 16, 12);
            // aviso de ataque (arriba, debajo del reloj): prioridad sobre ayuda contextual
            attackBox = Ui.PanelBox("Attack", root, new Vector2(0.5f, 1f), new Vector2(0, -Mathf.Max(150f, Ui.Margin * 0.5f + Ui.K(96) + 14f)), new Vector2(760, 24 + Ui.K(44) + Ui.K(34) + Ui.K(28)), true);
            attackIcon = Ui.Icon(attackBox, "Icon", "warning", new Vector2(0, 0.5f), new Vector2(60, 0), 72, Ui.Danger);
            attackTitle = Ui.Label(attackBox, "Title", "", Ui.FontSection + 4, Ui.Text, TextAnchor.UpperLeft, true); Inset(attackTitle, 120, 14, 20, 0);
            attackHint = Ui.Label(attackBox, "Hint", "", Ui.FontHud + 2, Ui.Text, TextAnchor.MiddleLeft); Inset(attackHint, 120, 14 + Ui.K(44), 20, 0); attackHint.rectTransform.offsetMin = new Vector2(120, 10 + Ui.K(28));
            attackSub = Ui.Label(attackBox, "Subtitle", "", Ui.FontHud, Ui.Muted, TextAnchor.LowerLeft); Inset(attackSub, 120, 0, 20, 8); attackSub.rectTransform.offsetMax = new Vector2(-20, -(14 + Ui.K(44) + Ui.K(34)));
            // interaccion (abajo al centro, encima de la energia)
            promptBox = Ui.PanelBox("Prompt", root, new Vector2(0.5f, 0f), new Vector2(0, 300 + Ui.K(50) - 50), new Vector2(820, Ui.K(80)));
            prompt = Ui.Label(promptBox, "Text", "", Ui.FontBody, Ui.Text, TextAnchor.MiddleCenter, true); Inset(prompt, 20, 4, 20, 18);
            promptBar = Ui.Bar(promptBox, "Progress", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(820, 12), true, out _);
            // subtitulos del tio (catalogo de dialogos; sin voz grabada)
            subBox = Ui.PanelBox("Subtitle", root, new Vector2(0.5f, 0f), new Vector2(0, 460 + Ui.K(110) - 110), new Vector2(980 * (1f + (Ui.TextScale - 1f) * 0.5f), Ui.K(64)));
            subtitle = Ui.Label(subBox, "Text", "", Ui.FontBody, Ui.Text, TextAnchor.MiddleCenter); Inset(subtitle, 20, 4, 20, 4);
            toastBox = Ui.PanelBox("Toast", root, new Vector2(0.5f, 0f), new Vector2(0, 390 + Ui.K(90) - 90), new Vector2(620, Ui.K(56)));
            toast = Ui.Label(toastBox, "Text", "", Ui.FontHud + 2, Ui.Text, TextAnchor.MiddleCenter);
            // caida
            fallBox = Ui.PanelBox("Fall", root, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(640, Ui.K(150)));
            fallTitle = Ui.Label(fallBox, "Title", "", Ui.FontSection + 4, Ui.Text, TextAnchor.UpperCenter, true); Inset(fallTitle, 20, 16, 20, 0);
            fallDetail = Ui.Label(fallBox, "Detail", "", Ui.FontHud + 2, Ui.Muted, TextAnchor.MiddleCenter); Inset(fallDetail, 20, Ui.K(30), 20, 0);
            fallBar = Ui.Bar(fallBox, "Progress", new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(520, 18), false, out _);
            objectiveUntil = Time.time + 12f;
            Loc.Changed += Relabel; Relabel();
        }

        static void Row(Text t, float left, float top, float height, float right)
        {
            var rt = t.rectTransform; rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -(top + height)); rt.offsetMax = new Vector2(-right, -top);
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
        }
        static readonly Vector3[] ca = new Vector3[4], cb = new Vector3[4];
        static bool Overlap(RectTransform a, RectTransform b)
        {
            a.GetWorldCorners(ca); b.GetWorldCorners(cb);
            return ca[0].x < cb[2].x && cb[0].x < ca[2].x && ca[0].y < cb[2].y && cb[0].y < ca[2].y;
        }
        static void HalfRow(Text t, float a0, float a1, float left, float right, float top, float height)
        {
            var rt = t.rectTransform; rt.anchorMin = new Vector2(a0, 1); rt.anchorMax = new Vector2(a1, 1); rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -(top + height)); rt.offsetMax = new Vector2(-right, -top);
        }
        static void Inset(Text t, float left, float top, float right, float bottom) { t.rectTransform.offsetMin = new Vector2(left, bottom); t.rectTransform.offsetMax = new Vector2(-right, -top); }

        void Relabel()
        {
            if (!root) return;
            hand[0].title.text = Loc.T("hud.left"); hand[1].title.text = Loc.T("hud.right");
            root.Find("Energy/EnergyLabel").GetComponent<Text>().text = Loc.T("hud.energy");
            objective.text = Loc.T("hud.objective"); objectiveDetail.text = Loc.T("hud.objective_detail");
            fallTitle.text = Loc.T("hud.fall_title");
        }

        static string Key(string s) => s.Replace("{key}", InputBindings.Display(Act.Interact)).Replace("{release}", InputBindings.Display(Act.ReleaseAll));

        void Update()
        {
            if (builtScale > 0f && Mathf.Abs(builtScale - Ui.TextScale) > 0.01f) { Rebuild(); return; }
            if (!root) return;
            var pc = PlayerControls.Local; var motor = pc ? pc.GetComponent<PlayerMotor>() : null; var hands = pc ? pc.GetComponent<PlayerHands>() : null;
            bool running = round && round.State == RoundState.Running && !round.InLobby; // en la sala no hay HUD de juego
            bool show = motor && hands && running && !(GameFlowUI.Instance && GameFlowUI.Instance.Paused);
            root.gameObject.SetActive(show);
            if (!show) return;
            bool control = motor.ControlsEnabled;

            for (int h = 0; h < 2; h++) UpdateHand(hand[h], h, hands, control);
            energy.fillAmount = Mathf.Lerp(energy.fillAmount, motor.Energy / LabBalance.EnergyMax, Smooth.Factor(0.2f)); // suavizado solo visual
            bool anyHeld = hands.AnyHeld;
            effortHint.text = !control ? "" : anyHeld ? $"{InputBindings.Display(Act.Effort)}: {Loc.T("hud.effort")}" : Loc.T("hud.grip_first");
            if (motor.Composure < lastComposure - 0.5f) composureShowUntil = Time.time + 4f;
            lastComposure = motor.Composure;
            composureBox.gameObject.SetActive(Time.time < composureShowUntil || motor.Composure < 40f);
            composure.fillAmount = motor.Composure / 100f;

            float limit = round.TimeLimit;
            timer.transform.parent.gameObject.SetActive(limit > 0f);
            if (limit > 0f) { float left = Mathf.Max(0f, limit - round.Elapsed); timer.text = $"{(int)(left / 60f):0}:{(int)(left % 60f):00}"; timer.color = left < 30f ? Ui.Danger : Ui.Text; }
            objective.transform.parent.gameObject.SetActive(Time.time < objectiveUntil);

            // ataque (prioridad: caida/control > ataque > ayuda contextual)
            bool windup = npc && (npc.State == NpcState.Windup || npc.State == NpcState.Attack) && !npc.WriggleAttempt;
            bool wriggle = npc && npc.State == NpcState.Windup && npc.WriggleAttempt;
            bool exhausted = npc && npc.State == NpcState.ForcedRecovery;
            attackBox.gameObject.SetActive(windup || wriggle || exhausted);
            if (windup)
            {
                var id = npc.CurrentAttack.id;
                attackTitle.text = Loc.T(AttackTelegraph.LabelKey(id));
                attackHint.text = Loc.T("hud.evade");
                attackSub.text = AttackSubtitles ? Loc.T(id == AttackId.Slap ? "hud.preparing_slap" : id == AttackId.Push ? "hud.preparing_push" : id == AttackId.Kick ? "hud.preparing_kick" : "hud.preparing_shake") : "";
                var icon = UiSkin.Load() ? UiSkin.Load().Icon(id == AttackId.Push ? "push" : id == AttackId.Kick ? "kick" : id == AttackId.Shake ? "shake" : "warning") : null;
                attackIcon.sprite = icon; attackIcon.enabled = icon;
            }
            else if (wriggle) { attackTitle.text = Loc.T("cart.wriggle"); attackHint.text = Loc.T("hud.evade"); attackSub.text = ""; }
            else if (exhausted) { attackTitle.text = Loc.T("hud.exhausted"); attackHint.text = ""; attackSub.text = ""; }

            // caida: reemplaza las pistas de agarre
            bool fallen = motor.State == PlayerState.Fallen || motor.State == PlayerState.GettingUp;
            fallBox.gameObject.SetActive(fallen);
            if (fallen) { fallDetail.text = Loc.T(motor.State == PlayerState.Fallen ? "hud.getting_up" : "hud.recovering"); fallBar.fillAmount = 1f - Mathf.Clamp01(motor.StateTimeLeft / LabBalance.FallLockMax); }

            // interaccion y avisos breves
            string label = control ? hands.InteractLabel : "";
            promptBox.gameObject.SetActive(label != "" && !fallen);
            if (label != "") { prompt.text = Key(Loc.Coded(label)); promptBar.fillAmount = hands.InteractProgress01; promptBar.transform.parent.gameObject.SetActive(hands.InteractProgress01 > 0.01f); }
            var dlg = UncleDialogue.Instance; string line = dlg ? dlg.Current : "";
            if (line != "") PlaceSubtitle();
            // jerarquia: si el aviso de peligro y el subtitulo no entran juntos (pantallas bajas con texto grande), manda el aviso
            subBox.gameObject.SetActive(line != "" && !(attackBox.gameObject.activeSelf && Overlap(attackBox, subBox)));
            if (line != "") subtitle.text = $"<b>{Loc.T(dlg.CurrentSpeaker == "bartender" ? "dialogue.bartender" : "dialogue.uncle")}:</b> {line}";
            bool grabToast = Time.time < GrabIntent.FeedbackUntil && GrabIntent.Feedback != "";
            bool toastOn = (Time.time < hands.FeedbackUntil && hands.LastFeedback != "" || grabToast) && !fallen;
            toastBox.gameObject.SetActive(toastOn);
            if (toastOn) toast.text = Key(Loc.Coded(Time.time < hands.FeedbackUntil && hands.LastFeedback != "" ? hands.LastFeedback : GrabIntent.Feedback));
        }

        /// El subtitulo no tapa la cara del tio (sesion 14): en su lugar bajo quedaba sobre la barbilla con el tio de cerca.
        /// Si la cabeza proyectada se cruza con ese lugar, sube debajo del reloj y del aviso de ataque (1,5 s de histeresis).
        float subHighUntil;
        public bool SubtitleHigh { get; private set; }
        void PlaceSubtitle()
        {
            float h = Ui.K(64), low = 460 + Ui.K(110) - 110, w = subBox.rect.width;
            var cam = Camera.main; var body = npc ? npc.GetComponent<NpcBody>() : null;
            if (cam && body && body.head && canvas)
            {
                float sf = canvas.scaleFactor;
                Vector3 c = cam.WorldToScreenPoint(body.head.position + Vector3.up * 0.1f);
                if (c.z > 0f)
                {
                    float r = (cam.WorldToScreenPoint(body.head.position + Vector3.up * 0.1f + cam.transform.right * 0.32f) - c).magnitude;
                    float top = (low + h) * sf, bottom = low * sf, cx = Screen.width * 0.5f;
                    if (c.y + r > bottom && c.y - r < top && Mathf.Abs(c.x - cx) < r + w * 0.5f * sf) subHighUntil = Time.time + 1.5f;
                }
            }
            SubtitleHigh = Time.time < subHighUntil;
            float bannerBottom = Mathf.Max(150f, Ui.Margin * 0.5f + Ui.K(96) + 14f) + 24 + Ui.K(44) + Ui.K(34) + Ui.K(28);
            float y = SubtitleHigh ? root.rect.height - bannerBottom - 16f - h : low;
            if (Mathf.Abs(subBox.anchoredPosition.y - y) > 0.5f) subBox.anchoredPosition = new Vector2(0f, y);
        }

        void UpdateHand(HandUi u, int h, PlayerHands hands, bool control)
        {
            string held = hands.HeldSocketId(h);
            float t = hands.Tension01(h);
            if (held == "" && u.lastHeld != "") u.releasedUntil = Time.time + 0.6f;
            u.lastHeld = held;
            bool button = LabInput.HeldForGameplay(h == 0 ? Act.GrabLeft : Act.GrabRight);
            HandState s;
            if (!control) s = HandState.Unavailable;
            else if (held != "") s = (u.s == HandState.Strained ? t > 0.5f : t > 0.65f) ? HandState.Strained : HandState.Holding; // histeresis visual
            else if (Time.time < u.releasedUntil) s = HandState.Released;
            else if (button && hands.Candidate) s = HandState.Pending; // pedido enviado, todavia sin agarre confirmado
            else s = HandState.Free;
            u.s = s;
            u.state.text = Loc.T(s == HandState.Free ? "hud.free" : s == HandState.Pending ? "hud.pending" : s == HandState.Holding ? "hud.holding" : s == HandState.Strained ? "hud.strained" : s == HandState.Released ? "hud.released" : "hud.unavailable");
            u.state.color = s == HandState.Strained ? Ui.Danger : s == HandState.Holding ? Ui.Accent : s == HandState.Unavailable ? Ui.Muted : Ui.Text;
            var sock = GrabSocket.ById(held);
            u.socket.text = held != "" ? GrabFeedback.SocketName(sock) : s == HandState.Free ? $"{InputBindings.Display(h == 0 ? Act.GrabLeft : Act.GrabRight)}: {Loc.T(GrabIntent.Toggle ? (GrabIntent.Armed(h) ? "hud.grab_armed" : "hud.tap_grab") : "hud.hold_grab")}" : "";
            u.tension.transform.parent.gameObject.SetActive(held != "");
            u.tension.fillAmount = t;
            u.icon.color = s == HandState.Strained ? Ui.Danger : s == HandState.Holding ? Ui.Accent : Ui.Text;
        }
    }
}
