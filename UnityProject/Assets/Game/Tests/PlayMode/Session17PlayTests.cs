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

        /// 4.1: la campana distrae al tio (o abre la ventana si esta aferrado), suena una vez por toque, tiene espera y tope por
        /// ronda, y el reinicio la rearma.
        [UnityTest]
        public IEnumerator LastCallBell_DistractsWithCooldown_AndRearmsEachRound()
        {
            yield return Restart(ChallengeMode.Standard);
            var npc = NpcBrain.Instance; var props = BarProps.Instance; var hands = PlayerControls.Local.GetComponent<PlayerHands>();
            Assert.IsTrue(props.HasBell, "la campana esta en la barra");
            for (float t = 0; t < 1f; t += Time.deltaTime) yield return null;
            int rings0 = props.BellRings; Assert.AreEqual("", props.BellBlock());
            props.RingBell(hands); yield return null;
            Assert.AreEqual(rings0 + 1, props.BellRings, "suena una vez");
            Assert.IsTrue(npc.State == NpcState.Distracted || npc.BellWindow, $"el tio reacciona (estado {npc.State})");
            Assert.AreNotEqual("", props.BellBlock(), "espera antes de volver a tocarla");
            props.RingBell(hands); yield return null;
            Assert.AreEqual(rings0 + 1, props.BellRings, "en espera no suena");
            Assert.IsTrue(RoundStats.Instance != null);
            yield return Restart(ChallengeMode.Standard);
            Assert.AreEqual(0, BarProps.Instance.BellUses, "el reinicio la rearma"); Assert.AreEqual("", BarProps.Instance.BellBlock());
        }

        /// 4.2: condicion inicial distinta (karaoke): el tio arranca en la tarima, se mantiene en rondas seguidas, la ronda
        /// estandar lo devuelve a la barra y se puede ganar arrastrandolo desde el karaoke.
        [UnityTest]
        public IEnumerator Karaoke_StartsAcrossTheRoom_PersistsAndRestores_AndIsWinnable()
        {
            var nb = NpcBrain.Instance.GetComponent<NpcBody>();
            Vector3 barStart = nb.SpawnPosition;
            for (int k = 0; k < 2; k++)
            {
                yield return Restart(ChallengeMode.Karaoke); yield return new WaitForSeconds(0.3f);
                Vector3 c = nb.ChestPosition; c.y = 0f;
                Assert.Less(Vector3.Distance(c, Challenges.KaraokeHome), 1.2f, $"ronda {k + 1}: arranca en el karaoke ({c})");
                Assert.AreEqual("Karaoke_Rail", NpcBrain.Instance.anchorRail.name, "se aferra a la baranda de la tarima");
            }
            yield return Restart(ChallengeMode.Standard); yield return new WaitForSeconds(0.3f);
            Vector3 b = nb.ChestPosition; b.y = 0f; Vector3 bs = barStart; bs.y = 0f;
            Assert.Less(Vector3.Distance(b, bs), 1.2f, $"Estandar: vuelve a la barra ({b})");
            Assert.AreNotEqual("Karaoke_Rail", NpcBrain.Instance.anchorRail.name);
            yield return Restart(ChallengeMode.Karaoke);
            auto.StartRun(AutopilotMode.Drag);
            float t0 = Time.time; while (round.State == RoundState.Running && Time.time - t0 < 280f) yield return null;
            Debug.Log($"[Test] karaoke arrastrando: {round.State} en {round.Elapsed:0.0}s");
            Assert.AreEqual(RoundState.Success, round.State);
        }
    }
}
