using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UncleExtraction.Tests
{
    public class GrabRegistryTests
    {
        class FakeOwner : IGrabOwner
        {
            public string OwnerName => "fake"; public bool CanGrab => true; public Vector3 origin;
            public Vector3 HandOrigin(int hand) => origin;
        }

        readonly List<GameObject> created = new List<GameObject>();

        GrabSocket MakeSocket(string id, int capacity = 1)
        {
            var go = new GameObject(id); created.Add(go);
            var rb = go.AddComponent<Rigidbody>();
            var s = go.AddComponent<GrabSocket>(); s.socketId = id; s.body = rb; s.capacity = capacity;
            return s;
        }

        [SetUp] public void SetUp() { GrabRegistry.ResetStatic(); GrabRegistry.Verbose = false; }
        [TearDown] public void TearDown() { GrabRegistry.ResetStatic(); foreach (var g in created) Object.DestroyImmediate(g); created.Clear(); }

        [Test] public void SameSocket_SecondOwnerGetsSocketBusy()
        {
            var s = MakeSocket("a"); var p1 = new FakeOwner(); var p2 = new FakeOwner();
            Assert.AreEqual(GrabResult.Ok, GrabRegistry.TryCreate(p1, 0, s, out _));
            Assert.AreEqual(GrabResult.SocketBusy, GrabRegistry.TryCreate(p2, 0, s, out _));
            Assert.AreEqual(1, GrabRegistry.Count);
        }

        [Test] public void SameHand_Twice_IsHandBusy()
        {
            var p = new FakeOwner();
            Assert.AreEqual(GrabResult.Ok, GrabRegistry.TryCreate(p, 0, MakeSocket("a"), out _));
            Assert.AreEqual(GrabResult.HandBusy, GrabRegistry.TryCreate(p, 0, MakeSocket("b"), out _));
        }

        [Test] public void Release_IsIdempotent_AndDoesNotTouchOthers()
        {
            var p1 = new FakeOwner(); var p2 = new FakeOwner();
            GrabRegistry.TryCreate(p1, 0, MakeSocket("a"), out var l1);
            GrabRegistry.TryCreate(p2, 0, MakeSocket("b"), out var l2);
            Assert.IsTrue(GrabRegistry.Release(l1.id, ReleaseReason.Released));
            Assert.IsFalse(GrabRegistry.Release(l1.id, ReleaseReason.Released));
            Assert.AreEqual(1, GrabRegistry.Count);
            Assert.AreSame(l2, GrabRegistry.Links[0]);
        }

        [Test] public void GlobalLimit_IsEightPlayerGrabs()
        {
            for (int i = 0; i < 4; i++)
            {
                var o = new FakeOwner();
                Assert.AreEqual(GrabResult.Ok, GrabRegistry.TryCreate(o, 0, MakeSocket("s" + i + "a"), out _));
                Assert.AreEqual(GrabResult.Ok, GrabRegistry.TryCreate(o, 1, MakeSocket("s" + i + "b"), out _));
            }
            Assert.AreEqual(GrabResult.GlobalLimit, GrabRegistry.TryCreate(new FakeOwner(), 0, MakeSocket("extra"), out _));
        }

        [Test] public void OutOfReach_IsRejected()
        {
            var s = MakeSocket("far"); s.transform.position = new Vector3(0, 0, 5);
            Assert.AreEqual(GrabResult.OutOfReach, GrabRegistry.TryCreate(new FakeOwner(), 0, s, out _));
        }

        [Test] public void ReleaseAllOf_CountsReason()
        {
            var p = new FakeOwner();
            GrabRegistry.TryCreate(p, 0, MakeSocket("a"), out _); GrabRegistry.TryCreate(p, 1, MakeSocket("b", 2), out _);
            GrabRegistry.ReleaseAllOf(p, ReleaseReason.Fell);
            Assert.AreEqual(0, GrabRegistry.Count);
            Assert.AreEqual(2, GrabRegistry.ReleasesByReason[ReleaseReason.Fell]);
        }
    }

    public class BalanceTests
    {
        [Test] public void EveryAttack_HasMinimumTelegraph_InEveryDifficulty()
        {
            foreach (Difficulty d in System.Enum.GetValues(typeof(Difficulty)))
                foreach (AttackId a in System.Enum.GetValues(typeof(AttackId)))
                    Assert.GreaterOrEqual(LabBalance.Windup(a, d), 0.8f, $"{a} {d}");
        }

        [Test] public void DerivedValues_MatchValidator()
        {
            Assert.AreEqual(645f, LabBalance.RoundTime(1, Difficulty.Normal), 1e-3);
            Assert.Less(LabBalance.RoundTime(2, Difficulty.Relaxed), 0f);
            Assert.AreEqual(2.88f, LabBalance.Resistance(4, Difficulty.Hard), 1e-3);
            Assert.AreEqual(8.75f, LabBalance.AttackInterval(1, Difficulty.Relaxed), 1e-3);
        }

        [Test] public void PushAndKick_KnockDownInNormal_SlapDoesNot()
        {
            float m = LabBalance.ImpactMultiplier(Difficulty.Normal);
            Assert.GreaterOrEqual(LabBalance.Attack(AttackId.Push).deltaV * m, LabBalance.DirectKnockdownDeltaV);
            Assert.GreaterOrEqual(LabBalance.Attack(AttackId.Kick).deltaV * m, LabBalance.DirectKnockdownDeltaV);
            Assert.Less(LabBalance.Attack(AttackId.Slap).deltaV * m, LabBalance.DirectKnockdownDeltaV);
        }
    }
}
