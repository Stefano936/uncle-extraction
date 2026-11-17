using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    public enum NpcState { Loiter, Protest, Anchor, Struggle, Windup, Attack, Recover, ForcedRecovery, Escape, Fallen, Carried, Secured, Distracted }

    /// Cerebro del tio (GDD 06-07). Decide cada 0,2 s; ataques con preparacion visible, ventana activa unica y recuperacion obligatoria.
    [RequireComponent(typeof(NpcBody))]
    public class NpcBrain : NetworkBehaviour, IResettable
    {
        public static NpcBrain Instance { get; private set; }

        public Transform home;
        public Collider anchorRail;
        public GrabSocket forearmL, forearmR;
        public bool brainEnabled = true;
        public int seed = 1234;
        public float anchorSearchRadius = 1.4f;
        public float maxAnchorTime = 9f;

        // Estado replicado: solo el anfitrion decide (especificacion 06/07); los clientes lo usan para presentacion, HUD y autopiloto.
        readonly NetworkVariable<NpcState> nState = new NetworkVariable<NpcState>(NpcState.Loiter);
        readonly NetworkVariable<FixedString64Bytes> nIntent = new NetworkVariable<FixedString64Bytes>();
        readonly NetworkVariable<AttackId> nAttack = new NetworkVariable<AttackId>();
        readonly NetworkVariable<float> nStateEnd = new NetworkVariable<float>();
        readonly NetworkVariable<float> nBudget01 = new NetworkVariable<float>(1f);
        readonly NetworkVariable<bool> nWriggle = new NetworkVariable<bool>();
        readonly NetworkVariable<FixedString32Bytes> nAnchorHand = new NetworkVariable<FixedString32Bytes>();
        readonly NetworkVariable<int> nAttacks = new NetworkVariable<int>(), nHits = new NetworkVariable<int>();
        bool Mirror => IsSpawned && !IsServer;

        NpcState state = NpcState.Loiter; string intent = ""; AttackDef currentAttack; bool wriggle;
        public NpcState State => Mirror ? nState.Value : state;
        public string Intent => Mirror ? nIntent.Value.ToString() : intent;
        public AttackDef CurrentAttack => Mirror ? LabBalance.Attack(nAttack.Value) : currentAttack;
        public bool AttackPending => State == NpcState.Windup || State == NpcState.Attack;
        public float StateTimeLeft => Mirror ? Mathf.Max(0f, nStateEnd.Value - NetAuth.ServerNow) : Mathf.Max(0f, stateUntil - Time.time);
        float budget;
        public float Budget { get => Mirror ? nBudget01.Value * BudgetMax : budget; private set => budget = value; }
        public float BudgetMax => LabBalance.NpcBudget * Resistance;
        public float Resistance => RoundDirector.Instance ? RoundDirector.Instance.Resistance : 1f;
        public bool IsAnchored => Mirror ? nAnchorHand.Value.Length > 0 : anchorJoint != null;
        GrabSocket anchorHand;
        public GrabSocket AnchorHandSocket { get => Mirror ? GrabSocket.ById(nAnchorHand.Value.ToString()) : anchorHand; private set => anchorHand = value; }
        public bool Strapped => TransportCart.Instance && TransportCart.Instance.IsStrapped;
        public Vector3 AttackDirection { get; private set; }
        int attacksStarted, hitsLanded, wrigglesStarted;
        /// Intentos de soltarse del carrito (su forma de resistencia mientras esta asegurado). Solo anfitrion.
        public int WrigglesStarted => wrigglesStarted;
        /// Presupuesto minimo (fraccion) para intentar soltarse del carrito: agotado no lo intenta (GDD 05, fatiga).
        public const float WriggleMinBudget01 = 0.4f;
        /// Intervalo maximo entre intentos de soltarse (7 + 0..3 s).
        public const float WriggleMaxInterval = 10f;
        public int AttacksStarted { get => Mirror ? nAttacks.Value : attacksStarted; private set => attacksStarted = value; }
        public int HitsLanded { get => Mirror ? nHits.Value : hitsLanded; private set => hitsLanded = value; }
        public string LastRejection { get; private set; } = "";
        public bool WriggleAttempt { get => Mirror ? nWriggle.Value : wriggle; private set => wriggle = value; }

        NpcBody body; System.Random rng;
        float stateUntil, nextDecision, nextAttackAt, lastGrabbedAt = -99f, anchorStart, nextWriggleAt;
        bool attackResolved; Vector3 lockedDir;
        ConfigurableJoint anchorJoint;
        readonly List<PlayerMotor> players = new List<PlayerMotor>();
        readonly List<IGrabOwner> owners = new List<IGrabOwner>();
        readonly HashSet<PlayerMotor> hitThisAttack = new HashSet<PlayerMotor>();

        void Awake() { Instance = this; body = GetComponent<NpcBody>(); rng = new System.Random(seed); Budget = LabBalance.NpcBudget; }
        public override void OnDestroy() { if (Instance == this) Instance = null; base.OnDestroy(); }
        void Start() { RefreshPlayers(); Budget = BudgetMax; ScheduleNextAttack(); if (!GetComponent<UncleFace>()) gameObject.AddComponent<UncleFace>(); }

        public void RefreshPlayers() { players.Clear(); players.AddRange(PlayerMotor.All); }

        int GrabCount => body.GrabCount();
        bool Grabbed => GrabCount > 0;
        float Now => Time.time;

        void Update()
        {
            if (!NetAuth.IsServer) return; // los clientes solo presentan el estado replicado
            Publish();
            if (State == NpcState.Secured) { body.desiredWalkVelocity = Vector3.zero; body.resistForce = Vector3.zero; return; }
            if (Grabbed || Strapped) lastGrabbedAt = Now;
            body.SetCarried(Strapped);

            // Estados con duracion fija: avanzan aunque el cerebro este pausado para depuracion.
            UpdateTimedStates();
            UpdateBudget(Time.deltaTime);
            ApplyIntentToBody();

            if (!brainEnabled || Now < nextDecision) return;
            if (RoundDirector.Instance && RoundDirector.Instance.InLobby) return; // en la sala el tio espera
            nextDecision = Now + 0.2f; // 5 Hz (balance.simulation.npc_decision_hz)
            Decide();
        }

        void UpdateTimedStates()
        {
            switch (State)
            {
                case NpcState.Windup:
                    if (Now < stateUntil - LabBalance.DirectionLockBeforeActive && !WriggleAttempt)
                    {   // giro limitado hacia el objetivo durante la preparacion
                        var t = NearestPlayer(out _);
                        if (t)
                        {
                            Vector3 want = Flat(t.transform.position - transform.position);
                            float a = Vector3.SignedAngle(lockedDir, want, Vector3.up);
                            float clamped = Mathf.Clamp(a, -LabBalance.MaxTurnDuringWindupDeg, LabBalance.MaxTurnDuringWindupDeg);
                            AttackDirection = Quaternion.Euler(0, clamped, 0) * lockedDir;
                        }
                    }
                    if (Now >= stateUntil) BeginActive();
                    break;
                case NpcState.Attack:
                    if (!attackResolved) { ResolveAttack(); attackResolved = true; }
                    if (Now >= stateUntil) Enter(NpcState.Recover, CurrentAttack.recovery, "recupera");
                    break;
                case NpcState.Recover:
                case NpcState.Protest:
                case NpcState.Struggle:
                    if (Now >= stateUntil) Enter(NpcState.Loiter, 0f, "");
                    break;
                case NpcState.ForcedRecovery:
                    if (Now >= stateUntil) { Budget = BudgetMax * LabBalance.ForcedRecoveryRefill; Enter(NpcState.Loiter, 0f, ""); }
                    break;
                case NpcState.Distracted:
                    if (Now >= stateUntil) { Enter(NpcState.Loiter, 0f, ""); currentDecoy = null; }
                    break;
                case NpcState.Anchor:
                    if (!IsAnchored || Now - anchorStart > maxAnchorTime) { ReleaseAnchor(IsAnchored ? "tiempo" : "perdido"); }
                    break;
            }
        }

        void UpdateBudget(float dt)
        {
            float drain = 0f;
            if (State == NpcState.Struggle) drain = 10f;
            else if (State == NpcState.Anchor) drain = LabBalance.AnchorBudgetPerS;
            else if (State == NpcState.Protest) drain = 4f;
            if (drain > 0f) Budget -= drain * dt;
            else if (!Grabbed && !Strapped && (State == NpcState.Loiter || State == NpcState.Escape)) Budget += 6f * dt;
            Budget = Mathf.Clamp(Budget, 0f, BudgetMax);
            if (Budget <= BudgetMax * LabBalance.ForcedRecoveryThreshold && State != NpcState.ForcedRecovery && !AttackPending)
            {
                if (IsAnchored) ReleaseAnchor("agotado");
                Enter(NpcState.ForcedRecovery, LabBalance.ForcedRecoveryTime(RoundDirector.Instance ? RoundDirector.Instance.difficulty : Difficulty.Normal), "AGOTADO: abertura");
            }
        }

        void ApplyIntentToBody()
        {
            float r = Resistance, f = 0f;
            switch (State)
            {
                case NpcState.Protest: f = 0.5f; break;
                case NpcState.Struggle: f = 1f; break;
                case NpcState.Windup: f = 0.6f; break;
                case NpcState.Anchor: f = 0.4f; break;
                case NpcState.Recover: f = 0.3f; break;
                case NpcState.ForcedRecovery: f = LabBalance.ForcedRecoveryResistance; break;
            }
            Vector3 toHome = EscapeDirection();
            body.resistForce = (Grabbed || Strapped) && f > 0f ? toHome * (LabBalance.NpcResistanceForceCap * r * f) : Vector3.zero;
            // Fuga: vuelve a su banco por la puerta (no en linea recta contra el muro). Si hay alguien cerca y nadie lo agarra,
            // arranca mas rapido (tope de balance: EscapeSpeedCap 2,2 m/s); si no, camina a 1,4 m/s.
            float escapeSpeed = NearestPlayer(out float nearD) && nearD < 3f ? LabBalance.EscapeSpeedCap : 1.4f;
            body.desiredWalkVelocity = State == NpcState.Escape ? toHome * escapeSpeed : Vector3.zero;
            body.desiredFacing = AttackPending ? AttackDirection : (State == NpcState.Escape ? toHome : Vector3.zero);
            if (State == NpcState.Distracted && currentDecoy)
            {   // mira el vaso y, si nadie lo sostiene, camina despacio hacia el
                Vector3 toDecoy = Flat(currentDecoy.Position - transform.position);
                body.desiredFacing = toDecoy;
                body.desiredWalkVelocity = !Grabbed && !Strapped && Flat3(currentDecoy.Position - transform.position).magnitude > 0.8f ? toDecoy * 0.8f : Vector3.zero;
            }
        }

        /// Direccion hacia el banco pasando por la puerta del escenario cuando esta afuera (vereda, rampa, camioneta).
        Vector3 EscapeDirection()
        {
            if (!home) return Vector3.zero;
            var L = LevelLayout.Current;
            Vector3 p = transform.position, target = home.position;
            if (L && p.z < L.doorZ + 0.4f)
            {   // afuera: primero alinearse con la puerta y despues cruzarla
                Vector3 outside = new Vector3(L.doorX, 0f, L.doorZ - 1.2f), inside = new Vector3(L.doorX, 0f, L.doorZ + 1.2f);
                target = Mathf.Abs(p.x - L.doorX) > 0.45f && p.z < L.doorZ - 0.6f ? outside : inside;
            }
            return Flat(target - p);
        }

        void Decide()
        {
            RefreshPlayers(); // entran y salen jugadores durante la ronda (red)
            // Distraccion: «si lo ve y no esta atacando» (GDD 09). Tambien durante Recover/apertura; nunca en el carrito.
            if (!AttackPending && State != NpcState.Distracted && State != NpcState.ForcedRecovery && !Strapped && Now >= distractionReadyAt && TryDistract()) return;
            if (AttackPending || State == NpcState.Recover || State == NpcState.ForcedRecovery) return;

            if (Strapped)
            {
                if (State != NpcState.Carried) Enter(NpcState.Carried, 0f, "en el carrito");
                if (Now >= nextWriggleAt && Budget > BudgetMax * WriggleMinBudget01) { StartWriggle(); return; }
                if (Grabbed && Now >= nextAttackAt) TryStartAttack();
                return;
            }
            if (State == NpcState.Carried) Enter(NpcState.Loiter, 0f, "");

            // Jerarquia (GDD 06): Secured > ataque iniciado > Recover > distraccion > anclaje > escape > protesta
            if (State == NpcState.Distracted) return;

            if (body.Posture == NpcPosture.Fallen)
            {
                if (Grabbed && Now >= nextAttackAt && TryStartAttack()) return;
                // Se levanta tras standUpDelay en el piso Y standUpDelay sin agarres: soltarlo no lo para al instante (decision M1, 2026-11-15).
                if (!Grabbed && body.CanTryStandUp && Now - lastGrabbedAt >= body.standUpDelay) body.TryStandUp();
                if (State != NpcState.Fallen) Enter(NpcState.Fallen, 0f, "en el piso");
                return;
            }
            if (State == NpcState.Fallen) Enter(NpcState.Loiter, 0f, "");
            if (State == NpcState.Anchor) { if (Grabbed && Now >= nextAttackAt) TryStartAttack(); return; }

            bool recentlyGrabbed = Now - lastGrabbedAt < 4f;
            if (Grabbed)
            {
                if (State == NpcState.Loiter || State == NpcState.Escape) { Enter(NpcState.Protest, 0.8f, "protesta"); return; }
                if (State == NpcState.Protest || State == NpcState.Struggle)
                {
                    if (Now >= nextAttackAt && TryStartAttack()) return;
                    if (State == NpcState.Protest && TryAnchor()) return;
                    if (State == NpcState.Protest) Enter(NpcState.Struggle, 3f, "forcejea");
                }
                return;
            }
            if (recentlyGrabbed && Now >= nextAttackAt && NearestPlayer(out float d) && d < 1.5f && TryStartAttack()) return;

            if (State == NpcState.Loiter && home && Flat3(home.position - transform.position).magnitude > 1.0f) Enter(NpcState.Escape, 0f, "vuelve a su banco");
            else if (State == NpcState.Escape && home && Flat3(home.position - transform.position).magnitude <= 0.6f) Enter(NpcState.Loiter, 0f, "");
        }

        // ---------- Ataques ----------
        public bool ForceAttack(AttackId id) => StartAttack(id, true);

        /// Extremidades controladas por jugadores (GDD: un brazo sujeto no puede dar manotazos; con los dos brazos sujetos no
        /// hay empujon; con los tobillos sujetos no hay patada). Se mira la ocupacion real de los puntos de este tio.
        public struct Limbs { public bool leftArm, rightArm, leftLeg, rightLeg; }
        public Limbs HeldLimbs()
        {
            var r = new Limbs();
            foreach (var s in GrabSocket.All)
            {
                if (!s || !s.body || GrabRegistry.OccupancyOf(s) == 0 || !body.Owns(s.body)) continue;
                string id = s.socketId;
                if (id == "forearm_l" || id == "shoulder_l") r.leftArm = true;
                else if (id == "forearm_r" || id == "shoulder_r") r.rightArm = true;
                else if (id == "ankle_l") r.leftLeg = true;
                else if (id == "ankle_r") r.rightLeg = true;
            }
            return r;
        }

        /// Regla pura (probada en EditMode): ataques posibles segun agarres, postura, distancia y extremidades libres.
        public static List<AttackId> AvailableAttacks(bool grabbed, bool upright, bool playerInReach, Limbs held)
        {
            var valid = new List<AttackId>();
            if (grabbed) valid.Add(AttackId.Shake);
            if (upright && playerInReach)
            {
                if (!held.leftArm || !held.rightArm) valid.Add(AttackId.Slap);
                if (!held.leftArm && !held.rightArm) valid.Add(AttackId.Push);
                if (!held.leftLeg || !held.rightLeg) valid.Add(AttackId.Kick);
            }
            return valid;
        }

        bool TryStartAttack()
        {
            var near = NearestPlayer(out float d);
            var valid = AvailableAttacks(Grabbed, body.Posture == NpcPosture.Upright, near && d <= 1.6f, HeldLimbs());
            if (valid.Count == 0) { LastRejection = "sin ataque valido (postura/espacio/extremidades)"; return false; }
            return StartAttack(valid[rng.Next(valid.Count)], false);
        }

        bool StartAttack(AttackId id, bool forced)
        {
            RefreshPlayers();
            if (AttackPending && !forced) return false;
            if (id == AttackId.Shake && !Grabbed && !forced) { LastRejection = "sacudida sin agarres"; return false; }
            if ((id == AttackId.Push || id == AttackId.Kick) && body.Posture != NpcPosture.Upright && !forced) { LastRejection = "requiere postura de pie"; return false; }
            if (id != AttackId.Shake && !forced)
            {
                var held = HeldLimbs();
                bool ok = id == AttackId.Slap ? !(held.leftArm && held.rightArm) : id == AttackId.Push ? !(held.leftArm || held.rightArm) : !(held.leftLeg && held.rightLeg);
                if (!ok) { LastRejection = "extremidad sujeta"; return false; }
            }
            currentAttack = LabBalance.Attack(id); WriggleAttempt = false;
            var t = NearestPlayer(out _);
            lockedDir = t ? Flat(t.transform.position - transform.position) : body.Forward;
            if (lockedDir.sqrMagnitude < 0.01f) lockedDir = body.Forward;
            AttackDirection = lockedDir;
            float windup = LabBalance.Windup(id, RoundDirector.Instance ? RoundDirector.Instance.difficulty : Difficulty.Normal);
            Enter(NpcState.Windup, windup, "prepara " + CurrentAttack.label);
            AttacksStarted++;
            Debug.Log($"[Npc] windup {id} {windup:0.00}s forced={forced}");
            return true;
        }

        void StartWriggle()
        {
            currentAttack = LabBalance.Attack(AttackId.Shake); WriggleAttempt = true;
            AttackDirection = lockedDir = body.Forward;
            Enter(NpcState.Windup, 1.4f, "SE QUIERE SOLTAR DEL CARRITO");
            wrigglesStarted++;
            nextWriggleAt = Now + 7f + (float)rng.NextDouble() * 3f;
        }

        void BeginActive()
        {
            attackResolved = false; hitThisAttack.Clear();
            Enter(NpcState.Attack, CurrentAttack.active, CurrentAttack.label + "!");
        }

        void ResolveAttack()
        {
            RefreshPlayers();
            Budget -= CurrentAttack.budgetCost * Resistance;
            ScheduleNextAttack();
            if (WriggleAttempt)
            {
                if (TransportCart.Instance && TransportCart.Instance.IsStrapped) TransportCart.Instance.Unstrap("tio");
                return;
            }
            var diff = RoundDirector.Instance ? RoundDirector.Instance.difficulty : Difficulty.Normal;
            float impact = LabBalance.ImpactMultiplier(diff);
            Vector3 origin = body.ChestPosition;
            var targets = new List<PlayerMotor>();

            if (CurrentAttack.id == AttackId.Shake)
            {
                body.OwnersOnBody(owners);
                int max = LabBalance.ShakeMaxTargets(diff);
                foreach (var o in owners)
                {
                    if (o is PlayerHands h && targets.Count < max) targets.Add(h.GetComponent<PlayerMotor>());
                    GrabRegistry.ReleaseAllOf(o, ReleaseReason.Attack); // la sacudida rompe los agarres
                }
            }
            else
            {
                var sorted = new List<(PlayerMotor p, float d)>();
                foreach (var p in players)
                {
                    if (!p) continue;
                    Vector3 to = p.transform.position + Vector3.up * 1.0f - origin; Vector3 flat = Flat3(to);
                    float d = flat.magnitude;
                    if (d > CurrentAttack.range + LabBalance.CapsuleRadius) continue;
                    if (Vector3.Angle(AttackDirection, flat) > CurrentAttack.arcDeg * 0.5f) continue;
                    if (Physics.Linecast(origin, p.transform.position + Vector3.up * 1.0f, out var hit, ~0, QueryTriggerInteraction.Ignore)
                        && hit.rigidbody != p.Body && hit.rigidbody != body.Body) continue; // bloqueado por geometria
                    sorted.Add((p, d));
                }
                sorted.Sort((a, b) => a.d.CompareTo(b.d));
                for (int i = 0; i < sorted.Count && i < CurrentAttack.maxTargets; i++) targets.Add(sorted[i].p);
            }

            foreach (var p in targets)
            {
                if (!p || hitThisAttack.Contains(p)) continue; // un contacto por objetivo y ataque
                hitThisAttack.Add(p);
                Vector3 dir = Flat(p.transform.position - transform.position); if (dir.sqrMagnitude < 0.01f) dir = AttackDirection;
                Vector3 dv = dir * (CurrentAttack.deltaV * impact) + Vector3.up * 1.2f;
                if (p.ApplyImpact(dv, CurrentAttack.composureLoss * impact, CurrentAttack.label)) HitsLanded++;
            }
            Debug.Log($"[Npc] active {CurrentAttack.id}: targets={targets.Count}");
        }

        void ScheduleNextAttack()
        {
            float interval = RoundDirector.Instance ? RoundDirector.Instance.AttackInterval : 7f;
            nextAttackAt = Now + interval;
        }

        // ---------- Distraccion (GDD 09; balance.distraction) ----------
        float distractionReadyAt; int distractionUses; DecoyGlass currentDecoy;
        public const float DistractionDuration = 2.5f, DistractionCooldown = 12f;
        public int DistractionUses => distractionUses;

        bool TryDistract()
        {
            Vector3 eye = body.head ? body.head.position : transform.position + Vector3.up * 1.7f;
            foreach (var d in DecoyGlass.All)
            {
                if (!d || !d.Resting || !d.VisibleFrom(eye)) continue;
                float dur = DistractionDuration * (distractionUses >= 2 ? 0.5f : 1f); // desde el tercer uso rinde la mitad
                distractionUses++;
                distractionReadyAt = Now + dur + DistractionCooldown;
                currentDecoy = d;
                if (IsAnchored) ReleaseAnchor("distraido");
                Enter(NpcState.Distracted, dur, "¡Uh, una cerveza!");
                Debug.Log($"[Npc] distraido {dur:0.0}s (uso {distractionUses})");
                return true;
            }
            return false;
        }

        // ---------- Anclaje ----------
        bool TryAnchor()
        {
            if (!anchorRail || body.Posture != NpcPosture.Upright || Budget < BudgetMax * 0.3f) return false;
            GrabSocket hand = null; float best = anchorSearchRadius; Vector3 bestPt = Vector3.zero;
            foreach (var s in new[] { forearmL, forearmR })
            {
                if (!s || GrabRegistry.OccupancyOf(s) > 0) continue; // mano agarrada por un jugador no se ancla
                Vector3 pt = anchorRail.ClosestPoint(s.Position); float d = Vector3.Distance(pt, s.Position);
                if (d < best) { best = d; hand = s; bestPt = pt; }
            }
            if (!hand) { LastRejection = "sin anclaje al alcance"; return false; }
            // la union sale de la parte que tiene la mano (antebrazo en el cuerpo articulado; la raiz en el central)
            var handBody = hand.body && body.Owns(hand.body) ? hand.body : body.Body;
            anchorJoint = handBody.gameObject.AddComponent<ConfigurableJoint>();
            anchorJoint.autoConfigureConnectedAnchor = false;
            anchorJoint.anchor = handBody.transform.InverseTransformPoint(hand.Position);
            anchorJoint.connectedAnchor = bestPt; // connectedBody null = mundo
            anchorJoint.xMotion = anchorJoint.yMotion = anchorJoint.zMotion = ConfigurableJointMotion.Limited;
            anchorJoint.linearLimit = new SoftJointLimit { limit = 0.12f };
            AnchorHandSocket = hand; anchorStart = Now;
            Enter(NpcState.Anchor, 0f, "se aferra a la barra");
            Debug.Log($"[Npc] anchor with {hand.socketId}");
            return true;
        }

        public void ReleaseAnchor(string why)
        {
            if (anchorJoint) { Destroy(anchorJoint); Debug.Log($"[Npc] anchor released ({why})"); }
            anchorJoint = null; AnchorHandSocket = null;
            if (State == NpcState.Anchor) Enter(NpcState.Recover, 1.0f, "suelta la barra");
        }

        public void Secure()
        {
            ReleaseAnchor("secured");
            state = NpcState.Secured; intent = "ASEGURADO";
        }

        // ---------- Utilidades ----------
        void Enter(NpcState s, float duration, string why)
        {
            state = s; stateUntil = Now + duration; intent = why;
        }

        void Publish()
        {
            if (!IsSpawned || !IsServer) return;
            if (nState.Value != state) nState.Value = state;
            var fi = new FixedString64Bytes(intent); if (!nIntent.Value.Equals(fi)) nIntent.Value = fi;
            if (nAttack.Value != currentAttack.id) nAttack.Value = currentAttack.id;
            float end = NetAuth.ServerNow + Mathf.Max(0f, stateUntil - Time.time);
            if (Mathf.Abs(nStateEnd.Value - end) > 0.05f) nStateEnd.Value = end;
            float b = BudgetMax > 0f ? budget / BudgetMax : 0f; if (Mathf.Abs(nBudget01.Value - b) > 0.01f) nBudget01.Value = b;
            if (nWriggle.Value != wriggle) nWriggle.Value = wriggle;
            var ah = new FixedString32Bytes(anchorJoint && anchorHand ? anchorHand.socketId : ""); if (!nAnchorHand.Value.Equals(ah)) nAnchorHand.Value = ah;
            if (nAttacks.Value != attacksStarted) nAttacks.Value = attacksStarted;
            if (nHits.Value != hitsLanded) nHits.Value = hitsLanded;
        }

        PlayerMotor NearestPlayer(out float dist)
        {
            PlayerMotor best = null; dist = float.MaxValue;
            foreach (var p in players)
            {
                if (!p || p.State == PlayerState.Fallen) continue;
                float d = Flat3(p.transform.position - transform.position).magnitude;
                if (d < dist) { dist = d; best = p; }
            }
            return best;
        }

        static Vector3 Flat3(Vector3 v) { v.y = 0; return v; }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.zero; }

        public void ResetState()
        {
            ReleaseAnchor("reset");
            if (anchorJoint) DestroyImmediate(anchorJoint);
            foreach (var j in GetComponents<ConfigurableJoint>()) DestroyImmediate(j);
            anchorJoint = null;
            rng = new System.Random(seed);
            state = NpcState.Loiter; intent = ""; WriggleAttempt = false;
            Budget = BudgetMax; lastGrabbedAt = -99f; nextDecision = 0f; nextWriggleAt = Now + 4f; distractionReadyAt = 0f; distractionUses = 0; currentDecoy = null;
            hitThisAttack.Clear(); RefreshPlayers(); ScheduleNextAttack();
        }
    }
}
