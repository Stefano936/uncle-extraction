using UnityEngine;

namespace UncleExtraction
{
    /// Carrito de transporte: herramienta principal del modo individual (GDD 05).
    /// Asa agarrable con dos manos; correa de retencion que el tio puede intentar soltar tras una senal.
    [RequireComponent(typeof(Rigidbody))]
    public class TransportCart : MonoBehaviour, IResettable
    {
        public static TransportCart Instance { get; private set; }
        public BoxCollider bed;              // volumen de carga (trigger o referencia)
        public NpcBody npc;
        public Transform strapPoint;

        public bool IsStrapped => strap != null;
        public int StrapCount { get; private set; }
        ConfigurableJoint strap; Rigidbody rb; Vector3 spawnPos; Quaternion spawnRot;

        void Awake() { Instance = this; rb = GetComponent<Rigidbody>(); spawnPos = transform.position; spawnRot = transform.rotation; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public bool NpcInBed
        {
            get
            {
                if (!bed || !npc) return false;
                Vector3 p = npc.pelvis ? npc.pelvis.position : npc.transform.position;
                Vector3 local = bed.transform.InverseTransformPoint(p) - bed.center;
                Vector3 h = bed.size * 0.5f + new Vector3(0.9f, 0.8f, 0.9f); // "cerca de la caja": la correa lo sube
                return Mathf.Abs(local.x) <= h.x && Mathf.Abs(local.y) <= h.y && Mathf.Abs(local.z) <= h.z;
            }
        }
        public bool CanStrap => !IsStrapped && NpcInBed && NpcBrain.Instance && NpcBrain.Instance.State != NpcState.Secured;

        public bool Strap()
        {
            if (!CanStrap) return false;
            if (NpcBrain.Instance && NpcBrain.Instance.IsAnchored) return false;
            strap = gameObject.AddComponent<ConfigurableJoint>();
            strap.connectedBody = npc.Body;
            strap.autoConfigureConnectedAnchor = false;
            Vector3 anchorWorld = strapPoint ? strapPoint.position : bed.transform.TransformPoint(bed.center);
            strap.anchor = transform.InverseTransformPoint(anchorWorld);
            strap.connectedAnchor = npc.Body.transform.InverseTransformPoint(npc.pelvis ? npc.pelvis.position : npc.transform.position);
            strap.xMotion = strap.yMotion = strap.zMotion = ConfigurableJointMotion.Limited;
            strap.linearLimit = new SoftJointLimit { limit = 0.15f };
            strap.linearLimitSpring = new SoftJointLimitSpring { spring = 2500f, damper = 250f }; // lo acerca de forma progresiva
            strap.enableCollision = true;
            StrapCount++;
            Debug.Log("[Cart] strapped");
            return true;
        }

        public void Unstrap(string why)
        {
            if (strap) { Destroy(strap); Debug.Log($"[Cart] unstrapped ({why})"); }
            strap = null;
        }

        public void ResetState()
        {
            if (strap) DestroyImmediate(strap); strap = null;
            foreach (var j in GetComponents<ConfigurableJoint>()) DestroyImmediate(j);
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos; rb.rotation = spawnRot; transform.SetPositionAndRotation(spawnPos, spawnRot);
        }
    }
}
