using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UncleExtraction
{
    /// Pausa y resultados con el kit de UI (UI_SPEC 3, 6 y 7). En solitario la pausa detiene la simulacion; en cooperativo
    /// el mundo sigue (es del anfitrion) y se avisa. Salir pide confirmacion con la consecuencia real (anfitrion: termina
    /// para todos; cliente: vuelve al menu). Resultados: solo datos reales (tiempo, caidas del grupo, ataques del tio); en
    /// derrota el tiempo de extraccion es un guion. El anfitrion reintenta; un cliente lo pide o sale al menu.
    public class GameFlowUI : MonoBehaviour
    {
        public RoundDirector round;
        public NpcBrain npc;
        bool paused;
        float resultsSince = -1f;
        Canvas canvas; RectTransform pauseRoot, resultsRoot, confirmRoot; OptionsView options;
        Text resTitle, resDetail, resStats; Button resRetry, resMenu; Text pauseTitle, pauseInfo;

        public bool Paused => paused;
        public static GameFlowUI Instance { get; private set; }
        /// Pausa o resultados en pantalla, o escuchando una tecla para remapear: no se controla al personaje.
        public static bool BlockingInput => InputBindings.Listening || Ui.Typing || (Instance && Instance.isActiveAndEnabled && (Instance.paused || (Instance.round && (Instance.round.State != RoundState.Running || Instance.round.InLobby))));
        public bool LobbyVisible => lobby != null && lobby.Root && lobby.Root.gameObject.activeSelf;
        LobbyView lobby; RectTransform connecting; Text connectingText;
        public bool ConnectingVisible => connecting && connecting.gameObject.activeSelf;
        public bool ResultsVisible => resultsRoot && resultsRoot.gameObject.activeSelf;
        public string ResultsTitle => resTitle ? resTitle.text : "";

        void Awake() { Instance = this; }
        void Start()
        {
            GameSettings.Load(); if (!round) round = FindAnyObjectByType<RoundDirector>(); if (!npc) npc = FindAnyObjectByType<NpcBrain>();
            if (!GetComponent<GameHud>()) gameObject.AddComponent<GameHud>();
            if (!GetComponent<GameAudio>()) gameObject.AddComponent<GameAudio>();
            if (!GetComponent<UncleDialogue>()) gameObject.AddComponent<UncleDialogue>();
            if (!GetComponent<FirstSteps>()) gameObject.AddComponent<FirstSteps>(); // guia de la primera partida (local)
            if (!GetComponent<CoopMarkers>()) gameObject.AddComponent<CoopMarkers>(); // marcador contextual y nombres de compañeros
            canvas = Ui.Canvas("FlowUI", 20);
            BuildPanels();
            Loc.Changed += Relabel; Relabel();
        }

        float builtScale = -1f;
        /// Paneles de pausa, resultados, sala y conexion. Se reconstruyen si cambia el tamano de texto (con Opciones cerrado).
        void BuildPanels()
        {
            builtScale = Ui.TextScale;
            BuildPause(); BuildResults(); lobby = new LobbyView(canvas.transform, round);
            // conectando (cliente): con salida controlada (UI_SPEC 3: un spinner tiene cancelacion)
            connecting = Ui.PanelBox("Connecting", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 280));
            connectingText = Ui.Label(connecting, "Text", "", Ui.FontBody, Ui.Text, TextAnchor.UpperCenter, true); Inset(connectingText, 30, 40, 30, 0);
            var cancel = Ui.Button(connecting, "Cancel", "", new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(420, Ui.ButtonH), NetSession.LeaveToMenu);
            cancel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f); cancel.gameObject.AddComponent<LocText>().key = "button.cancel";
            connecting.gameObject.SetActive(false);
        }

        void RebuildPanels()
        {
            bool wasPaused = paused && pauseRoot && pauseRoot.gameObject.activeSelf;
            foreach (var t in new Transform[] { pauseRoot, resultsRoot, lobby?.Root, connecting, confirmRoot }) if (t) Destroy(t.gameObject);
            confirmRoot = null; BuildPanels(); Relabel();
            if (wasPaused) ShowPause();
        }
        void OnDisable() { if (Time.timeScale == 0f) Time.timeScale = 1f; }
        void OnDestroy() { if (Instance == this) Instance = null; Loc.Changed -= Relabel; if (canvas) Destroy(canvas.gameObject); }

        void BuildPause()
        {
            // alturas que crecen con el texto: titulo, aviso de partida online (2 lineas) y botones debajo
            float infoTop = 30 + Ui.K(80), y = -(infoTop + Ui.K(90));
            pauseRoot = Ui.PanelBox("Pause", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Ui.K(700), Mathf.Max(700, -y + 360 + Ui.ButtonH + 60)));
            Ui.FitInCanvas(pauseRoot);
            pauseTitle = Ui.Label(pauseRoot, "Title", "", Ui.FontTitle, Ui.Text, TextAnchor.UpperCenter, true); Inset(pauseTitle, 20, 30, 20, 0); Top(pauseTitle, 30, infoTop - 34);
            pauseInfo = Ui.Label(pauseRoot, "Info", "", Ui.FontHud + 2, Ui.Focus, TextAnchor.UpperCenter); Inset(pauseInfo, 40, infoTop, 40, 0);
            Btn(pauseRoot, "Resume", "button.resume", y, () => SetPaused(false));
            Btn(pauseRoot, "Settings", "button.settings", y - 90, () => { pauseRoot.gameObject.SetActive(false); options = new OptionsView(canvas.transform, () => { options = null; if (paused) ShowPause(); }); });
            Btn(pauseRoot, "Retry", "button.retry_same", y - 180, () => { SetPaused(false); round.Restart(); });
            var guide = Btn(pauseRoot, "Guide", "tut.repeat", y - 270, () => { var fs = FirstSteps.Instance; if (fs) { if (fs.Active) fs.Skip(); else fs.Begin(); } SetPaused(false); });
            Btn(pauseRoot, "Leave", "button.leave_match", y - 360, AskLeave);
            pauseRoot.gameObject.SetActive(false);
        }

        void BuildResults()
        {
            float detailTop = 30 + Ui.K(90), statsTop = detailTop + Ui.K(80), retryY = -(statsTop + Ui.K(420)); // sesion 15: lugar para el parte
            resultsRoot = Ui.PanelBox("Results", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Ui.K(900), -retryY + 90 + Ui.ButtonH + 48));
            Ui.FitInCanvas(resultsRoot);
            resTitle = Ui.Label(resultsRoot, "Title", "", Ui.FontTitle + 10, Ui.Text, TextAnchor.UpperCenter, true); Inset(resTitle, 20, 30, 20, 0); Top(resTitle, 30, detailTop - 34);
            resDetail = Ui.Label(resultsRoot, "Detail", "", Ui.FontBody, Ui.Muted, TextAnchor.UpperCenter); Inset(resDetail, 40, detailTop, 40, 0);
            resStats = Ui.Label(resultsRoot, "Stats", "", Ui.FontBody, Ui.Text, TextAnchor.UpperLeft); Inset(resStats, 120, statsTop, 120, -retryY - statsTop > 0 ? 0 : 0);
            resStats.rectTransform.offsetMin = new Vector2(120, resultsRoot.sizeDelta.y + retryY + 8);
            resRetry = Btn(resultsRoot, "Retry", "button.retry_same", retryY, () => { if (NetSession.Mode == NetMode.Solo) round.Restart(); else round.BackToLobby(); }); // en red: vuelve a la sala con confirmacion limpia
            resMenu = Btn(resultsRoot, "Menu", "button.back_menu", retryY - 90, () => NetSession.LeaveToMenu());
            // vista limpia para capturas: solo el parte, sin botones ni HUD; cualquier tecla o clic vuelve
            var shot = Ui.Button(resultsRoot, "Snapshot", "", new Vector2(1f, 0f), new Vector2(-16, 16), new Vector2(Ui.K(190), Ui.K(40)), () => CleanView(true)); // abajo a la derecha: no pisa el titulo
            shot.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f); var st = shot.GetComponentInChildren<Text>(); st.fontSize = Ui.FontHud; st.gameObject.AddComponent<LocText>().key = "report.snapshot";
            resultsRoot.gameObject.SetActive(false);
        }

        Button Btn(Transform p, string name, string key, float y, System.Action a)
        {
            var b = Ui.Button(p, name, "", new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(520, Ui.ButtonH), a);
            b.gameObject.AddComponent<LocText>().key = key; return b;
        }
        /// Limita un texto a una franja desde arriba (para que un titulo largo se ajuste en vez de pisar lo de abajo).
        static void Top(Text t, float top, float height)
        {
            var rt = t.rectTransform; float l = rt.offsetMin.x, r = rt.offsetMax.x;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(l, -(top + height)); rt.offsetMax = new Vector2(r, -top);
        }
        static void Inset(Text t, float l, float top, float r, float b) { t.rectTransform.offsetMin = new Vector2(l, b); t.rectTransform.offsetMax = new Vector2(-r, -top); }

        void Relabel()
        {
            if (pauseTitle) pauseTitle.text = Loc.T("round.pause");
            if (pauseInfo) pauseInfo.text = NetSession.Mode != NetMode.Solo ? Loc.T("round.online_continues") + "\n" + Loc.T("round.online_detail") : "";
        }

        void Update()
        {
            if (options == null && builtScale > 0f && Mathf.Abs(builtScale - Ui.TextScale) > 0.01f) RebuildPanels();
            if (!round) return;
            bool ended = round.State != RoundState.Running;
            if (ended && resultsSince < 0f) { resultsSince = Time.unscaledTime; CloseOverlays(); paused = false; if (Time.timeScale == 0f) Time.timeScale = 1f; }
            if (!ended) resultsSince = -1f;
            if (LabInput.Pressed(Act.Pause) && !ended && !InputBindings.Listening && !InputBindings.JustListened)
            {
                if (confirmRoot) { Destroy(confirmRoot.gameObject); confirmRoot = null; ShowPause(); }
                else if (options != null) { /* Esc dentro de opciones: lo maneja el boton Volver */ }
                else SetPaused(!paused);
            }
            if (BlockingInput) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            UpdateResults(ended);
            lobby?.Update();
            var nmc = Unity.Netcode.NetworkManager.Singleton;
            bool creating = NetSession.Mode == NetMode.Host && NetSession.Online && (nmc == null || !nmc.IsListening);
            bool conn = (NetSession.Mode == NetMode.Client && PlayerControls.Local == null) || creating;
            if (connecting.gameObject.activeSelf != conn) { connecting.gameObject.SetActive(conn); if (conn) EventSystem.current?.SetSelectedGameObject(connecting.Find("Cancel").gameObject); }
            if (conn) { connectingText.text = creating ? Loc.T("lobby.creating") : Loc.T("lobby.connecting") + "\n" + (NetSession.Online ? Loc.T("lobby.code") + ": " + NetSession.JoinCode : NetSession.Address + ":" + NetSession.Port); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        void CloseOverlays()
        {
            options?.Close(); options = null;
            if (confirmRoot) { Destroy(confirmRoot.gameObject); confirmRoot = null; }
            if (pauseRoot) pauseRoot.gameObject.SetActive(false);
        }

        void ShowPause()
        {
            pauseRoot.gameObject.SetActive(true); Relabel();
            var retry = pauseRoot.Find("Retry"); if (retry) retry.gameObject.SetActive(NetAuth.IsServer); // en red reinicia el anfitrion
            var g = pauseRoot.Find("Guide"); if (g) g.GetComponent<LocText>().key = FirstSteps.Instance && FirstSteps.Instance.Active ? "tut.skip" : "tut.repeat";
            EventSystem.current?.SetSelectedGameObject(pauseRoot.Find("Resume").gameObject);
        }

        public void SetPaused(bool on)
        {
            paused = on;
            if (on) ShowPause(); else CloseOverlays();
            if (NetSession.Mode == NetMode.Solo) Time.timeScale = on ? 0f : 1f;
            Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = on;
            // al pausar: soltar entradas mantenidas (no quedan teclas pegadas por perder el foco)
            if (on) { LabInput.ClearSimulated(); }
        }

        void AskLeave()
        {
            pauseRoot.gameObject.SetActive(false);
            confirmRoot = Ui.PanelBox("ConfirmLeave", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 300), true);
            var q = Ui.Label(confirmRoot, "Q", Loc.T(NetAuth.IsServer && NetSession.Mode == NetMode.Host ? "round.host_leave" : "round.client_leave"), Ui.FontBody, Ui.Text, TextAnchor.UpperCenter, true);
            Inset(q, 30, 40, 30, 0);
            var leave = Ui.Button(confirmRoot, "Leave", Loc.T("button.leave_match"), new Vector2(0.5f, 0f), new Vector2(-200, 40), new Vector2(370, Ui.ButtonH), () => { SetPaused(false); NetSession.LeaveToMenu(); });
            Ui.Button(confirmRoot, "Cancel", Loc.T("button.cancel"), new Vector2(0.5f, 0f), new Vector2(200, 40), new Vector2(370, Ui.ButtonH), () => { Destroy(confirmRoot.gameObject); confirmRoot = null; ShowPause(); });
            EventSystem.current?.SetSelectedGameObject(leave.gameObject);
        }

        /// Vista para capturas (sesion 15): oculta HUD, botones y guia; deja el panel del parte. Cualquier tecla/clic vuelve.
        public bool CleanViewOn { get; private set; }
        void CleanView(bool on)
        {
            CleanViewOn = on;
            foreach (var name in new[] { "HUD", "FirstSteps", "CoopMarkers" }) { var go = GameObject.Find(name); if (go) { var c = go.GetComponent<Canvas>(); if (c) c.enabled = !on; } }
            foreach (var b in new[] { "Retry", "Menu", "Snapshot" }) { var t = resultsRoot.Find(b); if (t) t.gameObject.SetActive(!on); }
        }

        void UpdateResults(bool ended)
        {
            if (CleanViewOn && (!ended || UnityEngine.InputSystem.Keyboard.current?.anyKey.wasPressedThisFrame == true || UnityEngine.InputSystem.Mouse.current?.leftButton.wasPressedThisFrame == true)) CleanView(false);
            bool show = ended && Time.unscaledTime - resultsSince > 0.4f;
            if (resultsRoot.gameObject.activeSelf != show)
            {
                resultsRoot.gameObject.SetActive(show);
                if (show) EventSystem.current?.SetSelectedGameObject((NetAuth.IsServer ? resRetry : resMenu).gameObject);
            }
            if (!show) return;
            bool win = round.State == RoundState.Success;
            resTitle.text = Loc.T(win ? "round.success" : "round.timeout");
            resTitle.color = win ? Ui.Accent : Ui.Danger;
            resDetail.text = Loc.T(win ? "round.success_detail" : "round.timeout_detail");
            int falls = 0; foreach (var pm in PlayerMotor.All) if (pm) falls += pm.FallCount;
            string time = win ? $"{(int)(round.Elapsed / 60f)}:{(int)(round.Elapsed % 60f):00}" : "—";
            string rep = RoundStats.Render(RoundStats.LastReport);
            if (rep != "") resStats.text = rep + "\n" + Loc.T(round.PartySize == 1 ? "lobby.players_one" : "lobby.players_other", ("count", round.PartySize.ToString())) + " · " + Loc.T(round.CurrentDifficulty == Difficulty.Relaxed ? "lobby.relaxed" : round.CurrentDifficulty == Difficulty.Hard ? "lobby.hard" : "lobby.normal") + (win ? "" : "\n" + Loc.T("round.hint"));
            else resStats.text = $"{Loc.T("round.time")}:  {time}\n{Loc.T("round.falls")}:  {falls}\n{Loc.T("lobby.difficulty")}:  {Loc.T(round.CurrentDifficulty == Difficulty.Relaxed ? "lobby.relaxed" : round.CurrentDifficulty == Difficulty.Hard ? "lobby.hard" : "lobby.normal")}\n{Loc.T(round.PartySize == 1 ? "lobby.players_one" : "lobby.players_other", ("count", round.PartySize.ToString()))}"
                            + (win ? "" : "\n\n" + Loc.T("round.hint"));
            // reintentar: el anfitrion decide; un invitado espera al anfitrion (o sale)
            bool host = NetAuth.IsServer;
            resRetry.interactable = host;
            resRetry.GetComponentInChildren<Text>().text = Loc.T(host ? "button.retry_same" : "button.wait_host");
        }
    }

    /// Texto de un boton o etiqueta que se traduce solo al cambiar el idioma.
    public class LocText : MonoBehaviour
    {
        [SerializeField] string _key;
        /// Asignar la clave aplica el texto en el acto (AddComponent llama a OnEnable antes de poder asignarla).
        public string key { get => _key; set { _key = value; Apply(); } }
        Text t;
        void OnEnable() { Loc.Changed += Apply; Apply(); }
        void OnDisable() { Loc.Changed -= Apply; }
        void Apply() { if (!t) t = GetComponentInChildren<Text>(); if (t && !string.IsNullOrEmpty(_key)) t.text = Loc.T(_key); }
    }
}
