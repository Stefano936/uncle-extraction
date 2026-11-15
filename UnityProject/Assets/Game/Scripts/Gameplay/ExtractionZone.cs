using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    /// Zona de la camioneta: los puntos requeridos (torso, pelvis, hombros) deben estar dentro 1,5 s seguidos.
    /// Pendiente: comprobacion de solapamiento con solidos y cierre de puerta (balance.extraction).
    [RequireComponent(typeof(BoxCollider))]
    public class ExtractionZone : NetworkBehaviour, IResettable
    {
        public NpcBody npc;
        readonly NetworkVariable<float> nProgress = new NetworkVariable<float>();
        readonly NetworkVariable<bool> nSecured = new NetworkVariable<bool>();
        bool Mirror => IsSpawned && !IsServer;
        bool secured;
        public float Progress01 => Mirror ? nProgress.Value : Mathf.Clamp01(held / LabBalance.ExtractionHold);
        public bool Secured { get => Mirror ? nSecured.Value : secured; private set => secured = value; }
        BoxCollider box; float held;

        void Awake() { box = GetComponent<BoxCollider>(); box.isTrigger = true; }

        bool Inside(Transform t)
        {
            if (!t) return false;
            Vector3 local = transform.InverseTransformPoint(t.position) - box.center;
            Vector3 h = box.size * 0.5f;
            return Mathf.Abs(local.x) <= h.x && Mathf.Abs(local.y) <= h.y && Mathf.Abs(local.z) <= h.z;
        }

        public bool AllPointsInside => npc && Inside(npc.torso) && Inside(npc.pelvis) && Inside(npc.shoulderL) && Inside(npc.shoulderR);

        void FixedUpdate()
        {
            if (!NetAuth.IsServer) return;
            if (IsSpawned)
            {
                float p = Mathf.Clamp01(held / LabBalance.ExtractionHold);
                if (Mathf.Abs(nProgress.Value - p) > 0.02f || (p == 0f && nProgress.Value != 0f)) nProgress.Value = p;
                if (nSecured.Value != secured) nSecured.Value = secured;
            }
            if (Secured || !npc) return;
            held = AllPointsInside ? held + Time.fixedDeltaTime : 0f;
            if (held >= LabBalance.ExtractionHold)
            {
                Secured = true;
                GrabRegistry.ReleaseAllOnBody(npc.Body, ReleaseReason.Secured);
                if (NpcBrain.Instance) NpcBrain.Instance.Secure();
                if (RoundDirector.Instance) RoundDirector.Instance.OnExtracted();
                Debug.Log("[Extraction] SECURED");
            }
        }

        public void ResetState() { Secured = false; held = 0f; }
    }
}
