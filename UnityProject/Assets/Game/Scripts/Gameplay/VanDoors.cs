using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    /// Puertas traseras de la camioneta y accion de cierre (GDD 03: «la puerta esta libre; un jugador mantiene la accion
    /// de cierre durante 1 segundo»; balance.extraction.door_close_hold_s = 1,0).
    /// Secuencia autoritativa: pose valida sostenida (ExtractionZone) -> un jugador mantiene E 1 s -> las hojas se cierran
    /// (0,5 s) mientras las condiciones siguen valiendo -> recien con las hojas cerradas se asegura la ronda.
    /// Si durante el cierre el tio se mueve, aparece alguien u algo en la abertura, las puertas se reabren y se empieza de cero.
    public class VanDoors : NetworkBehaviour, IResettable
    {
        public static VanDoors Instance { get; private set; }

        public Transform leftLeaf, rightLeaf;      // bisagras en x = -/+ medio ancho; cerradas = 0 grados
        public float openAngle = 100f;
        public Vector3 doorwayCenter = new Vector3(0f, 1.3f, -8.9f);   // franja que barren las hojas al cerrar
        public Vector3 doorwayHalf = new Vector3(1.15f, 0.85f, 0.32f);
        public Vector3 closePoint = new Vector3(0f, 1.2f, -9.25f);     // centro de la abertura
        public float closeRange = 2.6f;
        public const float CloseHold = 1.0f, SwingTime = 0.5f;

        public enum Phase { Open, Holding, Closing, Closed }

        readonly NetworkVariable<float> nClosed = new NetworkVariable<float>();
        readonly NetworkVariable<float> nProgress = new NetworkVariable<float>();
        readonly NetworkVariable<Phase> nPhase = new NetworkVariable<Phase>();
        bool Mirror => IsSpawned && !IsServer;

        float progress, closed01, lastHold = -99f; int lastHoldFrame = -1;
        Phase phase = Phase.Open;
        public int Aborts { get; private set; }

        public float CloseProgress01 => Mirror ? nProgress.Value : progress;
        public float Closed01 => Mirror ? nClosed.Value : closed01;
        public Phase CurrentPhase => Mirror ? nPhase.Value : phase;

        void Awake() { Instance = this; }
        public override void OnDestroy() { if (Instance == this) Instance = null; base.OnDestroy(); }

        /// Jugador lo bastante cerca y AFUERA de la camioneta (no se cierra con alguien adentro de la abertura).
        public bool InRange(Vector3 playerPos) =>
            Vector3.Distance(new Vector3(playerPos.x, closePoint.y, playerPos.z), closePoint) <= closeRange && playerPos.z > closePoint.z + 0.35f;

        static readonly Collider[] buf = new Collider[16];
        /// Algo en la franja de barrido de las hojas: jugadores, carrito, tio u objetos.
        /// Donde esta lo que bloqueo la abertura en la ultima consulta (para marcarlo en pantalla); null si estaba libre.
        public Vector3? BlockPoint { get; private set; }
        public bool DoorwayBlocked(out string what)
        {
            what = ""; BlockPoint = null;
            int n = Physics.OverlapBoxNonAlloc(doorwayCenter, doorwayHalf, buf, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = buf[i];
                if (c.transform.IsChildOf(transform)) continue; // las propias hojas
                var rb = c.attachedRigidbody;
                if (!rb) continue;                              // piso, rampa y paredes no bloquean
                BlockPoint = c.bounds.center;
                if (rb.GetComponent<PlayerMotor>()) { what = "Hay alguien en la puerta"; return true; }
                if (NpcBody.Of(rb)) { what = $"Le queda {NpcBody.PartLabel(rb)} afuera: empujalo adentro"; return true; }
                what = $"{rb.name} bloquea la puerta"; return true;
            }
            return false;
        }

        /// Condiciones finales (sin el tiempo de pose): tio adentro, sin solapes y abertura libre.
        string ConditionsFail()
        {
            var zone = ExtractionZone.Instance;
            if (!zone) return "Sin zona";
            if (!zone.AllPointsInside) return "El tio no esta adentro";
            if (zone.SolidOverlap) return "Esta trabado con algo: acomodalo";
            if (DoorwayBlocked(out string what)) return what;
            return "";
        }

        /// Motivo por el que no se puede cerrar ("" = se puede). En clientes es una aproximacion con estado replicado.
        public string BlockReason()
        {
            var zone = ExtractionZone.Instance;
            BlockPoint = null;
            if (!zone || zone.Secured) return "";
            if (CurrentPhase == Phase.Closing) return "Cerrando...";
            string f = ConditionsFail(); if (f != "") return f;
            if (!zone.PoseReady) return $"Acomodandolo... {zone.Progress01 * 100f:0}%";
            return "";
        }

        /// Anfitrion: un jugador mantiene E este frame. El progreso avanza UNA vez por frame aunque varios jugadores
        /// mantengan E (no se acelera por acumulacion). Devuelve el progreso 0..1.
        public float HoldClose(PlayerHands who, float dt)
        {
            if (!NetAuth.IsServer || phase == Phase.Closing || phase == Phase.Closed) return progress;
            if (BlockReason() != "") { progress = 0f; phase = Phase.Open; return 0f; }
            lastHold = Time.time;
            if (lastHoldFrame == Time.frameCount) return progress;
            lastHoldFrame = Time.frameCount;
            phase = Phase.Holding;
            progress += dt / CloseHold;
            if (progress >= 1f)
            {
                progress = 1f; phase = Phase.Closing;
                Debug.Log($"[Van] cierre iniciado por {(who ? who.OwnerName : "?")}");
            }
            return progress;
        }

        void Update()
        {
            if (NetAuth.IsServer) ServerTick();
            float c = Closed01;
            if (leftLeaf) leftLeaf.localRotation = Quaternion.Euler(0f, -openAngle * (1f - c), 0f);
            if (rightLeaf) rightLeaf.localRotation = Quaternion.Euler(0f, openAngle * (1f - c), 0f);
        }

        void ServerTick()
        {
            var zone = ExtractionZone.Instance;
            switch (phase)
            {
                case Phase.Holding:
                    if (Time.time - lastHold > 0.15f) { progress = 0f; phase = Phase.Open; } // soltar E corta el cierre
                    break;
                case Phase.Closing:
                    string fail = ConditionsFail();
                    if (fail != "")
                    {   // se movio el tio, alguien entro en la abertura o aparecio un obstaculo: reabrir
                        Aborts++; Debug.Log($"[Van] cierre cancelado: {fail}");
                        phase = Phase.Open; progress = 0f;
                        break;
                    }
                    if (closed01 >= 1f)
                    {   // hojas efectivamente cerradas y condiciones vigentes: recien ahora se asegura
                        phase = Phase.Closed;
                        Debug.Log("[Van] puertas cerradas");
                        if (zone) zone.Secure();
                    }
                    break;
            }
            float target = phase == Phase.Closing || phase == Phase.Closed ? 1f : 0f;
            closed01 = Mathf.MoveTowards(closed01, target, Time.deltaTime / SwingTime);
            if (IsSpawned)
            {
                if (Mathf.Abs(nClosed.Value - closed01) > 0.02f || (closed01 == 1f && nClosed.Value != 1f) || (closed01 == 0f && nClosed.Value != 0f)) nClosed.Value = closed01;
                if (Mathf.Abs(nProgress.Value - progress) > 0.02f || (progress == 0f && nProgress.Value != 0f) || (progress == 1f && nProgress.Value != 1f)) nProgress.Value = progress;
                if (nPhase.Value != phase) nPhase.Value = phase;
            }
        }

        public void ResetState() { progress = 0f; closed01 = 0f; phase = Phase.Open; lastHold = -99f; lastHoldFrame = -1; }
    }
}
