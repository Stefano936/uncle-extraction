using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UncleExtraction
{
    /// Ayudantes de uGUI con los tokens del kit (design_tokens.json): referencia 1920x1080, margen 64 px, botones de 72 px,
    /// carbon/crema/verde azulado, ambar para foco y esfuerzo, coral para peligro. El color nunca es la unica senal.
    public static class Ui
    {
        public static readonly Color Ink = Hex("#10171D"), Panel = Hex("#19252D"), Primary = Hex("#205F5D"), Accent = Hex("#3CA6A0"),
            Text = Hex("#F3E7CF"), Muted = Hex("#B7C3BE"), Focus = Hex("#E9A84A"), Danger = Hex("#F07861"), Disabled = Hex("#344048");
        public const int Margin = 64, ButtonH = 72, FontButton = 27, FontBody = 27, FontTitle = 54, FontSection = 31, FontHud = 20, FontTimer = 36;
        /// Escala de texto de las opciones (100/125/150 %).
        public static float TextScale = 1f;

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        static UiSkin S => UiSkin.Load();

        public static Canvas Canvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current || UnityEngine.Object.FindAnyObjectByType<EventSystem>()) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if UE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }
        public static RectTransform Stretch(string name, Transform parent) => Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool sliced = true)
        {
            var rt = Stretch(name, parent); var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.type = sliced && sprite && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.raycastTarget = false; return img;
        }

        public static RectTransform PanelBox(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, bool danger = false)
        {
            var rt = Rect(name, parent, anchor, anchor, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>(); img.sprite = danger ? S?.panelDanger : S?.panel; img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = img.sprite ? Color.white : (danger ? new Color(0.27f, 0.16f, 0.15f, 0.92f) : new Color(0.1f, 0.15f, 0.18f, 0.92f));
            return rt;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            var rt = Stretch(name, parent); var t = rt.gameObject.AddComponent<Text>();
            t.font = (bold ? S?.fontBold : S?.font) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text; t.fontSize = Mathf.RoundToInt(size * TextScale); t.color = color; t.alignment = align; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Max(10, t.fontSize * 2 / 3); t.resizeTextMaxSize = t.fontSize; // traducciones largas: achicar un poco, no cortar
            return t;
        }

        public static Button Button(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
        {
            var rt = Rect(name, parent, anchor, anchor, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>(); img.sprite = S?.button; img.type = UnityEngine.UI.Image.Type.Sliced; img.color = img.sprite ? Color.white : Primary;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            if (S && S.buttonHover)
            {
                b.transition = Selectable.Transition.SpriteSwap;
                b.spriteState = new SpriteState { highlightedSprite = S.buttonHover, selectedSprite = S.buttonHover, pressedSprite = S.buttonPressed, disabledSprite = S.buttonDisabled };
            }
            var lab = Label(rt, "Text", text, FontButton, Text, TextAnchor.MiddleCenter, true);
            lab.rectTransform.offsetMin = new Vector2(16, 0); lab.rectTransform.offsetMax = new Vector2(-16, 0);
            // foco visible para teclado y mando (no solo hover): anillo ambar
            if (S && S.focus)
            {
                var ring = Image(rt, "Focus", S.focus, Focus); ring.rectTransform.offsetMin = new Vector2(-6, -6); ring.rectTransform.offsetMax = new Vector2(6, 6);
                rt.gameObject.AddComponent<FocusRing>().ring = ring;
            }
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        public static Image Bar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, bool amber, out Image track)
        {
            var rt = Rect(name, parent, anchor, anchor, anchor, pos, size);
            track = rt.gameObject.AddComponent<Image>(); track.sprite = S?.barTrack; track.type = UnityEngine.UI.Image.Type.Sliced; track.color = track.sprite ? Color.white : Disabled;
            var fill = Image(rt, "Fill", amber ? S?.barAmber : S?.barTeal, track.sprite ? Color.white : (amber ? Focus : Accent));
            fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillAmount = 1f;
            fill.rectTransform.offsetMin = new Vector2(4, 4); fill.rectTransform.offsetMax = new Vector2(-4, -4);
            return fill;
        }

        public static Image Icon(Transform parent, string name, string icon, Vector2 anchor, Vector2 pos, float size, Color color)
        {
            var rt = Rect(name, parent, anchor, anchor, anchor, pos, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>(); img.sprite = S?.Icon(icon); img.color = color; img.preserveAspect = true; img.raycastTarget = false;
            img.enabled = img.sprite; return img;
        }
    }

    /// Anillo de foco visible solo cuando el boton esta seleccionado (teclado/mando).
    public class FocusRing : MonoBehaviour
    {
        public Image ring;
        void Update() { if (ring) ring.enabled = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject; }
    }
}
