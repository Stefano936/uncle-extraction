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
        Image promptBar, attackIcon, fallBar; RectTransform attackBox, promptBox, fallBox, toastBox;
        float objectiveUntil;

        public HandState StateOf(int h) => hand[h] != null ? hand[h].s : HandState.Unavailable;
        public string AttackText => attackBox && attackBox.gameObject.activeSelf ? attackTitle.text : "";
        public string PromptText => promptBox && promptBox.gameObject.activeSelf ? prompt.text : "";
        public bool Visible => root && root.gameObject.activeSelf;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; Loc.Changed -= Relabel; if (canvas) Destroy(canvas.gameObject); }

        void Start()
        {
            round = FindAnyObjectByType<RoundDirector>(); npc = FindAnyObjectByType<NpcBrain>();
            canvas = Ui.Canvas("HUD", 10); root = Ui.Stretch("Root", canvas.transform);
            // manos: abajo a la izquierda y a la derecha
            for (int h = 0; h < 2; h++)
            {
                var a = new Vector2(h == 0 ? 0f : 1f, 0f);
                var u = new HandUi { box = Ui.PanelBox(h == 0 ? "LeftHand" : "RightHand", root, a, new Vector2(h == 0 ? Ui.Margin : -Ui.Margin, Ui.Margin), new Vector2(380, 150)) };
                u.icon = Ui.Icon(u.box, "Icon", h == 0 ? "hand_left" : "hand_right", new Vector2(0, 0.5f), new Vector2(46, 8), 64, Ui.Text);
                u.title = Ui.Label(u.box, "Title", "", Ui.FontHud, Ui.Muted, TextAnchor.UpperLeft, true); Inset(u.title, 92, 14, 16, 0);
                u.state = Ui.Label(u.box, "State", "", Ui.FontBody, Ui.Text, TextAnchor.MiddleLeft, true); Inset(u.state, 92, 46, 16, 0);
                u.socket = Ui.Label(u.box, "Socket", "", Ui.FontHud, Ui.Muted, TextAnchor.LowerLeft); Inset(u.socket, 92, 0, 16, 34);
                u.tension = Ui.Bar(u.box, "Tension", new Vector2(0.5f, 0f), new Vector2(46, 16), new Vector2(260, 18), true, out _);
                hand[h] = u;
            }
            // energia (abajo al centro) y compostura
            var eb = Ui.PanelBox("Energy", root, new Vector2(0.5f, 0f), new Vector2(0, Ui.Margin), new Vector2(420, 84));
            Ui.Icon(eb, "Icon", "energy", new Vector2(0, 0.5f), new Vector2(36, 0), 44, Ui.Focus);
            var el = Ui.Label(eb, "Label", "", Ui.FontHud, Ui.Muted, TextAnchor.UpperLeft, true); Inset(el, 70, 8, 12, 0); el.name = "EnergyLabel";
            energy = Ui.Bar(eb, "Bar", new Vector2(0.5f, 0f), new Vector2(26, 14), new Vector2(320, 22), true, out _);
            effortHint = Ui.Label(eb, "EffortHint", "", Ui.FontHud, Ui.Muted, TextAnchor.UpperRight); Inset(effortHint, 70, 8, 14, 0);
            composureBox = Ui.PanelBox("Composure", root, new Vector2(0.5f, 0f), new Vector2(0, Ui.Margin + 92), new Vector2(420, 56));
            Ui.Icon(composureBox, "Icon", "composure", new Vector2(0, 0.5f), new Vector2(30, 0), 36, Ui.Danger);
            composure = Ui.Bar(composureBox, "Bar", new Vector2(0.5f, 0.5f), new Vector2(26, 0), new Vector2(320, 20), false, out _);
            // reloj y objetivo (arriba)
            var tb = Ui.PanelBox("Timer", root, new Vector2(0.5f, 1f), new Vector2(0, -Ui.Margin * 0.5f), new Vector2(200, 64));
            timer = Ui.Label(tb, "Text", "", Ui.FontTimer, Ui.Text, TextAnchor.MiddleCenter, true);
            var ob = Ui.PanelBox("Objective", root, new Vector2(0f, 1f), new Vector2(Ui.Margin, -Ui.Margin * 0.5f), new Vector2(460, 96));
            objective = Ui.Label(ob, "Title", "", Ui.FontSection, Ui.Text, TextAnchor.UpperLeft, true); Inset(objective, 20, 10, 16, 0);
            objectiveDetail = Ui.Label(ob, "Detail", "", Ui.FontHud, Ui.Muted, TextAnchor.LowerLeft); Inset(objectiveDetail, 20, 0, 16, 12);
            // aviso de ataque (arriba, debajo del reloj): prioridad sobre ayuda contextual
            attackBox = Ui.PanelBox("Attack", root, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(760, 132), true);
            attackIcon = Ui.Icon(attackBox, "Icon", "warning", new Vector2(0, 0.5f), new Vector2(60, 0), 72, Ui.Danger);
            attackTitle = Ui.Label(attackBox, "Title", "", Ui.FontSection + 4, Ui.Text, TextAnchor.UpperLeft, true); Inset(attackTitle, 120, 14, 20, 0);
            attackHint = Ui.Label(attackBox, "Hint", "", Ui.FontHud + 2, Ui.Text, TextAnchor.MiddleLeft); Inset(attackHint, 120, 58, 20, 0);
            attackSub = Ui.Label(attackBox, "Subtitle", "", Ui.FontHud, Ui.Muted, TextAnchor.LowerLeft); Inset(attackSub, 120, 0, 20, 10);
            // interaccion (abajo al centro, encima de la energia)
            promptBox = Ui.PanelBox("Prompt", root, new Vector2(0.5f, 0f), new Vector2(0, 300), new Vector2(820, 80));
            prompt = Ui.Label(promptBox, "Text", "", Ui.FontBody, Ui.Text, TextAnchor.MiddleCenter, true); Inset(prompt, 20, 4, 20, 18);
            promptBar = Ui.Bar(promptBox, "Progress", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(820, 12), true, out _);
            toastBox = Ui.PanelBox("Toast", root, new Vector2(0.5f, 0f), new Vector2(0, 390), new Vector2(620, 56));
            toast = Ui.Label(toastBox, "Text", "", Ui.FontHud + 2, Ui.Text, TextAnchor.MiddleCenter);
            // caida
            fallBox = Ui.PanelBox("Fall", root, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(640, 150));
            fallTitle = Ui.Label(fallBox, "Title", "", Ui.FontSection + 4, Ui.Text, TextAnchor.UpperCenter, true); Inset(fallTitle, 20, 16, 20, 0);
            fallDetail = Ui.Label(fallBox, "Detail", "", Ui.FontHud + 2, Ui.Muted, TextAnchor.MiddleCenter); Inset(fallDetail, 20, 30, 20, 0);
            fallBar = Ui.Bar(fallBox, "Progress", new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(520, 18), false, out _);
            objectiveUntil = Time.time + 12f;
            Loc.Changed += Relabel; Relabel();
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
            if (!root) return;
            var pc = PlayerControls.Local; var motor = pc ? pc.GetComponent<PlayerMotor>() : null; var hands = pc ? pc.GetComponent<PlayerHands>() : null;
            bool running = round && round.State == RoundState.Running;
            bool show = motor && hands && running && !(GameFlowUI.Instance && GameFlowUI.Instance.Paused);
            root.gameObject.SetActive(show);
            if (!show) return;
            bool control = motor.ControlsEnabled;

            for (int h = 0; h < 2; h++) UpdateHand(hand[h], h, hands, control);
            energy.fillAmount = Mathf.Lerp(energy.fillAmount, motor.Energy / LabBalance.EnergyMax, 0.2f); // suavizado solo visual
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
            bool toastOn = Time.time < hands.FeedbackUntil && hands.LastFeedback != "" && !fallen;
            toastBox.gameObject.SetActive(toastOn);
            if (toastOn) toast.text = Key(Loc.Coded(hands.LastFeedback));
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
            u.socket.text = held != "" ? GrabFeedback.SocketName(sock) : s == HandState.Free ? $"{InputBindings.Display(h == 0 ? Act.GrabLeft : Act.GrabRight)}: {Loc.T("hud.hold_grab")}" : "";
            u.tension.transform.parent.gameObject.SetActive(held != "");
            u.tension.fillAmount = t;
            u.icon.color = s == HandState.Strained ? Ui.Danger : s == HandState.Holding ? Ui.Accent : Ui.Text;
        }
    }
}
