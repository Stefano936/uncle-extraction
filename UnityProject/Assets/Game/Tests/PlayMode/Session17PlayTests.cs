using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Sesion 17: Sin carrito en cada ronda y torre de vasos ligada al numero de ronda.
    [Timeout(330000)]
    public class Session17PlayTests
    {
        RoundDirector round; LabAutopilot auto;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false; LabInput.ClearSimulated();
            SceneManager.LoadScene("Bar_LastRound");
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            round = Object.FindAnyObjectByType<RoundDirector>(); auto = Object.FindAnyObjectByType<LabAutopilot>();
        }
        [UnityTearDown] public IEnumerator TearDown() { if (auto) auto.StopRun(); LabInput.ClearSimulated(); if (round) round.challenge = ChallengeMode.Standard; yield return null; }

        static bool CartUsable(TransportCart c) =>
            c.GetComponentsInChildren<Collider>(true).Any(x => x.enabled) && c.GetComponentsInChildren<GrabSocket>(true).Any(s => s.enabled && GrabSocket.All.Contains(s));
        static bool CartVisible(TransportCart c) => c.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled);
        IEnumerator Restart(ChallengeMode m) { round.challenge = m; round.Restart(); yield return null; yield return null; yield return new WaitForFixedUpdate(); yield return null; }

        [UnityTest]
        public IEnumerator NoCart_StaysAway_AcrossRestarts_AndStandardRestoresAPlayableCart()
        {
            var cart = TransportCart.Instance; Vector3 home = cart.SpawnPos;
            for (int k = 0; k < 3; k++)
            {
                yield return Restart(ChallengeMode.NoCart);
                Assert.Greater(Vector3.Distance(cart.transform.position, home), 10f, $"ronda {k + 1} en Sin carrito: el carrito sigue guardado");
                Assert.IsFalse(CartVisible(cart), "no se ve"); Assert.IsFalse(CartUsable(cart), "no choca ni se agarra");
            }
            yield return Restart(ChallengeMode.Standard);
            Assert.Less(Vector3.Distance(cart.transform.position, home), 0.5f, "Estandar: vuelve a su lugar");
            Assert.IsTrue(CartVisible(cart) && CartUsable(cart), "Estandar: visible, con colision y asa");
            Assert.IsFalse(cart.GetComponent<Rigidbody>().isKinematic, "con fisica");
            // y se puede ganar con el carrito
            auto.StartRun(AutopilotMode.Cart);
            float t0 = Time.time; while (round.State == RoundState.Running && Time.time - t0 < 280f) yield return null;
            Debug.Log($"[Test] estandar con carrito tras Sin carrito: {round.State} en {round.Elapsed:0.0}s");
            Assert.AreEqual(RoundState.Success, round.State);
        }

        [UnityTest]
        public IEnumerator GlassTower_RearmsEveryRound_FallsOncePerRound_AndIgnoresStaleEvents()
        {
            var npc = NpcBrain.Instance; npc.brainEnabled = false;
            var props = BarProps.Instance;
            for (int k = 0; k < 2; k++)
            {
                yield return Restart(ChallengeMode.Standard); npc.brainEnabled = false; yield return null;
                props = BarProps.Instance; Assert.IsFalse(props.Fallen, $"ronda {k + 1}: torre armada");
                int r = round.Restarts;
                props.Fall(Vector3.right, r - 1); Assert.IsFalse(props.Fallen, "un evento de la ronda anterior se descarta");
                int falls0 = props.Falls;
                round.TowerFall(Vector3.right); round.TowerFall(Vector3.right); yield return null;
                Assert.IsTrue(props.Fallen); Assert.AreEqual(falls0 + 1, props.Falls, "una sola caida por ronda");
            }
        }
    }
}
