using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    /// Intencion de un jugador enviada al anfitrion (especificacion 05/07): no decide resultados.
    public struct PlayerIntent : INetworkSerializable
    {
        public uint seq;
        public Vector2 move;
        public float yaw, pitch;
        public ushort buttons;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref seq); s.SerializeValue(ref move); s.SerializeValue(ref yaw); s.SerializeValue(ref pitch); s.SerializeValue(ref buttons);
        }
        public bool Has(Act a) => (buttons & (1 << (int)a)) != 0;

        /// Bits que un invitado puede pedir (acciones de juego; R, Tab, Esc y Marcar no viajan en la intencion).
        public const ushort GameplayMask = (1 << ((int)Act.Dodge + 1)) - 1;
        /// Validacion del anfitrion (sesion 13): descarta valores no finitos y acota el resto; nunca confia en el payload.
        public static bool Sanitize(ref PlayerIntent i)
        {
            if (!Finite(i.move.x) || !Finite(i.move.y) || !Finite(i.yaw) || !Finite(i.pitch)) return false;
            i.move = Vector2.ClampMagnitude(i.move, 1f);
            i.yaw = Mathf.Repeat(i.yaw, 360f);
            i.pitch = Mathf.Clamp(i.pitch, -89f, 89f);
            i.buttons &= GameplayMask;
            return true;
        }
        static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    }

    /// Fuente de controles de UN jugador. Separa la entrada del jugador local (dueno) de los personajes remotos:
    /// - dueno: lee teclado/raton/autopiloto (LabInput) y, si no es anfitrion, envia su intencion al anfitrion;
    /// - anfitrion con jugador remoto: usa la ultima intencion recibida (con secuencia y timeout).
    /// PlayerMotor y PlayerHands leen siempre de aqui; nunca de LabInput directamente.
    public class PlayerControls : NetworkBehaviour
    {
        public static PlayerControls Local { get; private set; }

        const float SendInterval = 1f / 60f;
        const float EffortTimeout = 0.5f, ReleaseTimeout = 1.0f; // especificacion 05

        PlayerMotor motor;
        PlayerIntent remote; uint lastSeq; float lastRemoteAt = -99f;
        ushort prevButtons, pressedMask; int pressedFrame = -1;
        uint sendSeq; float nextSend;

        /// Verdadero en la instancia que controla este personaje con su teclado (o sin red).
        public bool IsLocal => !IsSpawned || IsOwner;
        readonly NetworkVariable<bool> nReady = new NetworkVariable<bool>();
        bool readyLocal;
        /// Confirmacion en la sala (la decide el anfitrion con el pedido del jugador).
        public bool Ready => IsSpawned ? nReady.Value : readyLocal;
        public bool IsHostPlayer => !IsSpawned || OwnerClientId == NetworkManager.ServerClientId;
        public void SetReady(bool on) { if (!IsSpawned || IsServer) SetReadyServer(on); else ReadyRpc(on); }
        public void SetReadyServer(bool on) { readyLocal = on; if (IsSpawned && IsServer) nReady.Value = on; }
        [Rpc(SendTo.Server)]
        void ReadyRpc(bool on, RpcParams p = default) { if (p.Receive.SenderClientId == OwnerClientId && RoundDirector.Instance && RoundDirector.Instance.InLobby) SetReadyServer(on); }
        public float SecondsSinceIntent => IsLocal ? 0f : Time.time - lastRemoteAt;

        void Awake() { motor = GetComponent<PlayerMotor>(); }

        void OnEnable() { if (!IsSpawned && Local == null) Local = this; }
        public override void OnNetworkSpawn() { if (IsOwner) Local = this; }
        public override void OnNetworkDespawn() { if (Local == this) Local = null; }
        public override void OnDestroy() { if (Local == this) Local = null; base.OnDestroy(); }

        public bool Held(Act a)
        {
            if (IsLocal) return !GameFlowUI.BlockingInput && LabInput.HeldForGameplay(a);
            if (Time.time - lastRemoteAt > ReleaseTimeout) return false;               // sin noticias: soltar todo
            if (a == Act.Effort && Time.time - lastRemoteAt > EffortTimeout) return false;
            return remote.Has(a);
        }

        public bool Pressed(Act a)
        {
            if (IsLocal) return !GameFlowUI.BlockingInput && LabInput.Pressed(a); // menus, campos de texto y reasignacion no disparan acciones de juego
            if (pressedFrame != Time.frameCount)
            {
                pressedFrame = Time.frameCount;
                ushort now = Time.time - lastRemoteAt > ReleaseTimeout ? (ushort)0 : remote.buttons;
                pressedMask = (ushort)(now & ~prevButtons);
                prevButtons = now;
            }
            return (pressedMask & (1 << (int)a)) != 0;
        }

        public Vector2 Move()
        {
            if (IsLocal) return GameFlowUI.BlockingInput ? Vector2.zero : LabInput.Move();
            return Time.time - lastRemoteAt > EffortTimeout ? Vector2.zero : remote.move;
        }

        public bool HasRemoteLook(out float yaw, out float pitch)
        {
            yaw = remote.yaw; pitch = remote.pitch;
            return !IsLocal && lastRemoteAt > 0f;
        }

        void Update()
        {
            if (this == Local && Pressed(Act.Mark)) TryMark();
            if (!IsSpawned || !IsOwner || IsServer) return; // el anfitrion lee su propio teclado directamente
            if (Time.unscaledTime < nextSend) return;
            nextSend = Time.unscaledTime + SendInterval;
            bool blocked = GameFlowUI.BlockingInput;
            var i = new PlayerIntent { seq = ++sendSeq, move = blocked ? Vector2.zero : LabInput.Move(), yaw = motor ? motor.Yaw : 0f, pitch = motor ? motor.Pitch : 0f };
            for (int a = 0; a <= (int)Act.Dodge; a++) if (!blocked && LabInput.HeldForGameplay((Act)a)) i.buttons |= (ushort)(1 << a);
            SubmitIntentRpc(i);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        void SubmitIntentRpc(PlayerIntent i, RpcParams p = default)
        {
            if (p.Receive.SenderClientId != OwnerClientId) return; // la identidad sale del canal, no del payload
            if (i.seq <= lastSeq) return;                          // fuera de orden o duplicado
            if (!PlayerIntent.Sanitize(ref i)) { rejectedIntents++; if (rejectedIntents == 1 || rejectedIntents % 100 == 0) Debug.LogWarning($"[Net] intencion invalida de {OwnerClientId} descartada (x{rejectedIntents})"); return; }
            lastSeq = i.seq; remote = i; lastRemoteAt = Time.time;
        }
        int rejectedIntents;

        // ---------- Marcador contextual ----------
        float lastMarkAt = -99f;
        /// Dueño: marca lo que hay bajo la mira (hasta CoopMarkers.MaxRange). Solo con la ronda en curso.
        void TryMark()
        {
            var round = RoundDirector.Instance; var cam = Camera.main;
            if (!round || round.InLobby || round.State != RoundState.Running || !cam || !motor || motor.State != PlayerState.Active) return;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            Vector3 pos = ray.origin + ray.direction * CoopMarkers.MaxRange; MarkKind kind = MarkKind.Point;
            var hits = Physics.RaycastAll(ray, CoopMarkers.MaxRange, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits) { if (h.collider.transform.IsChildOf(transform) || (h.collider.attachedRigidbody && h.collider.attachedRigidbody.transform.IsChildOf(transform))) continue; pos = h.point; kind = CoopMarkers.Classify(h.collider); if (RoundDirector.DiagnosticKeys) Debug.Log($"[Mark] mira sobre {h.collider.name} -> {kind}"); break; }
            if (kind == MarkKind.Point) CoopMarkers.AimAssist(ray, Vector3.Distance(ray.origin, pos), ref kind, ref pos); // rozar al tio o al carrito cuenta
            if (!IsSpawned) { ShowMark(OwnerClientId, kind, pos); return; }
            if (IsServer) ServerMark(kind, pos); else MarkRpc((byte)kind, pos);
        }
        [Rpc(SendTo.Server)]
        void MarkRpc(byte kind, Vector3 pos, RpcParams p = default)
        {
            if (p.Receive.SenderClientId != OwnerClientId || kind > (byte)MarkKind.Van) return;
            ServerMark((MarkKind)kind, pos);
        }
        void ServerMark(MarkKind kind, Vector3 pos)
        {
            var round = RoundDirector.Instance;
            if (!round || round.InLobby || round.State != RoundState.Running) return;
            if (!CoopMarkers.Accept(motor ? motor.EyePosition : transform.position, pos, lastMarkAt, Time.time))
            { Debug.Log($"[Mark] rechazado de {(motor ? motor.playerName : "?")}: {(Time.time - lastMarkAt < CoopMarkers.MinInterval ? "demasiado seguido" : "fuera de alcance o invalido")}"); return; }
            lastMarkAt = Time.time;
            ShowMarkRpc((byte)kind, pos);
        }
        /// Pruebas de red: manda un pedido de marca tal cual (sin las comprobaciones del dueño) para probar la validacion del anfitrion.
        public void DebugMarkRaw(Vector3 pos) { if (IsSpawned && !IsServer) MarkRpc((byte)MarkKind.Point, pos); else ServerMark(MarkKind.Point, pos); }
        public void DebugMark() => TryMark();
        [Rpc(SendTo.Everyone)]
        void ShowMarkRpc(byte kind, Vector3 pos) => ShowMark(OwnerClientId, (MarkKind)kind, pos);
        void ShowMark(ulong owner, MarkKind kind, Vector3 pos) { if (CoopMarkers.Instance) CoopMarkers.Instance.Show(owner, motor ? motor.playerName : "?", kind, pos); }
    }
}
