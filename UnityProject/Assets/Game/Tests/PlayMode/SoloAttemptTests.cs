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

        protected virtual string Scene => "PhysicsLab";
        NpcBrain npc; NpcBody body; RoundDirector round; ExtractionZone zone; TransportCart cart; LabAutopilot auto;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false;
            LabInput.ClearSimulated();
            SceneManager.LoadScene(Scene);
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
            float wriggleEligible = 0f; // tiempo atado al carrito con fuerzas para intentar soltarse (regla del cerebro)
            while (round.State == RoundState.Running && Time.time - start < SimBudget)
            {
                if (cart.IsStrapped && npc.Budget > npc.BudgetMax * NpcBrain.WriggleMinBudget01) wriggleEligible += Time.deltaTime;
                yield return null;
            }
            if (auto.Running) { auto.StopRun(); Debug.Log("[Autopilot] TIMELINE (sin terminar)\n" + string.Join("\n", auto.Timeline)); }
            Debug.Log($"[Solo] {Scene} {mode}/{diff}: {round.State} en {round.Elapsed:0.0}s, ataques={npc.AttacksStarted}, correas={cart.StrapCount}");
            Assert.AreEqual(RoundState.Success, round.State, $"{mode}/{diff}: el intento no termino (tio en {body.pelvis.position})");
            Assert.IsTrue(zone.Secured);
            Assert.AreEqual(NpcState.Secured, npc.State);
            // Resistencia activa, separada por modo: arrastrandolo, el tio TIENE que atacar; en el carrito, TIENE que intentar
            // soltarse (GDD 05). Aceptar una u otra en general ocultaria que alguna dejo de funcionar.
            if (mode == AutopilotMode.Drag) Assert.Greater(npc.AttacksStarted, 0, "arrastrandolo, el tio deberia haber atacado");
            else
            {   // Agotado (presupuesto < 40 %) el tio no intenta soltarse: es la regla de fatiga, no un fallo. Si estuvo atado
                // con fuerzas mas que el intervalo maximo entre intentos, TIENE que haberlo intentado. La conducta en si la
                // exige siempre Cart_StrappedUncle_SignalsAndTriesToWriggleFree.
                Debug.Log($"[Solo] {Scene} carrito: atado con fuerzas {wriggleEligible:0.0}s, intentos de soltarse={npc.WrigglesStarted}");
                if (wriggleEligible > NpcBrain.WriggleMaxInterval + 0.5f)
                    Assert.Greater(npc.WrigglesStarted, 0, $"atado {wriggleEligible:0.0}s con fuerzas, el tio deberia haber intentado soltarse");
            }
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

    /// Los mismos intentos completos en el escenario principal (no se valida Bar_LastRound con resultados de PhysicsLab).
    [Timeout(330000)] // los atributos de la clase base no se heredan en NUnit
    public class SoloAttemptTests_Bar : SoloAttemptTests
    {
        protected override string Scene => "Bar_LastRound";
    }

    /// Intentos completos con el tio articulado en el laboratorio.
    [Timeout(330000)]
    public class SoloAttemptTests_Articulated : SoloAttemptTests
    {
        protected override string Scene => "PhysicsLab_Articulated";
    }
}
