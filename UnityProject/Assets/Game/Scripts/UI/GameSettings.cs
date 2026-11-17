using UnityEngine;

namespace UncleExtraction
{
    /// Opciones locales del jugador (especificacion 02: ProfileStore, instancia local). Se guardan en PlayerPrefs.
    public static class GameSettings
    {
        public static float MouseSensitivity = 0.12f;
        public static bool InvertY;
        public static float FieldOfView = 85f;
        public static float MasterVolume = 0.8f;
        public static Difficulty SoloDifficulty = Difficulty.Normal;
        public static bool AttackSubtitles = true;
        public static bool ProfanityFilter;
        public static string PlayerName = "";
        /// Calidad grafica baja (sin eleccion guardada: se detecta segun la GPU; ver GraphicsQuality).
        public static bool LowQuality;
        public static float TextScale = 1f;
        static bool loaded;

        public static void Load()
        {
            if (loaded) return; loaded = true;
            try
            {
                MouseSensitivity = PlayerPrefs.GetFloat("ue.mouse", MouseSensitivity);
                InvertY = PlayerPrefs.GetInt("ue.invertY", 0) == 1;
                FieldOfView = PlayerPrefs.GetFloat("ue.fov", FieldOfView);
                MasterVolume = PlayerPrefs.GetFloat("ue.volume", MasterVolume);
                SoloDifficulty = (Difficulty)PlayerPrefs.GetInt("ue.difficulty", (int)SoloDifficulty);
                AttackSubtitles = PlayerPrefs.GetInt("ue.attackSubs", 1) == 1;
                ProfanityFilter = PlayerPrefs.GetInt("ue.profanity", 0) == 1;
                PlayerName = PlayerPrefs.GetString("ue.name", "");
                LowQuality = PlayerPrefs.HasKey("ue.quality") ? PlayerPrefs.GetInt("ue.quality") == 0 : GraphicsQuality.DetectLow();
                TextScale = Mathf.Clamp(PlayerPrefs.GetFloat("ue.textScale", 1f), 1f, 1.5f);
            }
            catch { /* preferencias corruptas: valores por defecto */ }
            // pruebas de interfaz: -ueTextScale 1.5 fuerza el tamaño de texto sin guardarlo
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] == "-ueQuality") LowQuality = a[i + 1] == "low";
                if (a[i] == "-ueTextScale" && float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ts)) TextScale = Mathf.Clamp(ts, 1f, 1.5f);
            }
            // pruebas en el editor: UE_QUALITY=low|high fuerza la calidad sin guardarla
            var env = System.Environment.GetEnvironmentVariable("UE_QUALITY"); if (env == "low" || env == "high") LowQuality = env == "low";
            Apply();
        }

        /// Olvida lo cargado y vuelve a leer lo guardado (equivale a reiniciar el juego; pruebas de persistencia).
        public static void ReloadFromDisk() { loaded = false; Load(); }

        public static void Save()
        {
            PlayerPrefs.SetFloat("ue.mouse", MouseSensitivity);
            PlayerPrefs.SetInt("ue.invertY", InvertY ? 1 : 0);
            PlayerPrefs.SetFloat("ue.fov", FieldOfView);
            PlayerPrefs.SetFloat("ue.volume", MasterVolume);
            PlayerPrefs.SetInt("ue.difficulty", (int)SoloDifficulty);
            PlayerPrefs.SetInt("ue.attackSubs", AttackSubtitles ? 1 : 0);
            PlayerPrefs.SetInt("ue.profanity", ProfanityFilter ? 1 : 0);
            PlayerPrefs.SetString("ue.name", PlayerName ?? "");
            PlayerPrefs.SetInt("ue.quality", LowQuality ? 0 : 1);
            PlayerPrefs.SetFloat("ue.textScale", TextScale);
            PlayerPrefs.Save();
            Apply();
        }

        public static void Apply() { AudioListener.volume = Mathf.Clamp01(MasterVolume); GameHud.AttackSubtitles = AttackSubtitles; Ui.TextScale = TextScale; UncleDialogue.ProfanityFilter = ProfanityFilter; GraphicsQuality.Low = LowQuality; GraphicsQuality.Apply(); }
    }
}
