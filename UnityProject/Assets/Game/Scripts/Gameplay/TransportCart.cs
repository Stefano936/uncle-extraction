using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    /// Carrito de transporte: herramienta principal del modo individual (GDD 05).
    /// Asa agarrable con dos manos; correa de retencion que el tio puede intentar soltar tras una senal.
    [RequireComponent(typeof(Rigidbody))]
    public class TransportCart : NetworkBehaviour, IResettable
    {
        public static TransportCart Instance { get; private set; }
        public BoxCollider bed;              // volumen de carga (trigger o referencia)
        public NpcBody npc;
        public Transform strapPoint;

        readonly NetworkVariable<bool> nStrapped = new NetworkVariable<bool>();
        readonly NetworkVariable<int> nStrapCount = new NetworkVariable<int>();
        bool Mirror => IsSpawned && !IsServer;
        int strapCount;
        public bool IsStrapped => Mirror ? nStrapped.Value : strap != null;
        public int StrapCount { get => Mirror ? nStrapCount.Value : strapCount; private set => strapCount = value; }
        ConfigurableJoint strap; Rigidbody rb; Vector3 spawnPos; Quaternion spawnRot;

        void Awake() { Instance = this; rb = GetComponent<Rigidbody>(); spawnPos = transform.position; spawnRot = transform.rotation; }
        public override void OnDestroy() { if (Instance == this) Instance = null; base.OnDestroy(); }

        void Update()
        {
            if (!IsSpawned || !IsServer) return;
            bool st = strap != null; if (nStrapped.Value != st) nStrapped.Value = st;
            if (nStrapCount.Value != strapCount) nStrapCount.Value = strapCount;
        }

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
        public bool CanStrap => !IsStrapped && !loading && NpcInBed && NpcBrain.Instance && NpcBrain.Instance.State != NpcState.Secured;

        public bool Strap()
        {
            if (!NetAuth.IsServer || !CanStrap) return false;
            if (NpcBrain.Instance && NpcBrain.Instance.IsAnchored) { LastFailure = "Primero soltale la mano de la barra"; return false; }
            if (loading) return false;
            // Carga comprensible (M1/M3): destino acostado a lo largo de la caja (cabeza hacia el frente, boca arriba),
            // validado contra paredes, jugadores y objetos; si no hay espacio, no se carga. Luego una subida corta de 0,45 s
            // (cinematica) y la correa bloquea su rotacion. Antes era una recolocacion instantanea sin validar: con un jugador
            // o una pared en el volumen, la fisica los despedia (incluso a traves de la pared).
            Vector3 bedPt = transform.TransformPoint(new Vector3(0f, 0.72f, 0f));
            Quaternion lying = Quaternion.LookRotation(Vector3.up, transform.forward);
            Vector3 pelvisLocal = npc.pelvis ? npc.transform.InverseTransformPoint(npc.pelvis.position) : new Vector3(0f, 0.95f, 0f);
            Vector3 rootPos = bedPt - lying * pelvisLocal;
            if (!LyingVolumeFree(rootPos, lying, out string blocker)) { LastFailure = "No hay lugar para acostarlo (" + blocker + ")"; Debug.Log($"[Cart] strap bloqueado por {blocker}"); return false; }
            GrabRegistry.ReleaseAllOnBody(npc.Body, ReleaseReason.Secured); // nadie lo sostiene mientras sube
            StartCoroutine(LoadRoutine(rootPos, lying, bedPt, pelvisLocal));
            LastFailure = "";
            return true;
        }

        public string LastFailure { get; private set; } = "";
        public bool IsLoading => loading;
        bool loading;
        static readonly Collider[] overlap = new Collider[16];

        bool LyingVolumeFree(Vector3 rootPos, Quaternion rot, out string blocker)
        {
            blocker = "";
            var cap = npc.GetComponent<CapsuleCollider>();
            float r = cap ? cap.radius : 0.33f, h = cap ? cap.height : 1.9f;
            Vector3 axis = rot * Vector3.up, c = rootPos + rot * (cap ? cap.center : new Vector3(0, 0.95f, 0));
            Vector3 p0 = c - axis * (h * 0.5f - r), p1 = c + axis * (h * 0.5f - r);
            int n = Physics.OverlapCapsuleNonAlloc(p0, p1, r * 0.95f, overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var col = overlap[i]; var arb = col.attachedRigidbody;
                if (arb == npc.Body || arb == rb) continue;
                blocker = arb && arb.GetComponent<PlayerMotor>() ? "un jugador" : arb ? arb.name : col.name; return false;
            }
            return true;
        }

        System.Collections.IEnumerator LoadRoutine(Vector3 rootPos, Quaternion lying, Vector3 bedPt, Vector3 pelvisLocal)
        {
            loading = true;
            var nb = npc.Body;
            IgnoreUncle(true); // el tio cinematico atravesaria las barandas y despediria al carrito
            Vector3 p0 = nb.position; Quaternion r0 = nb.rotation;
            nb.linearVelocity = Vector3.zero; nb.angularVelocity = Vector3.zero; nb.isKinematic = true;
            const float dur = 0.45f;
            for (float t = 0; t < dur; t += Time.fixedDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                // el destino sigue al carrito si se mueve durante la subida
                Vector3 bp = transform.TransformPoint(new Vector3(0f, 0.72f, 0f)); Quaternion lr = Quaternion.LookRotation(Vector3.up, transform.forward);
                nb.MovePosition(Vector3.Lerp(p0, bp - lr * pelvisLocal, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.25f);
                nb.MoveRotation(Quaternion.Slerp(r0, lr, k));
                yield return new WaitForFixedUpdate();
            }
            bedPt = transform.TransformPoint(new Vector3(0f, 0.72f, 0f)); lying = Quaternion.LookRotation(Vector3.up, transform.forward);
            nb.position = bedPt - lying * pelvisLocal; nb.rotation = lying;
            nb.isKinematic = false;
            loading = false;
            if (npc == null || (NpcBrain.Instance && NpcBrain.Instance.State == NpcState.Secured)) { IgnoreUncle(false); yield break; }

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
        }

        /// Colisiones tio-carrito: ignoradas mientras sube y esta asegurado (el prototipo se superpone con las barandas).
        void IgnoreUncle(bool ignore)
        {
            if (!npc) return;
            var mine = GetComponentsInChildren<Collider>(); var his = npc.GetComponentsInChildren<Collider>();
            foreach (var a in mine) foreach (var b in his) if (a && b) Physics.IgnoreCollision(a, b, ignore);
        }

        public void Unstrap(string why)
        {
            IgnoreUncle(false);
            if (strap) { Destroy(strap); Debug.Log($"[Cart] unstrapped ({why})"); }
            strap = null;
        }

        public void ResetState()
        {
            StopAllCoroutines(); loading = false; IgnoreUncle(false);
            if (npc && npc.Body) npc.Body.isKinematic = false;
            if (strap) DestroyImmediate(strap); strap = null;
            foreach (var j in GetComponents<ConfigurableJoint>()) DestroyImmediate(j);
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos; rb.rotation = spawnRot; transform.SetPositionAndRotation(spawnPos, spawnRot);
        }
    }
}
