using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Importa el kit de UI (preproduction/uncle_extraction_kit_UI, INTEGRAR_UI.md) de forma reproducible:
    /// - sprites de assets/sprites como Sprite (2D and UI) con los bordes 9-slice de data/sprite_import.json;
    /// - iconos PNG de 64 px como Sprite;
    /// - fuentes DejaVu (TTF sin modificar + FONT_LICENSE.txt), usadas como Font dinamica de uGUI;
    /// - catalogos de idioma a Resources/Localization (ui_strings.json = localization_en_es.json sin cambios).
    /// Las fuentes originales quedan en preproduction/. No se inventan .meta: Unity los genera al importar.
    public static class UiKitImporter
    {
        const string Kit = "../preproduction/uncle_extraction_kit_UI";
        public const string UiDir = "Assets/Game/Art/UI";
        public const string LocDir = "Assets/Game/Resources/Localization";
        // Fuentes: los TTF DejaVu se usan como Font dinamica con UnityEngine.UI.Text (glifos con acentos bajo demanda).
        // No se crea un TMP_FontAsset: el proyecto no tiene TMP Essentials (TMP_Settings) y su importacion es interactiva.
        public const string FontPath = UiDir + "/Fonts/DejaVuSans.ttf";
        public const string FontBoldPath = UiDir + "/Fonts/DejaVuSans-Bold.ttf";

        [System.Serializable] class SpriteRecipe { public string id, file; public int[] border_lbrt_px; public int ppu; }
        [System.Serializable] class SpriteList { public List<SpriteRecipe> sprites; }

        [MenuItem("Uncle Extraction/Importar kit de UI")]
        public static void Import()
        {
            foreach (var d in new[] { UiDir + "/Sprites", UiDir + "/Icons", UiDir + "/Fonts", LocDir }) Directory.CreateDirectory(d);
            int n = 0;
            var recipes = JsonUtility.FromJson<SpriteList>(File.ReadAllText(Path.Combine(Kit, "data/sprite_import.json"))).sprites;
            foreach (var r in recipes)
            {
                string dst = $"{UiDir}/Sprites/{Path.GetFileName(r.file)}";
                CopyIfChanged(Path.Combine(Kit, r.file), dst);
                var b = r.border_lbrt_px ?? new int[4];
                ConfigureSprite(dst, new Vector4(b[0], b[1], b[2], b[3]), r.ppu > 0 ? r.ppu : 100); n++;
            }
            foreach (var f in Directory.GetFiles(Path.Combine(Kit, "assets/icons_png"), "*.png"))
            {
                string dst = $"{UiDir}/Icons/{Path.GetFileName(f)}";
                CopyIfChanged(f, dst); ConfigureSprite(dst, Vector4.zero, 100); n++;
            }
            foreach (var f in new[] { "DejaVuSans.ttf", "DejaVuSans-Bold.ttf", "FONT_LICENSE.txt" })
                CopyIfChanged(Path.Combine(Kit, "assets/fonts", f), $"{UiDir}/Fonts/{f}");
            // sesion 20: los catalogos de idioma son del JUEGO una vez copiados (se les agregan textos y lineas: version, campana).
            // Antes cada regeneracion de escenas los pisaba con la fuente del kit y borraba esos cambios. Solo se copian si faltan.
            CopyIfMissing(Path.Combine(Kit, "data/localization_en_es.json"), $"{LocDir}/ui_strings.json");
            CopyIfMissing("../preproduction/uncle_extraction/data/dialogue_en_es.csv", $"{LocDir}/dialogue_en_es.csv"); // catalogo de dialogos (subtitulos)
            Directory.CreateDirectory(UiDir + "/Backgrounds");
            CopyIfChanged(Path.Combine(Kit, "assets/backgrounds/bar_idle.png"), $"{UiDir}/Backgrounds/bar_idle.png");
            ConfigureSprite($"{UiDir}/Backgrounds/bar_idle.png", Vector4.zero, 100);
            AssetDatabase.Refresh();
            BuildSkin();
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiKit] {n} sprites e iconos importados; fuentes DejaVu y catalogo EN/es_UY en {LocDir}");
        }

        public const string SkinPath = "Assets/Game/Resources/UI/UiSkin.asset";
        /// UiSkin en Resources (las pantallas se arman en tiempo de ejecucion con estos sprites y fuentes).
        static void BuildSkin()
        {
            Directory.CreateDirectory("Assets/Game/Resources/UI");
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            if (!skin) { skin = ScriptableObject.CreateInstance<UiSkin>(); AssetDatabase.CreateAsset(skin, SkinPath); }
            Sprite Sp(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiDir}/Sprites/{file}.png");
            skin.panel = Sp("panel_dark"); skin.panelDanger = Sp("panel_danger"); skin.button = Sp("button_primary"); skin.buttonHover = Sp("button_hover");
            skin.buttonPressed = Sp("button_pressed"); skin.buttonDisabled = Sp("button_disabled"); skin.focus = Sp("focus_ring"); skin.keycap = Sp("keycap");
            skin.barTrack = Sp("bar_track"); skin.barAmber = Sp("bar_fill_amber"); skin.barTeal = Sp("bar_fill_teal");
            // fondo: captura del bar actual (CamTour «menu_fondo», sesion 11) si existe; si no, el render original del kit
            string night = $"{UiDir}/Backgrounds/bar_night.png";
            if (File.Exists(night)) ConfigureSprite(night, Vector4.zero, 100);
            skin.background = AssetDatabase.LoadAssetAtPath<Sprite>(File.Exists(night) ? night : $"{UiDir}/Backgrounds/bar_idle.png");
            skin.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath); skin.fontBold = AssetDatabase.LoadAssetAtPath<Font>(FontBoldPath);
            skin.icons = Directory.GetFiles(UiDir + "/Icons", "*.png").Select(f => AssetDatabase.LoadAssetAtPath<Sprite>(f.Replace('\\', '/'))).Where(x => x).ToList();
            EditorUtility.SetDirty(skin);
            Debug.Log($"[UiKit] UiSkin: {skin.icons.Count} iconos, fuente {(skin.font ? skin.font.name : "FALTA")}, panel {(skin.panel ? "ok" : "FALTA")}");
        }

        static void CopyIfMissing(string src, string dst) { if (!File.Exists(dst)) CopyIfChanged(src, dst); }

        static void CopyIfChanged(string src, string dst)
        {
            if (!File.Exists(src)) { Debug.LogWarning($"[UiKit] falta {src}"); return; }
            if (File.Exists(dst) && new FileInfo(dst).Length == new FileInfo(src).Length && File.ReadAllBytes(dst).SequenceEqual(File.ReadAllBytes(src))) return;
            File.Copy(src, dst, true); AssetDatabase.ImportAsset(dst);
        }

        static void ConfigureSprite(string path, Vector4 border, int ppu)
        {
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path); if (!ti) return;
            bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteBorder != border || ti.mipmapEnabled || !ti.alphaIsTransparency
                         || ti.spritePixelsPerUnit != ppu || ti.textureCompression != TextureImporterCompression.Uncompressed;
            if (!dirty) return;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.spriteBorder = border; ti.spritePixelsPerUnit = ppu; ti.mipmapEnabled = false; ti.alphaIsTransparency = true;
            ti.sRGBTexture = true; ti.filterMode = FilterMode.Bilinear; ti.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); ti.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }

    }
}
