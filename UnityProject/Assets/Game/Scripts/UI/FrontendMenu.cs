using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UncleExtraction
{
    /// Menu principal con el kit de UI (UI_SPEC 3): jugar solo (dificultad y escenario), jugar con amigos (LAN directa por
    /// IP mientras Relay espera el vinculo de Unity Cloud), como jugar, opciones y salir. Con argumentos de prueba
    /// (-ueHost, -ueClient, -ueAutopilot) entra directo a la escena pedida.
    public class FrontendMenu : MonoBehaviour
    {
        public const string GameScene = "Bar_LastRound";   // escenario principal
        public const string LabScene = "PhysicsLab";       // laboratorio de diagnostico
        public const string LabArticulatedScene = "PhysicsLab_Articulated"; // laboratorio con el tio articulado (ragdoll reducido)
        public static string SelectedScene = GameScene;
        static bool argsForwarded;
        public enum Page { Main, Solo, Friends, HowTo, Options }
        public Page Current { get; private set; } = Page.Main;
        Canvas canvas; RectTransform pageRoot; Text status; OptionsView options;
        InputField ipField, portField;

        void Start()
        {
            GameSettings.Load();
            Debug.Log("[Menu] en el menu");
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            NetAuth.Frozen = false;
            var a = Environment.GetCommandLineArgs();
            if (!argsForwarded)
            {   // argumentos de prueba: entrar directo una sola vez por proceso (al volver al menu, quedarse en el menu)
                argsForwarded = true;
                string scene = GameScene;
                for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-ueScene") scene = a[i + 1];
                foreach (var s in a) if (s == "-ueHost" || s == "-ueClient" || s == "-ueAutopilot") { SceneManager.LoadScene(scene); return; }
            }
            canvas = Ui.Canvas("Menu", 0);
            var skin = UiSkin.Load();
            var bg = Ui.Image(canvas.transform, "Background", skin ? skin.background : null, skin && skin.background ? Color.white : Ui.Ink, false);
            bg.preserveAspect = false;
            var shade = Ui.Image(canvas.transform, "Shade", null, new Color(0.06f, 0.09f, 0.11f, 0.55f), false);
            var title = Ui.Label(canvas.transform, "Title", "UNCLE EXTRACTION", 112, Ui.Text, TextAnchor.UpperLeft, true);
            title.rectTransform.offsetMin = new Vector2(Ui.Margin, 0); title.rectTransform.offsetMax = new Vector2(-Ui.Margin, -Ui.Margin);
            var tag = Ui.Label(canvas.transform, "Tagline", "", Ui.FontSection, Ui.Focus, TextAnchor.UpperLeft);
            tag.rectTransform.offsetMin = new Vector2(Ui.Margin + 6, 0); tag.rectTransform.offsetMax = new Vector2(-Ui.Margin, -Ui.Margin - 140);
            tag.gameObject.AddComponent<LocText>().key = "lobby.tagline";
            status = Ui.Label(canvas.transform, "Status", "", Ui.FontHud + 4, Ui.Danger, TextAnchor.LowerCenter);
            status.rectTransform.offsetMin = new Vector2(Ui.Margin, Ui.Margin); status.rectTransform.offsetMax = new Vector2(-Ui.Margin, 0);
            Show(Page.Main);
        }

        void Update()
        {
            if (status) status.text = Time.unscaledTime < NetSession.StatusUntil && NetSession.StatusMessage != "" ? Loc.Coded(NetSession.StatusMessage) : "";
            if (LabInput.Pressed(Act.Pause) && Current != Page.Main && options == null && !InputBindings.Listening) Show(Page.Main);
        }

        public void Show(Page p)
        {
            Current = p;
            if (pageRoot) Destroy(pageRoot.gameObject);
            options?.Close(); options = null;
            pageRoot = Ui.Rect("Page", canvas.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(Ui.Margin, -70), new Vector2(620, -480));
            GameObject first = null;
            float y = 0;
            Button B(string key, Action a, float h = Ui.ButtonH)
            {
                var b = Ui.Button(pageRoot, key, "", new Vector2(0, 1), new Vector2(0, y), new Vector2(560, h), a);
                b.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                if (Loc.Has(key)) b.gameObject.AddComponent<LocText>().key = key;
                if (!first) first = b.gameObject;
                y -= h + 16; return b;
            }
            Text Note(string text, float h = 70)
            {
                var rt = Ui.Rect("Note", pageRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y), new Vector2(560, h));
                var t = Ui.Label(rt, "T", text, Ui.FontHud + 2, Ui.Muted, TextAnchor.UpperLeft); y -= h + 8; return t;
            }
            switch (p)
            {
                case Page.Main:
                    B("button.play_solo", () => Show(Page.Solo));
                    B("button.play_friends", () => Show(Page.Friends));
                    B("button.how_to", () => Show(Page.HowTo));
                    B("button.settings", () => Show(Page.Options));
                    B("button.quit", Application.Quit);
                    break;
                case Page.Solo:
                case Page.Friends:
                    var diff = B("lobby.difficulty", null, 60);
                    diff.onClick.AddListener(() => { GameSettings.SoloDifficulty = (Difficulty)(((int)GameSettings.SoloDifficulty + 1) % 3); GameSettings.Save(); Relabel(diff); });
                    Relabel(diff);
                    if (p == Page.Solo)
                    {
                        B("button.start", () => { SelectedScene = GameScene; Play(NetMode.Solo); });
                        B("menu.lab", () => { SelectedScene = LabScene; Play(NetMode.Solo); }, 56);
                        B("menu.lab_articulated", () => { SelectedScene = LabArticulatedScene; Play(NetMode.Solo); }, 56);
                    }
                    else
                    {
                        Note(Loc.T("menu.online_pending"), 64);
                        portField = Field("Port", Loc.T("menu.port"), NetSession.Port.ToString(), ref y);
                        B("menu.host_lan", () => { SelectedScene = GameScene; Play(NetMode.Host); });
                        ipField = Field("Ip", Loc.T("menu.ip"), NetSession.Address, ref y);
                        B("menu.join_lan", () => Play(NetMode.Client));
                    }
                    B("button.back", () => Show(Page.Main));
                    break;
                case Page.HowTo:
                    foreach (var (t, d) in new[] { ("tutorial.grab", "tutorial.grab_detail"), ("tutorial.watch", "tutorial.watch_detail"), ("tutorial.move", "tutorial.move_detail") })
                    {
                        var card = Ui.PanelBox(t, pageRoot, new Vector2(0, 1), new Vector2(0, y), new Vector2(900, 120)); card.pivot = new Vector2(0, 1);
                        var tt = Ui.Label(card, "T", Loc.T(t), Ui.FontSection, Ui.Focus, TextAnchor.UpperLeft, true); tt.rectTransform.offsetMin = new Vector2(24, 0); tt.rectTransform.offsetMax = new Vector2(-24, -12);
                        var dd = Ui.Label(card, "D", Loc.T(d), Ui.FontHud + 2, Ui.Text, TextAnchor.LowerLeft); dd.rectTransform.offsetMin = new Vector2(24, 14); dd.rectTransform.offsetMax = new Vector2(-24, 0);
                        y -= 136;
                    }
                    Note($"{InputBindings.Display(Act.GrabLeft)} / {InputBindings.Display(Act.GrabRight)}: {Loc.T("hud.hold_grab")}  ·  {InputBindings.Display(Act.ReleaseAll)}: {Loc.T("action.ReleaseAll")}  ·  {InputBindings.Display(Act.Effort)}: {Loc.T("hud.effort")}  ·  {InputBindings.Display(Act.Interact)}: {Loc.T("action.Interact")}", 80);
                    B("button.back", () => Show(Page.Main));
                    break;
                case Page.Options:
                    options = new OptionsView(canvas.transform, () => { options = null; Show(Page.Main); });
                    break;
            }
            if (first) EventSystem.current?.SetSelectedGameObject(first);
        }

        void Relabel(Button diff)
        {
            var d = GameSettings.SoloDifficulty;
            diff.GetComponentInChildren<Text>().text = $"{Loc.T("lobby.difficulty")}: {Loc.T(d == Difficulty.Relaxed ? "lobby.relaxed" : d == Difficulty.Hard ? "lobby.hard" : "lobby.normal")}";
        }

        InputField Field(string name, string label, string value, ref float y)
        {
            var rt = Ui.Rect(name, pageRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y), new Vector2(560, 56));
            var lab = Ui.Label(rt, "Label", label, Ui.FontHud + 2, Ui.Muted, TextAnchor.MiddleLeft); lab.rectTransform.offsetMax = new Vector2(-330, 0);
            var box = Ui.Rect("Box", rt, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(320, 0));
            var img = box.gameObject.AddComponent<Image>(); img.sprite = UiSkin.Load()?.keycap; img.type = Image.Type.Sliced; img.color = img.sprite ? Color.white : Ui.Panel;
            var txt = Ui.Label(box, "Text", "", Ui.FontBody, Ui.Text, TextAnchor.MiddleCenter); txt.supportRichText = false; txt.raycastTarget = false;
            var f = box.gameObject.AddComponent<InputField>(); f.textComponent = txt; f.targetGraphic = img; f.text = value; f.characterLimit = 40;
            y -= 72; return f;
        }

        void Play(NetMode mode)
        {
            NetSession.Mode = mode;
            if (mode == NetMode.Client && ipField) NetSession.Address = ipField.text.Trim();
            if (portField && ushort.TryParse(portField.text, out var p)) NetSession.Port = p;
            RoundDirector.RequestedDifficulty = GameSettings.SoloDifficulty;
            GameSettings.Save();
            SceneManager.LoadScene(SelectedScene);
        }

        void OnDestroy() { options?.Close(); if (canvas) Destroy(canvas.gameObject); }
    }
}
