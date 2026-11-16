using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Recursos del kit de UI para construir pantallas en tiempo de ejecucion (Resources/UI/UiSkin, lo crea UiKitImporter).
    [CreateAssetMenu(menuName = "Uncle Extraction/UI Skin")]
    public class UiSkin : ScriptableObject
    {
        public Sprite background;
        public Sprite panel, panelDanger, button, buttonHover, buttonPressed, buttonDisabled, focus, keycap, barTrack, barAmber, barTeal;
        public Font font, fontBold;
        public List<Sprite> icons = new List<Sprite>();
        public Sprite Icon(string name) { foreach (var s in icons) if (s && s.name == name) return s; return null; }

        static UiSkin cached;
        public static UiSkin Load() { if (!cached) cached = Resources.Load<UiSkin>("UI/UiSkin"); return cached; }
    }

}
