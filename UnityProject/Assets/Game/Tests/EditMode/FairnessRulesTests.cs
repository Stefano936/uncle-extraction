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

        [Test] public void VisualSmoothing_ConvergesTheSameAt30_60_And240Fps()
        {
            float Run(float fps) { float x = 0f; for (float t = 0f; t < 0.25f - 1e-5f; t += 1f / fps) x = Mathf.Lerp(x, 1f, Smooth.Factor(0.5f, 1f / fps)); return x; }
            float a = Run(30f), b = Run(60f), c = Run(240f);
            Assert.AreEqual(b, a, 0.01f); Assert.AreEqual(b, c, 0.01f);
            Assert.AreEqual(0.5f, Smooth.Factor(0.5f, 1f / 60f), 1e-4f, "a 60 fps queda igual que antes");
        }

        [Test] public void Side_IsTheFreeLimb_PreferringTheTargetSide()
        {
            Assert.AreEqual(AttackSide.Right, NpcBrain.ChooseSide(AttackId.Slap, L(), 1f), "libre los dos: el del lado del objetivo");
            Assert.AreEqual(AttackSide.Left, NpcBrain.ChooseSide(AttackId.Slap, L(), -1f));
            Assert.AreEqual(AttackSide.Left, NpcBrain.ChooseSide(AttackId.Slap, L(ra: true), 1f), "brazo derecho sujeto: manotazo con el izquierdo");
            Assert.AreEqual(AttackSide.Right, NpcBrain.ChooseSide(AttackId.Slap, L(la: true), -1f), "brazo izquierdo sujeto: con el derecho");
            Assert.AreEqual(AttackSide.Left, NpcBrain.ChooseSide(AttackId.Kick, L(rl: true), 1f), "tobillo derecho sujeto: patea con la izquierda");
            Assert.AreEqual(AttackSide.Right, NpcBrain.ChooseSide(AttackId.Kick, L(ll: true), -1f));
            Assert.AreEqual(AttackSide.None, NpcBrain.ChooseSide(AttackId.Slap, L(true, true), 1f), "sin brazos libres no hay miembro");
            Assert.AreEqual(AttackSide.None, NpcBrain.ChooseSide(AttackId.Push, L(), 1f), "el empujon usa los dos");
        }
        [Test] public void GrabbingTheChosenLimb_DuringWindup_CutsTheAttack()
        {
            Assert.IsTrue(NpcBrain.SideStillFree(AttackId.Slap, AttackSide.Right, L(la: true)), "sujetar el otro brazo no lo corta");
            Assert.IsFalse(NpcBrain.SideStillFree(AttackId.Slap, AttackSide.Right, L(ra: true)), "sujetar el brazo elegido lo corta");
            Assert.IsFalse(NpcBrain.SideStillFree(AttackId.Kick, AttackSide.Left, L(ll: true)));
            Assert.IsFalse(NpcBrain.SideStillFree(AttackId.Push, AttackSide.None, L(la: true)), "empujon: cualquier brazo sujeto lo corta");
            Assert.IsTrue(NpcBrain.SideStillFree(AttackId.Shake, AttackSide.None, L(true, true, true, true)), "la sacudida no depende de miembros");
        }

        [Test] public void InteractPriority_AllFourAtOnce_IsComprehensible()
        {
            var K = PlayerHands.InteractKind.None;
            Assert.AreEqual(PlayerHands.InteractKind.HelpUp, PlayerHands.ChooseInteract(true, true, true, true, K, false), "compañero caido que se mira primero");
            Assert.AreEqual(PlayerHands.InteractKind.Unanchor, PlayerHands.ChooseInteract(false, true, true, true, K, false));
            Assert.AreEqual(PlayerHands.InteractKind.Van, PlayerHands.ChooseInteract(false, false, true, true, K, false), "camioneta antes que carrito");
            Assert.AreEqual(PlayerHands.InteractKind.Cart, PlayerHands.ChooseInteract(false, false, false, true, K, false));
            Assert.AreEqual(PlayerHands.InteractKind.None, PlayerHands.ChooseInteract(false, false, false, false, K, false));
        }
        [Test] public void InteractPriority_HoldingE_KeepsTheTarget_WhileItStaysValid()
        {
            Assert.AreEqual(PlayerHands.InteractKind.Van, PlayerHands.ChooseInteract(true, false, true, false, PlayerHands.InteractKind.Van, true), "cerrando la camioneta, un compañero que cae a la vista no corta el cierre");
            Assert.AreEqual(PlayerHands.InteractKind.HelpUp, PlayerHands.ChooseInteract(true, false, false, false, PlayerHands.InteractKind.Van, true), "si el objetivo deja de valer, pasa al siguiente");
            Assert.AreEqual(PlayerHands.InteractKind.HelpUp, PlayerHands.ChooseInteract(true, false, true, false, PlayerHands.InteractKind.Van, false), "al soltar E vuelve la prioridad normal");
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
