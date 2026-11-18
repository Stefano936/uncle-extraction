using UnityEngine;
using UnityEngine.UI;

namespace UncleExtraction
{
    /// HUD del kit de UI (UI_SPEC 4 y 5; data/ui_bindings.csv) conectado al estado real y replicado: manos independientes
    /// (Free / Pending / Holding / Strained / Released / Unavailable, con el punto sostenido y la tension), energia,
    /// compostura (solo al recibir golpes o baja), reloj (oculto sin limite), objetivo, aviso de ataque con icono, texto y
    /// subtitulo descriptivo, interaccion con progreso, avisos breves y caida con recuperacion. Al terminar la ronda se
    /// limpian los avisos de juego. Solo presenta: no decide nada.
    [DefaultExecutionOrder(50)]
    public class GameHud : MonoBehaviour
    {
        public enum HandState { Free, Pending, Holding, Strained, Released, Unavailable }
        public static GameHud Instance { get; private set; }
        public static bool AttackSubtitles = true;

        RoundDirector round; NpcBrain npc; Canvas canvas; RectTransform root;
        class HandUi { public RectTransform box; public Image icon; public Text title, state, socket; public Image tension; public HandState s; public float releasedUntil; public string lastHeld = ""; }
        readonly HandUi[] hand = new HandUi[2];
        Image energy, composure, timerPanel, promptPanel; RectTransform energyBox, composureBox, reticle; float composureShowUntil, lastComposure = 100f;
        Text timer, objective, objectiveDetail, prompt, toast, fallTitle, fallDetail, attackTitle, attackHint, attackSub, effortHint;
        Color promptPanelColor;
        Image promptBar, promptTrack, attackIcon, fallBar; RectTransform attackBox, promptBox, fallBox, toastBox, subBox; Text subtitle;
        public string SubtitleText => subBox && subBox.gameObject.activeSelf ? subtitle.text : "";
        float objectiveUntil; ChallengeMode shownChallenge = ChallengeMode.Standard;
        HudProfile shownProfile = (HudProfile)(-1);
        int shownRound = -1;
        bool readyAnnounced, energyLow;
        float energyUntil, noticeSince;
        string notice = "";

        public bool AttentionRequired { get; private set; }
        public bool AimPromptVisible { get; private set; }
        public bool ShowVanBlockMarker { get; private set; }
        public string ShownInteraction { get; private set; } = "";

        public HandState StateOf(int h) => hand[h] != null ? hand[h].s : HandState.Unavailable;
        public string AttackText => Visible && attackBox && attackBox.gameObject.activeSelf ? attackTitle.text : "";
        public string PromptText => Visible && promptBox && promptBox.gameObject.activeSelf ? prompt.text : "";
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
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ueNoHud") >= 0) canvas.enabled = false; // material de tienda (sesion 15)
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
            var eb = energyBox = Ui.PanelBox("Energy", root, new Vector2(0.5f, 0f), new Vector2(0, Ui.Margin), new Vector2(420, energyH));
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
            timerPanel = tb.GetComponent<Image>();
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
            promptPanel = promptBox.GetComponent<Image>();
            promptPanelColor = promptPanel.color;
            reticle = Ui.Rect("Reticle", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));
            Ui.Image(reticle, "Dot", null, Ui.Text, false).gameObject.AddComponent<Outline>().effectColor = Ui.Ink;
            prompt = Ui.Label(promptBox, "Text", "", Ui.FontBody, Ui.Text, TextAnchor.MiddleCenter, true); Inset(prompt, 20, 4, 20, 18);
            promptBar = Ui.Bar(promptBox, "Progress", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(820, 12), true, out promptTrack);
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
            shownProfile = (HudProfile)(-1);
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
            AttentionRequired = AimPromptVisible = ShowVanBlockMarker = false;
            ShownInteraction = "";
            if (!show) { notice = ""; energyUntil = 0f; return; }
            bool control = motor.ControlsEnabled;
            bool contextual = GameSettings.Hud == HudProfile.Contextual;
            if (GameSettings.Hud != shownProfile) ApplyProfile(contextual);
            if (shownRound != round.Restarts)
            {
                shownRound = round.Restarts;
                readyAnnounced = energyLow = false;
                energyUntil = composureShowUntil = 0f;
                lastComposure = motor.Composure;
                notice = "";
                objectiveUntil = Time.time + (contextual ? 7f : 12f);
                foreach (var u in hand) { u.lastHeld = ""; u.releasedUntil = 0f; }
            }

            for (int h = 0; h < 2; h++) UpdateHand(hand[h], h, hands, control);
            UpdateEnergy(motor, hands, contextual, control);
            if (motor.Composure < lastComposure - 0.5f) composureShowUntil = Time.time + 4f;
            lastComposure = motor.Composure;
            composureBox.gameObject.SetActive(Time.time < composureShowUntil || motor.Composure < 40f);
            composure.fillAmount = motor.Composure / 100f;

            float limit = round.TimeLimit;
            timer.transform.parent.gameObject.SetActive(limit > 0f);
            if (limit > 0f)
            {
                float left = Mathf.Max(0f, limit - round.Elapsed);
                timer.text = $"{(int)(left / 60f):0}:{(int)(left % 60f):00}";
                timer.color = left < 30f ? Ui.Danger : contextual ? Ui.Muted : Ui.Text;
                timerPanel.enabled = !contextual || left < 30f;
            }

            // ataque (prioridad: caida/control > ataque > ayuda contextual)
            bool windup = npc && (npc.State == NpcState.Windup || npc.State == NpcState.Attack) && !npc.WriggleAttempt;
            bool wriggle = npc && npc.State == NpcState.Windup && npc.WriggleAttempt;
            bool exhausted = npc && npc.State == NpcState.ForcedRecovery;
            attackBox.gameObject.SetActive(windup || wriggle || !contextual && exhausted);
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

            AttentionRequired = contextual && (fallen || windup || wriggle);
            if (contextual) attackBox.gameObject.SetActive(!fallen && (windup || wriggle));
            reticle.gameObject.SetActive(contextual && control && !AttentionRequired && LabInput.Focused);
            string interaction = control ? hands.InteractLabel : "";
            string feedback = Time.time < hands.FeedbackUntil ? hands.LastFeedback : "";
            string intent = Time.time < GrabIntent.FeedbackUntil ? GrabIntent.Feedback : "";
            if (contextual)
                ContextPrompt(hands, interaction, feedback, intent, control);
            else
            {
                SetPrompt(interaction == "" || fallen ? "" : Key(Loc.Coded(interaction)), hands.InteractProgress01);
                string message = feedback != "" ? feedback : intent;
                toastBox.gameObject.SetActive(!fallen && message != "");
                if (message != "") toast.text = Key(Loc.Coded(message));
            }
            UpdateObjective(contextual);
            var dlg = UncleDialogue.Instance; string line = dlg ? dlg.Current : "";
            if (line != "") PlaceSubtitle(contextual);
            bool subtitleFits = !(attackBox.gameObject.activeSelf && Overlap(attackBox, subBox));
            subBox.gameObject.SetActive(line != "" && subtitleFits);
            if (line != "") subtitle.text = $"<b>{Loc.T(dlg.CurrentSpeaker == "bartender" ? "dialogue.bartender" : "dialogue.uncle")}:</b> {line}";

        }

        void ApplyProfile(bool contextual)
        {
            bool wasBuilt = (int)shownProfile >= 0;
            shownProfile = GameSettings.Hud;
            if (wasBuilt && Time.time < objectiveUntil && contextual) objectiveUntil = Mathf.Min(objectiveUntil, Time.time + 7f);
            foreach (var u in hand) u.box.gameObject.SetActive(!contextual);
            timer.fontSize = Mathf.RoundToInt(Ui.K(contextual ? Ui.FontHud + 2 : Ui.FontTimer));
            timer.resizeTextMaxSize = timer.fontSize;
            timer.resizeTextMinSize = timer.fontSize * 2 / 3;

            Vector2 anchor = new Vector2(0.5f, contextual ? 0.5f : 0f);
            promptBox.anchorMin = promptBox.anchorMax = anchor;
            promptBox.pivot = new Vector2(0.5f, contextual ? 1f : 0f);
            promptBox.anchoredPosition = new Vector2(0f, contextual ? -28f : 300 + Ui.K(50) - 50);
            promptBox.sizeDelta = new Vector2(820f, Ui.K(contextual ? 64f : 80f));
            prompt.fontSize = Mathf.RoundToInt(Ui.K(contextual ? Ui.FontHud + 2 : Ui.FontBody));
            prompt.resizeTextMaxSize = prompt.fontSize;
            prompt.resizeTextMinSize = prompt.fontSize * 2 / 3;
            prompt.supportRichText = !contextual;
            prompt.text = "";
            promptPanel.color = promptPanelColor;
            promptTrack.rectTransform.sizeDelta = new Vector2(820f, 12f);
            prompt.color = Ui.Text;
            Inset(prompt, 20, 4, 20, contextual ? 10 : 18);
            attackBox.anchoredPosition = new Vector2(0f, -AttackTop(contextual));
            subHighUntil = 0f;
            notice = "";
        }

        void UpdateObjective(bool contextual)
        {
            var challenge = round.CurrentChallenge;
            if (challenge != shownChallenge)
            {
                shownChallenge = challenge;
                objectiveUntil = Time.time + (contextual ? 7f : 12f);
            }

            var zone = ExtractionZone.Instance;
            if (contextual && !readyAnnounced && zone && zone.PoseReady && zone.AllPointsInside)
            {
                readyAnnounced = true;
                objectiveUntil = Time.time + 7f;
            }
            bool show = Time.time < objectiveUntil && (!contextual || !AttentionRequired && !ShownInteraction.StartsWith("van."));
            objective.transform.parent.gameObject.SetActive(show);
            if (!show) return;
            bool close = contextual && readyAnnounced && zone && zone.AllPointsInside;
            objectiveDetail.text = Loc.T(close ? "hud.goal_close" : challenge == ChallengeMode.Standard
                ? "hud.objective_detail" : "challenge.hint_" + challenge.ToString().ToLowerInvariant());
        }

        void UpdateEnergy(PlayerMotor motor, PlayerHands hands, bool contextual, bool control)
        {
            float fraction = motor.Energy / LabBalance.EnergyMax;
            energy.fillAmount = Mathf.Lerp(energy.fillAmount, fraction, Smooth.Factor(0.2f));
            energyLow = fraction < (energyLow ? 0.32f : 0.24f);
            if (hands.EffortActive && hands.AnyHeld && control && LabInput.Focused) energyUntil = Time.time + 1.1f;
            energyBox.gameObject.SetActive(!contextual || control && (energyLow || Time.time < energyUntil));
            if (contextual)
                effortHint.text = fraction < 0.05f ? Loc.T("hud.effort_spent") : energyLow ? Loc.T("hud.effort_low") : "";
            else
                effortHint.text = !control ? "" : hands.AnyHeld
                    ? $"{InputBindings.Display(Act.Effort)}: {Loc.T("hud.effort")}" : Loc.T("hud.grip_first");
        }

        void ContextPrompt(PlayerHands hands, string interaction, string feedback, string intent, bool control)
        {
            toastBox.gameObject.SetActive(false);
            string text = "";
            float progress = 0f;
            bool recurringNotice = false;
            if (control && LabInput.Focused && !GameFlowUI.BlockingInput && !AttentionRequired)
            {
                if (interaction != "")
                {
                    ShownInteraction = interaction;
                    text = Key(Loc.Coded(interaction));
                    progress = hands.InteractProgress01;
                    ShowVanBlockMarker = interaction.StartsWith("van.") && interaction != "van.hold_close";
                }
                else
                {
                    var aim = GrabFeedback.ResolveAim(hands, out var socket);
                    if (feedback == "grab.lost_distance") text = Loc.T(feedback);
                    else if (aim == GrabFeedback.AimState.Busy || aim == GrabFeedback.AimState.OutOfReach)
                    {
                        AimPromptVisible = true;
                        text = GrabFeedback.ContextInstruction(hands, aim, socket);
                        recurringNotice = true;
                    }
                    else if (intent != "") text = Loc.T(intent);
                    else if (aim == GrabFeedback.AimState.Valid)
                    {
                        AimPromptVisible = true;
                        text = GrabFeedback.ContextInstruction(hands, aim, socket);
                    }
                    else if (feedback != "hud.grip_busy" && feedback != "hud.too_far")
                    {
                        text = Key(Loc.Coded(feedback));
                        recurringNotice = true;
                    }
                }
            }
            if (text != notice) { notice = text; noticeSince = Time.unscaledTime; }
            bool muted = recurringNotice && Time.unscaledTime - noticeSince > 2f;
            Color panel = promptPanelColor;
            if (muted) panel.a *= 0.65f;
            promptPanel.color = panel;
            prompt.color = muted ? Ui.Muted : Ui.Text;
            SetPrompt(text, progress);
        }

        void SetPrompt(string label, float progress)
        {
            promptBox.gameObject.SetActive(label != "");
            if (label == "") return;
            if (prompt.text != label)
            {
                prompt.text = label;
                if (shownProfile == HudProfile.Contextual)
                {
                    float width = Mathf.Clamp(prompt.preferredWidth + 44f, 200f, 820f);
                    promptBox.sizeDelta = new Vector2(width, promptBox.sizeDelta.y);
                    promptTrack.rectTransform.sizeDelta = new Vector2(width, 12f);
                }
            }
            promptBar.fillAmount = progress;
            promptBar.transform.parent.gameObject.SetActive(progress > 0.01f);
        }

        static float AttackTop(bool contextual) => contextual ? 84f : Mathf.Max(150f, Ui.Margin * 0.5f + Ui.K(96) + 14f);

        /// El subtitulo no tapa la cara del tio (sesion 14): en su lugar bajo quedaba sobre la barbilla con el tio de cerca.
        /// Si la cabeza proyectada se cruza con ese lugar, sube debajo del reloj y del aviso de ataque (1,5 s de histeresis).
        float subHighUntil;
        public bool SubtitleHigh { get; private set; }
        void PlaceSubtitle(bool contextual)
        {
            float h = Ui.K(64), low = contextual ? 228f : 460 + Ui.K(110) - 110, w = subBox.rect.width;
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
            float bannerBottom = AttackTop(contextual) + 24 + Ui.K(44) + Ui.K(34) + Ui.K(28);
            float y = SubtitleHigh ? root.rect.height - bannerBottom - 16f - h : low;
            subBox.anchoredPosition = new Vector2(0f, y);
            if (contextual && (fallBox.gameObject.activeSelf && Overlap(fallBox, subBox)
                || promptBox.gameObject.activeSelf && Overlap(promptBox, subBox)))
            {
                float gap = fallBox.gameObject.activeSelf ? fallBox.rect.height * 0.5f - 44f : 24f;
                subBox.anchoredPosition = new Vector2(0f, root.rect.height * 0.5f + gap);
            }
        }

        void UpdateHand(HandUi u, int h, PlayerHands hands, bool control)
        {
            string held = hands.HeldSocketId(h);
            float t = hands.Tension01(h);
            if (held == "" && u.lastHeld != "") u.releasedUntil = Time.time + 0.6f;
            u.lastHeld = held;

            HandState s;
            if (!control) s = HandState.Unavailable;
            else if (held != "") s = (u.s == HandState.Strained ? t > 0.5f : t > 0.65f) ? HandState.Strained : HandState.Holding; // histeresis visual
            else if (hands.IsGrabPending(h)) s = HandState.Pending;
            else if (Time.time < u.releasedUntil) s = HandState.Released;
            else s = HandState.Free;
            u.s = s;
            if (shownProfile == HudProfile.Contextual) return;
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
