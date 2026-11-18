using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UncleExtraction.Tests
{
    /// Interfaz del kit conectada al estado real (UI_SPEC 4-7; ui_acceptance): manos, aviso de ataque, idioma,
    /// resultados, menu y remapeo. No prueba legibilidad ni comodidad: eso queda para la prueba humana.
    [Timeout(120000)]
    public class UiTests
    {
        string lang0;
        [SetUp] public void Remember() { lang0 = Loc.Language; LabInput.ClearSimulated(); }
        [TearDown] public void Restore() { Loc.SetLanguage(lang0); InputBindings.ResetAll(); LabInput.ClearSimulated(); }

        IEnumerator LoadGame(string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(PlayerControls.Local);
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator Hud_ShowsRealHandsAttackLanguageAndResults()
        {
            yield return LoadGame("Bar_LastRound");
            var hud = GameHud.Instance; Assert.IsNotNull(hud, "HUD del kit en la escena de juego");
            var rd = RoundDirector.Instance; Assert.IsTrue(hud.Visible, $"HUD oculto: ronda={(rd ? rd.State.ToString() : "-")} sala={(rd && rd.InLobby)} pausa={(GameFlowUI.Instance && GameFlowUI.Instance.Paused)} local={(PlayerControls.Local != null)} modo={NetSession.Mode}");
            var npc = Object.FindAnyObjectByType<NpcBrain>(); npc.brainEnabled = false;
            var body = npc.GetComponent<NpcBody>(); var hands = PlayerControls.Local.GetComponent<PlayerHands>(); var player = hands.GetComponent<PlayerMotor>();
            Assert.AreEqual(GameHud.HandState.Free, hud.StateOf(0));
            // agarrar el cinturon: la mano izquierda pasa a SUJETANDO con el punto
            Vector3 front = body.Body.position + body.Forward * 0.9f; front.y = 0f;
            player.Body.position = front; player.transform.position = front;
            yield return new WaitForFixedUpdate();
            hands.DebugHoldLeft = true;
            Assert.AreEqual(GrabResult.Ok, hands.ForceGrab(0, GrabSocket.ById("belt")));
            yield return null; yield return null;
            Assert.That(hud.StateOf(0), Is.EqualTo(GameHud.HandState.Holding).Or.EqualTo(GameHud.HandState.Strained));
            hands.DebugHoldLeft = false;
            // aviso de ataque localizado; cambia con el idioma sin reiniciar
            Loc.SetLanguage(Loc.EsUY);
            Assert.IsTrue(npc.ForceAttack(AttackId.Kick));
            yield return null; yield return null;
            Assert.AreEqual(Loc.T("hud.kick"), hud.AttackText);
            StringAssert.Contains("PATEAR", hud.AttackText);
            Loc.SetLanguage(Loc.En);
            yield return null;
            Assert.AreEqual("KICK INCOMING", hud.AttackText);
            yield return new WaitForSeconds(3f);
            // resultados con datos reales al asegurar
            Object.FindAnyObjectByType<ExtractionZone>().Secure();
            yield return new WaitForSecondsRealtime(0.8f);
            var flow = GameFlowUI.Instance; Assert.IsNotNull(flow);
            Assert.IsTrue(flow.ResultsVisible, "resultados en pantalla");
            Assert.AreEqual(Loc.T("round.success"), flow.ResultsTitle);
            Assert.IsFalse(hud.Visible, "al terminar se limpian los avisos de juego");
            Assert.IsTrue(GameFlowUI.BlockingInput);
        }

        [UnityTest]
        public IEnumerator Menu_PagesAndLanguage()
        {
            SceneManager.LoadScene("Frontend");
            yield return null; yield return null;
            var menu = Object.FindAnyObjectByType<FrontendMenu>(); Assert.IsNotNull(menu);
            Assert.AreEqual(FrontendMenu.Page.Main, menu.Current);
            Loc.SetLanguage(Loc.EsUY); yield return null;
            var texts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).ToList();
            CollectionAssert.Contains(texts, "JUGAR SOLO");
            Loc.SetLanguage(Loc.En); yield return null;
            texts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).ToList();
            CollectionAssert.Contains(texts, "PLAY SOLO");
            menu.Show(FrontendMenu.Page.Options); yield return null;
            Assert.IsNotNull(GameObject.Find("Options"), "panel de opciones");
            menu.Show(FrontendMenu.Page.Friends); yield return null;
            Assert.IsNull(GameObject.Find("Options"), "al cambiar de pagina se cierra");
            menu.Show(FrontendMenu.Page.Main); yield return null;
        }

        [Test]
        public void Rebind_SwapsConflicts_AndResets()
        {
            InputBindings.ResetAll();
            Assert.AreEqual("<Keyboard>/e", InputBindings.Path(Act.Interact));
            var swapped = InputBindings.Set(Act.Interact, "<Keyboard>/q"); // Q era «soltar todo»
            Assert.AreEqual(Act.ReleaseAll, swapped);
            Assert.AreEqual("<Keyboard>/q", InputBindings.Path(Act.Interact));
            Assert.AreEqual("<Keyboard>/e", InputBindings.Path(Act.ReleaseAll), "no quedan dos acciones en la misma tecla");
            InputBindings.ResetAll();
            Assert.AreEqual("<Keyboard>/q", InputBindings.Path(Act.ReleaseAll));
        }
    
        [UnityTest]
        public IEnumerator Lobby_HostStartsOnlyWhenReady_AndRetryReturnsToLobby()
        {
            NetSession.Mode = NetMode.Host; NetSession.Port = 7795; NetSession.ListenAddress = "127.0.0.1";
            try
            {
                yield return LoadGame("Bar_LastRound");
                var round = RoundDirector.Instance; var flow = GameFlowUI.Instance;
                Assert.IsTrue(round.InLobby, "en red la ronda empieza en la sala");
                Assert.IsTrue(GameFlowUI.BlockingInput, "en la sala no se controla al personaje");
                float t0 = round.Elapsed; yield return new WaitForSeconds(0.6f);
                Assert.AreEqual(t0, round.Elapsed, 0.01f, "el reloj no corre en la sala");
                Assert.IsTrue(flow.LobbyVisible);
                Assert.IsFalse(round.StartFromLobby(), "sin confirmar no se puede iniciar");
                PlayerControls.Local.SetReady(true); yield return null;
                Assert.IsTrue(round.StartFromLobby());
                yield return null;
                Assert.IsFalse(round.InLobby); Assert.IsFalse(flow.LobbyVisible);
                // reintento en red: vuelve a la sala con la confirmacion limpia
                round.BackToLobby(); yield return null;
                Assert.IsTrue(round.InLobby);
                Assert.IsFalse(PlayerControls.Local.Ready, "la confirmacion se limpia al volver a la sala");
            }
            finally { NetSession.Mode = NetMode.Solo; NetSession.Port = 7777; NetSession.ListenAddress = "0.0.0.0"; }
        }
    }
}
