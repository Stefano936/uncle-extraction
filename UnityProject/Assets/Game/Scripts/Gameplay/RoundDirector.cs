using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    public enum RoundState { Running, Success, Timeout }

    /// Reloj, exito, derrota y reinicio limpio (especificacion 02). En PhysicsLab el "anfitrion" es la instancia local.
    public class RoundDirector : NetworkBehaviour
    {
        public static RoundDirector Instance { get; private set; }
        public Difficulty difficulty = Difficulty.Normal;
        [Range(1, 4)] public int partySize = 1;
        /// El escalado usa el numero efectivo de jugadores conectados; el panel de laboratorio puede fijarlo a mano.
        public bool autoPartySize = true;
        /// Dificultad elegida en el menu; la aplica el anfitrion al empezar.
        public static Difficulty? RequestedDifficulty;
        /// Desafio elegido en el menu/sala (sesion 15); lo aplica el anfitrion. -ueChallenge NoCart|Narrow para pruebas.
        public static ChallengeMode? RequestedChallenge = ParseChallenge();
        static ChallengeMode? ParseChallenge() { var a = System.Environment.GetCommandLineArgs(); for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-ueChallenge" && System.Enum.TryParse(a[i + 1], true, out ChallengeMode m)) return m; return null; }
        public ChallengeMode challenge = ChallengeMode.Standard;
        readonly NetworkVariable<ChallengeMode> nChallenge = new NetworkVariable<ChallengeMode>(ChallengeMode.Standard);
        public ChallengeMode CurrentChallenge => Mirror ? nChallenge.Value : challenge;
        /// Anfitrion, en la sala o en solitario antes de empezar: elegir desafio (los invitados quedan sin LISTO para confirmar).
        public void SetChallenge(ChallengeMode m) { if (!NetAuth.IsServer || (!InLobby && NetSession.Mode != NetMode.Solo)) return; challenge = m; foreach (var pm in PlayerMotor.All) if (pm && pm.Controls && !pm.Controls.IsHostPlayer) pm.Controls.SetReadyServer(false); }

        readonly NetworkVariable<RoundState> nState = new NetworkVariable<RoundState>(RoundState.Running);
        readonly NetworkVariable<float> nElapsed = new NetworkVariable<float>();
        readonly NetworkVariable<Difficulty> nDifficulty = new NetworkVariable<Difficulty>(Difficulty.Normal);
        readonly NetworkVariable<int> nParty = new NetworkVariable<int>(1);
        readonly NetworkVariable<int> nRestarts = new NetworkVariable<int>();
        readonly NetworkVariable<bool> nLobby = new NetworkVariable<bool>();
        bool inLobby;
        /// Sala previa (UI_SPEC 3; especificacion 07): en partidas en red la ronda empieza en sala. Reloj quieto, el tio no
        /// decide, nadie controla su personaje; el anfitrion inicia cuando todos los presentes estan listos. En solitario no hay sala.
        public bool InLobby => Mirror ? nLobby.Value : inLobby;
        bool Mirror => IsSpawned && !IsServer;

        RoundState state = RoundState.Running; float elapsed; int restarts;
        public RoundState State { get => Mirror ? nState.Value : state; private set => state = value; }
        public float Elapsed { get => Mirror ? nElapsed.Value : elapsed; private set => elapsed = value; }
        public Difficulty CurrentDifficulty => Mirror ? nDifficulty.Value : difficulty;
        public int PartySize => Mirror ? nParty.Value : partySize;
        public float TimeLimit => AddTime(TimeLimitOverride > 0f ? TimeLimitOverride : LabBalance.RoundTime(PartySize, CurrentDifficulty), Mirror ? nTimeBonus.Value : timeBonus);
        static float AddTime(float limit, float bonus) => limit < 0f ? limit : limit + bonus;
        /// Transicion tras una desconexion (sesiones 13 y 14). Politica:
        /// - El TIEMPO extra que corresponde a un grupo mas chico (LabBalance: menos jugadores = mas tiempo) se da en el
        ///   momento: una desconexion en los ultimos segundos no hace perder por reloj antes de que llegue la ayuda.
        /// - Resistencia e intervalo de ataques del grupo nuevo se aplican tras DisconnectGrace s (sin cambios bruscos del tio
        ///   en medio de un forcejeo); el transcurrido se conserva y quedan al menos MinRemainingAfterDrop s.
        /// - Todos ven un aviso localizado. La gracia NO es una reconexion: la admision rechaza entrar con la ronda empezada
        ///   (limitacion conocida del alcance inicial); un regreso dentro de los 10 s solo ocurre si el transporte no llego a
        ///   desconectar al jugador.
        public const float DisconnectGrace = 10f, MinRemainingAfterDrop = 60f;
        readonly NetworkVariable<float> nTimeBonus = new NetworkVariable<float>();
        float timeBonus, dropSince = -1f, dropTimeCredit;
        /// Segundos que faltan para aplicar una baja pendiente (-1 = ninguna).
        /// Solo pruebas: fijar el tiempo transcurrido.
        public void SetElapsedForTest(float t) => elapsed = t;
        public float PendingDropIn => dropSince < 0f ? -1f : Mathf.Max(0f, DisconnectGrace - (Time.time - dropSince));
        /// Solo pruebas: -ueRoundTime s en el anfitrion (verificar derrota y reintento sin esperar 10 minutos).
        public static float TimeLimitOverride = ParseRoundTime();
        static float ParseRoundTime()
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-ueRoundTime" && float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var t)) return t;
            return -1f;
        }
        public float Resistance => LabBalance.Resistance(PartySize, CurrentDifficulty);
        public float AttackInterval => LabBalance.AttackInterval(PartySize, CurrentDifficulty);
        public int Restarts { get => Mirror ? nRestarts.Value : restarts; private set => restarts = value; }
        public string LastStressReport { get; private set; } = "";
        public bool StressRunning { get; private set; }

        void Start() { if (VanDoors.Instance && !VanDoors.Instance.GetComponent<VanDetails>()) VanDoors.Instance.gameObject.AddComponent<VanDetails>(); if (NetAuth.IsServer && RequestedDifficulty.HasValue) difficulty = RequestedDifficulty.Value; if (NetAuth.IsServer && RequestedChallenge.HasValue) challenge = RequestedChallenge.Value; }
        public override void OnNetworkSpawn()
        {
            if (IsServer && NetSession.Mode != NetMode.Solo) { inLobby = true; nLobby.Value = true; Debug.Log("[Lobby] sala abierta"); }
        }

        /// Todos los jugadores presentes confirmaron (no se exigen lugares vacios).
        public static bool AllReady(out string waitingFor)
        {
            waitingFor = "";
            foreach (var pm in PlayerMotor.All)
            {
                if (!pm) continue; var pc = pm.Controls;
                if (pc && !pc.Ready) { waitingFor = pm.playerName; return false; }
            }
            return PlayerMotor.All.Count > 0;
        }

        /// Anfitrion: empezar la ronda desde la sala (solo con todos listos).
        public bool StartFromLobby()
        {
            if (!NetAuth.IsServer || !inLobby || !AllReady(out _)) return false;
            inLobby = false; if (IsSpawned) nLobby.Value = false;
            Restart();
            Debug.Log($"[Lobby] ronda iniciada con {PlayerMotor.All.Count} jugador(es), dificultad {difficulty}");
            return true;
        }

        /// Anfitrion: volver a la sala con la misma configuracion y la confirmacion limpia (reintento en red).
        public void BackToLobby()
        {
            if (!NetAuth.IsServer) { RequestRestartRpc(); return; }
            foreach (var pm in PlayerMotor.All) if (pm && pm.Controls) pm.Controls.SetReadyServer(false);
            Restart();
            if (NetSession.Mode != NetMode.Solo) { inLobby = true; if (IsSpawned) nLobby.Value = true; Debug.Log("[Lobby] vuelta a la sala"); }
        }

        /// Anfitrion: dificultad desde la sala (los invitados la ven de solo lectura).
        public void SetDifficulty(Difficulty d) { if (NetAuth.IsServer && InLobby) { difficulty = d; foreach (var pm in PlayerMotor.All) if (pm && pm.Controls && !pm.Controls.IsHostPlayer) pm.Controls.SetReadyServer(false); } }
        void Awake() { Instance = this; Time.fixedDeltaTime = 1f / 60f; Physics.gravity = new Vector3(0, -9.81f, 0); if (!GetComponent<RoundStats>()) gameObject.AddComponent<RoundStats>(); if (!GetComponent<Challenges>()) gameObject.AddComponent<Challenges>(); if (!GetComponent<BarProps>()) gameObject.AddComponent<BarProps>(); }
        /// Anfitrion: la torre de vasos se cae para todos (sesion 15).
        public void TowerFall(Vector3 dir) { if (IsSpawned && IsServer) TowerFallRpc(dir, Restarts); else if (BarProps.Instance) BarProps.Instance.Fall(dir, Restarts); }
        [Rpc(SendTo.Everyone)] void TowerFallRpc(Vector3 dir, int round) { if (BarProps.Instance) BarProps.Instance.Fall(dir, round); }
        /// Campana de ultima ronda (sesion 17): la decide el anfitrion; suena en todos con el numero de ronda.
        public void BellRang() { if (IsSpawned && IsServer) BellRpc(Restarts); else if (BarProps.Instance) BarProps.Instance.BellFx(Restarts); }
        [Rpc(SendTo.Everyone)] void BellRpc(int round) { if (BarProps.Instance) BarProps.Instance.BellFx(round); }
        /// Parte de extraccion: lo arma el anfitrion y llega igual a todos (sesion 15).
        public const int MaxReportBytes = 12000;
        public void PublishReport(string json)
        {
            if (!IsSpawned || !IsServer) return;
            if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxReportBytes) { Debug.LogWarning("[Report] parte demasiado grande: se envia sin momentos"); var r = RoundStats.Last; r.moments.Clear(); json = JsonUtility.ToJson(r); }
            ReportRpc(json);
        }
        [Rpc(SendTo.NotServer)] void ReportRpc(string json) => RoundStats.Receive(json);
        public override void OnDestroy() { if (Instance == this) Instance = null; base.OnDestroy(); }

        void Update()
        {
            // R reinicia solo como herramienta de diagnostico (editor, build de desarrollo o -ueDebug), solo en el anfitrion y
            // nunca con un menu o campo de texto activo. El reintento normal es el boton de resultados o de la pausa.
            if (DiagnosticKeys && NetAuth.IsServer && !GameFlowUI.BlockingInput && LabInput.Pressed(Act.Restart)) Restart();
            if (!NetAuth.IsServer) return;
            if (autoPartySize) UpdatePartySize(Mathf.Clamp(NetAuth.ConnectedPlayers, 1, 4), Time.time);
            if (IsSpawned)
            {
                if (nState.Value != state) nState.Value = state;
                if (Mathf.Abs(nElapsed.Value - elapsed) > 0.1f) nElapsed.Value = elapsed;
                if (nDifficulty.Value != difficulty) nDifficulty.Value = difficulty;
                if (nChallenge.Value != challenge) nChallenge.Value = challenge;
                if (nParty.Value != partySize) nParty.Value = partySize;
                if (nRestarts.Value != restarts) nRestarts.Value = restarts;
                if (!Mathf.Approximately(nTimeBonus.Value, timeBonus)) nTimeBonus.Value = timeBonus;
                if (nLobby.Value != inLobby) nLobby.Value = inLobby;
            }
            if (State == RoundState.Running && !inLobby)
            {
                Elapsed += Time.deltaTime;
                if (TimeLimit > 0f && Elapsed >= TimeLimit) { State = RoundState.Timeout; GrabRegistry.ReleaseEverything(ReleaseReason.Reset); Debug.Log($"[Round] TIMEOUT at {Elapsed:0.0}s rescates={PhysicsWatchdog.RoundRescues}"); }
            }
        }

        /// Altas: inmediatas. Bajas: inmediatas fuera de la ronda; en ronda, tras el periodo de gracia y sin acortar el tiempo
        /// por debajo de MinRemainingAfterDrop. Publica para las pruebas (reloj inyectado).
        public void UpdatePartySize(int connected, float now)
        {
            if (connected >= partySize || inLobby || State != RoundState.Running)
            {
                if (dropSince >= 0f && connected >= partySize) { timeBonus -= dropTimeCredit; dropTimeCredit = 0f; Debug.Log("[Round] baja cancelada: el grupo volvio a estar completo"); }
                else if (dropSince >= 0f) Debug.Log("[Round] la ronda termino durante la gracia: no hubo ajuste del tio");
                partySize = connected; dropSince = -1f; return;
            }
            if (dropSince < 0f)
            {
                dropSince = now;
                float a = LabBalance.RoundTime(connected, difficulty), b = LabBalance.RoundTime(partySize, difficulty);
                dropTimeCredit = a > 0f && b > 0f ? Mathf.Max(0f, a - b) : 0f; timeBonus += dropTimeCredit; // tiempo: ya
                Debug.Log($"[Round] baja de jugador ({partySize} -> {connected}) a los {Elapsed:0.0}s, quedaban {(TimeLimit > 0f ? TimeLimit - dropTimeCredit - Elapsed : -1f):0.0}s; tiempo +{dropTimeCredit:0}s ya; resistencia y ataques en {DisconnectGrace:0}s");
                Notice($"round.player_left|secs={DisconnectGrace:0}|time={dropTimeCredit:0}");
                return;
            }
            if (now - dropSince < DisconnectGrace) return;
            timeBonus -= dropTimeCredit; dropTimeCredit = 0f; // el credito pasa a estar en el limite del grupo nuevo
            float before = TimeLimit; int was = partySize; partySize = connected; dropSince = -1f;
            float after = TimeLimit;
            if (after > 0f && after - Elapsed < MinRemainingAfterDrop) timeBonus += MinRemainingAfterDrop - (after - Elapsed);
            Debug.Log($"[Round] grupo {was} -> {partySize} tras la gracia; transcurrido {Elapsed:0.0}s conservado; limite {before:0} -> {TimeLimit:0}s");
            Notice($"round.party_adjusted|n={partySize}");
        }
        void Notice(string code) { if (IsSpawned && IsServer) NoticeRpc(new Unity.Collections.FixedString128Bytes(code)); else NetSession.Say(code, 6f); }
        [Rpc(SendTo.Everyone)] void NoticeRpc(Unity.Collections.FixedString128Bytes code) => NetSession.Say(code.ToString(), 6f);

        public void OnExtracted() { if (State == RoundState.Running) { State = RoundState.Success; Debug.Log($"[Round] SUCCESS in {Elapsed:0.0}s rescates={PhysicsWatchdog.RoundRescues}"); } }

        /// Cualquier jugador puede pedir reintentar; decide y ejecuta el anfitrion.
        [Rpc(SendTo.Server)]
        void RequestRestartRpc(RpcParams p = default)
        {   // pedido explicito de un invitado (boton de resultados): se acepta solo con la ronda terminada; decide el anfitrion
            ulong who = p.Receive.SenderClientId;
            bool ok = State != RoundState.Running && !inLobby && NetworkManager.ConnectedClients.ContainsKey(who);
            Debug.Log($"[Round] reintento pedido por cliente {who}: {(ok ? "aceptado (vuelta a la sala)" : "rechazado (ronda en curso)")}");
            if (ok) BackToLobby();
        }
        /// Atajos de diagnostico (R = reiniciar): editor, build de desarrollo o -ueDebug.
#if UE_PLAYER_BUILD
        public static bool DiagnosticKeys => false; // build de jugadores: sin atajos ni laboratorios
#else
        public static bool DiagnosticKeys => Application.isEditor || Debug.isDebugBuild || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ueDebug") >= 0;
#endif

        public void Restart()
        {
            if (!NetAuth.IsServer) { RequestRestartRpc(); return; }
            GrabRegistry.ReleaseEverything(ReleaseReason.Reset);
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb is IResettable r) r.ResetState();
            State = RoundState.Running; Elapsed = 0f; Restarts++; timeBonus = 0f; dropSince = -1f; dropTimeCredit = 0f;
            Debug.Log($"[Round] restart #{Restarts} grabs={GrabRegistry.Count} joints={CountGameplayJoints()}");
        }

        /// Uniones creadas por juego (anclaje, correa). Excluye la bisagra de la puerta.
        public static int CountGameplayJoints()
        {
            int n = 0;
            var ragdoll = new System.Collections.Generic.HashSet<Joint>();
            foreach (var p in FindObjectsByType<RagdollPart>(FindObjectsSortMode.None)) if (p.joint) ragdoll.Add(p.joint);
            foreach (var j in FindObjectsByType<ConfigurableJoint>(FindObjectsSortMode.None)) if (!ragdoll.Contains(j)) n++; // las articulaciones del tio no son de juego
            return n;
        }

        public void RunResetStress(int times) { if (!StressRunning) StartCoroutine(ResetStress(times)); }

        IEnumerator ResetStress(int times)
        {
            StressRunning = true;
            var rig = FindAnyObjectByType<LabForceRig>();
            int failures = 0; var notes = new List<string>();
            for (int i = 0; i < times; i++)
            {
                if (rig) rig.SetVirtualPullers(4);
                if (TransportCart.Instance) { /* sin efecto si el tio no esta en la caja */ TransportCart.Instance.Strap(); }
                yield return new WaitForSeconds(0.6f);
                int before = GrabRegistry.Count;
                if (rig) rig.SetVirtualPullers(0, false);
                Restart();
                yield return new WaitForFixedUpdate();
                int grabs = GrabRegistry.Count, joints = CountGameplayJoints();
                if (grabs != 0 || joints != 0) { failures++; notes.Add($"#{i + 1}: grabs={grabs} joints={joints}"); }
                Debug.Log($"[ResetStress] {i + 1}/{times} grabsBefore={before} after={grabs} joints={joints}");
            }
            LastStressReport = failures == 0 ? $"OK: {times} reinicios, 0 agarres/uniones huerfanos" : $"FALLO: {string.Join("; ", notes)}";
            Debug.Log($"[ResetStress] {LastStressReport}");
            StressRunning = false;
        }
    }
}
