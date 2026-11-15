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
            // La correa lo acuesta a lo largo de la caja (cabeza hacia el frente, boca arriba) y bloquea su rotacion:
            // con rotacion libre quedaba atravesado (1,9 m) y no pasaba por la puerta de 1,5 m (decision M1, 2026-11-15).
            var nb = npc.Body;
            Vector3 bedPt = transform.TransformPoint(new Vector3(0f, 0.72f, 0f));
            Quaternion lying = Quaternion.LookRotation(Vector3.up, transform.forward);
            Vector3 pelvisLocal = npc.pelvis ? npc.transform.InverseTransformPoint(npc.pelvis.position) : new Vector3(0f, 0.95f, 0f);
            nb.linearVelocity = Vector3.zero; nb.angularVelocity = Vector3.zero;
            nb.rotation = lying; nb.position = bedPt - lying * pelvisLocal;
            npc.transform.SetPositionAndRotation(nb.position, nb.rotation);

            strap = gameObject.AddComponent<ConfigurableJoint>();
            strap.connectedBody = nb;
            strap.autoConfigureConnectedAnchor = false;
            strap.anchor = transform.InverseTransformPoint(bedPt);
            strap.connectedAnchor = pelvisLocal;
            strap.xMotion = strap.yMotion = strap.zMotion = ConfigurableJointMotion.Limited;
            strap.angularXMotion = strap.angularYMotion = strap.angularZMotion = ConfigurableJointMotion.Locked;
            strap.linearLimit = new SoftJointLimit { limit = 0.08f };
            strap.linearLimitSpring = new SoftJointLimitSpring { spring = 2500f, damper = 250f };
            strap.enableCollision = false; // el tio acostado se superpone con las barandas del carrito de prototipo
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
