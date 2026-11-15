using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Criterio de salida M1: intentos individuales completos con el cerebro del tio ENCENDIDO y sin teletransportes.
    /// LabAutopilot juega usando solo entradas de jugador (mirar, moverse, clics, E, Q, Espacio, agacharse).
    [Timeout(330000)] // por defecto Unity corta a los 180 s; un intento dificil puede durar mas
    public class SoloAttemptTests
    {
        const float TimeScale = 1f;   // tiempo real: con 4x el autopiloto controla mas grueso y el carrito pasaba en falso
        const float SimBudget = 300f; // segundos de juego (la ronda Normal individual dura 645 s; Dificil, 525 s)

        NpcBrain npc; NpcBody body; RoundDirector round; ExtractionZone zone; TransportCart cart; LabAutopilot auto;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false;
            LabInput.ClearSimulated();
            SceneManager.LoadScene("PhysicsLab");
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(PlayerControls.Local, "el anfitrion local deberia generar al jugador");
            yield return new WaitForFixedUpdate();
            npc = Object.FindAnyObjectByType<NpcBrain>(); body = npc.GetComponent<NpcBody>();
            round = Object.FindAnyObjectByType<RoundDirector>(); zone = Object.FindAnyObjectByType<ExtractionZone>();
            cart = Object.FindAnyObjectByType<TransportCart>(); auto = Object.FindAnyObjectByType<LabAutopilot>();
            Assert.IsNotNull(auto, "PhysicsLab debe incluir LabAutopilot (reconstruir la escena)");
            Assert.IsTrue(npc.brainEnabled, "el intento completo exige el cerebro encendido");
            Assert.AreEqual(1, round.partySize);
            Time.timeScale = TimeScale;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (auto) auto.StopRun();
            LabInput.ClearSimulated();
            yield return null;
        }

        IEnumerator Attempt(AutopilotMode mode, Difficulty diff)
        {
            round.difficulty = diff;
            round.Restart(); // aplica la dificultad desde cero (presupuesto del tio, intervalo de ataques)
            yield return new WaitForFixedUpdate();
            float start = Time.time;
            auto.StartRun(mode);
            while (round.State == RoundState.Running && Time.time - start < SimBudget) yield return null;
            if (auto.Running) { auto.StopRun(); Debug.Log("[Autopilot] TIMELINE (sin terminar)\n" + string.Join("\n", auto.Timeline)); }
            Debug.Log($"[Solo] {mode}/{diff}: {round.State} en {round.Elapsed:0.0}s, ataques={npc.AttacksStarted}, correas={cart.StrapCount}");
            Assert.AreEqual(RoundState.Success, round.State, $"{mode}/{diff}: el intento no termino (tio en {body.Body.position})");
            Assert.IsTrue(zone.Secured);
            Assert.AreEqual(NpcState.Secured, npc.State);
            Assert.Greater(npc.AttacksStarted, 0, "el tio deberia haber atacado durante el intento");
            Assert.AreEqual(0, GrabRegistry.Count, "asegurar suelta todos los agarres");
        }

        [UnityTest] public IEnumerator SoloAttempt_Drag_Normal_Succeeds() => Attempt(AutopilotMode.Drag, Difficulty.Normal);
        [UnityTest] public IEnumerator SoloAttempt_Drag_Hard_Succeeds() => Attempt(AutopilotMode.Drag, Difficulty.Hard);

        [UnityTest]
        public IEnumerator SoloAttempt_Cart_Normal_Succeeds()
        {
            yield return Attempt(AutopilotMode.Cart, Difficulty.Normal);
            Assert.Greater(cart.StrapCount, 0, "el modo carrito debe asegurar al tio con la correa (E)");
        }
    }
}
