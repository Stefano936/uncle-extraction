using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Ciclo de la guia de primeros pasos (sesion 14, hallazgo A): ganar la completa, perder la deja donde estaba y se
    /// retoma al reintentar, oculta no avanza. El recorrido real lo hace el autopiloto con entradas de jugador.
    [Timeout(330000)]
    public class GuideLifecycleTests
    {
        RoundDirector round; LabAutopilot auto; FirstSteps fs;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false; LabInput.ClearSimulated();
            PlayerPrefs.DeleteKey("ue.firstSteps"); PlayerPrefs.DeleteKey("ue.firstSteps.step");
            SceneManager.LoadScene("Bar_LastRound");
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(PlayerControls.Local);
            round = Object.FindAnyObjectByType<RoundDirector>(); auto = Object.FindAnyObjectByType<LabAutopilot>();
            for (float t = 0; t < 2f && FirstSteps.Instance == null; t += Time.unscaledDeltaTime) yield return null;
            fs = FirstSteps.Instance; Assert.IsNotNull(fs, "la guia existe en la partida");
            Assert.IsTrue(fs.Active, "sin estado guardado, la guia empieza sola");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (auto) auto.StopRun(); LabInput.ClearSimulated(); RoundDirector.TimeLimitOverride = -1f;
            var gf = Object.FindAnyObjectByType<GameFlowUI>(); if (gf) gf.SetPaused(false);
            PlayerPrefs.DeleteKey("ue.firstSteps"); PlayerPrefs.DeleteKey("ue.firstSteps.step");
            yield return null;
        }

        [UnityTest]
        public IEnumerator WinningByDragging_CompletesTheGuide_WithoutRequiringTheCart()
        {
            Assert.IsNotNull(auto, "Bar_LastRound incluye el autopiloto");
            var seen = new List<FirstSteps.Step>();
            auto.StartRun(AutopilotMode.Drag);
            float start = Time.time;
            while (round.State == RoundState.Running && Time.time - start < 280f)
            {
                if (fs.Active && (seen.Count == 0 || seen[seen.Count - 1] != fs.Current)) seen.Add(fs.Current);
                yield return null;
            }
            yield return null; yield return null;
            Debug.Log("[Test] pasos de la guia vistos: " + string.Join(" > ", seen));
            Assert.AreEqual(RoundState.Success, round.State, "el autopiloto gana arrastrando");
            Assert.IsFalse(fs.Active, "ganar cierra la guia");
            Assert.AreEqual(FirstSteps.Outcome.Completed, FirstSteps.Saved, "queda registrada como completada (no como omitida)");
            Assert.IsFalse(TransportCart.Instance && TransportCart.Instance.IsStrapped, "se gano sin el carrito");
            Assert.Contains(FirstSteps.Step.Extract, seen, "el traslado arrastrando lleva al paso de la camioneta sin exigir el carrito");
            // otra partida (recarga de escena): no reaparece sola
            SceneManager.LoadScene("Bar_LastRound"); yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 3f && (FirstSteps.Instance == null || FirstSteps.Instance == fs); t += Time.unscaledDeltaTime) yield return null;
            yield return null;
            Assert.IsFalse(FirstSteps.Instance.Active, "completada: no vuelve a aparecer en otra partida");
            FirstSteps.Instance.Begin(); yield return null;
            Assert.AreEqual("tut.move", FirstSteps.Instance.CurrentKey, "repetir desde la pausa sigue funcionando");
        }

        [UnityTest]
        public IEnumerator Losing_DoesNotCompleteIt_AndRetryResumesTheSameStep()
        {
            RoundDirector.TimeLimitOverride = 4f; round.Restart(); yield return null;
            LabInput.SimulatedMove = new Vector2(0f, 1f); yield return new WaitForSeconds(1.2f); LabInput.SimulatedMove = null;
            Assert.AreEqual("tut.aim", fs.CurrentKey, "caminar pasa al segundo paso");
            while (round.State == RoundState.Running) yield return null;
            Assert.AreEqual(RoundState.Timeout, round.State);
            yield return null;
            Assert.IsTrue(fs.Active, "perder no termina la guia");
            Assert.AreEqual(FirstSteps.Outcome.None, FirstSteps.Saved, "perder no se registra como aprendido");
            Assert.AreEqual((int)FirstSteps.Step.Aim, PlayerPrefs.GetInt("ue.firstSteps.step", -1), "el paso queda guardado");
            RoundDirector.TimeLimitOverride = -1f; round.Restart(); yield return null; yield return null;
            Assert.AreEqual("tut.aim", fs.CurrentKey, "el reintento sigue desde el mismo paso");
            // otra partida: tambien sigue desde ahi
            SceneManager.LoadScene("Bar_LastRound"); yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 3f && (FirstSteps.Instance == null || FirstSteps.Instance == fs); t += Time.unscaledDeltaTime) yield return null;
            yield return null;
            Assert.AreEqual("tut.aim", FirstSteps.Instance.CurrentKey, "otra partida retoma el paso guardado");
        }

        [UnityTest]
        public IEnumerator Hidden_ByPauseOrOptions_DoesNotAdvance_NorRunItsTimers()
        {
            var gf = Object.FindAnyObjectByType<GameFlowUI>(); Assert.IsNotNull(gf);
            gf.SetPaused(true); yield return null;
            Assert.IsTrue(GameFlowUI.BlockingInput);
            // con la pausa abierta, algo mueve al jugador (empujon de un compañero, simulado): la leccion no avanza
            var pc = PlayerControls.Local; var rb = pc.GetComponent<Rigidbody>();
            Vector3 p = rb.position + pc.transform.forward * 2.5f; rb.position = p; pc.transform.position = p;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual("tut.move", fs.CurrentKey, "oculta no avanza por movimientos ajenos");
            gf.SetPaused(false); yield return null; yield return null;
            LabInput.SimulatedMove = new Vector2(0f, 1f); yield return new WaitForSeconds(1.2f); LabInput.SimulatedMove = null;
            Assert.AreEqual("tut.aim", fs.CurrentKey, "visible, avanza con el movimiento propio");
        }
    }
}
