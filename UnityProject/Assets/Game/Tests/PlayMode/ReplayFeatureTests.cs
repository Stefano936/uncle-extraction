using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Sesion 15: desafios opcionales viables, torre de vasos que reacciona y parte de extraccion con datos reales.
    [Timeout(330000)]
    public class ReplayFeatureTests
    {
        RoundDirector round; LabAutopilot auto; NpcBrain npc;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false; LabInput.ClearSimulated();
            SceneManager.LoadScene("Bar_LastRound");
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            round = Object.FindAnyObjectByType<RoundDirector>(); auto = Object.FindAnyObjectByType<LabAutopilot>(); npc = Object.FindAnyObjectByType<NpcBrain>();
        }
        [UnityTearDown]
        public IEnumerator TearDown() { if (auto) auto.StopRun(); LabInput.ClearSimulated(); if (round) round.challenge = ChallengeMode.Standard; yield return null; }

        IEnumerator Play(ChallengeMode mode)
        {
            round.challenge = mode; round.Restart(); yield return null; yield return null; yield return new WaitForFixedUpdate();
            auto.StartRun(AutopilotMode.Drag);
            float t0 = Time.time; while (round.State == RoundState.Running && Time.time - t0 < 280f) yield return null;
            yield return null; yield return null;
        }

        [UnityTest]
        public IEnumerator NoCart_TheCartIsPutAway_AndDraggingStillWins()
        {
            round.challenge = ChallengeMode.NoCart; round.Restart(); yield return null; yield return null;
            var cart = TransportCart.Instance;
            Assert.Greater(Vector3.Distance(cart.transform.position, Vector3.zero), 15f, "el carrito queda fuera de juego");
            yield return Play(ChallengeMode.NoCart);
            Debug.Log($"[Test] sin carrito: {round.State} en {round.Elapsed:0.0}s");
            Assert.AreEqual(RoundState.Success, round.State, "uno solo puede ganar sin carrito");
            StringAssert.Contains("challenge.played_nocart", RoundStats.LastReport);
            round.challenge = ChallengeMode.Standard; round.Restart(); yield return null; yield return null;
            Assert.Less(Vector3.Distance(cart.transform.position, new Vector3(5.6f, 0, 2.4f)), 1.5f, "vuelve a la ronda estandar con el carrito en su lugar");
        }

        [UnityTest]
        public IEnumerator Narrow_OneGapBarricade_DraggingThroughTheGapWins()
        {
            round.challenge = ChallengeMode.Narrow; round.Restart(); yield return null; yield return null;
            Assert.IsNotNull(GameObject.Find("Challenge_Narrow"), "la barricada existe");
            // el paso existe y mide al menos el ancho del carrito; a los costados esta cerrado
            Assert.IsFalse(Physics.CheckBox(new Vector3(-2.6f, 0.5f, Challenges.BarricadeZ), new Vector3(0.5f, 0.3f, 0.2f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore), "paso libre de 1,0 m");
            Assert.IsTrue(Physics.Raycast(new Vector3(1.5f, 0.5f, Challenges.BarricadeZ + 1f), Vector3.back, 2f), "el centro del salon esta cortado");
            yield return Play(ChallengeMode.Narrow);
            Debug.Log($"[Test] ruta estrecha: {round.State} en {round.Elapsed:0.0}s");
            Assert.AreEqual(RoundState.Success, round.State, "la ruta alternativa es viable para uno solo");
            StringAssert.Contains("report.m_first_grab", RoundStats.LastReport, "el parte trae momentos reales");
        }

        [UnityTest]
        public IEnumerator GlassTower_CartRushingIn_KnocksItDown_AndDistractsTheUncle()
        {
            npc.brainEnabled = false; yield return null;
            var props = BarProps.Instance; Assert.IsNotNull(props); Assert.IsFalse(props.Fallen);
            var cart = TransportCart.Instance; var rb = cart.GetComponent<Rigidbody>();
            Vector3 from = BarProps.TowerPos + new Vector3(2.0f, 0f, 0f); rb.position = from; cart.transform.position = from; yield return new WaitForFixedUpdate();
            for (int i = 0; i < 60 && !props.Fallen; i++) { rb.linearVelocity = new Vector3(-2.2f, 0f, 0f); yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(props.Fallen, "el carrito a 2,2 m/s tira la torre");
            Assert.AreEqual(NpcState.Distracted, npc.State, "el tio se distrae mirando el desastre");
            round.Restart(); yield return null; yield return null;
            Assert.IsFalse(BarProps.Instance.Fallen, "la torre se rearma al reiniciar");
        }
    }
}
