using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Flujo completo sin intervencion del editor (GDD P0): menu -> partida individual -> fin de ronda -> volver al menu.
    public class GameFlowTests
    {
        [UnityTearDown]
        public IEnumerator TearDown() { Time.timeScale = 1f; LabInput.ClearSimulated(); RoundDirector.RequestedDifficulty = null; yield return null; }

        [UnityTest]
        public IEnumerator Menu_SoloHard_Timeout_Results_BackToMenu()
        {
            SceneManager.LoadScene("Frontend");
            yield return null; yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<FrontendMenu>(), "Frontend carga el menu");

            // Elegir "Jugar solo" en Dificil (mismo camino que los botones del menu)
            GameSettings.SoloDifficulty = Difficulty.Hard;
            NetSession.Mode = NetMode.Solo; RoundDirector.RequestedDifficulty = GameSettings.SoloDifficulty;
            SceneManager.LoadScene(FrontendMenu.GameScene);
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(PlayerControls.Local, "la partida individual genera al jugador local");
            Assert.IsTrue(NetworkManager.Singleton.IsHost, "individual = anfitrion en loopback");
            var round = Object.FindAnyObjectByType<RoundDirector>();
            yield return null;
            Assert.AreEqual(Difficulty.Hard, round.CurrentDifficulty, "la dificultad del menu llega a la ronda");

            // Derrota por tiempo: adelantar el reloj de la ronda (solo prueba) y comprobar resultados
            typeof(RoundDirector).GetField("elapsed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(round, round.TimeLimit - 0.2f);
            for (float t = 0; t < 2f && round.State == RoundState.Running; t += Time.deltaTime) yield return null;
            Assert.AreEqual(RoundState.Timeout, round.State, "se pierde al agotar el tiempo");
            Assert.IsTrue(GameFlowUI.BlockingInput, "con resultados en pantalla no se controla al personaje");

            // Reintentar desde resultados
            round.Restart();
            yield return null;
            Assert.AreEqual(RoundState.Running, round.State);
            Assert.AreEqual(1, round.Restarts);

            // Volver al menu
            NetSession.LeaveToMenu();
            for (float t = 0; t < 5f && SceneManager.GetActiveScene().name != "Frontend"; t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual("Frontend", SceneManager.GetActiveScene().name);
            yield return null;
            Assert.IsFalse(NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening, "la sesion se cerro");
        }
    }
}
