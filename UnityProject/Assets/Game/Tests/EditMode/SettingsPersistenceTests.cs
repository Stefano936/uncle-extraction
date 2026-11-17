using NUnit.Framework;
using UnityEngine;

namespace UncleExtraction.Tests
{
    /// Opciones, idioma y controles se conservan al reiniciar: se guardan, se olvida todo lo cargado (como un proceso nuevo)
    /// y se vuelve a leer. Restaura las preferencias del editor al terminar.
    public class SettingsPersistenceTests
    {
        static readonly string[] Keys = { "ue.mouse", "ue.invertY", "ue.fov", "ue.volume", "ue.difficulty", "ue.attackSubs", "ue.profanity", "ue.name", "ue.textScale", "ue.quality", "ue.lang", "ue.bind.Interact", "ue.bind.GrabLeft", "ue.bind.Dodge", "ue.bind.MoveForward", "ue.bind.MoveLeft", "ue.bind.ReleaseAll", "ue.grabToggle", "ue.reduceMotion" };
        readonly System.Collections.Generic.List<System.Action> restore = new System.Collections.Generic.List<System.Action>();

        [SetUp]
        public void Backup()
        {   // guarda cada preferencia con su tipo para dejar el editor como estaba
            restore.Clear();
            foreach (var k in Keys)
            {
                string key = k;
                if (!PlayerPrefs.HasKey(key)) continue;
                string str = PlayerPrefs.GetString(key, "");
                if (str != "") { restore.Add(() => PlayerPrefs.SetString(key, str)); continue; }
                float f = PlayerPrefs.GetFloat(key, float.NaN);
                if (!float.IsNaN(f)) { restore.Add(() => PlayerPrefs.SetFloat(key, f)); continue; }
                int i = PlayerPrefs.GetInt(key, 0); restore.Add(() => PlayerPrefs.SetInt(key, i));
            }
        }

        [TearDown]
        public void Restore()
        {
            foreach (var k in Keys) PlayerPrefs.DeleteKey(k);
            foreach (var r in restore) r();
            PlayerPrefs.Save();
            GameSettings.ReloadFromDisk(); InputBindings.ReloadFromDisk(); Loc.Reload();
        }

        [Test]
        public void Options_Language_And_Bindings_SurviveRestart()
        {
            GameSettings.Load();
            GameSettings.MouseSensitivity = 0.31f; GameSettings.InvertY = true; GameSettings.FieldOfView = 95f; GameSettings.MasterVolume = 0.35f;
            GameSettings.SoloDifficulty = Difficulty.Hard; GameSettings.AttackSubtitles = false; GameSettings.ProfanityFilter = true;
            GameSettings.PlayerName = "Prueba"; GameSettings.TextScale = 1.5f;
            bool detected = GraphicsQuality.DetectLow(); GameSettings.LowQuality = !detected; // eleccion manual opuesta a la deteccion
            GameSettings.Save();
            Loc.SetLanguage("en");
            InputBindings.Set(Act.Interact, "<Keyboard>/f");
            InputBindings.Set(Act.Dodge, "<Keyboard>/c");
            InputBindings.Set(InputBindings.Id(MoveKey.Forward), "<Keyboard>/i");
            InputBindings.SaveAll(); // las asignaciones son borrador hasta Aplicar

            // «reinicio»: se pisan los valores en memoria y se vuelve a leer lo guardado
            GameSettings.MouseSensitivity = 0f; GameSettings.InvertY = false; GameSettings.FieldOfView = 0f; GameSettings.MasterVolume = 1f;
            GameSettings.SoloDifficulty = Difficulty.Relaxed; GameSettings.AttackSubtitles = true; GameSettings.ProfanityFilter = false;
            GameSettings.PlayerName = ""; GameSettings.TextScale = 1f;
            GameSettings.ReloadFromDisk(); InputBindings.ReloadFromDisk(); Loc.Reload();

            Assert.AreEqual(0.31f, GameSettings.MouseSensitivity, 1e-4f);
            Assert.IsTrue(GameSettings.InvertY);
            Assert.AreEqual(95f, GameSettings.FieldOfView, 1e-3f);
            Assert.AreEqual(0.35f, GameSettings.MasterVolume, 1e-4f);
            Assert.AreEqual(Difficulty.Hard, GameSettings.SoloDifficulty);
            Assert.IsFalse(GameSettings.AttackSubtitles);
            Assert.IsTrue(GameSettings.ProfanityFilter);
            Assert.AreEqual("Prueba", GameSettings.PlayerName);
            Assert.AreEqual(1.5f, GameSettings.TextScale, 1e-4f);
            Assert.AreEqual(!detected, GameSettings.LowQuality, "la calidad elegida a mano manda sobre la deteccion de GPU");
            Assert.AreEqual(1.5f, Ui.TextScale, 1e-4f, "el tamaño de texto se aplica al cargar");
            Assert.AreEqual("en", Loc.Language);
            Assert.AreEqual("<Keyboard>/f", InputBindings.Path(Act.Interact));
            Assert.AreEqual("<Keyboard>/c", InputBindings.Path(Act.Dodge));
            Assert.AreEqual("<Keyboard>/i", InputBindings.Path(MoveKey.Forward), "el movimiento tambien se reasigna y se conserva");
            Assert.AreEqual("<Mouse>/leftButton", InputBindings.Path(Act.GrabLeft), "lo no cambiado conserva el valor por defecto");
        }

        [Test]
        public void CorruptOrOutOfRangeValues_FallBackSafely()
        {
            PlayerPrefs.SetFloat("ue.textScale", 9f); PlayerPrefs.SetString("ue.lang", "klingon"); PlayerPrefs.SetString("ue.bind.Interact", "");
            PlayerPrefs.Save();
            GameSettings.ReloadFromDisk(); InputBindings.ReloadFromDisk(); Loc.Reload();
            Assert.AreEqual(1.5f, GameSettings.TextScale, 1e-4f, "tamaño de texto acotado");
            Assert.That(Loc.Language == "en" || Loc.Language == "es_UY");
            Assert.AreEqual("<Keyboard>/e", InputBindings.Path(Act.Interact), "asignacion vacia vuelve a la de fabrica");
        }

        [Test]
        public void DamagedPreferences_AreValidatedBeforeUse()
        {
            PlayerPrefs.SetInt("ue.difficulty", 99); PlayerPrefs.SetFloat("ue.mouse", float.NaN); PlayerPrefs.SetFloat("ue.fov", 500f);
            PlayerPrefs.SetFloat("ue.volume", -3f); PlayerPrefs.SetFloat("ue.textScale", 1.3f); PlayerPrefs.SetString("ue.name", "<b>Tio</b> con un nombre larguisimo de mas");
            PlayerPrefs.SetString("ue.bind.ReleaseAll", "<Keyboard>/escape"); PlayerPrefs.SetString("ue.bind.MoveLeft", "basura sin formato");
            PlayerPrefs.Save();
            GameSettings.ReloadFromDisk(); InputBindings.ReloadFromDisk();
            Assert.AreEqual(Difficulty.Normal, GameSettings.SoloDifficulty, "dificultad fuera del enum");
            Assert.AreEqual(0.12f, GameSettings.MouseSensitivity, 1e-4f, "sensibilidad no finita");
            Assert.AreEqual(100f, GameSettings.FieldOfView, 1e-3f, "FOV acotado");
            Assert.AreEqual(0f, GameSettings.MasterVolume, 1e-4f, "volumen acotado");
            Assert.AreEqual(1.25f, GameSettings.TextScale, 1e-4f, "tamaño de texto a un valor ofrecido");
            Assert.That(GameSettings.PlayerName.Length <= 20 && !GameSettings.PlayerName.Contains("<"), "nombre saneado");
            Assert.AreEqual("<Keyboard>/q", InputBindings.Path(Act.ReleaseAll), "tecla reservada no se acepta");
            Assert.AreEqual("<Keyboard>/a", InputBindings.Path(MoveKey.Left), "ruta sin formato vuelve a la de fabrica");
        }
    }
}
