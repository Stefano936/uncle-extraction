using NUnit.Framework;
using UnityEngine;

namespace UncleExtraction.Tests
{
    /// Reglas de justicia (sesion 13): ataques segun extremidades libres y transicion tras una desconexion.
    public class FairnessRulesTests
    {
        static NpcBrain.Limbs L(bool la = false, bool ra = false, bool ll = false, bool rl = false) => new NpcBrain.Limbs { leftArm = la, rightArm = ra, leftLeg = ll, rightLeg = rl };

        [Test] public void FreeUncle_InReach_CanUseEveryLimbAttack()
        {
            var v = NpcBrain.AvailableAttacks(false, true, true, L());
            CollectionAssert.AreEquivalent(new[] { AttackId.Slap, AttackId.Push, AttackId.Kick }, v);
        }
        [Test] public void OneArmHeld_SlapWithTheOther_NoPush()
        {
            var v = NpcBrain.AvailableAttacks(true, true, true, L(la: true));
            CollectionAssert.Contains(v, AttackId.Slap); CollectionAssert.DoesNotContain(v, AttackId.Push);
        }
        [Test] public void BothArmsHeld_NoSlapNoPush_KickAndShakeRemain()
        {
            var v = NpcBrain.AvailableAttacks(true, true, true, L(true, true));
            CollectionAssert.AreEquivalent(new[] { AttackId.Shake, AttackId.Kick }, v);
        }
        [Test] public void BothAnklesHeld_NoKick()
        {
            var v = NpcBrain.AvailableAttacks(true, true, true, L(ll: true, rl: true));
            CollectionAssert.DoesNotContain(v, AttackId.Kick); CollectionAssert.Contains(v, AttackId.Slap);
        }
        [Test] public void AllLimbsHeld_OnlyShake()
            => CollectionAssert.AreEquivalent(new[] { AttackId.Shake }, NpcBrain.AvailableAttacks(true, true, true, L(true, true, true, true)));
        [Test] public void NoSpaceOrFallen_NoLimbAttacks()
        {
            CollectionAssert.IsEmpty(NpcBrain.AvailableAttacks(false, true, false, L()), "nadie al alcance");
            CollectionAssert.IsEmpty(NpcBrain.AvailableAttacks(false, false, true, L()), "caido");
        }

        RoundDirector NewRound(Difficulty d, int party)
        {
            var go = new GameObject("RoundTest"); var r = go.AddComponent<RoundDirector>();
            r.difficulty = d; r.UpdatePartySize(party, 0f); return r;
        }

        [Test] public void Drop_IsAppliedOnlyAfterGrace_AndKeepsElapsed()
        {
            var r = NewRound(Difficulty.Normal, 4);
            try
            {
                r.SetElapsedForTest(100f);
                r.UpdatePartySize(3, 10f);
                Assert.AreEqual(4, r.PartySize, "al instante no cambia");
                r.UpdatePartySize(3, 10f + RoundDirector.DisconnectGrace - 0.5f);
                Assert.AreEqual(4, r.PartySize, "dentro de la gracia no cambia");
                r.UpdatePartySize(3, 10f + RoundDirector.DisconnectGrace + 0.1f);
                Assert.AreEqual(3, r.PartySize, "tras la gracia se aplica");
                Assert.AreEqual(100f, r.Elapsed, 1e-3, "el tiempo transcurrido se conserva");
            }
            finally { Object.DestroyImmediate(r.gameObject); }
        }
        [Test] public void Reconnect_WithinGrace_CancelsTheDrop()
        {
            var r = NewRound(Difficulty.Normal, 3);
            try
            {
                r.UpdatePartySize(2, 0f); r.UpdatePartySize(3, 5f); r.UpdatePartySize(3, 20f);
                Assert.AreEqual(3, r.PartySize); Assert.Less(r.PendingDropIn, 0f);
            }
            finally { Object.DestroyImmediate(r.gameObject); }
        }
        [Test] public void Drop_NeverLeavesLessThanTheMinimumRemaining()
        {
            var r = NewRound(Difficulty.Hard, 4);
            try
            {
                float limit4 = r.TimeLimit; r.SetElapsedForTest(limit4 - 5f);
                r.UpdatePartySize(1, 0f); r.UpdatePartySize(1, RoundDirector.DisconnectGrace + 1f);
                Assert.AreEqual(1, r.PartySize);
                Assert.GreaterOrEqual(r.TimeLimit - r.Elapsed, RoundDirector.MinRemainingAfterDrop - 1e-3, "quedan al menos 60 s");
                Assert.AreEqual(RoundState.Running, r.State);
            }
            finally { Object.DestroyImmediate(r.gameObject); }
        }
    }
}
