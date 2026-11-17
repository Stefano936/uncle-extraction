using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UncleExtraction
{
    /// Sala previa con el kit de UI (UI_SPEC 3; ui_bindings lobby.*): solo miembros presentes (1 a 4) con anfitrion, «vos»
    /// y su confirmacion; dificultad editable por el anfitrion y de solo lectura para invitados; LISTO / SIN CONFIRMAR;
    /// INICIAR EXTRACCION solo para el anfitrion, habilitado con todos listos y con el motivo visible si no; salir de la sala.
    /// Presenta estado replicado y emite pedidos (listo, iniciar, dificultad); decide el anfitrion.
    public class LobbyView
    {
        public RectTransform Root { get; private set; }
        readonly RoundDirector round;
        readonly Text title, members, difficulty, reason, mode, codeLabel, codeText;
        readonly Button copyBtn;
        float copiedUntil;
        public string CodeText => codeText && codeText.gameObject.activeInHierarchy ? codeText.text : "";
        readonly Button readyBtn, startBtn, diffBtn, leaveBtn;
        bool wasVisible;

        public string MembersText => members ? members.text : "";
        public string ReasonText => reason ? reason.text : "";
        public bool StartInteractable => startBtn && startBtn.interactable;

        public LobbyView(Transform parent, RoundDirector round)
        {
            this.round = round;
            Root = Ui.PanelBox("Lobby", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 760));
            title = Ui.Label(Root, "Title", "", Ui.FontTitle, Ui.Text, TextAnchor.UpperCenter, true); Inset(title, 20, 26, 20, 0);
            mode = Ui.Label(Root, "Mode", "", Ui.FontHud + 2, Ui.Muted, TextAnchor.UpperCenter); Inset(mode, 40, 100, 40, 0);
            var crew = Ui.PanelBox("Crew", Root, new Vector2(0f, 1f), new Vector2(48, -160), new Vector2(560, 380)); crew.pivot = new Vector2(0, 1);
            members = Ui.Label(crew, "Members", "", Ui.FontBody, Ui.Text, TextAnchor.UpperLeft); Inset(members, 24, 20, 24, 20);
            difficulty = Ui.Label(Root, "Difficulty", "", Ui.FontBody, Ui.Text, TextAnchor.UpperLeft, true);
            var drt = difficulty.rectTransform; drt.anchorMin = drt.anchorMax = new Vector2(1, 1); drt.pivot = new Vector2(1, 1); drt.anchoredPosition = new Vector2(-48, -170); drt.sizeDelta = new Vector2(400, 40);
            diffBtn = Ui.Button(Root, "DiffBtn", "", new Vector2(1, 1), new Vector2(-48, -220), new Vector2(400, 64), () => { var d = round.CurrentDifficulty; round.SetDifficulty((Difficulty)(((int)d + 1) % 3)); });
            diffBtn.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
            // codigo de sala (internet): visible para todos, con copiar (UI_SPEC 3: lobby.code, button.copy_code)
            codeLabel = Ui.Label(Root, "CodeLabel", "", Ui.FontHud + 2, Ui.Muted, TextAnchor.UpperLeft);
            var crt = codeLabel.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(1, 1); crt.anchoredPosition = new Vector2(-48, -310); crt.sizeDelta = new Vector2(400, 36);
            codeText = Ui.Label(Root, "Code", "", Ui.FontTitle, Ui.Focus, TextAnchor.UpperLeft); codeText.supportRichText = false;
            var ctr = codeText.rectTransform; ctr.anchorMin = ctr.anchorMax = new Vector2(1, 1); ctr.pivot = new Vector2(1, 1); ctr.anchoredPosition = new Vector2(-48, -346); ctr.sizeDelta = new Vector2(400, 70);
            copyBtn = Ui.Button(Root, "Copy", "", new Vector2(1, 1), new Vector2(-48, -424), new Vector2(400, 56), () =>
            {
                try { GUIUtility.systemCopyBuffer = NetSession.JoinCode; copiedUntil = Time.unscaledTime + 3f; }
                catch { NetSession.Say("error.copy_failed"); }
            });
            copyBtn.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
            reason = Ui.Label(Root, "Reason", "", Ui.FontHud + 2, Ui.Focus, TextAnchor.MiddleCenter); Inset(reason, 40, 560, 40, 150);
            readyBtn = Ui.Button(Root, "Ready", "", new Vector2(0.5f, 0f), new Vector2(-270, 50), new Vector2(500, Ui.ButtonH), () => { var pc = PlayerControls.Local; if (pc) pc.SetReady(!pc.Ready); });
            startBtn = Ui.Button(Root, "Start", "", new Vector2(0.5f, 0f), new Vector2(270, 50), new Vector2(500, Ui.ButtonH), () => round.StartFromLobby());
            leaveBtn = Ui.Button(Root, "Leave", "", new Vector2(1f, 0f), new Vector2(-48, 140), new Vector2(360, 56), NetSession.LeaveToMenu);
            readyBtn.GetComponent<RectTransform>().pivot = startBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            leaveBtn.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);
            Root.gameObject.SetActive(false);
        }

        static void Inset(Text t, float l, float top, float r, float b) { t.rectTransform.offsetMin = new Vector2(l, b); t.rectTransform.offsetMax = new Vector2(-r, -top); }

        public void Update()
        {
            bool show = round && round.InLobby;
            if (Root.gameObject.activeSelf != show) Root.gameObject.SetActive(show);
            if (!show) { wasVisible = false; return; }
            if (!wasVisible) { wasVisible = true; EventSystem.current?.SetSelectedGameObject(readyBtn.gameObject); }
            bool host = NetAuth.IsServer;
            title.text = Loc.T("lobby.crew");
            mode.text = Loc.T("lobby.variant") + "  ·  " + Loc.T("lobby.mode_hint");
            var lines = new List<string>();
            var local = PlayerControls.Local;
            foreach (var pm in PlayerMotor.All)
            {
                if (!pm) continue; var pc = pm.Controls;
                string name = Sanitize(pm.playerName);
                string role = pc && pc.IsHostPlayer ? Loc.T("lobby.host") : Loc.T("lobby.joined");
                string you = pc && pc == local ? $"  ({Loc.T("lobby.you")})" : "";
                string ready = pc && pc.Ready ? "✓ " + Loc.T("button.ready") : "· " + Loc.T("button.not_ready"); // forma y texto, no solo color
                lines.Add($"{name}{you}  —  {role}\n    {ready}");
            }
            members.text = Loc.T(lines.Count == 1 ? "lobby.players_one" : "lobby.players_other", ("count", lines.Count.ToString())) + "\n\n" + string.Join("\n", lines);
            var d = round.CurrentDifficulty;
            string dn = Loc.T(d == Difficulty.Relaxed ? "lobby.relaxed" : d == Difficulty.Hard ? "lobby.hard" : "lobby.normal");
            difficulty.text = Loc.T("lobby.difficulty");
            diffBtn.GetComponentInChildren<Text>().text = dn + (host ? "  ▸" : "");
            diffBtn.interactable = host; // invitados: solo lectura
            // accion clara: «LISTO» para confirmar; ya confirmado se marca con ✓ (pulsar de nuevo lo deshace)
            readyBtn.GetComponentInChildren<Text>().text = local && local.Ready ? "✓ " + Loc.T("button.ready") : Loc.T("button.ready");
            bool all = RoundDirector.AllReady(out string waiting);
            startBtn.interactable = host && all;
            startBtn.GetComponentInChildren<Text>().text = Loc.T(host ? "button.start" : "button.wait_host");
            reason.text = all ? (host ? "" : Loc.T("button.wait_host")) : Loc.T("lobby.waiting_player", ("player", Sanitize(waiting)));
            leaveBtn.GetComponentInChildren<Text>().text = Loc.T("button.leave_lobby");
            bool code = NetSession.Online && !string.IsNullOrEmpty(NetSession.JoinCode);
            codeLabel.gameObject.SetActive(code); codeText.gameObject.SetActive(code); copyBtn.gameObject.SetActive(code);
            if (code)
            {
                codeLabel.text = Loc.T("lobby.code") + (NetSession.RelayRegion != "" ? "  ·  " + NetSession.RelayRegion : "");
                codeText.text = NetSession.JoinCode;
                copyBtn.GetComponentInChildren<Text>().text = Time.unscaledTime < copiedUntil ? "✓ " + Loc.T("lobby.copied") : Loc.T("button.copy_code");
            }
        }

        /// Nombres como datos de presentacion: largo limitado y sin etiquetas de texto enriquecido (UI_SPEC 7).
        static string Sanitize(string n)
        {
            if (string.IsNullOrEmpty(n)) return "?";
            n = n.Replace("<", "").Replace(">", "");
            return n.Length > 20 ? n.Substring(0, 19) + "…" : n;
        }
    }
}
