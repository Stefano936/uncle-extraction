using UnityEngine;

namespace UncleExtraction
{
    /// HUD minimo y panel de depuracion de PhysicsLab (IMGUI: sin dependencias de paquetes de UI).
    /// El arte de UI del kit (preproduction/uncle_extraction_kit_UI) se integra en M6.
    public class LabDebugHud : MonoBehaviour
    {
        public PlayerMotor player; public PlayerHands hands;
        public NpcBrain npc; public NpcBody npcBody;
        public TransportCart cart; public ExtractionZone zone;
        public RoundDirector round; public LabForceRig rig; public LabAutopilot autopilot;

        bool panel = false;
        GUIStyle big, mid, small, box;

        void Start() { LockCursor(true); }

        void Update()
        {
            if (LabInput.Pressed(Act.ToggleCursor)) { panel = !panel; LockCursor(!panel); }
            // El jugador local lo genera la red al conectarse: enlazar el HUD a SU personaje, nunca al de otro.
            var local = PlayerControls.Local;
            if (local && (!player || player.gameObject != local.gameObject))
            {
                player = local.GetComponent<PlayerMotor>(); hands = local.GetComponent<PlayerHands>();
                if (autopilot) { autopilot.player = player; autopilot.hands = hands; }
            }
        }

        static void LockCursor(bool on) { Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !on; }

        void Styles()
        {
            if (big != null) return;
            big = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            mid = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13 };
        }

        void OnGUI()
        {
            Styles();
            float W = Screen.width, H = Screen.height;
            // Reticula y candidato
            GUI.Label(new Rect(W / 2 - 10, H / 2 - 12, 20, 24), "+", mid);
            if (hands && hands.Candidate) GUI.Label(new Rect(W / 2 - 200, H / 2 + 14, 400, 24), $"[{hands.Candidate.label}]", mid);
            if (hands && Time.time < hands.FeedbackUntil) GUI.Label(new Rect(W / 2 - 200, H / 2 + 38, 400, 24), hands.LastFeedback, mid);
            if (hands && hands.InteractLabel != "")
            {
                GUI.Label(new Rect(W / 2 - 250, H / 2 + 62, 500, 24), hands.InteractLabel, mid);
                if (hands.InteractProgress01 > 0) GUI.Box(new Rect(W / 2 - 100, H / 2 + 88, 200 * hands.InteractProgress01, 10), GUIContent.none);
            }

            // Senal de ataque (accesible: texto + cuenta regresiva, ademas del color/pose)
            if (npc && (npc.State == NpcState.Windup || npc.State == NpcState.Attack))
            {
                var prev = GUI.color; GUI.color = npc.State == NpcState.Attack ? Color.red : new Color(1f, 0.75f, 0.2f);
                string t = npc.State == NpcState.Windup ? $"{(npc.WriggleAttempt ? "SE QUIERE SOLTAR" : npc.CurrentAttack.label)}  {npc.StateTimeLeft:0.0}s" : npc.CurrentAttack.label + "!";
                GUI.Label(new Rect(0, H * 0.18f, W, 50), t, big);
                GUI.color = prev;
            }
            if (npc && npc.State == NpcState.ForcedRecovery) GUI.Label(new Rect(0, H * 0.18f, W, 50), $"EL TIO ESTA AGOTADO  {npc.StateTimeLeft:0.0}s", big);

            if (player && player.State == PlayerState.Fallen) GUI.Label(new Rect(0, H * 0.45f, W, 50), $"CAIDO - te levantas en {player.StateTimeLeft:0.0}s", big);
            if (round && round.State == RoundState.Success) GUI.Label(new Rect(0, H * 0.3f, W, 60), $"EXTRAIDO en {round.Elapsed:0.0}s  (R para reiniciar)", big);
            if (round && round.State == RoundState.Timeout) GUI.Label(new Rect(0, H * 0.3f, W, 60), "SE ACABO EL TIEMPO  (R para reiniciar)", big);

            // Barras del jugador
            if (player)
            {
                Bar(20, H - 70, "Compostura", player.Composure / LabBalance.ComposureMax, new Color(0.2f, 0.85f, 0.75f));
                Bar(20, H - 45, "Energia", player.Energy / LabBalance.EnergyMax, new Color(1f, 0.7f, 0.2f));
            }
            if (hands)
            {
                GUI.Label(new Rect(W - 330, H - 70, 320, 22), HandText(0), small);
                GUI.Label(new Rect(W - 330, H - 48, 320, 22), HandText(1), small);
            }
            if (zone && zone.Progress01 > 0f && !zone.Secured) GUI.Label(new Rect(0, H * 0.6f, W, 30), $"Cargando en la camioneta {zone.Progress01 * 100f:0}%", mid);

            string timer = round ? (round.TimeLimit > 0 ? $"{Mathf.Max(0, round.TimeLimit - round.Elapsed):0}s" : "sin limite") : "";
            GUI.Label(new Rect(W / 2 - 150, 8, 300, 26), $"{(LevelLayout.Current ? LevelLayout.Current.displayName : "")}  {timer}", mid);

            if (autopilot && autopilot.Running) GUI.Label(new Rect(0, 40, W, 26), $"AUTOPILOTO ({autopilot.Mode}): {autopilot.Status}", mid);
            if (Time.unscaledTime < NetSession.StatusUntil) GUI.Label(new Rect(0, H * 0.72f, W, 30), NetSession.StatusMessage, mid);
            if (NetAuth.Listening)
            {
                var nm = Unity.Netcode.NetworkManager.Singleton;
                string role = nm.IsHost ? (NetSession.Mode == NetMode.Solo ? "Solo" : "Anfitrion") : "Cliente";
                GUI.Label(new Rect(W - 330, 8, 320, 22), $"{role} | jugadores {(nm.IsServer ? nm.ConnectedClientsIds.Count : (round ? round.PartySize : 0))}", small);
            }
            else GUI.Label(new Rect(W - 330, 8, 320, 22), NetSession.Mode == NetMode.Client ? "Conectando..." : "", small);
            if (panel) DrawPanel(W, H);
            else GUI.Label(new Rect(10, 8, 600, 22), "Tab: panel de depuracion / liberar raton", small);
        }

        string HandText(int hand)
        {
            var s = hands.HeldSocket(hand);
            string n = hand == 0 ? "Mano izq (clic izq)" : "Mano der (clic der)";
            if (s == null) return $"{n}: libre";
            return $"{n}: {s.label}  tension {hands.Tension01(hand) * 100:0}%{(hands.EffortActive ? "  ESFUERZO" : "")}";
        }

        void Bar(float x, float y, string label, float v, Color c)
        {
            GUI.Label(new Rect(x, y, 90, 20), label, small);
            GUI.Box(new Rect(x + 90, y + 3, 200, 14), GUIContent.none);
            var prev = GUI.color; GUI.color = c; GUI.Box(new Rect(x + 90, y + 3, 200 * Mathf.Clamp01(v), 14), GUIContent.none); GUI.color = prev;
        }

        void DrawPanel(float W, float H)
        {
            GUILayout.BeginArea(new Rect(10, 40, 340, H - 140), box);
            GUILayout.Label("<b>PhysicsLab - depuracion</b> (Tab cierra)", new GUIStyle(small) { richText = true });
            bool host = NetAuth.IsServer;
            if (!host) GUILayout.Label("Cliente: los ajustes de ronda, IA y fuerzas los decide el anfitrion.", small);
            GUI.enabled = host;
            if (round)
            {
                GUILayout.Label($"Ronda: {round.State}  t={round.Elapsed:0.0}s  reinicios={round.Restarts}", small);
                GUILayout.Label($"Dificultad {round.CurrentDifficulty} | jugadores (escala) {round.PartySize}{(round.autoPartySize ? " auto" : "")} | R={round.Resistance:0.00} | intervalo={round.AttackInterval:0.00}s", small);
                GUILayout.BeginHorizontal();
                foreach (Difficulty d in System.Enum.GetValues(typeof(Difficulty))) if (GUILayout.Button(d.ToString())) round.difficulty = d;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("auto")) round.autoPartySize = true;
                for (int n = 1; n <= 4; n++) if (GUILayout.Button($"N={n}")) { round.autoPartySize = false; round.partySize = n; }
                GUILayout.EndHorizontal();
            }
            if (npc)
            {
                GUILayout.Label($"Tio: {npc.State} ({npc.Intent}) postura={npcBody.Posture} incl={npcBody.TiltDeg:0}deg", small);
                GUILayout.Label($"Presupuesto {npc.Budget:0}/{npc.BudgetMax:0}  agarres={GrabRegistry.CountOnBody(npcBody.Body)}  anclado={npc.IsAnchored}  rechazo: {npc.LastRejection}", small);
                npc.brainEnabled = GUILayout.Toggle(npc.brainEnabled, "IA del tio activa");
                GUILayout.Label("Forzar ataque:", small);
                GUILayout.BeginHorizontal();
                foreach (AttackId a in System.Enum.GetValues(typeof(AttackId))) if (GUILayout.Button(a.ToString())) npc.ForceAttack(a);
                GUILayout.EndHorizontal();
            }
            if (rig)
            {
                GUILayout.Label($"Fuerzas virtuales hacia la camioneta: {rig.ActivePullers}", small);
                GUILayout.BeginHorizontal();
                foreach (int n in new[] { 0, 1, 2, 4 }) if (GUILayout.Button(n.ToString())) rig.SetVirtualPullers(n);
                GUILayout.EndHorizontal();
            }
            GUI.enabled = true;
            if (autopilot)
            {
                GUILayout.Label(autopilot.Running ? $"Autopiloto: {autopilot.Mode} - {autopilot.Status}" : "Autopiloto (juega solo, para mirar):", small);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Arrastrar")) { if (host) round.Restart(); autopilot.StartRun(AutopilotMode.Drag); }
                if (GUILayout.Button("Carrito")) { if (host) round.Restart(); autopilot.StartRun(AutopilotMode.Cart); }
                if (GUILayout.Button("Parar")) autopilot.StopRun();
                GUILayout.EndHorizontal();
            }
            GUI.enabled = host;
            GUILayout.Label($"Resorte de agarre k={GrabSpring.K:0} N/m  c={GrabSpring.C:0} N*s/m", small);
            GrabSpring.K = GUILayout.HorizontalSlider(GrabSpring.K, 300f, 4000f);
            GrabSpring.C = GUILayout.HorizontalSlider(GrabSpring.C, 20f, 600f);
            if (npcBody)
            {
                GUILayout.Label($"Equilibrio del tio (rad/s2) {npcBody.balanceAccelCap:0.0}", small);
                npcBody.balanceAccelCap = GUILayout.HorizontalSlider(npcBody.balanceAccelCap, 4f, 60f);
            }
            GUILayout.BeginHorizontal();
            if (player && GUILayout.Button("Derribarme")) player.Fall("debug");
            if (hands && GUILayout.Button("Simular desconexion")) hands.ReleaseAll(ReleaseReason.Disconnect);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (round && GUILayout.Button("Reiniciar (R)")) round.Restart();
            if (round && GUILayout.Button("10 reinicios")) round.RunResetStress(10);
            GUILayout.EndHorizontal();
            if (round && round.LastStressReport != "") GUILayout.Label(round.LastStressReport, small);
            GUI.enabled = true;
            GUILayout.Label($"Agarres: activos {GrabRegistry.Count} creados {GrabRegistry.TotalCreated} liberados {GrabRegistry.TotalReleased} uniones {RoundDirector.CountGameplayJoints()}", small);
            GUILayout.Label("WASD mover | raton mirar | clic izq/der manos | Shift esfuerzo | Q soltar | E interactuar | Ctrl agacharse | Espacio paso evasivo | R reiniciar", small);
            GUILayout.EndArea();
        }
    }
}
