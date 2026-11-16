using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UncleExtraction
{
    /// Menu principal (GDD: inicio; jugar solo o con amigos; dificultad; opciones; salir).
    /// Provisional con IMGUI: el kit de UI (preproduction/uncle_extraction_kit_UI) se integra en la etapa de presentacion.
    /// Con argumentos de linea de comandos de prueba (-ueHost, -ueClient, -ueAutopilot) entra directo al laboratorio.
    public class FrontendMenu : MonoBehaviour
    {
        public const string GameScene = "PhysicsLab";
        static bool argsForwarded;
        enum Page { Main, Solo, Friends, Options }
        Page page = Page.Main;
        string ip = "127.0.0.1", port = "7777";
        GUIStyle title, big, label, small, box;

        void Start()
        {
            GameSettings.Load();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            NetAuth.Frozen = false;
            var a = Environment.GetCommandLineArgs();
            if (!argsForwarded)
            {   // argumentos de prueba: entrar directo una sola vez por proceso (al volver al menu, quedarse en el menu)
                argsForwarded = true;
                foreach (var s in a) if (s == "-ueHost" || s == "-ueClient" || s == "-ueAutopilot") { SceneManager.LoadScene(GameScene); return; }
            }
            port = NetSession.Port.ToString();
        }

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 54, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big = new GUIStyle(GUI.skin.button) { fontSize = 24 };
            label = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
            small = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            box = new GUIStyle(GUI.skin.box);
        }

        void Play(NetMode mode)
        {
            NetSession.Mode = mode;
            if (mode == NetMode.Client) NetSession.Address = ip.Trim();
            if (ushort.TryParse(port, out var p)) NetSession.Port = p;
            RoundDirector.RequestedDifficulty = GameSettings.SoloDifficulty;
            GameSettings.Save();
            SceneManager.LoadScene(GameScene);
        }

        void OnGUI()
        {
            Styles();
            float W = Screen.width, H = Screen.height, w = Mathf.Min(520, W - 40), x = (W - w) / 2f;
            GUI.Label(new Rect(0, H * 0.08f, W, 70), "UNCLE EXTRACTION", title);
            GUI.Label(new Rect(0, H * 0.08f + 64, W, 30), "Sacalo del bar. El no quiere.", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 18 });
            if (Time.unscaledTime < NetSession.StatusUntil && NetSession.StatusMessage != "")
                GUI.Box(new Rect(x, H * 0.08f + 100, w, 34), NetSession.StatusMessage);
            GUILayout.BeginArea(new Rect(x, H * 0.3f, w, H * 0.65f));
            switch (page)
            {
                case Page.Main:
                    if (GUILayout.Button("Jugar solo", big, GUILayout.Height(52))) page = Page.Solo;
                    if (GUILayout.Button("Jugar con amigos", big, GUILayout.Height(52))) page = Page.Friends;
                    if (GUILayout.Button("Opciones", big, GUILayout.Height(52))) page = Page.Options;
                    if (GUILayout.Button("Salir", big, GUILayout.Height(52))) Application.Quit();
                    GUILayout.Space(12);
                    GUILayout.Label("Prototipo jugable (PhysicsLab). Arte, sonido y escenario definitivos pendientes.", small);
                    break;
                case Page.Solo:
                    DifficultyPicker();
                    if (GUILayout.Button("Empezar", big, GUILayout.Height(52))) Play(NetMode.Solo);
                    if (GUILayout.Button("Volver", big, GUILayout.Height(44))) page = Page.Main;
                    break;
                case Page.Friends:
                    GUILayout.Label("Conexion directa (LAN o misma PC). Internet con codigo: proximamente.", small);
                    DifficultyPicker();
                    GUILayout.BeginHorizontal(); GUILayout.Label("Puerto", label, GUILayout.Width(110)); port = GUILayout.TextField(port, 6, GUILayout.Height(30)); GUILayout.EndHorizontal();
                    if (GUILayout.Button("Crear partida (anfitrion)", big, GUILayout.Height(50))) Play(NetMode.Host);
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal(); GUILayout.Label("IP del anfitrion", label, GUILayout.Width(170)); ip = GUILayout.TextField(ip, 40, GUILayout.Height(30)); GUILayout.EndHorizontal();
                    if (GUILayout.Button("Unirse", big, GUILayout.Height(50))) Play(NetMode.Client);
                    if (GUILayout.Button("Volver", big, GUILayout.Height(44))) page = Page.Main;
                    break;
                case Page.Options:
                    OptionsPanel(label);
                    if (GUILayout.Button("Guardar y volver", big, GUILayout.Height(48))) { GameSettings.Save(); page = Page.Main; }
                    break;
            }
            GUILayout.EndArea();
        }

        void DifficultyPicker()
        {
            GUILayout.Label("Dificultad", label);
            GUILayout.BeginHorizontal();
            string[] names = { "Relajada", "Normal", "Dificil" };
            for (int i = 0; i < 3; i++)
                if (GUILayout.Toggle((int)GameSettings.SoloDifficulty == i, names[i], big, GUILayout.Height(40))) GameSettings.SoloDifficulty = (Difficulty)i;
            GUILayout.EndHorizontal();
            GUILayout.Label(GameSettings.SoloDifficulty == Difficulty.Relaxed ? "Sin reloj, golpes mas suaves y avisos mas largos." :
                            GameSettings.SoloDifficulty == Difficulty.Hard ? "Menos tiempo, mas resistencia y golpes mas fuertes." : "La experiencia prevista.", small);
        }

        /// Panel de opciones compartido con la pausa.
        public static void OptionsPanel(GUIStyle label)
        {
            GUILayout.Label($"Sensibilidad del mouse: {GameSettings.MouseSensitivity:0.00}", label);
            GameSettings.MouseSensitivity = GUILayout.HorizontalSlider(GameSettings.MouseSensitivity, 0.03f, 0.4f);
            GameSettings.InvertY = GUILayout.Toggle(GameSettings.InvertY, " Invertir eje vertical");
            GUILayout.Label($"Campo de vision: {GameSettings.FieldOfView:0}", label);
            GameSettings.FieldOfView = GUILayout.HorizontalSlider(GameSettings.FieldOfView, 65f, 100f);
            GUILayout.Label($"Volumen general: {GameSettings.MasterVolume * 100f:0}%", label);
            GameSettings.MasterVolume = GUILayout.HorizontalSlider(GameSettings.MasterVolume, 0f, 1f);
            GameSettings.Apply();
            GUILayout.Label("Controles: WASD mover, mouse mirar, clic izq./der. cada mano, Shift esfuerzo, Q soltar todo, E interactuar, Ctrl agacharse, Espacio paso evasivo, Esc pausa.", new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 14 });
        }
    }
}
