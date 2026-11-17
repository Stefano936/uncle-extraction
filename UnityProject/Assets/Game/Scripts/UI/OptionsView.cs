using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UncleExtraction
{
    /// Opciones con el kit de UI (UI_SPEC 6), compartidas por el menu y la pausa. Borrador local: los cambios se ven al
    /// editar (el volumen se previsualiza), APLICAR los guarda y VOLVER con cambios ofrece aplicar o descartar (descartar
    /// restaura lo guardado). Idioma y accesibilidad son locales: no cambian a otros jugadores. Remapeo: elegir accion,
    /// escuchar la tecla, avisar el conflicto (se intercambian) o cancelar con Esc.
    public class OptionsView
    {
        public RectTransform Root { get; private set; }
        readonly Action onClose;
        struct Draft { public float mouse, fov, volume, textScale; public bool invertY, subtitles, profanity, lowQuality, grabToggle, reduceMotion; public string lang, name; }
        Draft saved, draft;
        readonly List<Action> refresh = new List<Action>();
        Text status; RectTransform confirm; IDisposable listening;

        public bool Dirty => !draft.Equals(saved) || InputBindings.DiffersFrom(bindSnap);
        System.Collections.Generic.Dictionary<string, string> bindSnap; // asignaciones al abrir o al ultimo Aplicar (Descartar vuelve aca)

        public OptionsView(Transform parent, Action onClose)
        {
            this.onClose = onClose;
            saved = Capture(); draft = saved; bindSnap = InputBindings.Snapshot();
            float g = Ui.K(20) - 20f; // aire extra bajo cada encabezado con texto grande
            Root = Ui.PanelBox("Options", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 1040 + 2 * g));
            Ui.FitInCanvas(Root);
            var title = Ui.Label(Root, "Title", "", Ui.FontTitle, Ui.Text, TextAnchor.UpperCenter, true); Inset(title, 0, 24, 0, 0);
            refresh.Add(() => title.text = Loc.T("button.settings"));
            float y = -120;
            // columna izquierda: audio, idioma, camara y accesibilidad
            Section(Root, "settings.audio_language", new Vector2(48, y));
            Slider(Root, "settings.master", new Vector2(48, y - 50 - g), 0f, 1f, () => draft.volume, v => { draft.volume = v; AudioListener.volume = v; }, v => $"{v * 100f:0}%");
            Choice(Root, "settings.language", new Vector2(48, y - 120 - g), new[] { "settings.spanish", "settings.english" }, () => draft.lang == Loc.EsUY ? 0 : 1, i => { draft.lang = i == 0 ? Loc.EsUY : Loc.En; Loc.SetLanguage(draft.lang); });
            Section(Root, "settings.comfort", new Vector2(48, y - 210 - g));
            Slider(Root, "settings.mouse", new Vector2(48, y - 260 - 2 * g), 0.03f, 0.4f, () => draft.mouse, v => draft.mouse = v, v => $"{v:0.00}");
            Slider(Root, "settings.fov", new Vector2(48, y - 330 - 2 * g), 65f, 100f, () => draft.fov, v => draft.fov = v, v => $"{v:0}");
            Toggle(Root, "settings.invert_y", new Vector2(48, y - 400 - 2 * g), () => draft.invertY, v => draft.invertY = v);
            Toggle(Root, "settings.attack_subtitles", new Vector2(48, y - 460 - 2 * g), () => draft.subtitles, v => draft.subtitles = v);
            Toggle(Root, "settings.profanity", new Vector2(48, y - 580 - 2 * g), () => draft.profanity, v => draft.profanity = v);
            Toggle(Root, "settings.reduce_motion", new Vector2(48, y - 640 - 2 * g), () => draft.reduceMotion, v => draft.reduceMotion = v);
            NameField(Root, new Vector2(48, y - 710 - 2 * g));
            Choice(Root, "settings.text_size", new Vector2(48, y - 520 - 2 * g), new[] { "100%", "125%", "150%" }, () => draft.textScale >= 1.4f ? 2 : draft.textScale >= 1.2f ? 1 : 0, i => draft.textScale = i == 2 ? 1.5f : i == 1 ? 1.25f : 1f, "percent");
            // columna derecha: controles
            Section(Root, "settings.controls", new Vector2(620, y));
            Choice(Root, "settings.grab_mode", new Vector2(620, y - 50 - g), new[] { "settings.grab_hold", "settings.grab_toggle" }, () => draft.grabToggle ? 1 : 0, i => draft.grabToggle = i == 1);
            float ry = y - 110 - g;
            foreach (var id in InputBindings.RemappableIds) { Rebind(Root, id, new Vector2(620, ry)); ry -= 46; }
            var reset = Ui.Button(Root, "ResetControls", "", new Vector2(0f, 1f), new Vector2(620 + 250, ry - 10), new Vector2(500, 56), () => { InputBindings.ResetAll(); Refresh(); }); // borrador: se guarda con Aplicar
            reset.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            refresh.Add(() => reset.GetComponentInChildren<Text>().text = Loc.T("settings.reset_controls"));
            // calidad grafica (columna derecha, debajo de los controles)
            Choice(Root, "settings.quality", new Vector2(620, ry - 76), new[] { "settings.quality_high", "settings.quality_low" }, () => draft.lowQuality ? 1 : 0, i => draft.lowQuality = i == 1);
            status = Ui.Label(Root, "Status", "", Ui.FontHud, Ui.Focus, TextAnchor.LowerCenter); Inset(status, 40, 0, 40, 120);
            // botones
            var apply = Ui.Button(Root, "Apply", "", new Vector2(0.5f, 0f), new Vector2(-260, 40), new Vector2(480, Ui.ButtonH), Apply);
            var back = Ui.Button(Root, "Back", "", new Vector2(0.5f, 0f), new Vector2(260, 40), new Vector2(480, Ui.ButtonH), Back);
            apply.GetComponent<RectTransform>().pivot = back.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            refresh.Add(() => { apply.GetComponentInChildren<Text>().text = Loc.T("button.apply"); back.GetComponentInChildren<Text>().text = Loc.T("button.back"); });
            Loc.Changed += Refresh;
            Refresh();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(apply.gameObject);
        }

        static Draft Capture() => new Draft { mouse = GameSettings.MouseSensitivity, fov = GameSettings.FieldOfView, volume = GameSettings.MasterVolume, invertY = GameSettings.InvertY, subtitles = GameHud.AttackSubtitles, profanity = GameSettings.ProfanityFilter, name = GameSettings.PlayerName ?? "", lowQuality = GameSettings.LowQuality, grabToggle = GameSettings.GrabToggle, reduceMotion = GameSettings.ReduceMotion, textScale = Ui.TextScale, lang = Loc.Language };
        static void Store(Draft d)
        {
            GameSettings.MouseSensitivity = d.mouse; GameSettings.FieldOfView = d.fov; GameSettings.MasterVolume = d.volume; GameSettings.InvertY = d.invertY;
            GameHud.AttackSubtitles = d.subtitles; GameSettings.AttackSubtitles = d.subtitles; GameSettings.ProfanityFilter = d.profanity; UncleDialogue.ProfanityFilter = d.profanity; GameSettings.PlayerName = PlayerMotor.Sanitize(d.name); GameSettings.TextScale = d.textScale; Ui.TextScale = d.textScale; GameSettings.LowQuality = d.lowQuality; GraphicsQuality.Low = d.lowQuality; GraphicsQuality.Apply(); GameSettings.GrabToggle = d.grabToggle; GameSettings.ReduceMotion = d.reduceMotion; GrabIntent.Reset();
            if (Loc.Language != d.lang) Loc.SetLanguage(d.lang);
        }

        void Refresh() { if (!Root) { Loc.Changed -= Refresh; return; } // panel destruido sin Close (cambio de escena)
            foreach (var r in refresh) r(); if (status && status.text == "" && Dirty) status.text = Loc.T("settings.pending"); else if (status && !Dirty && status.text == Loc.T("settings.pending")) status.text = ""; }
        static void Inset(Text t, float l, float top, float r, float b) { t.rectTransform.offsetMin = new Vector2(l, b); t.rectTransform.offsetMax = new Vector2(-r, -top); }

        void Section(Transform p, string key, Vector2 pos)
        {
            var rt = Ui.Rect(key, p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(520, 40));
            var t = Ui.Label(rt, "T", "", Ui.FontSection, Ui.Focus, TextAnchor.MiddleLeft, true); refresh.Add(() => t.text = Loc.T(key));
        }

        void Slider(Transform p, string key, Vector2 pos, float min, float max, Func<float> get, Action<float> set, Func<float, string> fmt)
        {
            var rt = Ui.Rect(key, p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(520, 60));
            // la etiqueta ocupa la franja de arriba y la barra la de abajo: con texto al 150 % no se pisan
            var t = Ui.Label(rt, "T", "", Ui.FontHud + 2, Ui.Text, TextAnchor.UpperLeft); t.rectTransform.offsetMin = new Vector2(0, 20);
            var srt = Ui.Rect("Slider", rt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(0, 16));
            var bg = srt.gameObject.AddComponent<Image>(); bg.sprite = UiSkin.Load()?.barTrack; bg.type = Image.Type.Sliced; bg.color = bg.sprite ? Color.white : Ui.Disabled;
            var fillArea = Ui.Stretch("Fill Area", srt); var fill = Ui.Image(fillArea, "Fill", UiSkin.Load()?.barTeal, UiSkin.Load() ? Color.white : Ui.Accent);
            var handleArea = Ui.Stretch("Handle Area", srt); var handle = Ui.Rect("Handle", handleArea, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 8));
            var himg = handle.gameObject.AddComponent<Image>(); himg.color = Ui.Text;
            var s = srt.gameObject.AddComponent<Slider>(); s.fillRect = fill.rectTransform; s.handleRect = handle; s.targetGraphic = himg; s.minValue = min; s.maxValue = max;
            s.SetValueWithoutNotify(get());
            s.onValueChanged.AddListener(v => { set(v); t.text = $"{Loc.T(key)}: {fmt(v)}"; });
            refresh.Add(() => { s.SetValueWithoutNotify(get()); t.text = $"{Loc.T(key)}: {fmt(get())}"; });
        }

        void Toggle(Transform p, string key, Vector2 pos, Func<bool> get, Action<bool> set)
        {
            var b = Ui.Button(p, key, "", new Vector2(0, 1), pos, new Vector2(520, 52), null);
            b.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            var t = b.GetComponentInChildren<Text>(); t.fontSize = Ui.FontHud + 2;
            b.onClick.AddListener(() => { set(!get()); Refresh(); });
            refresh.Add(() => t.text = $"{Loc.T(key)}: {Loc.T(get() ? "settings.on" : "settings.off")}");
        }

        void Choice(Transform p, string key, Vector2 pos, string[] options, Func<int> get, Action<int> set, string token = "")
        {
            var b = Ui.Button(p, key, "", new Vector2(0, 1), pos, new Vector2(520, 52), null);
            b.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            var t = b.GetComponentInChildren<Text>(); t.fontSize = Ui.FontHud + 2;
            b.onClick.AddListener(() => { set((get() + 1) % options.Length); Refresh(); });
            refresh.Add(() =>
            {
                string opt = Loc.Has(options[get()]) ? Loc.T(options[get()]) : options[get()];
                t.text = token != "" ? Loc.T(key, (token, opt.TrimEnd('%'))) : $"{Loc.T(key)}: {opt}";
            });
        }

        void NameField(Transform p, Vector2 pos)
        {
            var rt = Ui.Rect("Name", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(520, 56));
            var lab = Ui.Label(rt, "Label", "", Ui.FontHud + 2, Ui.Text, TextAnchor.MiddleLeft); lab.rectTransform.offsetMax = new Vector2(-300, 0);
            var box = Ui.Rect("Box", rt, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(290, 0));
            var img = box.gameObject.AddComponent<Image>(); img.sprite = UiSkin.Load()?.keycap; img.type = Image.Type.Sliced; img.color = img.sprite ? Color.white : Ui.Panel;
            var txt = Ui.Label(box, "Text", "", Ui.FontHud + 2, Ui.Text, TextAnchor.MiddleCenter); txt.supportRichText = false;
            var f = box.gameObject.AddComponent<InputField>(); f.textComponent = txt; f.targetGraphic = img; f.characterLimit = 20;
            f.SetTextWithoutNotify(draft.name); f.onValueChanged.AddListener(v => draft.name = v);
            refresh.Add(() => { lab.text = Loc.T("settings.name"); if (!f.isFocused) f.SetTextWithoutNotify(draft.name); });
        }

        void Rebind(Transform p, string id, Vector2 pos)
        {
            var b = Ui.Button(p, "Bind_" + id, "", new Vector2(0, 1), pos, new Vector2(500, 42), null);
            b.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            var t = b.GetComponentInChildren<Text>(); t.fontSize = Ui.FontHud + 1;
            b.onClick.AddListener(() =>
            {
                listening?.Dispose();
                status.text = Loc.T("settings.rebind", ("action", Loc.T("action." + id)));
                listening = InputBindings.Listen(id, (path, swapped) =>
                {
                    listening = null;
                    status.text = path == "reserved" ? Loc.T("settings.reserved") : path == "" ? "" : swapped != null ? Loc.T("settings.conflict", ("key", InputBindings.Display(id)), ("action", Loc.T("action." + swapped))) : "";
                    Refresh();
                });
            });
            refresh.Add(() => t.text = $"{Loc.T("action." + id)}:  [{InputBindings.Display(id)}]");
        }

        public void ApplyChanges() => Apply();
        void Apply()
        {
            Store(draft); GameSettings.Save(); InputBindings.SaveAll(); bindSnap = InputBindings.Snapshot(); saved = Capture(); draft = saved;
            status.text = ""; Refresh();
        }

        void Back()
        {
            if (!Dirty) { Close(); return; }
            if (confirm) return;
            // borrador sin aplicar: aplicar o descartar
            confirm = Ui.PanelBox("Unsaved", Root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 260), true);
            var q = Ui.Label(confirm, "Q", Loc.T("settings.unsaved"), Ui.FontBody, Ui.Text, TextAnchor.UpperCenter, true); Inset(q, 20, 30, 20, 0);
            var ap = Ui.Button(confirm, "Apply", Loc.T("button.apply"), new Vector2(0.5f, 0f), new Vector2(-180, 30), new Vector2(330, 64), () => { Apply(); Close(); });
            Ui.Button(confirm, "Discard", Loc.T("settings.discard"), new Vector2(0.5f, 0f), new Vector2(180, 30), new Vector2(330, 64), () => { Store(saved); GameSettings.Apply(); draft = saved; InputBindings.Restore(bindSnap); Close(); });
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(ap.gameObject);
        }

        public void Close()
        {
            listening?.Dispose(); listening = null;
            if (InputBindings.DiffersFrom(bindSnap)) InputBindings.Restore(bindSnap); // salir sin aplicar no deja asignaciones a medias
            Loc.Changed -= Refresh;
            if (Root) UnityEngine.Object.Destroy(Root.gameObject);
            onClose?.Invoke();
        }
    }
}
