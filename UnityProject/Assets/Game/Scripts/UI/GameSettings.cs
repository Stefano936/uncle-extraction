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
                TextScale = Mathf.Clamp(PlayerPrefs.GetFloat("ue.textScale", 1f), 1f, 1.5f);
            }
            catch { /* preferencias corruptas: valores por defecto */ }
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("ue.mouse", MouseSensitivity);
            PlayerPrefs.SetInt("ue.invertY", InvertY ? 1 : 0);
            PlayerPrefs.SetFloat("ue.fov", FieldOfView);
            PlayerPrefs.SetFloat("ue.volume", MasterVolume);
            PlayerPrefs.SetInt("ue.difficulty", (int)SoloDifficulty);
            PlayerPrefs.SetInt("ue.attackSubs", AttackSubtitles ? 1 : 0);
            PlayerPrefs.SetFloat("ue.textScale", TextScale);
            PlayerPrefs.Save();
            Apply();
        }

        public static void Apply() { AudioListener.volume = Mathf.Clamp01(MasterVolume); GameHud.AttackSubtitles = AttackSubtitles; Ui.TextScale = TextScale; }
    }
}
