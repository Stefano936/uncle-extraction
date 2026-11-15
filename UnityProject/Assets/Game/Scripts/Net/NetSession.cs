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
        public static string StatusMessage = "";
        public static float StatusUntil;
        public static NetSession Instance { get; private set; }

        static bool argsParsed;
        const ushort SoloPortBase = 7790;

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
                Debug.Log($"[NetDiag] cliente {nm.LocalClientId}: rtt={utp.GetCurrentRtt(NetworkManager.ServerClientId)}ms ronda={(RoundDirector.Instance ? RoundDirector.Instance.State.ToString() : "-")} t={(RoundDirector.Instance ? RoundDirector.Instance.Elapsed : 0f):0.0}");
            else if (nm.IsServer && Mode != NetMode.Solo)
            {
                string rtts = "";
                foreach (var id in nm.ConnectedClientsIds) if (id != NetworkManager.ServerClientId) rtts += $" c{id}={utp.GetCurrentRtt(id)}ms";
                Debug.Log($"[NetDiag] anfitrion: jugadores={nm.ConnectedClientsIds.Count}{rtts} agarres={GrabRegistry.Count} ronda={(RoundDirector.Instance ? RoundDirector.Instance.State.ToString() : "-")}");
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
                else if (a[i] == "-uePort" && i + 1 < a.Length && ushort.TryParse(a[i + 1], out var p)) Port = p;
                else if (a[i] == "-ueListen" && i + 1 < a.Length) ListenAddress = a[i + 1];
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

        static void Approve(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
        {
            var nm = NetworkManager.Singleton;
            bool full = nm.ConnectedClientsIds.Count >= MaxPlayers;
            res.Approved = !full;
            res.CreatePlayerObject = true;
            res.Reason = full ? "Partida llena (4 jugadores)" : "";
            // Decision M2: en el laboratorio se admite unirse con la ronda en curso (el escalado se recalcula).
            // El lobby de M4 bloquea incorporaciones durante la ronda (especificacion 07).
        }

        static void OnClientDisconnect(ulong id)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            if (nm.IsServer) { if (id != NetworkManager.ServerClientId) Say($"Se fue el jugador {id + 1}: sus agarres se liberaron"); return; }
            if (id == nm.LocalClientId)
            {
                NetAuth.Frozen = true;
                string reason = string.IsNullOrEmpty(nm.DisconnectReason) ? "Se perdio la conexion con el anfitrion" : nm.DisconnectReason;
                Say(reason + ". Volviendo a jugar solo.", 6f);
            }
        }

        static void OnClientStopped(bool wasHost)
        {
            if (wasHost || !Application.isPlaying || Instance == null || Mode != NetMode.Client) return;
            NetAuth.Frozen = true;
            // Sin migracion de anfitrion (especificacion 07): la partida termina y se vuelve a un estado seguro.
            Mode = NetMode.Solo;
            Instance.StartCoroutine(Instance.BackToSolo());
        }

        IEnumerator BackToSolo()
        {
            yield return new WaitForSecondsRealtime(2f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
                    utp.SetConnectionData(ListenAddress, Port, ListenAddress);
                    ok = nm.StartHost();
                    if (ok) Say($"Anfitrion en el puerto {Port} (conexion directa LAN/localhost)");
                    break;
                case NetMode.Client:
                    utp.SetConnectionData(Address, Port);
                    ok = nm.StartClient();
                    if (ok) Say($"Conectando a {Address}:{Port}...");
                    break;
            }
            Debug.Log($"[Net] begin {mode} ok={ok} protocol={ProtocolVersion}");
            if (!ok)
            {
                Say($"No se pudo iniciar {mode}. Jugando solo.");
                if (mode != NetMode.Solo) yield return Begin(NetMode.Solo);
            }
        }
    }
}
