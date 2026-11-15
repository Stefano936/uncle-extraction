using UnityEngine;

namespace UncleExtraction
{
    public enum NpcPosture { Upright, Fallen, Carried }

    /// Cuerpo central simplificado del tio (especificacion 04: "el prototipo comienza con un cuerpo central").
    /// Masa 110 kg constante. Equilibrio con torque limitado: tirones fuertes o bajos lo derriban.
    [RequireComponent(typeof(Rigidbody))]
    public class NpcBody : MonoBehaviour, IResettable
    {
        public Transform pelvis, torso, shoulderL, shoulderR, head;
        public float balanceKp = 60f, balanceKd = 12f;
        public float balanceAccelCap = 18f;     // rad/s^2: limita cuanto aguanta parado frente a tirones
        public float fallAngleDeg = 45f;
        public float standUpDelay = 2.5f;
        public float walkAccel = 6f;

        public NpcPosture Posture { get; private set; } = NpcPosture.Upright;
        public Rigidbody Body => rb;
        public float TiltDeg => Vector3.Angle(transform.up, Vector3.up);
        public Vector3 Forward { get { var f = transform.forward; f.y = 0; return f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward; } }
        public Vector3 ChestPosition => torso ? torso.position : transform.position + Vector3.up * 1.3f;
        public int FallCount { get; private set; }

        // Ordenes del cerebro (se aplican en FixedUpdate)
        [System.NonSerialized] public Vector3 desiredWalkVelocity;
        [System.NonSerialized] public Vector3 desiredFacing;
        [System.NonSerialized] public Vector3 resistForce;

        Rigidbody rb; float fallenSince; Vector3 spawnPos; Quaternion spawnRot;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.mass = LabBalance.NpcMass;
            rb.maxAngularVelocity = LabBalance.NpcMaxAngularVel;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.centerOfMass = new Vector3(0f, 0.85f, 0f);
            spawnPos = transform.position; spawnRot = transform.rotation;
        }

        void FixedUpdate()
        {
            // Salvaguardas de velocidad (especificacion 04)
            if (rb.linearVelocity.magnitude > LabBalance.NpcMaxLinearVel) rb.linearVelocity = rb.linearVelocity.normalized * LabBalance.NpcMaxLinearVel;

            if (Posture == NpcPosture.Upright)
            {
                Vector3 axis = Vector3.Cross(transform.up, Vector3.up);
                float ang = Vector3.Angle(transform.up, Vector3.up) * Mathf.Deg2Rad;
                Vector3 corr = (axis.sqrMagnitude > 1e-6f ? axis.normalized * ang * balanceKp : Vector3.zero);
                Vector3 angVel = rb.angularVelocity; Vector3 tiltVel = new Vector3(angVel.x, 0, angVel.z);
                Vector3 torque = corr - tiltVel * balanceKd;
                // giro (yaw) hacia la direccion deseada
                if (desiredFacing.sqrMagnitude > 0.01f)
                {
                    float yawErr = Vector3.SignedAngle(Forward, desiredFacing, Vector3.up) * Mathf.Deg2Rad;
                    torque += Vector3.up * (yawErr * 25f - angVel.y * 6f);
                }
                rb.AddTorque(Vector3.ClampMagnitude(torque, balanceAccelCap), ForceMode.Acceleration);

                if (TiltDeg > fallAngleDeg) SetFallen("tilt");

                Vector3 hv = rb.linearVelocity; hv.y = 0f;
                Vector3 want = Vector3.ClampMagnitude(desiredWalkVelocity, LabBalance.EscapeSpeedCap);
                if (want.sqrMagnitude > 0.0001f)
                    rb.AddForce(Vector3.ClampMagnitude(want - hv, walkAccel * Time.fixedDeltaTime), ForceMode.VelocityChange);
            }
            if (resistForce.sqrMagnitude > 0.01f && Posture != NpcPosture.Carried)
                rb.AddForceAtPosition(resistForce, pelvis ? pelvis.position : rb.worldCenterOfMass, ForceMode.Force);
        }

        public void SetFallen(string why)
        {
            if (Posture == NpcPosture.Fallen) return;
            Posture = NpcPosture.Fallen; fallenSince = Time.time; FallCount++;
            Debug.Log($"[Npc] fallen ({why})");
        }

        public void SetCarried(bool on)
        {
            if (on) Posture = NpcPosture.Carried;
            else if (Posture == NpcPosture.Carried) { Posture = NpcPosture.Fallen; fallenSince = Time.time; }
        }

        public bool CanTryStandUp => Posture == NpcPosture.Fallen && Time.time - fallenSince >= standUpDelay;

        /// Se levanta si hay espacio. Simplificacion de prototipo: recoloca la capsula de pie (sin animacion).
        public bool TryStandUp()
        {
            if (!CanTryStandUp) return false;
            Vector3 p = rb.worldCenterOfMass; p.y = transform.position.y;
            if (Physics.Raycast(rb.worldCenterOfMass + Vector3.up * 0.5f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            Vector3 a = p + Vector3.up * 0.4f, b = p + Vector3.up * 1.6f;
            foreach (var c in Physics.OverlapCapsule(a, b, 0.3f, ~0, QueryTriggerInteraction.Ignore))
                if (c.attachedRigidbody != rb && !c.isTrigger) { fallenSince = Time.time - standUpDelay + 0.5f; return false; }
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.position = p; rb.rotation = Quaternion.LookRotation(Forward, Vector3.up);
            Posture = NpcPosture.Upright;
            Debug.Log("[Npc] stood up");
            return true;
        }

        public void ResetState()
        {
            Posture = NpcPosture.Upright; desiredWalkVelocity = desiredFacing = resistForce = Vector3.zero;
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos; rb.rotation = spawnRot; transform.SetPositionAndRotation(spawnPos, spawnRot);
        }
    }
}
