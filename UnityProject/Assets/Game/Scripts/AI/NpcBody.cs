using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    public enum NpcPosture { Upright, Fallen, Carried }

    /// Cuerpo del tio. Dos variantes con el mismo contrato:
    /// - Central (respaldo estable): un Rigidbody de 110 kg con equilibrio por torque limitado.
    /// - Articulado (especificacion 04): la raiz es la pelvis y 10 RagdollPart unidas por ConfigurableJoint; masa total
    ///   110 kg repartida. De pie, un sosten vertical de la pelvis + el mismo equilibrio por torque (prototipo de
    ///   "ragdoll activo"); caido, articulaciones flojas. Las colisiones internas se ignoran.
    /// Todo el codigo de juego usa este contrato (Owns, GrabCount, ReleaseAllGrabs, TeleportTo, AllBodies, Colliders),
    /// nunca "un solo Rigidbody". Fisica solo en el anfitrion; los clientes reciben la pose (NetworkTransform por parte).
    [RequireComponent(typeof(Rigidbody))]
    public class NpcBody : NetworkBehaviour, IResettable
    {
        public Transform pelvis, torso, shoulderL, shoulderR, head;
        public float balanceKp = 60f, balanceKd = 12f;
        public float balanceAccelCap = 18f;     // rad/s^2: limita cuanto aguanta parado frente a tirones
        public float fallAngleDeg = 45f;
        public float standUpDelay = 2.5f;
        public float walkAccel = 6f;
        [Header("Articulado (vacio = cuerpo central)")]
        public RagdollPart[] parts = new RagdollPart[0];
        public float footFriction = 0.42f; // mu de los pies sostenidos por el resorte: promedio tio 0,25 / piso 0,6, como el central apoyado
        public float hoverHeight = 0.92f, hoverK = 9000f, hoverC = 900f; // altura del centro de la pelvis de pie (m) y resorte (N/m, N*s/m)

        readonly NetworkVariable<NpcPosture> nPosture = new NetworkVariable<NpcPosture>(NpcPosture.Upright);
        readonly NetworkVariable<int> nFalls = new NetworkVariable<int>();
        bool Mirror => IsSpawned && !IsServer;
        NpcPosture posture = NpcPosture.Upright;
        public NpcPosture Posture { get => Mirror ? nPosture.Value : posture; private set => posture = value; }
        public Rigidbody Body => rb;
        public bool Articulated => parts != null && parts.Length > 0;
        public float TiltDeg => Vector3.Angle(UpAxis, Vector3.up);
        Vector3 UpAxis => chest ? chest.transform.up : transform.up;
        public Vector3 Forward { get { var f = transform.forward; f.y = 0; return f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward; } }
        public Vector3 ChestPosition => torso ? torso.position : transform.position + Vector3.up * 1.3f;
        int fallCount;
        public int FallCount { get => Mirror ? nFalls.Value : fallCount; private set => fallCount = value; }
        public float TotalMass { get { float m = 0; foreach (var b in AllBodies) m += b.mass; return m; } }

        // Ordenes del cerebro (se aplican en FixedUpdate)
        [System.NonSerialized] public Vector3 desiredWalkVelocity;
        [System.NonSerialized] public Vector3 desiredFacing;
        [System.NonSerialized] public Vector3 resistForce;

        Rigidbody rb; RagdollPart chest; float fallenSince; float wholeTiltInertia; // kg*m^2 de todo el cuerpo (articulado)
        Vector3 spawnPos; Quaternion spawnRot;
        readonly List<Rigidbody> bodies = new List<Rigidbody>();
        readonly List<Collider> colliders = new List<Collider>();
        public IReadOnlyList<Rigidbody> AllBodies => bodies;
        public IReadOnlyList<Collider> Colliders => colliders;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            bodies.Clear(); bodies.Add(rb);
            if (!Articulated)
            {
                rb.mass = LabBalance.NpcMass;
                rb.centerOfMass = new Vector3(0f, 0.85f, 0f);
            }
            rb.maxAngularVelocity = LabBalance.NpcMaxAngularVel;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            foreach (var p in parts)
            {
                if (!p) continue;
                var b = p.GetComponent<Rigidbody>(); bodies.Add(b);
                b.maxAngularVelocity = LabBalance.NpcMaxAngularVel; b.maxDepenetrationVelocity = 3f; b.solverIterations = 12;
                p.restLocalPos = transform.InverseTransformPoint(p.transform.position);
                p.restLocalRot = Quaternion.Inverse(transform.rotation) * p.transform.rotation;
                if (p.role == "chest") chest = p;
            }
            if (Articulated)
            {
                rb.maxDepenetrationVelocity = 3f; rb.solverIterations = 12;
                // Inercia de inclinacion de todo el cuerpo en reposo (respecto del centro de masa total)
                Vector3 com = Vector3.zero; float m = 0f;
                foreach (var b in bodies) { com += b.worldCenterOfMass * b.mass; m += b.mass; }
                com /= Mathf.Max(m, 1e-3f);
                wholeTiltInertia = 0f;
                foreach (var b in bodies) wholeTiltInertia += b.mass * (b.worldCenterOfMass - com).sqrMagnitude + (b.inertiaTensor.x + b.inertiaTensor.z) * 0.5f;
            }
            foreach (var b in bodies) foreach (var c in b.GetComponents<Collider>()) if (!c.isTrigger) colliders.Add(c);
            // Colisiones internas: ignoradas (evita impulsos explosivos entre partes vecinas y superpuestas)
            for (int i = 0; i < colliders.Count; i++) for (int j = i + 1; j < colliders.Count; j++) Physics.IgnoreCollision(colliders[i], colliders[j], true);
            spawnPos = transform.position; spawnRot = transform.rotation;
            SetStrength(true);
        }

        public override void OnNetworkSpawn()
        {   // clientes: presentan la pose del anfitrion, no simulan (evita simulaciones contradictorias)
            if (!IsServer) foreach (var b in bodies) if (b != rb) b.isKinematic = true;
        }

        // ---------- contrato comun ----------
        public bool Owns(Rigidbody b) => b && bodies.Contains(b);
        public static NpcBody Of(Rigidbody b) { if (!b) return null; var n = b.GetComponent<NpcBody>(); return n ? n : b.GetComponentInParent<NpcBody>(); }
        public int GrabCount() { int n = 0; foreach (var b in bodies) n += GrabRegistry.CountOnBody(b); return n; }
        public void ReleaseAllGrabs(ReleaseReason r) { foreach (var b in bodies) GrabRegistry.ReleaseAllOnBody(b, r); }
        public void OwnersOnBody(List<IGrabOwner> result)
        {
            result.Clear(); var tmp = new List<IGrabOwner>();
            foreach (var b in bodies) { GrabRegistry.OwnersOnBody(b, tmp); foreach (var o in tmp) if (!result.Contains(o)) result.Add(o); }
        }
        public void SetKinematic(bool on) { foreach (var b in bodies) b.isKinematic = on; }

        /// Coloca el cuerpo entero (todas las partes en su pose de reposo relativa a la raiz). Velocidades a cero.
        public void TeleportTo(Vector3 pos, Quaternion rot)
        {
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.position = pos; rb.rotation = rot; transform.SetPositionAndRotation(pos, rot);
            foreach (var p in parts)
            {
                if (!p) continue;
                var b = p.Body ? p.Body : p.GetComponent<Rigidbody>();
                Vector3 pp = pos + rot * p.restLocalPos; Quaternion pr = rot * p.restLocalRot;
                b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero;
                b.position = pp; b.rotation = pr; p.transform.SetPositionAndRotation(pp, pr);
            }
        }
        /// Mueve el cuerpo entero de forma cinematica (subida al carrito): raiz y partes conservan la pose de reposo.
        public void MoveKinematic(Vector3 pos, Quaternion rot)
        {
            rb.MovePosition(pos); rb.MoveRotation(rot);
            foreach (var p in parts) if (p) { var b = p.GetComponent<Rigidbody>(); b.MovePosition(pos + rot * p.restLocalPos); b.MoveRotation(rot * p.restLocalRot); }
        }

        void SetStrength(bool upright) { foreach (var p in parts) if (p) p.SetStrength(upright); }

        void FixedUpdate()
        {
            if (!NetAuth.IsServer) return; // fisica del tio: solo el anfitrion
            if (IsSpawned)
            {
                if (nPosture.Value != posture) nPosture.Value = posture;
                if (nFalls.Value != fallCount) nFalls.Value = fallCount;
            }
            // Salvaguardas de velocidad (especificacion 04), en todas las partes
            foreach (var b in bodies)
                if (!b.isKinematic && b.linearVelocity.magnitude > LabBalance.NpcMaxLinearVel) b.linearVelocity = b.linearVelocity.normalized * LabBalance.NpcMaxLinearVel;

            if (Posture == NpcPosture.Upright)
            {
                if (Articulated)
                {   // El par de equilibrio del cuerpo central actua sobre los 110 kg; repartido solo en pelvis y pecho (50 kg)
                    // con aceleracion fija, el articulado caia con un empujon leve. Mismo par efectivo: aceleracion x inercia total.
                    Balance(rb, wholeTiltInertia * 0.4f);
                    if (chest) Balance(chest.Body, wholeTiltInertia * 0.6f);
                }
                else Balance(rb, 0f);
                if (Articulated && !rb.isKinematic) Hover();
                if (TiltDeg > fallAngleDeg) SetFallen("tilt");

                Vector3 hv = rb.linearVelocity; hv.y = 0f;
                Vector3 want = Vector3.ClampMagnitude(desiredWalkVelocity, LabBalance.EscapeSpeedCap);
                if (want.sqrMagnitude > 0.0001f)
                {
                    Vector3 dv = Vector3.ClampMagnitude(want - hv, walkAccel * Time.fixedDeltaTime);
                    foreach (var b in bodies) if (!b.isKinematic) b.AddForce(dv, ForceMode.VelocityChange); // todo el cuerpo camina junto
                }
            }
            if (resistForce.sqrMagnitude > 0.01f && Posture != NpcPosture.Carried)
                rb.AddForceAtPosition(resistForce, pelvis ? pelvis.position : rb.worldCenterOfMass, ForceMode.Force);
        }

        /// inertia mayor que 0: aplica el par como fuerza con esa inercia (articulado); 0: aceleracion sobre el cuerpo (central).
        void Balance(Rigidbody b, float inertia)
        {
            if (!b || b.isKinematic) return;
            Vector3 up = b.transform.up;
            Vector3 axis = Vector3.Cross(up, Vector3.up);
            float ang = Vector3.Angle(up, Vector3.up) * Mathf.Deg2Rad;
            Vector3 corr = axis.sqrMagnitude > 1e-6f ? axis.normalized * ang * balanceKp : Vector3.zero;
            Vector3 angVel = b.angularVelocity; Vector3 tiltVel = new Vector3(angVel.x, 0, angVel.z);
            Vector3 torque = corr - tiltVel * balanceKd;
            if (desiredFacing.sqrMagnitude > 0.01f)
            {   // giro (yaw) hacia la direccion deseada
                float yawErr = Vector3.SignedAngle(Forward, desiredFacing, Vector3.up) * Mathf.Deg2Rad;
                torque += Vector3.up * (yawErr * 25f - angVel.y * 6f);
            }
            torque = Vector3.ClampMagnitude(torque, balanceAccelCap);
            if (inertia > 0f) b.AddTorque(torque * inertia, ForceMode.Force);
            else b.AddTorque(torque, ForceMode.Acceleration);
        }

        static readonly RaycastHit[] hoverHits = new RaycastHit[8];
        /// Sosten de la pelvis a la altura de pie (prototipo de ragdoll activo): resorte vertical acotado al peso total.
        void Hover()
        {
            Vector3 c = rb.worldCenterOfMass;
            int n = Physics.RaycastNonAlloc(c + Vector3.up * 0.2f, Vector3.down, hoverHits, hoverHeight + 0.8f, ~0, QueryTriggerInteraction.Ignore);
            float ground = float.NegativeInfinity;
            for (int i = 0; i < n; i++) if (!Owns(hoverHits[i].rigidbody) && hoverHits[i].point.y > ground) ground = hoverHits[i].point.y;
            if (float.IsNegativeInfinity(ground)) return; // sin piso debajo: que caiga
            float err = ground + hoverHeight - c.y;
            float f = Mathf.Clamp(hoverK * err - hoverC * rb.linearVelocity.y, 0f, TotalMass * 9.81f * 1.6f);
            rb.AddForce(Vector3.up * f, ForceMode.Force);
            // El resorte reemplaza el apoyo de los pies: aporta tambien su friccion (Coulomb, mu del material x fuerza que
            // sostiene). Sin esto, de pie se deslizaba como sobre hielo y arrastrarlo era mucho mas facil que al central.
            Vector3 hv = Vector3.zero; float m = 0f;
            foreach (var b in bodies) { hv += b.linearVelocity * b.mass; m += b.mass; }
            hv /= Mathf.Max(m, 1e-3f); hv.y = 0f;
            float speed = hv.magnitude;
            if (speed > 1e-3f && f > 0f)
            {
                float fr = Mathf.Min(footFriction * f, m * speed / Time.fixedDeltaTime);
                Vector3 df = -hv / speed * fr;
                foreach (var b in bodies) b.AddForce(df * (b.mass / m), ForceMode.Force); // repartida: no hace girar al cuerpo
            }
        }

        public void SetFallen(string why)
        {
            if (Posture == NpcPosture.Fallen) return;
            Posture = NpcPosture.Fallen; fallenSince = Time.time; FallCount++;
            SetStrength(false);
            Debug.Log($"[Npc] fallen ({why}) {Diag()}");
        }

        /// Diagnostico de caida: inclinacion, velocidades y jugadores cercanos (para separar empujones de inestabilidad propia).
        string Diag()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"tilt={TiltDeg:0} v={rb.linearVelocity.magnitude:0.00} w={rb.angularVelocity.magnitude:0.00} pos={rb.position}");
            foreach (var pm in PlayerMotor.All) if (pm) { float d = Vector3.Distance(pm.transform.position, rb.position); if (d < 2f) sb.Append($" jugador@{d:0.00}m"); }
            if (NpcBrain.Instance) sb.Append($" estado={NpcBrain.Instance.State} agarres={GrabCount()}");
            return sb.ToString();
        }

        public void SetCarried(bool on)
        {
            if (on) { if (Posture != NpcPosture.Carried) SetStrength(false); Posture = NpcPosture.Carried; }
            else if (Posture == NpcPosture.Carried) { Posture = NpcPosture.Fallen; fallenSince = Time.time; }
        }

        public bool CanTryStandUp => Posture == NpcPosture.Fallen && Time.time - fallenSince >= standUpDelay;

        /// Se levanta si hay espacio. Simplificacion de prototipo: recoloca el cuerpo de pie (sin animacion).
        static readonly RaycastHit[] standHits = new RaycastHit[12];
        bool StandSpaceFree(Vector3 p)
        {
            foreach (var c in Physics.OverlapCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.6f, 0.3f, ~0, QueryTriggerInteraction.Ignore))
                if (!Owns(c.attachedRigidbody) && !c.isTrigger) return false;
            return true;
        }

        public bool TryStandUp()
        {
            if (!CanTryStandUp) return false;
            // Lugar libre para pararse: debajo del centro de masa o, si ahi hay algo (baranda, mueble, pared), el punto libre
            // mas cercano hasta 0,8 m. Antes solo probaba el centro y, caido junto a la barra, no se levantaba nunca.
            Vector3 com = rb.worldCenterOfMass;
            Vector3 p = com; bool found = false;
            foreach (float r in new[] { 0f, 0.4f, 0.8f })
            {
                for (int k = 0; k < (r == 0f ? 1 : 8) && !found; k++)
                {
                    Vector3 c = com + Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward * r;
                    float gy = transform.position.y;
                    int n = Physics.RaycastNonAlloc(c + Vector3.up * 0.8f, Vector3.down, standHits, 3f, ~0, QueryTriggerInteraction.Ignore);
                    float best = float.MaxValue;
                    for (int i = 0; i < n; i++) if (!Owns(standHits[i].collider.attachedRigidbody) && standHits[i].distance < best) { best = standHits[i].distance; gy = standHits[i].point.y; }
                    if (best == float.MaxValue || gy > com.y + 0.3f) continue; // sin piso, o encima de un mueble
                    Vector3 cand = new Vector3(c.x, gy, c.z);
                    if (StandSpaceFree(cand)) { p = cand; found = true; }
                }
                if (found) break;
            }
            if (!found) { fallenSince = Time.time - standUpDelay + 0.5f; return false; }
            TeleportTo(p, Quaternion.LookRotation(Forward, Vector3.up));
            Posture = NpcPosture.Upright; SetStrength(true);
            Debug.Log("[Npc] stood up");
            return true;
        }

        public void ResetState()
        {
            Posture = NpcPosture.Upright; desiredWalkVelocity = desiredFacing = resistForce = Vector3.zero;
            SetKinematic(false);
            TeleportTo(spawnPos, spawnRot);
            SetStrength(true);
        }
    }
}
