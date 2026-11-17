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
    ///  - encimado: los glifos de dos textos visibles se pisan.
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
            Directory.CreateDirectory(dir);
            tag = $"{Screen.width}x{Screen.height}_t{Ui.TextScale:0.0#}";
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
            NetSession.Mode = NetMode.Host; NetSession.Online = false; NetSession.ListenAddress = "127.0.0.1"; NetSession.Port = 7795;
            SceneManager.LoadScene(FrontendMenu.GameScene);
            while (RoundDirector.Instance == null || PlayerControls.Local == null) yield return null;
            yield return Seconds(2f);
            yield return Check("lobby");
            PlayerControls.Local.SetReady(true); yield return Frames(3);
            RoundDirector.Instance.StartFromLobby();
            yield return Seconds(3f);
            yield return Check("hud");
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
            // 3) encimados: glifos de dos textos que se pisan (descontando el que este tapado por un panel opaco de otra capa)
            for (int i = 0; i < items.Count; i++)
                for (int j = i + 1; j < items.Count; j++)
                {
                    var a = items[i]; var b = items[j];
                    if (a.t.transform.IsChildOf(b.t.transform) || b.t.transform.IsChildOf(a.t.transform)) continue;
                    var inter = Intersect(a.glyphs, b.glyphs);
                    if (inter.width <= 2f || inter.height <= 2f) continue;
                    float frac = inter.width * inter.height / Mathf.Min(Area(a.glyphs), Area(b.glyphs));
                    if (frac < 0.15f) continue;
                    if (Occluded(a.t, b.t) || Occluded(b.t, a.t)) continue;
                    issues++;
                    Debug.Log($"[UiAudit] {screen} ENCIMADO '{a.path}' «{Short(a.t.text)}» con '{b.path}' «{Short(b.t.text)}» ({frac:P0})");
                }
            return issues;
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
