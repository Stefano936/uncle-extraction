using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    public enum NpcState { Loiter, Protest, Anchor, Struggle, Windup, Attack, Recover, ForcedRecovery, Escape, Fallen, Carried, Secured }

    /// Cerebro del tio (GDD 06-07). Decide cada 0,2 s; ataques con preparacion visible, ventana activa unica y recuperacion obligatoria.
    [RequireComponent(typeof(NpcBody))]
    public class NpcBrain : MonoBehaviour, IResettable
    {
        public static NpcBrain Instance { get; private set; }

        public Transform home;
        public Collider anchorRail;
        public GrabSocket forearmL, forearmR;
        public bool brainEnabled = true;
        public int seed = 1234;
        public float anchorSearchRadius = 1.4f;
        public float maxAnchorTime = 9f;

        public NpcState State { get; private set; } = NpcState.Loiter;
        public string Intent { get; private set; } = "";
        public AttackDef CurrentAttack { get; private set; }
        public bool AttackPending => State == NpcState.Windup || State == NpcState.Attack;
        public float StateTimeLeft => Mathf.Max(0f, stateUntil - Time.time);
        public float Budget { get; private set; }
        public float BudgetMax => LabBalance.NpcBudget * Resistance;
        public float Resistance => RoundDirector.Instance ? RoundDirector.Instance.Resistance : 1f;
        public bool IsAnchored => anchorJoint != null;
        public GrabSocket AnchorHandSocket { get; private set; }
        public bool Strapped => TransportCart.Instance && TransportCart.Instance.IsStrapped;
        public Vector3 AttackDirection { get; private set; }
        public int AttacksStarted { get; private set; }
        public int HitsLanded { get; private set; }
        public string LastRejection { get; private set; } = "";
        public bool WriggleAttempt { get; private set; }

        NpcBody body; System.Random rng;
        float stateUntil, nextDecision, nextAttackAt, lastGrabbedAt = -99f, anchorStart, nextWriggleAt;
        bool attackResolved; Vector3 lockedDir;
        ConfigurableJoint anchorJoint;
        readonly List<PlayerMotor> players = new List<PlayerMotor>();
        readonly List<IGrabOwner> owners = new List<IGrabOwner>();
        readonly HashSet<PlayerMotor> hitThisAttack = new HashSet<PlayerMotor>();

        void Awake() { Instance = this; body = GetComponent<NpcBody>(); rng = new System.Random(seed); Budget = LabBalance.NpcBudget; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Start() { RefreshPlayers(); Budget = BudgetMax; ScheduleNextAttack(); }

        public void RefreshPlayers() { players.Clear(); players.AddRange(FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None)); }

        int GrabCount => GrabRegistry.CountOnBody(body.Body);
        bool Grabbed => GrabCount > 0;
        float Now => Time.time;

        void Update()
        {
            if (State == NpcState.Secured) { body.desiredWalkVelocity = Vector3.zero; body.resistForce = Vector3.zero; return; }
            if (Grabbed || Strapped) lastGrabbedAt = Now;
            body.SetCarried(Strapped);

            // Estados con duracion fija: avanzan aunque el cerebro este pausado para depuracion.
            UpdateTimedStates();
            UpdateBudget(Time.deltaTime);
            ApplyIntentToBody();

            if (!brainEnabled || Now < nextDecision) return;
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
            Vector3 toHome = home ? Flat(home.position - transform.position) : Vector3.zero;
            body.resistForce = (Grabbed || Strapped) && f > 0f ? toHome * (LabBalance.NpcResistanceForceCap * r * f) : Vector3.zero;
            body.desiredWalkVelocity = State == NpcState.Escape ? toHome * 1.4f : Vector3.zero;
            body.desiredFacing = AttackPending ? AttackDirection : (State == NpcState.Escape ? toHome : Vector3.zero);
        }

        void Decide()
        {
            if (AttackPending || State == NpcState.Recover || State == NpcState.ForcedRecovery) return;

            if (Strapped)
            {
                if (State != NpcState.Carried) Enter(NpcState.Carried, 0f, "en el carrito");
                if (Now >= nextWriggleAt && Budget > BudgetMax * 0.4f) { StartWriggle(); return; }
                if (Grabbed && Now >= nextAttackAt) TryStartAttack();
                return;
            }
            if (State == NpcState.Carried) Enter(NpcState.Loiter, 0f, "");

            if (body.Posture == NpcPosture.Fallen)
            {
                if (Grabbed && Now >= nextAttackAt && TryStartAttack()) return;
                if (!Grabbed && body.CanTryStandUp) body.TryStandUp();
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

        bool TryStartAttack()
        {
            var valid = new List<AttackId>();
            bool upright = body.Posture == NpcPosture.Upright;
            var near = NearestPlayer(out float d);
            if (Grabbed) valid.Add(AttackId.Shake);
            if (upright && near && d <= 1.6f) { valid.Add(AttackId.Slap); valid.Add(AttackId.Push); valid.Add(AttackId.Kick); }
            if (valid.Count == 0) { LastRejection = "sin ataque valido (postura/espacio)"; return false; }
            return StartAttack(valid[rng.Next(valid.Count)], false);
        }

        bool StartAttack(AttackId id, bool forced)
        {
            if (AttackPending && !forced) return false;
            if (id == AttackId.Shake && !Grabbed && !forced) { LastRejection = "sacudida sin agarres"; return false; }
            if ((id == AttackId.Push || id == AttackId.Kick) && body.Posture != NpcPosture.Upright && !forced) { LastRejection = "requiere postura de pie"; return false; }
            CurrentAttack = LabBalance.Attack(id); WriggleAttempt = false;
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
            CurrentAttack = LabBalance.Attack(AttackId.Shake); WriggleAttempt = true;
            AttackDirection = lockedDir = body.Forward;
            Enter(NpcState.Windup, 1.4f, "SE QUIERE SOLTAR DEL CARRITO");
            nextWriggleAt = Now + 7f + (float)rng.NextDouble() * 3f;
        }

        void BeginActive()
        {
            attackResolved = false; hitThisAttack.Clear();
            Enter(NpcState.Attack, CurrentAttack.active, CurrentAttack.label + "!");
        }

        void ResolveAttack()
        {
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
                GrabRegistry.OwnersOnBody(body.Body, owners);
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
            anchorJoint = gameObject.AddComponent<ConfigurableJoint>();
            anchorJoint.autoConfigureConnectedAnchor = false;
            anchorJoint.anchor = transform.InverseTransformPoint(hand.Position);
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
            State = NpcState.Secured; Intent = "ASEGURADO";
        }

        // ---------- Utilidades ----------
        void Enter(NpcState s, float duration, string intent)
        {
            State = s; stateUntil = Now + duration; Intent = intent;
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
            State = NpcState.Loiter; Intent = ""; WriggleAttempt = false;
            Budget = BudgetMax; lastGrabbedAt = -99f; nextDecision = 0f; nextWriggleAt = Now + 4f;
            hitThisAttack.Clear(); RefreshPlayers(); ScheduleNextAttack();
        }
    }
}
