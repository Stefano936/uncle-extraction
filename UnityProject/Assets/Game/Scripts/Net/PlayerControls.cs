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
        public float SecondsSinceIntent => IsLocal ? 0f : Time.time - lastRemoteAt;

        void Awake() { motor = GetComponent<PlayerMotor>(); }

        void OnEnable() { if (!IsSpawned && Local == null) Local = this; }
        public override void OnNetworkSpawn() { if (IsOwner) Local = this; }
        public override void OnNetworkDespawn() { if (Local == this) Local = null; }
        public override void OnDestroy() { if (Local == this) Local = null; base.OnDestroy(); }

        public bool Held(Act a)
        {
            if (IsLocal) return LabInput.HeldForGameplay(a);
            if (Time.time - lastRemoteAt > ReleaseTimeout) return false;               // sin noticias: soltar todo
            if (a == Act.Effort && Time.time - lastRemoteAt > EffortTimeout) return false;
            return remote.Has(a);
        }

        public bool Pressed(Act a)
        {
            if (IsLocal) return LabInput.Pressed(a);
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
            if (IsLocal) return LabInput.Move();
            return Time.time - lastRemoteAt > EffortTimeout ? Vector2.zero : remote.move;
        }

        public bool HasRemoteLook(out float yaw, out float pitch)
        {
            yaw = remote.yaw; pitch = remote.pitch;
            return !IsLocal && lastRemoteAt > 0f;
        }

        void Update()
        {
            if (!IsSpawned || !IsOwner || IsServer) return; // el anfitrion lee su propio teclado directamente
            if (Time.unscaledTime < nextSend) return;
            nextSend = Time.unscaledTime + SendInterval;
            var i = new PlayerIntent { seq = ++sendSeq, move = LabInput.Move(), yaw = motor ? motor.Yaw : 0f, pitch = motor ? motor.Pitch : 0f };
            for (int a = 0; a <= (int)Act.ToggleCursor; a++) if (LabInput.HeldForGameplay((Act)a)) i.buttons |= (ushort)(1 << a);
            SubmitIntentRpc(i);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        void SubmitIntentRpc(PlayerIntent i, RpcParams p = default)
        {
            if (p.Receive.SenderClientId != OwnerClientId) return; // la identidad sale del canal, no del payload
            if (i.seq <= lastSeq) return;                          // fuera de orden o duplicado
            lastSeq = i.seq; remote = i; lastRemoteAt = Time.time;
        }
    }
}
