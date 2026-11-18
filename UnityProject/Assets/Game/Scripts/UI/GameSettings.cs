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
        /// Categorias (sesion 13): efectos del mundo (agarres, golpes, puertas) y avisos (señales de ataque, marcadores, fin de
        /// ronda). Voz y musica no existen todavia (sin fuentes con licencia). Se multiplican por el volumen general.
        public static float EffectsVolume = 1f, AlertsVolume = 1f;
        /// Desafio elegido para la proxima partida (no se guarda: cada sesion empieza en la ronda estandar).
        public static ChallengeMode Challenge = ChallengeMode.Standard;
        public static Difficulty SoloDifficulty = Difficulty.Normal;
        public static bool AttackSubtitles = true;
        public static bool ProfanityFilter;
        public static string PlayerName = "";
        /// Calidad grafica baja (sin eleccion guardada: se detecta segun la GPU; ver GraphicsQuality).
        public static bool LowQuality;
        /// Agarre por alternancia (una pulsacion agarra, otra suelta). Local y persistente.
        public static bool GrabToggle;
        /// Reduce la inclinacion de camara al caer (comodidad).
        public static bool ReduceMotion;
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
                EffectsVolume = PlayerPrefs.GetFloat("ue.vol.sfx", EffectsVolume); AlertsVolume = PlayerPrefs.GetFloat("ue.vol.alerts", AlertsVolume);
                int dif = PlayerPrefs.GetInt("ue.difficulty", (int)Difficulty.Normal);
                SoloDifficulty = System.Enum.IsDefined(typeof(Difficulty), dif) ? (Difficulty)dif : Difficulty.Normal; // fuera del enum: Normal
                AttackSubtitles = PlayerPrefs.GetInt("ue.attackSubs", 1) == 1;
                ProfanityFilter = PlayerPrefs.GetInt("ue.profanity", 0) == 1;
                PlayerName = PlayerPrefs.GetString("ue.name", "");
                GrabToggle = PlayerPrefs.GetInt("ue.grabToggle", 0) == 1;
                ReduceMotion = PlayerPrefs.GetInt("ue.reduceMotion", 0) == 1;
                LowQuality = PlayerPrefs.HasKey("ue.quality") ? PlayerPrefs.GetInt("ue.quality") == 0 : GraphicsQuality.DetectLow();
                TextScale = Mathf.Clamp(PlayerPrefs.GetFloat("ue.textScale", 1f), 1f, 1.5f);
            }
            catch { /* preferencias corruptas: valores por defecto */ }
            Validate();
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

        /// Rangos que usa el juego (los mismos de Opciones); numeros no finitos o fuera de rango vuelven a un valor seguro.
        public static void Validate()
        {
            MouseSensitivity = Safe(MouseSensitivity, 0.03f, 0.4f, 0.12f);
            FieldOfView = Safe(FieldOfView, 65f, 100f, 85f);           // campo de vision VERTICAL en grados (convencion de Unity)
            MasterVolume = Safe(MasterVolume, 0f, 1f, 0.8f);
            EffectsVolume = Safe(EffectsVolume, 0f, 1f, 1f); AlertsVolume = Safe(AlertsVolume, 0f, 1f, 1f);
            float ts = Safe(TextScale, 1f, 1.5f, 1f);                    // solo los tamanos ofrecidos: 100, 125 y 150 %
            TextScale = ts >= 1.4f ? 1.5f : ts >= 1.2f ? 1.25f : 1f;
            if (!System.Enum.IsDefined(typeof(Difficulty), SoloDifficulty)) SoloDifficulty = Difficulty.Normal;
            PlayerName = PlayerMotor.Sanitize(PlayerName ?? "");
        }
        static float Safe(float v, float lo, float hi, float def) => float.IsNaN(v) || float.IsInfinity(v) ? def : Mathf.Clamp(v, lo, hi);

        public static void Save()
        {
            PlayerPrefs.SetFloat("ue.mouse", MouseSensitivity);
            PlayerPrefs.SetInt("ue.invertY", InvertY ? 1 : 0);
            PlayerPrefs.SetFloat("ue.fov", FieldOfView);
            PlayerPrefs.SetFloat("ue.volume", MasterVolume);
            PlayerPrefs.SetFloat("ue.vol.sfx", EffectsVolume); PlayerPrefs.SetFloat("ue.vol.alerts", AlertsVolume);
            PlayerPrefs.SetInt("ue.difficulty", (int)SoloDifficulty);
            PlayerPrefs.SetInt("ue.attackSubs", AttackSubtitles ? 1 : 0);
            PlayerPrefs.SetInt("ue.profanity", ProfanityFilter ? 1 : 0);
            PlayerPrefs.SetString("ue.name", PlayerName ?? "");
            PlayerPrefs.SetInt("ue.grabToggle", GrabToggle ? 1 : 0);
            PlayerPrefs.SetInt("ue.reduceMotion", ReduceMotion ? 1 : 0);
            PlayerPrefs.SetInt("ue.quality", LowQuality ? 0 : 1);
            PlayerPrefs.SetFloat("ue.textScale", TextScale);
            PlayerPrefs.Save();
            Apply();
        }

        public static void Apply() { AudioListener.volume = Mathf.Clamp01(MasterVolume); GameHud.AttackSubtitles = AttackSubtitles; Ui.TextScale = TextScale; UncleDialogue.ProfanityFilter = ProfanityFilter; GraphicsQuality.Low = LowQuality; GraphicsQuality.Apply(); }
    }
}
