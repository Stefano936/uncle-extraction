using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UncleExtraction
{
    public enum NetMode { Solo, Host, Client }

    /// Arranque de red de la escena (especificacion 02: IConnectionBootstrap; 07: listen server).
    /// - Solo: anfitrion en loopback (127.0.0.1), misma autoridad que en cooperativo, sin servicios externos.
    /// - Host: anfitrion escuchando en LAN/localhost (conexion directa: solo pruebas; internet llega con Relay en M4).
    /// - Client: conexion directa a una IP.
    /// Linea de comandos: -ueHost [-uePort 7777] | -ueClient 127.0.0.1 [-uePort 7777]. Sin argumentos: Solo.
    public class NetSession : MonoBehaviour
    {
        public const ushort ProtocolVersion = 1; // subir cuando cambie el contenido replicado (version de compatibilidad)
        public const int MaxPlayers = 4;

        public GameObject playerPrefab;
        public bool autoStart = true;

        public static NetMode Mode = NetMode.Solo;
        public static string Address = "127.0.0.1";
        public static ushort Port = 7777;
        /// Direccion de escucha del anfitrion: 0.0.0.0 para LAN; 127.0.0.1 para pruebas en la misma PC (sin aviso del firewall).
        public static string ListenAddress = "0.0.0.0";
        /// Internet por Relay (M4): mismo anfitrion autoritativo; cambia solo el transporte. false = conexion directa por IP.
        public static bool Online;
        /// Codigo de sala Relay: lo obtiene el anfitrion y lo escribe el invitado.
        public static string JoinCode = "";
        public static string RelayRegion = "";
        public static string StatusMessage = "";
        public static float StatusUntil;
        public static NetSession Instance { get; private set; }

        static bool argsParsed;
        /// Red degradada para pruebas (-ueNetSim latencia_ms jitter_ms perdida_%). Solo tiene efecto en editor y builds de desarrollo.
        public static int SimDelayMs, SimJitterMs, SimLossPct;
        const ushort SoloPortBase = 7790;
        public const int DisconnectTimeoutMs = 10000;

        void Awake() { Instance = this; ParseArgsOnce(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start() { if (autoStart) yield return Begin(Mode); }

        float nextDiag; RoundState lastRound = RoundState.Running;
        void Update()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening || Time.unscaledTime < nextDiag) return;
            nextDiag = Time.unscaledTime + 5f;
            var utp = nm.GetComponent<UnityTransport>();
            if (!nm.IsServer && nm.IsConnectedClient)
                Debug.Log($"[NetDiag] cliente {nm.LocalClientId}: rtt={utp.GetCurrentRtt(NetworkManager.ServerClientId)}ms {WorldState()}");
            else if (nm.IsServer && Mode != NetMode.Solo)
            {
                string rtts = "";
                foreach (var id in nm.ConnectedClientsIds) if (id != NetworkManager.ServerClientId) rtts += $" c{id}={utp.GetCurrentRtt(id)}ms";
                Debug.Log($"[NetDiag] anfitrion: jugadores={nm.ConnectedClientsIds.Count}{rtts} agarres={GrabRegistry.Count} {WorldState()}");
            }
        }

        static void ParseArgsOnce()
        {
            if (argsParsed) return; argsParsed = true;
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] == "-ueHost") Mode = NetMode.Host;
                else if (a[i] == "-ueClient") { Mode = NetMode.Client; if (i + 1 < a.Length && !a[i + 1].StartsWith("-")) Address = a[i + 1]; }
                else if (a[i] == "-ueRelay") Online = true;
                else if (a[i] == "-ueDifficulty" && i + 1 < a.Length && Enum.TryParse<Difficulty>(a[i + 1], true, out var dif)) RoundDirector.RequestedDifficulty = dif;
                else if (a[i] == "-ueJoinCode" && i + 1 < a.Length) { Online = true; Mode = NetMode.Client; JoinCode = a[i + 1]; }
                else if (a[i] == "-ueProfile" && i + 1 < a.Length) RelayConnector.Profile = a[i + 1];
                else if (a[i] == "-uePort" && i + 1 < a.Length && ushort.TryParse(a[i + 1], out var p)) Port = p;
                else if (a[i] == "-ueListen" && i + 1 < a.Length) ListenAddress = a[i + 1];
                else if (a[i] == "-ueNetSim" && i + 3 < a.Length)
                { int.TryParse(a[i + 1], out SimDelayMs); int.TryParse(a[i + 2], out SimJitterMs); int.TryParse(a[i + 3], out SimLossPct); }
            }
        }

        public static void Say(string msg, float seconds = 5f) { StatusMessage = msg; StatusUntil = Time.unscaledTime + seconds; Debug.Log("[Net] " + msg); }

        NetworkManager EnsureManager()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                var go = new GameObject("NetworkManager");
                var utp = go.AddComponent<UnityTransport>();
                // Caida brusca del anfitrion: con el valor por defecto (30 s) el invitado quedaba congelado 30 s antes de ver
                // el motivo (medido: 30,1 s). 10 s sigue tolerando cortes breves con Relay o Wi-Fi.
                utp.DisconnectTimeoutMS = DisconnectTimeoutMs;
                nm = go.AddComponent<NetworkManager>();
                nm.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = utp,
                    PlayerPrefab = playerPrefab,
                    EnableSceneManagement = true,    // sin esto NGO borra en el cliente los objetos de escena (tio, carrito, sistemas)
                    ConnectionApproval = true,
                    ProtocolVersion = ProtocolVersion,
                    TickRate = 30,
                };
                nm.AddNetworkPrefab(playerPrefab);
                nm.ConnectionApprovalCallback = Approve;
                nm.OnClientDisconnectCallback += OnClientDisconnect;
                nm.OnClientStopped += OnClientStopped;
                nm.OnClientConnectedCallback += id => { if (nm.IsServer && id != NetworkManager.ServerClientId) Say($"Se unio el jugador {id + 1}"); };
            }
            return nm;
        }

        /// Version que el cliente envia al conectarse; si no coincide con la del anfitrion se rechaza con un motivo claro.
        public static string VersionTag => Application.version + "/" + ProtocolVersion;

        static void Approve(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
        {
            var nm = NetworkManager.Singleton;
            res.CreatePlayerObject = true;
            if (req.ClientNetworkId == NetworkManager.ServerClientId) { res.Approved = true; return; } // el propio anfitrion
            string theirs = req.Payload != null && req.Payload.Length > 0 ? System.Text.Encoding.UTF8.GetString(req.Payload) : "";
            var r = RoundDirector.Instance;
            string reason = Decide(nm.ConnectedClientsIds.Count, MaxPlayers, theirs, VersionTag, !r || r.InLobby);
            res.Approved = reason == "";
            res.Reason = reason;
            if (reason != "") Debug.Log($"[Net] rechazo de conexion {req.ClientNetworkId}: {reason} (version {theirs})");
        }

        /// Regla de admision (pura, probada en EditMode): sala llena, version distinta o ronda ya iniciada.
        /// Devuelve la clave del motivo ("" = se admite).
        public static string Decide(int connected, int max, string theirVersion, string ourVersion, bool inLobby)
        {
            if (connected >= max) return "error.full";
            if (theirVersion != ourVersion) return "error.version";
            if (!inLobby) return "error.already_started"; // sin incorporacion con la ronda en curso (alcance original)
            return "";
        }

        static void OnClientDisconnect(ulong id)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            if (nm.IsServer) { if (id != NetworkManager.ServerClientId) Say($"Se fue el jugador {id + 1}: sus agarres se liberaron"); return; }
            if (id == nm.LocalClientId)
            {
                NetAuth.Frozen = true;
                string reason = string.IsNullOrEmpty(nm.DisconnectReason) ? "error.host_left" : nm.DisconnectReason;
                Say(reason, 10f); // clave localizada: el menu la traduce
            }
        }

        static void OnClientStopped(bool wasHost)
        {
            if (wasHost || !Application.isPlaying || Instance == null || Mode != NetMode.Client) return;
            NetAuth.Frozen = true;
            // Sin migracion de anfitrion (especificacion 07): la partida termina y se vuelve a un estado seguro.
            Mode = NetMode.Solo;
            Instance.StartCoroutine(Instance.BackToMenu());
        }

        IEnumerator BackToMenu()
        {
            yield return new WaitForSecondsRealtime(2f);
            LeaveToMenu();
        }

        public const string MenuScenePath = "Assets/Game/Scenes/Frontend.unity";
        /// Cierra la sesion y vuelve al menu (sin menu en la build, por ejemplo en pruebas, vuelve a jugar solo).
        public static void LeaveToMenu()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && (nm.IsListening || nm.IsClient)) nm.Shutdown();
            Time.timeScale = 1f;
            int menu = SceneUtility.GetBuildIndexByScenePath(MenuScenePath);
            Mode = NetMode.Solo; Online = false; JoinCode = ""; RelayRegion = "";
            if (menu >= 0) SceneManager.LoadScene(menu);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// Resumen del estado replicado para comparar anfitrion y cliente en los registros.
        public static string WorldState()
        {
            var r = RoundDirector.Instance; var n = NpcBrain.Instance; var v = VanDoors.Instance; var z = ExtractionZone.Instance;
            return $"ronda={(r ? r.State.ToString() : "-")} t={(r ? r.Elapsed : 0f):0.0} reinicios={(r ? r.Restarts : 0)} dificultad={(r ? r.CurrentDifficulty.ToString() : "-")} tio={(n ? n.State.ToString() : "-")} " +
                   $"puertas={(v ? v.CurrentPhase.ToString() : "-")}/{(v ? v.Closed01 : 0f):0.00} pose={(z ? z.Progress01 : 0f):0.00} asegurado={(z && z.Secured)} " +
                   $"carrito={(TransportCart.Instance ? (TransportCart.Instance.IsLoading ? "Cargando" : TransportCart.Instance.IsStrapped ? "Asegurado" : "Vacio") : "-")} escena={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";
        }

        /// Fuerza un diagnostico ya (por ejemplo al terminar una ronda).
        public static void LogWorldNow(string tag) => Debug.Log($"[NetDiag] {tag}: {WorldState()}");
        public static void LogWorldNow(string tag, string extra) => Debug.Log($"[NetDiag] {tag}: {WorldState()} {extra}");

        static void ApplyNetSim(NetworkManager nm)
        {
            var sim = nm.GetComponent<Unity.Multiplayer.Tools.NetworkSimulator.Runtime.NetworkSimulator>();
            if (!sim) sim = nm.gameObject.AddComponent<Unity.Multiplayer.Tools.NetworkSimulator.Runtime.NetworkSimulator>();
            var preset = Unity.Multiplayer.Tools.NetworkSimulator.Runtime.NetworkSimulatorPreset.Create("UE prueba", "latencia y perdida simuladas", SimDelayMs, SimJitterMs, 0, SimLossPct);
            sim.ChangeConnectionPreset(preset);
            bool active = Debug.isDebugBuild || Application.isEditor;
            Debug.Log($"[Net] simulador de red: {SimDelayMs} ms +-{SimJitterMs} ms, perdida {SimLossPct}% (activo={active}; en builds de release no tiene efecto)");
        }

        /// Fallo de internet: no se cae a jugar solo en silencio; vuelve al menu con el motivo (UI_SPEC: error.*).
        static void OnlineFailed(string reason)
        {
            Debug.Log($"[Net] internet no disponible: {reason}");
            NetAuth.Frozen = true;
            LeaveToMenu();
            Say(reason, 10f);
        }

        /// Relay responde de forma asincronica: si mientras tanto el jugador cancelo (Mode volvio a Solo o se pidio otra sesion),
        /// la respuesta tardia se descarta y no inicia ninguna conexion. (Ademas el coroutine muere con la escena al cancelar.)
        bool CancelledWhileWaiting(NetMode mode)
        {
            if (this && Mode == mode && Online) return false;
            Debug.Log($"[Net] respuesta de Relay descartada: la sesion {mode} se cancelo mientras se esperaba");
            return true;
        }

        public IEnumerator Begin(NetMode mode)
        {
            var nm = EnsureManager();
            if (nm.IsListening && nm.IsClient && !nm.IsServer && mode == NetMode.Client)
            {   // la escena la recargo la sincronizacion de NGO al conectarse: la sesion sigue viva
                Debug.Log("[Net] escena sincronizada por el anfitrion; se mantiene la conexion");
                yield break;
            }
            if (nm.IsListening || nm.ShutdownInProgress)
            {   // escena recargada (reinicio de prueba o volver a solo): cerrar la sesion anterior antes de abrir otra
                nm.Shutdown();
                while (nm.ShutdownInProgress) yield return null;
                yield return null;
            }
            var utp = nm.GetComponent<UnityTransport>();
            Mode = mode; NetAuth.Frozen = false;
            bool ok = false;
            switch (mode)
            {
                case NetMode.Solo:
                    for (ushort k = 0; k < 10 && !ok; k++)
                    {   // puerto local libre: permite varias instancias en la misma PC
                        utp.SetConnectionData("127.0.0.1", (ushort)(SoloPortBase + k), "127.0.0.1");
                        ok = nm.StartHost();
                        if (!ok) { nm.Shutdown(); while (nm.ShutdownInProgress) yield return null; }
                    }
                    break;
                case NetMode.Host:
                    if (Online)
                    {   // internet: el codigo de sala sale de la asignacion Relay
                        Say("lobby.creating", 20f);
                        var t = RelayConnector.Host(utp, MaxPlayers - 1);
                        while (!t.IsCompleted) yield return null;
                        if (CancelledWhileWaiting(mode)) yield break;
                        if (!t.Result.ok) { OnlineFailed(t.Result.error); yield break; }
                        JoinCode = t.Result.code; RelayRegion = t.Result.region ?? "";
                        ok = nm.StartHost();
                        if (ok) Say("relay.hosting|code=" + JoinCode);
                        break;
                    }
                    utp.SetConnectionData(ListenAddress, Port, ListenAddress);
                    ok = nm.StartHost();
                    if (ok) Say($"Anfitrion en el puerto {Port} (conexion directa LAN/localhost)");
                    break;
                case NetMode.Client:
                    nm.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(VersionTag);
                    if (Online)
                    {
                        var t = RelayConnector.Join(utp, JoinCode);
                        while (!t.IsCompleted) yield return null;
                        if (CancelledWhileWaiting(mode)) yield break;
                        if (!t.Result.ok) { OnlineFailed(t.Result.error); yield break; }
                        JoinCode = t.Result.code; RelayRegion = t.Result.region ?? "";
                        ok = nm.StartClient();
                        break;
                    }
                    utp.SetConnectionData(Address, Port);
                    ok = nm.StartClient();
                    if (ok) Say($"Conectando a {Address}:{Port}...");
                    break;
            }
            Debug.Log($"[Net] begin {mode} ok={ok} protocol={ProtocolVersion} transporte={(Online ? "relay codigo=" + JoinCode + " region=" + RelayRegion : "directo")}");
            if (ok && (SimDelayMs > 0 || SimLossPct > 0) && mode != NetMode.Solo) ApplyNetSim(nm);
            if (!ok && Online) { OnlineFailed("error.service"); yield break; }
            if (!ok)
            {
                Say($"No se pudo iniciar {mode}. Jugando solo.");
                if (mode != NetMode.Solo) yield return Begin(NetMode.Solo);
            }
        }
    }
}
