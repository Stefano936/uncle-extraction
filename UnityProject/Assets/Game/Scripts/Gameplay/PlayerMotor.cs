using UnityEngine;

namespace UncleExtraction
{
    public enum PlayerState { Active, Stumble, Fallen, GettingUp }

    /// Capsula dinamica con rotacion bloqueada (especificacion 03). Mueve por aceleracion limitada, nunca escribiendo transform.position.
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMotor : MonoBehaviour, IResettable
    {
        public Transform cameraPivot;
        public float acceleration = 16f;      // m/s^2 en suelo
        public float airControl = 0.25f;
        public float mouseSensitivity = 0.12f;
        public float standEyeHeight = 1.62f, crouchEyeHeight = 1.05f;
        public float getUpDuration = 0.6f;
        public string playerName = "Jugador 1";

        public PlayerState State { get; private set; } = PlayerState.Active;
        public float Composure { get; private set; } = LabBalance.ComposureMax;
        public float Energy { get; private set; } = LabBalance.EnergyMax;
        public bool Crouching { get; private set; }
        public bool Invulnerable => Time.time < invulnUntil;
        public float StateTimeLeft => Mathf.Max(0f, stateUntil - Time.time);
        public float DodgeCooldownLeft => Mathf.Max(0f, dodgeReadyAt - Time.time);
        public Rigidbody Body => rb;
        public bool ControlsEnabled => State == PlayerState.Active;
        public int FallCount { get; private set; }

        /// Entrada simulada para pruebas automaticas (null = usar teclado/raton).
        public Vector2? DebugMove;

        Rigidbody rb; CapsuleCollider col; PlayerHands hands;
        float yaw, pitch, invulnUntil, stateUntil, composureRegenAt, energyRegenAt, dodgeReadyAt, dodgeUntil;
        float roll; Vector3 spawnPos; Quaternion spawnRot;

        void Awake()
        {
            rb = GetComponent<Rigidbody>(); col = GetComponent<CapsuleCollider>(); hands = GetComponent<PlayerHands>();
            rb.mass = LabBalance.PlayerMass; rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            col.height = LabBalance.CapsuleHeight; col.radius = LabBalance.CapsuleRadius; col.center = new Vector3(0, LabBalance.CapsuleHeight * 0.5f, 0);
            yaw = transform.eulerAngles.y; spawnPos = transform.position; spawnRot = transform.rotation;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool canLook = State == PlayerState.Active || State == PlayerState.Stumble;
            if (canLook && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = LabInput.Look() * mouseSensitivity;
                yaw += look.x; pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            }

            if (State == PlayerState.Active)
            {
                bool wantCrouch = LabInput.Held(Act.Crouch);
                if (wantCrouch != Crouching) SetCrouch(wantCrouch);
                if (LabInput.Pressed(Act.Dodge)) TryDodge();
            }
            UpdateStateTimers();
            UpdateRegen(dt);
            UpdateCamera(dt);
        }

        void FixedUpdate()
        {
            rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
            if (Time.time < dodgeUntil) return;

            Vector2 input = State == PlayerState.Active ? (DebugMove ?? LabInput.Move()) : Vector2.zero;
            float speed = Crouching ? LabBalance.CrouchSpeed : (hands != null && hands.AnyHeld ? LabBalance.LoadedSpeed : LabBalance.WalkSpeed);
            Vector3 wish = (Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y)) * speed;
            Vector3 v = rb.linearVelocity; Vector3 hv = new Vector3(v.x, 0, v.z);
            bool grounded = IsGrounded();
            float accel = acceleration * (grounded ? 1f : airControl);
            if (State == PlayerState.Fallen) accel *= 0.5f; // friccion de suelo mientras esta caido
            Vector3 dv = Vector3.ClampMagnitude(wish - hv, accel * Time.fixedDeltaTime);
            rb.AddForce(dv, ForceMode.VelocityChange);
        }

        bool IsGrounded()
        {
            float r = col.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (r + 0.05f);
            return Physics.SphereCast(origin, r, Vector3.down, out _, 0.12f, ~0, QueryTriggerInteraction.Ignore);
        }

        void SetCrouch(bool on)
        {
            if (!on)
            {   // levantarse solo con espacio libre
                Vector3 a = transform.position + Vector3.up * (col.radius + 0.05f);
                Vector3 b = transform.position + Vector3.up * (LabBalance.CapsuleHeight - col.radius);
                if (Physics.CheckCapsule(a, b, col.radius * 0.95f, ~0, QueryTriggerInteraction.Ignore) && OverlapsOther(a, b)) return;
            }
            Crouching = on;
            float h = on ? 1.2f : LabBalance.CapsuleHeight;
            col.height = h; col.center = new Vector3(0, h * 0.5f, 0);
        }

        bool OverlapsOther(Vector3 a, Vector3 b)
        {
            foreach (var c in Physics.OverlapCapsule(a, b, col.radius * 0.95f, ~0, QueryTriggerInteraction.Ignore))
                if (c.attachedRigidbody != rb) return true;
            return false;
        }

        void TryDodge()
        {
            if (Time.time < dodgeReadyAt) return;
            Vector2 input = DebugMove ?? LabInput.Move();
            if (input.sqrMagnitude < 0.01f) return; // el paso evasivo necesita direccion
            Vector3 dir = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y).normalized;
            float dist = LabBalance.DodgeDistance;
            if (rb.SweepTest(dir, out RaycastHit hit, dist, QueryTriggerInteraction.Ignore)) dist = Mathf.Max(0f, hit.distance - 0.05f);
            const float dur = 0.18f;
            rb.linearVelocity = dir * (dist / dur) + Vector3.up * rb.linearVelocity.y;
            dodgeUntil = Time.time + dur; dodgeReadyAt = Time.time + LabBalance.DodgeCooldown;
        }

        /// Impacto de un ataque o empujon. Devuelve true si afecto al jugador.
        public bool ApplyImpact(Vector3 deltaV, float composureLoss, string source)
        {
            if (Invulnerable || State == PlayerState.Fallen || State == PlayerState.GettingUp) return false;
            rb.AddForce(deltaV, ForceMode.VelocityChange);
            Composure = Mathf.Max(0f, Composure - composureLoss);
            composureRegenAt = Time.time + LabBalance.ComposureRegenDelay;
            float dvHoriz = new Vector3(deltaV.x, 0, deltaV.z).magnitude;
            bool knockdown = Composure <= 0f || dvHoriz >= LabBalance.DirectKnockdownDeltaV;
            Debug.Log($"[Player] {playerName} impact from {source}: dv={dvHoriz:0.00} composure={Composure:0} knockdown={knockdown}");
            if (knockdown) Fall(source);
            else { State = PlayerState.Stumble; stateUntil = Time.time + LabBalance.MinorStumbleLock; }
            return true;
        }

        public void Fall(string source)
        {
            if (State == PlayerState.Fallen) return;
            FallCount++;
            if (hands) hands.ReleaseAll(ReleaseReason.Fell);
            if (Crouching) SetCrouch(false);
            State = PlayerState.Fallen;
            stateUntil = Time.time + LabBalance.FallLockMax;
            Debug.Log($"[Player] {playerName} FALL ({source})");
        }

        void UpdateStateTimers()
        {
            if (State == PlayerState.Stumble && Time.time >= stateUntil) State = PlayerState.Active;
            else if (State == PlayerState.Fallen && Time.time >= stateUntil)
            {
                FindSafeStandPosition();
                State = PlayerState.GettingUp; stateUntil = Time.time + getUpDuration;
            }
            else if (State == PlayerState.GettingUp && Time.time >= stateUntil)
            {
                State = PlayerState.Active;
                invulnUntil = Time.time + LabBalance.StandupInvulnerability;
                Composure = Mathf.Max(Composure, LabBalance.ComposureMax * 0.6f); // decision de laboratorio, ver DECISIONES_IMPLEMENTACION
            }
        }

        void FindSafeStandPosition()
        {
            Vector3 basePos = transform.position;
            if (FreeAt(basePos)) return;
            for (float r = 0.5f; r <= 2.0f; r += 0.5f)
                for (int i = 0; i < 8; i++)
                {
                    Vector3 p = basePos + Quaternion.Euler(0, i * 45f, 0) * Vector3.forward * r;
                    if (FreeAt(p)) { rb.position = p; Debug.Log($"[Player] getup relocated {r:0.0}m"); return; }
                }
            Debug.LogWarning("[Player] getup: no free capsule nearby; staying");
        }

        bool FreeAt(Vector3 p)
        {
            Vector3 a = p + Vector3.up * (col.radius + 0.05f), b = p + Vector3.up * (LabBalance.CapsuleHeight - col.radius);
            foreach (var c in Physics.OverlapCapsule(a, b, col.radius * 0.95f, ~0, QueryTriggerInteraction.Ignore))
                if (c.attachedRigidbody != rb) return false;
            return true;
        }

        void UpdateRegen(float dt)
        {
            if (Time.time >= composureRegenAt && State != PlayerState.Fallen)
                Composure = Mathf.Min(LabBalance.ComposureMax, Composure + LabBalance.ComposureRegenPerS * dt);
            if (Time.time >= energyRegenAt)
                Energy = Mathf.Min(LabBalance.EnergyMax, Energy + LabBalance.EnergyRegenPerS * dt);
        }

        /// Consume energia para el esfuerzo. Devuelve false si no queda.
        public bool ConsumeEffort(float dt)
        {
            if (Energy <= 0f) return false;
            Energy = Mathf.Max(0f, Energy - LabBalance.EffortEnergyPerS * dt);
            energyRegenAt = Time.time + LabBalance.EnergyRegenDelay;
            return true;
        }

        void UpdateCamera(float dt)
        {
            if (!cameraPivot) return;
            float targetRoll = State == PlayerState.Fallen ? 65f : 0f;
            float targetEye = State == PlayerState.Fallen ? 0.45f : (Crouching ? crouchEyeHeight : standEyeHeight);
            roll = Mathf.MoveTowards(roll, targetRoll, 260f * dt);
            Vector3 lp = cameraPivot.localPosition;
            lp.y = Mathf.MoveTowards(lp.y, targetEye, 4f * dt);
            cameraPivot.localPosition = lp;
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }

        public void ResetState()
        {
            if (hands) hands.ReleaseAll(ReleaseReason.Reset);
            State = PlayerState.Active; Composure = LabBalance.ComposureMax; Energy = LabBalance.EnergyMax;
            invulnUntil = stateUntil = composureRegenAt = energyRegenAt = dodgeReadyAt = dodgeUntil = 0f;
            if (Crouching) SetCrouch(false);
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos; rb.rotation = spawnRot; transform.SetPositionAndRotation(spawnPos, spawnRot);
            yaw = spawnRot.eulerAngles.y; pitch = 0f; roll = 0f; DebugMove = null;
        }

        public void SetYawPitch(float newYaw, float newPitch) { yaw = newYaw; pitch = newPitch; }
    }
}
