using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Pruebas de comportamiento sobre la escena real PhysicsLab (debe estar en Build Settings).
    public class PhysicsLabPlayTests
    {
        NpcBrain npc; NpcBody body; PlayerMotor player; PlayerHands hands; RoundDirector round; LabForceRig rig;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false;
            SceneManager.LoadScene("PhysicsLab");
            yield return null; yield return new WaitForFixedUpdate();
            npc = Object.FindAnyObjectByType<NpcBrain>(); body = npc.GetComponent<NpcBody>();
            player = Object.FindAnyObjectByType<PlayerMotor>(); hands = player.GetComponent<PlayerHands>();
            round = Object.FindAnyObjectByType<RoundDirector>(); rig = Object.FindAnyObjectByType<LabForceRig>();
            npc.brainEnabled = false;
        }

        static GrabSocket Sock(string id) => GrabSocket.All.First(s => s.socketId == id);

        void PlacePlayer(Vector3 pos, float yaw)
        {
            player.Body.position = pos; player.transform.position = pos; player.Body.linearVelocity = Vector3.zero;
            player.SetYawPitch(yaw, 20f);
        }

        [UnityTest]
        public IEnumerator EightGrabs_ThenTenResets_LeaveNoOrphans()
        {
            rig.SetVirtualPullers(4);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(8, GrabRegistry.Count, "4 jugadores virtuales x 2 manos");
            for (int i = 0; i < 10; i++)
            {
                rig.SetVirtualPullers(4);
                yield return new WaitForSeconds(0.2f);
                round.Restart();
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(0, GrabRegistry.Count, $"agarres huerfanos tras reinicio {i + 1}");
                Assert.AreEqual(0, RoundDirector.CountGameplayJoints(), $"uniones huerfanas tras reinicio {i + 1}");
            }
            Assert.AreEqual(10, round.Restarts);
        }

        [UnityTest]
        public IEnumerator Grab_And_Drag_MovesUncle()
        {
            PlacePlayer(new Vector3(0, 0, 5.9f), 0f);
            yield return new WaitForFixedUpdate();
            hands.DebugHoldLeft = true; hands.DebugHoldRight = true;
            Assert.AreEqual(GrabResult.Ok, hands.ForceGrab(0, Sock("belt")));
            Assert.AreEqual(GrabResult.Ok, hands.ForceGrab(1, Sock("forearm_l")));
            Vector3 start = body.Body.position;
            player.DebugMove = new Vector2(0, -1);
            yield return new WaitForSeconds(3f);
            float moved = start.z - body.Body.position.z;
            Debug.Log($"[Test] drag moved {moved:0.00} m, grabs={GrabRegistry.Count}");
            Assert.Greater(moved, 0.3f, "el tio deberia acercarse al jugador que retrocede");
        }

        [UnityTest]
        public IEnumerator Push_KnocksPlayerDown_ReleasesHands_AndRecovers()
        {
            PlacePlayer(new Vector3(0, 0, 6.0f), 0f);
            yield return new WaitForFixedUpdate();
            hands.DebugHoldLeft = true;
            Assert.AreEqual(GrabResult.Ok, hands.ForceGrab(0, Sock("belt")));
            Assert.IsTrue(npc.ForceAttack(AttackId.Push));
            yield return new WaitForSeconds(LabBalance.Windup(AttackId.Push, Difficulty.Normal) + 0.3f);
            Assert.GreaterOrEqual(player.FallCount, 1, "el empujon en Normal debe derribar");
            Assert.IsFalse(hands.AnyHeld, "caer suelta todo");
            hands.DebugHoldLeft = false;
            yield return new WaitForSeconds(LabBalance.FallLockMax + player.getUpDuration + 0.2f);
            Assert.AreEqual(PlayerState.Active, player.State);
            Assert.IsTrue(player.Invulnerable, "invulnerabilidad al levantarse");
        }

        [UnityTest]
        public IEnumerator Extraction_SecuresWhenRequiredPointsStayInZone()
        {
            var zone = Object.FindAnyObjectByType<ExtractionZone>();
            Vector3 p = zone.transform.position; p.y = 0.02f;
            body.Body.position = p; body.transform.position = p; body.Body.linearVelocity = Vector3.zero;
            yield return new WaitForSeconds(LabBalance.ExtractionHold + 0.5f);
            Assert.IsTrue(zone.Secured);
            Assert.AreEqual(RoundState.Success, round.State);
            Assert.AreEqual(NpcState.Secured, npc.State);
        }
    }
}
