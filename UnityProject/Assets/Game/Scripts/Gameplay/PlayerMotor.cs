using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    public enum PlayerState { Active, Stumble, Fallen, GettingUp }

    /// Capsula dinamica con rotacion bloqueada (especificacion 03). Mueve por aceleracion limitada, nunca escribiendo transform.position.
    /// Red (especificacion 07): el anfitrion simula movimiento, compostura, caidas y recuperacion de TODOS los jugadores;
    /// el dueno solo mueve su camara (mirada) localmente; el resto ve el estado replicado.
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMotor : NetworkBehaviour, IResettable
    {
        public Transform cameraPivot;
        public float acceleration = 16f;      // m/s^2 en suelo
        public float airControl = 0.25f;
        public float mouseSensitivity = 0.12f;
        public float standEyeHeight = 1.62f, crouchEyeHeight = 1.05f;
        public float getUpDuration = 0.6f;
        public string playerName = "Jugador 1";


        // Estado replicado (escribe solo el anfitrion).
        readonly NetworkVariable<PlayerState> nState = new NetworkVariable<PlayerState>(PlayerState.Active);
        readonly NetworkVariable<Unity.Collections.FixedString32Bytes> nName = new NetworkVariable<Unity.Collections.FixedString32Bytes>();
        /// Nombre elegido en Opciones: lo sanea el anfitrion (sin etiquetas, maximo 20) y lo replica; vacio = «Jugador N».
        public static string Sanitize(string n)
        {
            if (string.IsNullOrWhiteSpace(n)) return "";
            n = n.Replace("<", "").Replace(">", "").Replace('\n', ' ').Trim();
            return n.Length > 20 ? n.Substring(0, 20) : n;
        }
        [Rpc(SendTo.Server)]
        void NameRpc(string name, RpcParams p = default) { if (p.Receive.SenderClientId == OwnerClientId) { string s = Sanitize(name); if (s != "") nName.Value = s; } }
        readonly NetworkVariable<float> nComposure = new NetworkVariable<float>(LabBalance.ComposureMax);
        readonly NetworkVariable<float> nEnergy = new NetworkVariable<float>(LabBalance.EnergyMax);
        readonly NetworkVariable<bool> nCrouch = new NetworkVariable<bool>(false);
        readonly NetworkVariable<float> nStateEnd = new NetworkVariable<float>(0f);
        readonly NetworkVariable<float> nDodgeReady = new NetworkVariable<float>(0f);
        readonly NetworkVariable<int> nFalls = new NetworkVariable<int>(0);

        bool Auth => NetAuth.IsServer;
        bool Mirror => IsSpawned && !IsServer;

        PlayerState state = PlayerState.Active; float composure = LabBalance.ComposureMax, energy = LabBalance.EnergyMax; bool crouching; int fallCount;

        public PlayerState State => Mirror ? nState.Value : state;
        public float Composure => Mirror ? nComposure.Value : composure;
        public float Energy => Mirror ? nEnergy.Value : energy;
        public bool Crouching => Mirror ? nCrouch.Value : crouching;
        public int FallCount => Mirror ? nFalls.Value : fallCount;
        public bool Invulnerable => Time.time < invulnUntil;
        public float StateTimeLeft => Mirror ? Mathf.Max(0f, nStateEnd.Value - NetAuth.ServerNow) : Mathf.Max(0f, stateUntil - Time.time);
        public float DodgeCooldownLeft => Mirror ? Mathf.Max(0f, nDodgeReady.Value - NetAuth.ServerNow) : Mathf.Max(0f, dodgeReadyAt - Time.time);
        public Rigidbody Body => rb;
        public bool ControlsEnabled => State == PlayerState.Active;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public PlayerControls Controls => controls;

        /// Entrada simulada para pruebas automaticas en el anfitrion (null = usar los controles del jugador).
        public Vector2? DebugMove;

        Rigidbody rb; CapsuleCollider col; PlayerHands hands; PlayerControls controls;
        float yaw, pitch, invulnUntil, stateUntil, composureRegenAt, energyRegenAt, dodgeReadyAt, dodgeUntil;
        float roll; Vector3 spawnPos; Quaternion spawnRot;

        /// Jugadores presentes (se generan y se van con la red): la IA no puede cachearlos en Start.
        public static readonly System.Collections.Generic.List<PlayerMotor> All = new System.Collections.Generic.List<PlayerMotor>();
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Awake()
        {
            rb = GetComponent<Rigidbody>(); col = GetComponent<CapsuleCollider>(); hands = GetComponent<PlayerHands>(); controls = GetComponent<PlayerControls>();
            rb.mass = LabBalance.PlayerMass; rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            col.height = LabBalance.CapsuleHeight; col.radius = LabBalance.CapsuleRadius; col.center = new Vector3(0, LabBalance.CapsuleHeight * 0.5f, 0);
            yaw = transform.eulerAngles.y; spawnPos = transform.position; spawnRot = transform.rotation;
        }

        public override void OnNetworkSpawn()
        {
            playerName = $"Jugador {OwnerClientId + 1}";
            if (IsOwner && Sanitize(GameSettings.PlayerName) != "") { if (IsServer) nName.Value = Sanitize(GameSettings.PlayerName); else NameRpc(GameSettings.PlayerName); }
            nName.OnValueChanged += (_, v) => { if (v.Length > 0) playerName = v.ToString(); };
            if (nName.Value.Length > 0) playerName = nName.Value.ToString();
            if (IsServer)
            {
                var layout = LevelLayout.Current;
                spawnPos = layout ? layout.Spawn(OwnerClientId) : transform.position;
                if (layout) { spawnRot = Quaternion.Euler(0f, layout.playerSpawnYaw, 0f); yaw = layout.playerSpawnYaw; rb.rotation = spawnRot; }
                rb.position = spawnPos; transform.position = spawnPos;
            }
            if (!IsOwner)
            {   // solo el dueno ve por esta camara y escucha por este oido
                var cam = GetComponentInChildren<Camera>(); if (cam) cam.enabled = false;
                var al = GetComponentInChildren<AudioListener>(); if (al) al.enabled = false;
            }
            Debug.Log($"[Player] spawn {playerName} owner={OwnerClientId} local={IsOwner} server={IsServer}");
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool local = controls == null || controls.IsLocal;
            bool canLook = State == PlayerState.Active || State == PlayerState.Stumble;
            if (local)
            {
                if (canLook && Cursor.lockState == CursorLockMode.Locked)
                {
                    Vector2 look = LabInput.Look() * GameSettings.MouseSensitivity;
                    if (GameSettings.InvertY) look.y = -look.y;
                    yaw += look.x; pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
                }
            }
            else if (controls.HasRemoteLook(out float ry, out float rp)) { yaw = ry; pitch = rp; }

            if (Auth)
            {
                if (state == PlayerState.Active)
                {
                    bool wantCrouch = Ctrl(Act.Crouch);
                    if (wantCrouch != crouching) SetCrouch(wantCrouch);
                    if (controls != null ? controls.Pressed(Act.Dodge) : LabInput.Pressed(Act.Dodge)) TryDodge();
                }
                UpdateStateTimers();
                UpdateRegen(dt);
                Publish();
            }
            UpdateCamera(dt);
        }

        bool Ctrl(Act a) => controls != null ? controls.Held(a) : LabInput.Held(a);
        Vector2 MoveInput() => DebugMove ?? (controls != null ? controls.Move() : LabInput.Move());

        void Publish()
        {
            if (!IsSpawned || !IsServer) return;
            if (nState.Value != state) nState.Value = state;
            if (!Mathf.Approximately(nComposure.Value, composure)) nComposure.Value = composure;
            if (!Mathf.Approximately(nEnergy.Value, energy)) nEnergy.Value = energy;
            if (nCrouch.Value != crouching) nCrouch.Value = crouching;
            if (nFalls.Value != fallCount) nFalls.Value = fallCount;
            float end = NetAuth.ServerNow + Mathf.Max(0f, stateUntil - Time.time);
            if (Mathf.Abs(nStateEnd.Value - end) > 0.05f) nStateEnd.Value = end;
            float dr = NetAuth.ServerNow + Mathf.Max(0f, dodgeReadyAt - Time.time);
            if (Mathf.Abs(nDodgeReady.Value - dr) > 0.05f) nDodgeReady.Value = dr;
        }

        void FixedUpdate()
        {
            if (!Auth) return;
            rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
            if (Time.time < dodgeUntil) return;

            Vector2 input = state == PlayerState.Active ? MoveInput() : Vector2.zero;
            float speed = crouching ? LabBalance.CrouchSpeed : (hands != null && hands.AnyHeld ? LabBalance.LoadedSpeed : LabBalance.WalkSpeed);
            Vector3 wish = (Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y)) * speed;
            Vector3 v = rb.linearVelocity; Vector3 hv = new Vector3(v.x, 0, v.z);
            bool grounded = IsGrounded();
            float accel = acceleration * (grounded ? 1f : airControl);
            if (state == PlayerState.Fallen) accel *= 0.5f; // friccion de suelo mientras esta caido
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
            crouching = on;
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
            Vector2 input = MoveInput();
            if (input.sqrMagnitude < 0.01f) return; // el paso evasivo necesita direccion
            Vector3 dir = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y).normalized;
            float dist = LabBalance.DodgeDistance;
            if (rb.SweepTest(dir, out RaycastHit hit, dist, QueryTriggerInteraction.Ignore)) dist = Mathf.Max(0f, hit.distance - 0.05f);
            const float dur = 0.18f;
            rb.linearVelocity = dir * (dist / dur) + Vector3.up * rb.linearVelocity.y;
            dodgeUntil = Time.time + dur; dodgeReadyAt = Time.time + LabBalance.DodgeCooldown;
        }

        /// Impacto de un ataque o empujon (solo anfitrion). Devuelve true si afecto al jugador.
        public bool ApplyImpact(Vector3 deltaV, float composureLoss, string source)
        {
            if (!Auth) return false;
            if (Invulnerable || state == PlayerState.Fallen || state == PlayerState.GettingUp) return false;
            rb.AddForce(deltaV, ForceMode.VelocityChange);
            composure = Mathf.Max(0f, composure - composureLoss);
            composureRegenAt = Time.time + LabBalance.ComposureRegenDelay;
            float dvHoriz = new Vector3(deltaV.x, 0, deltaV.z).magnitude;
            bool knockdown = composure <= 0f || dvHoriz >= LabBalance.DirectKnockdownDeltaV;
            Debug.Log($"[Player] {playerName} impact from {source}: dv={dvHoriz:0.00} composure={composure:0} knockdown={knockdown}");
            if (knockdown) Fall(source);
            else { state = PlayerState.Stumble; stateUntil = Time.time + LabBalance.MinorStumbleLock; }
            return true;
        }

        /// Ayuda de un compañero (GDD: reduce el tiempo de levantada; nunca obligatoria). Cada segundo de ayuda
        /// descuenta un segundo extra de la caida: con ayuda continua se levanta al doble de velocidad.
        public bool AssistGetUp(float dt)
        {
            if (!Auth || state != PlayerState.Fallen) return false;
            stateUntil -= dt; assistedTime += dt;
            return true;
        }
        float assistedTime;
        public float AssistedTime => assistedTime;

        public void Fall(string source)
        {
            if (!Auth || state == PlayerState.Fallen) return;
            fallCount++;
            if (hands) hands.ReleaseAll(ReleaseReason.Fell);
            if (crouching) SetCrouch(false);
            state = PlayerState.Fallen;
            stateUntil = Time.time + LabBalance.FallLockMax;
            Debug.Log($"[Player] {playerName} FALL ({source})");
        }

        void UpdateStateTimers()
        {
            if (state == PlayerState.Stumble && Time.time >= stateUntil) state = PlayerState.Active;
            else if (state == PlayerState.Fallen && Time.time >= stateUntil)
            {
                FindSafeStandPosition();
                state = PlayerState.GettingUp; stateUntil = Time.time + getUpDuration;
            }
            else if (state == PlayerState.GettingUp && Time.time >= stateUntil)
            {
                state = PlayerState.Active;
                invulnUntil = Time.time + LabBalance.StandupInvulnerability;
                composure = Mathf.Max(composure, LabBalance.ComposureMax * 0.6f); // decision de laboratorio, ver DECISIONES_IMPLEMENTACION
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
            if (Time.time >= composureRegenAt && state != PlayerState.Fallen)
                composure = Mathf.Min(LabBalance.ComposureMax, composure + LabBalance.ComposureRegenPerS * dt);
            if (Time.time >= energyRegenAt)
                energy = Mathf.Min(LabBalance.EnergyMax, energy + LabBalance.EnergyRegenPerS * dt);
        }

        /// Consume energia para el esfuerzo (anfitrion). Devuelve false si no queda.
        public bool ConsumeEffort(float dt)
        {
            if (energy <= 0f) return false;
            energy = Mathf.Max(0f, energy - LabBalance.EffortEnergyPerS * dt);
            energyRegenAt = Time.time + LabBalance.EnergyRegenDelay;
            return true;
        }

        Camera ownCam;
        void UpdateCamera(float dt)
        {
            if (!cameraPivot) return;
            if (!ownCam) ownCam = cameraPivot.GetComponentInChildren<Camera>();
            if (ownCam && ownCam.enabled) ownCam.fieldOfView = GameSettings.FieldOfView;
            var st = State;
            float targetRoll = st == PlayerState.Fallen ? 65f : 0f;
            float targetEye = st == PlayerState.Fallen ? 0.45f : (Crouching ? crouchEyeHeight : standEyeHeight);
            roll = Mathf.MoveTowards(roll, targetRoll, 260f * dt);
            Vector3 lp = cameraPivot.localPosition;
            lp.y = Mathf.MoveTowards(lp.y, targetEye, 4f * dt);
            cameraPivot.localPosition = lp;
            // La raiz gira en el anfitrion; la camara aplica la diferencia para que la mirada local responda sin esperar a la red.
            float yawOffset = Mathf.DeltaAngle(transform.eulerAngles.y, yaw);
            cameraPivot.localRotation = Quaternion.Euler(0f, yawOffset, 0f) * Quaternion.Euler(pitch, 0f, roll);
        }

        public void ResetState()
        {
            if (!Auth) return;
            if (hands) hands.ReleaseAll(ReleaseReason.Reset);
            state = PlayerState.Active; composure = LabBalance.ComposureMax; energy = LabBalance.EnergyMax;
            invulnUntil = stateUntil = composureRegenAt = energyRegenAt = dodgeReadyAt = dodgeUntil = 0f;
            if (crouching) SetCrouch(false);
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos; rb.rotation = spawnRot; transform.SetPositionAndRotation(spawnPos, spawnRot);
            yaw = spawnRot.eulerAngles.y; pitch = 0f; roll = 0f; DebugMove = null;
            if (IsSpawned && !IsOwner) ResetLookRpc(yaw);
        }

        /// El anfitrion reubica al jugador remoto al reiniciar: su mirada local debe acompanar.
        [Rpc(SendTo.Owner)]
        void ResetLookRpc(float newYaw) { yaw = newYaw; pitch = 0f; }

        public void SetYawPitch(float newYaw, float newPitch) { yaw = newYaw; pitch = newPitch; }
    }
}
