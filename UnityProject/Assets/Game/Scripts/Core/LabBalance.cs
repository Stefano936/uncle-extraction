using System;
using UnityEngine;

namespace UncleExtraction
{
    /// Valores iniciales copiados de preproduction/uncle_extraction/data/game_balance.json (PREPRODUCTION_UNTESTED).
    /// Son hipotesis para PhysicsLab; ajustar con evidencia y registrar en docs/DECISIONES_IMPLEMENTACION.md.
    public enum Difficulty { Relaxed, Normal, Hard }
    public enum AttackId { Slap, Push, Kick, Shake }

    [Serializable]
    public struct AttackDef
    {
        public AttackId id; public string label;
        public float windup, active, recovery, composureLoss, budgetCost, deltaV, range, arcDeg;
        public int maxTargets;
    }

    public static class LabBalance
    {
        // Jugador
        public const float WalkSpeed = 4.2f, LoadedSpeed = 2.8f, CrouchSpeed = 2.0f, PlayerMass = 80f;
        public const float CapsuleHeight = 1.8f, CapsuleRadius = 0.32f;
        public const float GrabReach = 1.4f, GrabWarnError = 1.6f, GrabBreakError = 2.2f, GrabBreakGrace = 0.15f;
        public const float HandForce = 420f, HandEffortForce = 560f, PlayerForceCap = 700f, PlayerEffortForceCap = 950f;
        public const float EffortMax = 1.25f, EnergyMax = 100f, EffortEnergyPerS = 35f, EnergyRegenPerS = 22f, EnergyRegenDelay = 1f;
        public const float ComposureMax = 100f, ComposureRegenDelay = 2.5f, ComposureRegenPerS = 20f;
        public const float FallLockMax = 2.2f, StandupInvulnerability = 1.5f, MinorStumbleLock = 0.6f;
        public const float DodgeDistance = 1.1f, DodgeCooldown = 1.2f;
        // NPC
        public const float NpcMass = 110f, NpcMaxLinearVel = 8f, NpcMaxAngularVel = 12f;
        public const int MaxPlayerGrabs = 8;
        public const float NpcResistanceForceCap = 280f, NpcBudget = 100f;
        public const float ForcedRecoveryThreshold = 0.15f, ForcedRecoveryRefill = 0.7f, ForcedRecoveryResistance = 0.2f;
        public const float AnchorBudgetPerS = 8f, AnchorReleaseHold = 0.9f, EscapeSpeedCap = 2.2f;
        // Ataques
        public const float MinTelegraph = 0.8f, DirectionLockBeforeActive = 0.2f, MaxTurnDuringWindupDeg = 35f;
        public const float DirectKnockdownDeltaV = 2.5f;
        // Extraccion
        public const float ExtractionHold = 1.5f;

        public static readonly AttackDef[] Attacks =
        {
            new AttackDef { id = AttackId.Slap,  label = "MANOTAZO",  windup = 0.9f, active = 0.18f, recovery = 1.5f, composureLoss = 35, budgetCost = 20, deltaV = 2.0f, range = 1.2f,  arcDeg = 100, maxTargets = 1 },
            new AttackDef { id = AttackId.Push,  label = "EMPUJON",   windup = 1.1f, active = 0.22f, recovery = 1.8f, composureLoss = 50, budgetCost = 25, deltaV = 2.8f, range = 1.35f, arcDeg = 80,  maxTargets = 2 },
            new AttackDef { id = AttackId.Kick,  label = "PATADA",    windup = 1.2f, active = 0.20f, recovery = 2.0f, composureLoss = 60, budgetCost = 30, deltaV = 3.2f, range = 1.45f, arcDeg = 40,  maxTargets = 1 },
            new AttackDef { id = AttackId.Shake, label = "SACUDIDA",  windup = 1.4f, active = 0.30f, recovery = 2.3f, composureLoss = 45, budgetCost = 35, deltaV = 2.4f, range = 0f,    arcDeg = 360, maxTargets = -1 },
        };
        public static AttackDef Attack(AttackId id) => Attacks[(int)id];

        // Escalado por grupo (indice = jugadores - 1) y dificultad (Relaxed, Normal, Hard)
        static readonly float[] PartyResistance = { 1.0f, 1.55f, 2.0f, 2.4f };
        static readonly float[] PartyAttackInterval = { 7.0f, 6.2f, 5.6f, 5.0f };
        static readonly float[] PartyTimeAssist = { 45f, 20f, 0f, 0f };
        static readonly float[] DiffResistance = { 0.75f, 1.0f, 1.2f };
        static readonly float[] DiffAttackInterval = { 1.25f, 1.0f, 0.9f };
        static readonly float[] DiffImpact = { 0.8f, 1.0f, 1.1f };
        static readonly float[] DiffTelegraph = { 1.25f, 1.0f, 0.9f };
        static readonly float[] DiffRoundTime = { -1f, 600f, 480f };
        static readonly float[] DiffForcedRecovery = { 4.5f, 3.5f, 3.0f };
        static readonly int[] DiffShakeTargets = { 2, 2, 4 };

        static int P(int players) => Mathf.Clamp(players, 1, 4) - 1;
        public static float Resistance(int players, Difficulty d) => PartyResistance[P(players)] * DiffResistance[(int)d];
        public static float AttackInterval(int players, Difficulty d) => PartyAttackInterval[P(players)] * DiffAttackInterval[(int)d];
        public static float ImpactMultiplier(Difficulty d) => DiffImpact[(int)d];
        public static float Windup(AttackId a, Difficulty d) => Mathf.Max(MinTelegraph, Attack(a).windup * DiffTelegraph[(int)d]);
        public static float ForcedRecoveryTime(Difficulty d) => DiffForcedRecovery[(int)d];
        public static int ShakeMaxTargets(Difficulty d) => DiffShakeTargets[(int)d];
        /// Tiempo de ronda en segundos; negativo = sin temporizador (Relajada).
        public static float RoundTime(int players, Difficulty d)
            => DiffRoundTime[(int)d] < 0 ? -1f : DiffRoundTime[(int)d] + PartyTimeAssist[P(players)];
    }
}
