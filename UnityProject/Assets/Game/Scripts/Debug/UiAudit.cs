using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UncleExtraction
{
    /// Auditoria automatica de la interfaz en la build (solo con -ueUiAudit carpeta; sin el argumento no hace nada).
    /// Recorre el menu (todas las paginas y el dialogo de error), la sala, el HUD, la pausa, opciones y resultados;
    /// en cada pantalla captura una imagen y revisa cada texto visible:
    ///  - fuera de pantalla: parte del texto dibujado queda fuera de la ventana;
    ///  - recortado: el texto no entra en su caja ni con el tamaño minimo de ajuste;
    ///  - encimado: los glifos de dos textos visibles se pisan;
    ///  - tapado: un boton o panel del mismo panel, dibujado encima, cubre parte del texto.
    /// Registra "[UiAudit] pantalla tipo ..." y un resumen. No juzga legibilidad ni estetica: eso queda para una persona.
    /// Usar con -screen-width/-screen-height y -ueTextScale 1.5 para las variantes.
    public class UiAudit : MonoBehaviour
    {
        string dir, tag;
        int total;
        readonly List<string> summary = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == "-ueUiAudit")
                {
                    var go = new GameObject("UiAudit"); DontDestroyOnLoad(go);
                    var u = go.AddComponent<UiAudit>(); u.dir = a[i + 1];
                    return;
                }
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Seconds(float s) { float t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < s) yield return null; }

        IEnumerator Start()
        {
            GameSettings.Hud = HudProfile.Contextual;
            Directory.CreateDirectory(dir);
            tag = $"{Screen.width}x{Screen.height}_t{Mathf.RoundToInt(Ui.TextScale * 100)}";
            dir = Path.GetFullPath(dir);
            Debug.Log($"[UiAudit] inicio {tag} pantalla={Screen.width}x{Screen.height} aspecto={(float)Screen.width / Screen.height:0.000} texto={Ui.TextScale:0.##} idioma={Loc.Language}");
            yield return Seconds(1.5f);
            var menu = FindAnyObjectByType<FrontendMenu>();
            if (menu)
            {
                foreach (FrontendMenu.Page p in Enum.GetValues(typeof(FrontendMenu.Page)))
                {
                    menu.Show(p); yield return Frames(4);
                    yield return Check("menu_" + p);
                }
                menu.Show(FrontendMenu.Page.Friends); yield return Frames(2);
                NetSession.Say("error.host_left", 30f); yield return Frames(4);
                yield return Check("menu_error");
                NetSession.StatusUntil = 0f; menu.Show(FrontendMenu.Page.Main); yield return Frames(2);
            }
            // sala: anfitrion solo en loopback (puerto aparte para no chocar con otras pruebas)
            NetSession.Mode = NetMode.Host; NetSession.Online = false; NetSession.ListenAddress = "127.0.0.1"; NetSession.Port = 7807; // fuera del rango del modo solo (7790-7799)
            SceneManager.LoadScene(FrontendMenu.GameScene);
            while (RoundDirector.Instance == null || PlayerControls.Local == null) yield return null;
            yield return Seconds(2f);
            yield return Check("lobby");
            PlayerControls.Local.SetReady(true); yield return Frames(3);
            RoundDirector.Instance.StartFromLobby();
            yield return Seconds(3f);
            yield return Check("hud");
            // sesion 14: guia de primeros pasos, marcador de un compañero y cambio de tamaño de texto en plena partida
            var fs = FirstSteps.Instance; if (fs) { fs.ShowForTool(); yield return Frames(3); yield return Check("hud_guide"); }
            var cm = CoopMarkers.Instance; var me = PlayerControls.Local;
            if (cm && me && NpcBrain.Instance) { cm.Show(me.OwnerClientId, me.GetComponent<PlayerMotor>().playerName, MarkKind.Uncle, NpcBrain.Instance.transform.position); yield return Frames(3); yield return Check("hud_mark"); }
            float ts0 = Ui.TextScale; Ui.TextScale = ts0 >= 1.4f ? 1f : 1.5f; GameSettings.TextScale = Ui.TextScale; yield return Frames(4);
            yield return Check("hud_textsize_live"); Ui.TextScale = ts0; GameSettings.TextScale = ts0; yield return Frames(4);
            if (fs) fs.HideForTool();
            // aviso de ataque (preparacion de la patada): titulo, consejo y subtitulo no se pisan
            if (NpcBrain.Instance) { NpcBrain.Instance.ForceAttack(AttackId.Kick); yield return Seconds(0.25f); yield return Check("hud_attack"); yield return Seconds(2.5f); }
            // La captura de marcadores ya se hizo. Dejar que caduquen antes de acercar la camara para el A/B.
            while (cm && cm.ActiveCount > 0) yield return null;
            if (NpcBrain.Instance && me)
            {
                var npc = NpcBrain.Instance;
                npc.brainEnabled = false;
                npc.ReleaseAnchor("ui audit");
                var motor = me.GetComponent<PlayerMotor>();
                var body = npc.GetComponent<NpcBody>();
                var socket = GrabSocket.ById("belt");
                if (body && socket)
                {
                    Vector3 at = socket.Position + body.Forward * 0.9f;
                    at.y = 0f;
                    motor.Body.position = motor.transform.position = at;
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 to = socket.Position - motor.EyePosition;
                        motor.SetYawPitch(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg);
                        yield return new WaitForFixedUpdate();
                    }
                    yield return Check("hud_contextual_aim");
                    GameSettings.Hud = HudProfile.Detailed;
                    yield return Frames(4);
                    yield return Check("hud_detailed_aim");
                    GameSettings.Hud = HudProfile.Contextual;
                }
            }
            var flow = GameFlowUI.Instance;
            flow.SetPaused(true); yield return Frames(4);
            yield return Check("pause");
            var settings = GameObject.Find("Settings")?.GetComponent<Button>();
            if (settings) { settings.onClick.Invoke(); yield return Frames(4); yield return Check("pause_options"); }
            flow.SetPaused(false); yield return Frames(2);
            var opt = GameObject.Find("Options"); if (opt) Destroy(opt);
            RoundDirector.TimeLimitOverride = RoundDirector.Instance.Elapsed + 1f;
            float t0 = Time.realtimeSinceStartup;
            while (RoundDirector.Instance.State == RoundState.Running && Time.realtimeSinceStartup - t0 < 10f) yield return null;
            yield return Seconds(1.5f);
            yield return Check("results");
            Debug.Log($"[UiAudit] resumen {tag} problemas={total} :: {string.Join(" | ", summary)}");
            yield return Seconds(0.5f);
            Application.Quit();
        }

        IEnumerator Check(string screen)
        {
            yield return Seconds(UiFade.Duration + 0.2f);
            yield return new WaitForEndOfFrame();
            string file = Path.Combine(dir, $"{tag}_{screen}.png");
            ScreenCapture.CaptureScreenshot(file);
            int n = Audit(screen);
            total += n; summary.Add($"{screen}={n}");
            Debug.Log($"[UiAudit] pantalla {screen} problemas={n} captura={file}");
        }

        struct Item { public Text t; public Rect glyphs; public string path; }

        int Audit(string screen)
        {
            int issues = 0;
            var screenRect = new Rect(0, 0, Screen.width, Screen.height);
            var items = new List<Item>();
            foreach (var t in FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text) || t.color.a < 0.05f) continue;
                var canvas = t.canvas; if (!canvas || !canvas.isActiveAndEnabled) continue;
                if (HiddenByGroup(t.transform)) continue;
                if (t.GetComponentInParent<Mask>() is Mask m && m && !m.isActiveAndEnabled) continue;
                string path = NodePath(t.transform);
                var g = GlyphRect(t);
                if (g.width <= 0f) continue;
                items.Add(new Item { t = t, glyphs = g, path = path });
                // 1) fuera de pantalla
                if (g.xMin < -1f || g.yMin < -1f || g.xMax > Screen.width + 1f || g.yMax > Screen.height + 1f)
                { issues++; Debug.Log($"[UiAudit] {screen} FUERA '{path}' «{Short(t.text)}» glifos={Fmt(g)} pantalla={Screen.width}x{Screen.height}"); }
                // 2) recortado: no entra ni al tamaño minimo
                if (Clipped(t, out string why))
                { issues++; Debug.Log($"[UiAudit] {screen} RECORTADO '{path}' «{Short(t.text)}» {why}"); }
            }
            // 3) tapados: un boton o panel del MISMO panel, dibujado despues, cubre parte del texto (p. ej. un boton sobre un aviso)
            var covers = new List<Image>();
            foreach (var im in FindObjectsByType<Image>(FindObjectsSortMode.None))
            {
                if (!im.isActiveAndEnabled || im.color.a < 0.5f || HiddenByGroup(im.transform) || !im.canvas || !im.canvas.isActiveAndEnabled) continue;
                string nm = im.gameObject.name;
                if (nm == "Focus" || nm == "Fill" || nm == "Icon" || nm == "Background" || nm == "Shade" || nm == "Handle") continue;
                bool control = im.GetComponent<Selectable>();
                var r = im.rectTransform.rect;
                if (!control && (r.width < 200f || r.height < 40f)) continue;
                covers.Add(im);
            }
            foreach (var it in items)
                foreach (var im in covers)
                {
                    if (it.t.transform.IsChildOf(im.transform)) continue;               // su propio boton o panel
                    if (TopPanel(it.t.transform) != TopPanel(im.transform)) continue;  // otra pantalla encima: no es defecto de esta
                    if (!Later(im.transform, it.t.transform)) continue;                 // dibujado antes: queda debajo del texto
                    var inter = Intersect(it.glyphs, ScreenRect(im.rectTransform));
                    if (inter.width <= 2f || inter.height <= 2f) continue;
                    float frac = inter.width * inter.height / Area(it.glyphs);
                    if (frac < 0.05f) continue;
                    issues++;
                    Debug.Log($"[UiAudit] {screen} TAPADO '{it.path}' «{Short(it.t.text)}» por '{NodePath(im.transform)}' ({frac:P0})");
                }
            // 4) paneles que se pisan entre si dentro de la misma pantalla (p. ej. aviso de ataque sobre el objetivo).
            //    Los paneles modales (sala, pausa, resultados, opciones, dialogos) cubren a proposito lo de atras.
            var modal = new HashSet<string> { "Lobby", "Pause", "Results", "Options", "Error", "Connecting", "ConfirmLeave", "Unsaved", "Page" };
            var panels = new List<RectTransform>();
            foreach (var f in FindObjectsByType<UiFade>(FindObjectsSortMode.None))
            {
                var rt = (RectTransform)f.transform;
                if (!f.isActiveAndEnabled || modal.Contains(rt.name) || HiddenByGroup(rt.parent)) continue;
                if (TopPanel(rt) != rt) continue; // solo paneles de primer nivel
                panels.Add(rt);
            }
            for (int i = 0; i < panels.Count; i++)
                for (int j = i + 1; j < panels.Count; j++)
                {
                    var a = panels[i]; var b = panels[j];
                    if (a.GetComponentInParent<Canvas>().rootCanvas != b.GetComponentInParent<Canvas>().rootCanvas) continue;
                    var inter = Intersect(ScreenRect(a), ScreenRect(b));
                    if (inter.width <= 3f || inter.height <= 3f) continue;
                    issues++;
                    Debug.Log($"[UiAudit] {screen} PANELES '{NodePath(a)}' y '{NodePath(b)}' se pisan ({inter.width:0}x{inter.height:0} px)");
                }
            // 5) encimados: glifos de dos textos que se pisan (descontando el que este tapado por un panel opaco de otra capa)
            for (int i = 0; i < items.Count; i++)
                for (int j = i + 1; j < items.Count; j++)
                {
                    var a = items[i]; var b = items[j];
                    if (a.t.transform.IsChildOf(b.t.transform) || b.t.transform.IsChildOf(a.t.transform)) continue;
                    var inter = Intersect(a.glyphs, b.glyphs);
                    if (inter.width <= 2f || inter.height <= 2f) continue;
                    float frac = inter.width * inter.height / Mathf.Min(Area(a.glyphs), Area(b.glyphs));
                    if (frac < 0.15f) continue;
                    bool samePanel = TopPanel(a.t.transform) == TopPanel(b.t.transform);
                    if (!samePanel && (Occluded(a.t, b.t) || Occluded(b.t, a.t))) continue;
                    issues++;
                    Debug.Log($"[UiAudit] {screen} ENCIMADO '{a.path}' «{Short(a.t.text)}» con '{b.path}' «{Short(b.t.text)}» ({frac:P0})");
                }
            return issues;
        }

        /// Panel de primer nivel (hijo directo del canvas raiz o de su raiz de contenido) al que pertenece un elemento.
        static Transform TopPanel(Transform t)
        {
            var root = t.GetComponentInParent<Canvas>()?.rootCanvas.transform;
            var p = t; while (p.parent && p.parent != root && !(p.parent.parent == root && p.parent.name == "Root")) p = p.parent;
            return p;
        }

        static bool HiddenByGroup(Transform tr)
        {
            for (var p = tr; p; p = p.parent) { var cg = p.GetComponent<CanvasGroup>(); if (cg && cg.alpha < 0.05f) return true; }
            return false;
        }

        /// Un texto esta tapado si otro canvas de mayor orden, o un panel posterior en la jerarquia, lo cubre con una imagen opaca.
        static bool Occluded(Text under, Text over)
        {
            if (!over.canvas || !under.canvas) return false;
            var overPanel = over.GetComponentsInParent<Image>().FirstOrDefault(im => im.color.a > 0.9f && im.rectTransform.rect.width > 300f);
            if (!overPanel) return false;
            if (under.transform.IsChildOf(overPanel.transform)) return false;
            bool overOnTop = over.canvas.rootCanvas.sortingOrder > under.canvas.rootCanvas.sortingOrder
                || (over.canvas.rootCanvas == under.canvas.rootCanvas && Later(overPanel.transform, under.transform));
            return overOnTop && ScreenRect(overPanel.rectTransform).Overlaps(GlyphRect(under));
        }

        static bool Later(Transform a, Transform b)
        {   // a se dibuja despues que b en el mismo canvas
            var pa = Chain(a); var pb = Chain(b);
            int k = 0; while (k < pa.Count && k < pb.Count && pa[k] == pb[k]) k++;
            if (k >= pa.Count || k >= pb.Count) return false;
            return pa[k].GetSiblingIndex() > pb[k].GetSiblingIndex();
        }
        static List<Transform> Chain(Transform t) { var l = new List<Transform>(); for (var p = t; p; p = p.parent) l.Insert(0, p); return l; }

        static bool Clipped(Text t, out string why)
        {
            why = "";
            var r = t.rectTransform.rect;
            if (r.width < 1f || r.height < 1f) return false;
            var settings = t.GetGenerationSettings(r.size);
            if (t.resizeTextForBestFit) { settings.resizeTextForBestFit = false; settings.fontSize = t.resizeTextMinSize; }
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var gen = t.cachedTextGeneratorForLayout;
            float ppu = t.pixelsPerUnit;
            float h = gen.GetPreferredHeight(t.text, settings) / ppu;
            if (t.verticalOverflow == VerticalWrapMode.Truncate && h > r.height + 2f) { why = $"alto {h:0} > caja {r.height:0}"; return true; }
            if (t.horizontalOverflow == HorizontalWrapMode.Wrap)
            {   // una palabra sola mas ancha que la caja tambien se corta
                settings.horizontalOverflow = HorizontalWrapMode.Overflow;
                foreach (var w in t.text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    float ww = gen.GetPreferredWidth(w, settings) / ppu;
                    if (ww > r.width + 2f) { why = $"palabra «{Short(w)}» ancho {ww:0} > caja {r.width:0}"; return true; }
                }
            }
            return false;
        }

        /// Rectangulo de pantalla que ocupan los glifos realmente dibujados.
        static Rect GlyphRect(Text t)
        {
            var gen = t.cachedTextGenerator;
            if (gen == null || gen.vertexCount == 0) return Rect.zero;
            var verts = gen.verts; float ppu = t.pixelsPerUnit;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            var cam = t.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : t.canvas.worldCamera;
            int count = Mathf.Min(verts.Count, gen.characterCountVisible * 4);
            for (int i = 0; i < count; i++)
            {
                var w = t.rectTransform.TransformPoint(verts[i].position / ppu);
                Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, w);
                min = Vector2.Min(min, s); max = Vector2.Max(max, s);
            }
            return min.x > max.x ? Rect.zero : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static Rect ScreenRect(RectTransform rt)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            var canvas = rt.GetComponentInParent<Canvas>();
            var cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, c[0]), b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static Rect Intersect(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
        static float Area(Rect r) => Mathf.Max(1f, r.width * r.height);
        static string Fmt(Rect r) => $"({r.xMin:0},{r.yMin:0})-({r.xMax:0},{r.yMax:0})";
        static string Short(string s) { s = s.Replace("\n", " / "); return s.Length > 40 ? s.Substring(0, 40) + "…" : s; }
        static string NodePath(Transform t) { var parts = new List<string>(); for (var p = t; p && parts.Count < 4; p = p.parent) parts.Insert(0, p.name); return string.Join("/", parts); }
    }
}
