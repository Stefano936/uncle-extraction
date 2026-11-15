using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    /// Puertas traseras de la camioneta y accion de cierre (GDD 03: «la puerta esta libre; un jugador mantiene la accion
    /// de cierre durante 1 segundo»; balance.extraction.door_close_hold_s = 1,0).
    /// El anfitrion valida y decide; las hojas se animan en todos los pares a partir del estado replicado.
    public class VanDoors : NetworkBehaviour, IResettable
    {
        public static VanDoors Instance { get; private set; }

        public Transform leftLeaf, rightLeaf;      // bisagras en x = -/+ medio ancho; cerradas = 0 grados
        public float openAngle = 100f;
        public Vector3 doorwayCenter = new Vector3(0f, 1.3f, -8.9f);   // franja que barren las hojas al cerrar
        public Vector3 doorwayHalf = new Vector3(1.15f, 0.85f, 0.32f);
        public Vector3 closePoint = new Vector3(0f, 1.2f, -9.25f);     // centro de la abertura
        public float closeRange = 2.6f;
        public const float CloseHold = 1.0f;

        readonly NetworkVariable<float> nClosed = new NetworkVariable<float>();
        readonly NetworkVariable<float> nProgress = new NetworkVariable<float>();
        bool Mirror => IsSpawned && !IsServer;

        float progress, closed01, lastHold = -99f;
        bool closing;

        public float CloseProgress01 => Mirror ? nProgress.Value : progress;
        public float Closed01 => Mirror ? nClosed.Value : closed01;

        void Awake() { Instance = this; }
        public override void OnDestroy() { if (Instance == this) Instance = null; base.OnDestroy(); }

        /// Jugador lo bastante cerca y AFUERA de la camioneta (no se cierra con alguien adentro de la abertura).
        public bool InRange(Vector3 playerPos) =>
            Vector3.Distance(new Vector3(playerPos.x, closePoint.y, playerPos.z), closePoint) <= closeRange && playerPos.z > closePoint.z + 0.35f;

        static readonly Collider[] buf = new Collider[16];
        /// Algo en la franja de barrido de las hojas: jugadores, carrito, tio u objetos.
        public bool DoorwayBlocked(out string what)
        {
            what = "";
            int n = Physics.OverlapBoxNonAlloc(doorwayCenter, doorwayHalf, buf, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = buf[i];
                if (c.transform.IsChildOf(transform)) continue; // las propias hojas
                var rb = c.attachedRigidbody;
                if (!rb) continue;                              // piso, rampa y paredes no bloquean
                if (rb.GetComponent<PlayerMotor>()) { what = "Hay alguien en la puerta"; return true; }
                if (rb.GetComponent<NpcBody>()) { what = "Le queda algo afuera"; return true; }
                what = $"{rb.name} bloquea la puerta"; return true;
            }
            return false;
        }

        /// Motivo por el que no se puede cerrar ("" = se puede). En clientes es una aproximacion con estado replicado.
        public string BlockReason()
        {
            var zone = ExtractionZone.Instance;
            if (!zone || zone.Secured || closing) return "";
            if (!zone.AllPointsInside) return "El tio no esta adentro";
            if (zone.SolidOverlap) return "Esta trabado con algo: acomodalo";
            if (!zone.PoseReady) return $"Acomodandolo... {zone.Progress01 * 100f:0}%";
            if (DoorwayBlocked(out string what)) return what;
            return "";
        }

        /// Anfitrion: un jugador mantiene E este frame. Devuelve el progreso 0..1.
        public float HoldClose(PlayerHands who, float dt)
        {
            if (!NetAuth.IsServer || closing) return progress;
            if (BlockReason() != "") { progress = 0f; return 0f; }
            lastHold = Time.time;
            progress += dt / CloseHold;
            if (progress >= 1f)
            {
                progress = 1f; closing = true;
                Debug.Log($"[Van] cerrada por {(who ? who.OwnerName : "?")}");
                if (ExtractionZone.Instance) ExtractionZone.Instance.Secure();
            }
            return progress;
        }

        void Update()
        {
            if (NetAuth.IsServer)
            {
                if (!closing && Time.time - lastHold > 0.15f) progress = 0f; // soltar E corta el cierre
                float target = closing ? 1f : 0f;
                closed01 = Mathf.MoveTowards(closed01, target, Time.deltaTime / 0.5f);
                if (IsSpawned)
                {
                    if (Mathf.Abs(nClosed.Value - closed01) > 0.02f || (closed01 == 1f && nClosed.Value != 1f) || (closed01 == 0f && nClosed.Value != 0f)) nClosed.Value = closed01;
                    if (Mathf.Abs(nProgress.Value - progress) > 0.02f || (progress == 0f && nProgress.Value != 0f)) nProgress.Value = progress;
                }
            }
            float c = Closed01;
            if (leftLeaf) leftLeaf.localRotation = Quaternion.Euler(0f, -openAngle * (1f - c), 0f);
            if (rightLeaf) rightLeaf.localRotation = Quaternion.Euler(0f, openAngle * (1f - c), 0f);
        }

        public void ResetState() { progress = 0f; closed01 = 0f; closing = false; lastHold = -99f; }
    }
}
