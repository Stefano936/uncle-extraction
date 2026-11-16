using UnityEngine;

namespace UncleExtraction
{
    /// Pausa y resultados de la partida (GDD: partida -> resultados -> repetir o volver).
    /// En solitario la pausa detiene el tiempo; en cooperativo no (el mundo es del anfitrion y sigue para todos).
    public class GameFlowUI : MonoBehaviour
    {
        public RoundDirector round;
        public NpcBrain npc;
        bool paused, showOptions;
        GUIStyle title, big, label, small;
        float resultsSince = -1f;

        public bool Paused => paused;
        static GameFlowUI instance;
        /// Pausa abierta o resultados en pantalla: no se controla al personaje (calculado al momento, sin depender del orden de Update).
        public static bool BlockingInput => instance && instance.isActiveAndEnabled && (instance.paused || (instance.round && instance.round.State != RoundState.Running));

        void Awake() { instance = this; }
        void Start() { GameSettings.Load(); if (!round) round = FindAnyObjectByType<RoundDirector>(); if (!npc) npc = FindAnyObjectByType<NpcBrain>(); }

        void OnDisable() { if (Time.timeScale == 0f) Time.timeScale = 1f; }
        void OnDestroy() { if (instance == this) instance = null; }

        void Update()
        {
            bool ended = round && round.State != RoundState.Running;
            if (ended && resultsSince < 0f) resultsSince = Time.unscaledTime;
            if (!ended) resultsSince = -1f;
            if (LabInput.Pressed(Act.Pause) && !ended) SetPaused(!paused);
            if (BlockingInput) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        void SetPaused(bool on)
        {
            paused = on; showOptions = false;
            if (NetSession.Mode == NetMode.Solo) Time.timeScale = on ? 0f : 1f;
            Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = on;
            if (!on) GameSettings.Save();
        }

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big = new GUIStyle(GUI.skin.button) { fontSize = 22 };
            label = new GUIStyle(GUI.skin.label) { fontSize = 19 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, alignment = TextAnchor.MiddleCenter };
        }

        void OnGUI()
        {
            if (!round) return;
            Styles();
            float W = Screen.width, H = Screen.height, w = Mathf.Min(520, W - 40), x = (W - w) / 2f;
            bool ended = round.State != RoundState.Running;
            if (!ended && !paused) return;
            GUI.Box(new Rect(x - 20, H * 0.18f, w + 40, H * 0.66f), GUIContent.none);
            GUILayout.BeginArea(new Rect(x, H * 0.2f, w, H * 0.62f));
            if (ended)
            {
                bool win = round.State == RoundState.Success;
                GUILayout.Label(win ? "¡ADENTRO!" : "SE ACABO EL TIEMPO", title, GUILayout.Height(60));
                GUILayout.Label(win ? "El tio esta en la camioneta. Nadie sabe como." : "El tio sigue en el bar. Pidio otra.", small);
                var local = PlayerControls.Local ? PlayerControls.Local.GetComponent<PlayerMotor>() : null;
                GUILayout.Label($"Tiempo: {round.Elapsed:0.0} s   |   Dificultad: {Name(round.CurrentDifficulty)}   |   Jugadores: {round.PartySize}", label);
                if (npc) GUILayout.Label($"Ataques del tio: {npc.AttacksStarted}   |   Te tiro al piso: {(local ? local.FallCount : 0)} veces", label);
                GUILayout.Space(10);
                if (Time.unscaledTime - resultsSince > 1f)
                {
                    if (GUILayout.Button(NetAuth.IsServer ? "Reintentar" : "Pedir reintentar al anfitrion", big, GUILayout.Height(48))) round.Restart();
                    if (GUILayout.Button("Volver al menu", big, GUILayout.Height(44))) NetSession.LeaveToMenu();
                }
            }
            else if (showOptions)
            {
                FrontendMenu.OptionsPanel(label);
                if (GUILayout.Button("Volver", big, GUILayout.Height(44))) { GameSettings.Save(); showOptions = false; }
            }
            else
            {
                GUILayout.Label("PAUSA", title, GUILayout.Height(60));
                if (NetSession.Mode != NetMode.Solo) GUILayout.Label("En cooperativo el juego sigue corriendo.", small);
                if (GUILayout.Button("Seguir", big, GUILayout.Height(48))) SetPaused(false);
                if (GUILayout.Button("Reiniciar intento", big, GUILayout.Height(44))) { SetPaused(false); round.Restart(); }
                if (GUILayout.Button("Opciones", big, GUILayout.Height(44))) showOptions = true;
                if (GUILayout.Button("Volver al menu", big, GUILayout.Height(44))) { SetPaused(false); NetSession.LeaveToMenu(); }
            }
            GUILayout.EndArea();
        }

        static string Name(Difficulty d) => d == Difficulty.Relaxed ? "Relajada" : d == Difficulty.Hard ? "Dificil" : "Normal";
    }
}
