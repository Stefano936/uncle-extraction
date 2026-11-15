using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    /// Volumen de carga DENTRO de la camioneta (GDD 03 «Victoria»; balance.extraction).
    /// Pose valida = torso, pelvis y hombros dentro del volumen + ninguna parte principal atravesando un solido.
    /// La pose debe sostenerse 1,5 s; despues un jugador cierra las puertas (VanDoors, 1 s) y recien ahi se asegura.
    /// Autoritativo: solo el anfitrion evalua; los clientes ven el estado replicado.
    [RequireComponent(typeof(BoxCollider))]
    public class ExtractionZone : NetworkBehaviour, IResettable
    {
        public static ExtractionZone Instance { get; private set; }
        public NpcBody npc;

        readonly NetworkVariable<float> nProgress = new NetworkVariable<float>();
        readonly NetworkVariable<bool> nSecured = new NetworkVariable<bool>();
        readonly NetworkVariable<bool> nPointsInside = new NetworkVariable<bool>();
        readonly NetworkVariable<bool> nSolidOverlap = new NetworkVariable<bool>();
        bool Mirror => IsSpawned && !IsServer;

        BoxCollider box; float held; bool secured, pointsInside, solidOverlap;

        /// Progreso de la pose valida (0..1 en 1,5 s). 1 = listo para cerrar.
        public float Progress01 => Mirror ? nProgress.Value : Mathf.Clamp01(held / LabBalance.ExtractionHold);
        public bool PoseReady => Progress01 >= 1f;
        public bool Secured { get => Mirror ? nSecured.Value : secured; private set => secured = value; }
        /// Puntos requeridos dentro (calculado localmente en clientes con poses replicadas; autoritativo en el anfitrion).
        public bool AllPointsInside => npc && Inside(npc.torso) && Inside(npc.pelvis) && Inside(npc.shoulderL) && Inside(npc.shoulderR);
        public bool SolidOverlap => Mirror ? nSolidOverlap.Value : solidOverlap;
        public bool PoseValid => Mirror ? nPointsInside.Value && !nSolidOverlap.Value : pointsInside && !solidOverlap;

        void Awake() { Instance = this; box = GetComponent<BoxCollider>(); box.isTrigger = true; }
        public override void OnDestroy() { if (Instance == this) Instance = null; base.OnDestroy(); }

        bool Inside(Transform t)
        {
            if (!t) return false;
            Vector3 local = transform.InverseTransformPoint(t.position) - box.center;
            Vector3 h = box.size * 0.5f;
            return Mathf.Abs(local.x) <= h.x && Mathf.Abs(local.y) <= h.y && Mathf.Abs(local.z) <= h.z;
        }

        static readonly Collider[] overlap = new Collider[16];
        /// Ninguna parte principal atravesando un solido: la capsula del tio (algo reducida) no se superpone con geometria
        /// estatica u objetos que no sean el propio tio, el carrito que lo lleva o los jugadores.
        bool OverlapsSolid(out string what)
        {
            what = "";
            var cap = npc ? npc.GetComponent<CapsuleCollider>() : null;
            if (!cap) return false;
            Transform t = npc.transform;
            float r = cap.radius * 0.8f, h = cap.height * 0.92f;
            Vector3 axis = t.up, c = t.TransformPoint(cap.center);
            Vector3 p0 = c - axis * (h * 0.5f - r), p1 = c + axis * (h * 0.5f - r);
            int n = Physics.OverlapCapsuleNonAlloc(p0, p1, r, overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var col = overlap[i]; var arb = col.attachedRigidbody;
                if (arb == npc.Body) continue;
                if (arb && (arb.GetComponent<PlayerMotor>() || arb.GetComponent<TransportCart>())) continue;
                if (VanDoors.Instance && col.transform.IsChildOf(VanDoors.Instance.transform)) continue; // hojas de la puerta
                what = arb ? arb.name : col.name; return true;
            }
            return false;
        }

        void FixedUpdate()
        {
            if (!NetAuth.IsServer || !npc) return;
            if (!secured)
            {
                pointsInside = AllPointsInside;
                solidOverlap = pointsInside && OverlapsSolid(out _);
                held = pointsInside && !solidOverlap ? held + Time.fixedDeltaTime : 0f;
            }
            if (IsSpawned)
            {
                float p = Mathf.Clamp01(held / LabBalance.ExtractionHold);
                if (Mathf.Abs(nProgress.Value - p) > 0.02f || (p == 0f && nProgress.Value != 0f) || (p == 1f && nProgress.Value != 1f)) nProgress.Value = p;
                if (nSecured.Value != secured) nSecured.Value = secured;
                if (nPointsInside.Value != pointsInside) nPointsInside.Value = pointsInside;
                if (nSolidOverlap.Value != solidOverlap) nSolidOverlap.Value = solidOverlap;
            }
        }

        /// Llamado por VanDoors al completar el cierre: una sola vez por intento (anfitrion).
        public void Secure()
        {
            if (!NetAuth.IsServer || secured) return;
            secured = true;
            GrabRegistry.ReleaseAllOnBody(npc.Body, ReleaseReason.Secured);
            if (TransportCart.Instance) TransportCart.Instance.Unstrap("asegurado");
            if (NpcBrain.Instance) NpcBrain.Instance.Secure();
            if (RoundDirector.Instance) RoundDirector.Instance.OnExtracted();
            Debug.Log("[Extraction] SECURED");
        }

        public void ResetState() { secured = false; held = 0f; pointsInside = solidOverlap = false; }
    }
}
