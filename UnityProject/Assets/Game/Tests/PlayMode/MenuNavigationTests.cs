using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UncleExtraction.Tests
{
    /// Menu con mando virtual (Input System) y dialogos de error. Comprueba navegacion y salidas; no la comodidad.
    [Timeout(90000)]
    public class MenuNavigationTests
    {
        Gamepad pad;
        [SetUp] public void AddPad() { pad = InputSystem.AddDevice<Gamepad>(); NetSession.StatusUntil = 0f; }
        [TearDown] public void RemovePad() { if (pad != null) InputSystem.RemoveDevice(pad); NetSession.StatusUntil = 0f; }

        IEnumerator Tap(GamepadButton b)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(b)); yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null;
        }

        static string Selected() { var s = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null; return s ? s.name : "(nada)"; }

        IEnumerator OpenMenu()
        {
            SceneManager.LoadScene("Frontend");
            yield return null; yield return null; yield return null;
        }

        [UnityTest]
        public IEnumerator Gamepad_NavigatesPages_AndGoesBack()
        {
            yield return OpenMenu();
            var menu = Object.FindAnyObjectByType<FrontendMenu>(); Assert.IsNotNull(menu);
            Assert.AreEqual("button.play_solo", Selected(), "el primer boton queda seleccionado para el mando");
            yield return Tap(GamepadButton.DpadDown);
            Assert.AreEqual("button.play_friends", Selected(), "la cruceta mueve el foco");
            yield return Tap(GamepadButton.South);
            Assert.AreEqual(FrontendMenu.Page.Friends, menu.Current, "A/cruz acepta");
            Assert.AreEqual("lobby.difficulty", Selected(), "al cambiar de pagina el foco va al primer control");
            yield return Tap(GamepadButton.East);
            Assert.AreEqual(FrontendMenu.Page.Main, menu.Current, "B/circulo vuelve");
            // opciones: entrar y salir sin teclado
            menu.Show(FrontendMenu.Page.Options); yield return null; yield return null;
            Assert.AreNotEqual("(nada)", Selected(), "opciones tiene foco inicial para el mando");
        }

        [UnityTest]
        public IEnumerator Gamepad_EntersRelayCode_AndCancelsWithoutLeavingFriends()
        {
            yield return OpenMenu();
            var menu = Object.FindAnyObjectByType<FrontendMenu>();
            menu.Show(FrontendMenu.Page.Friends);
            for (int i = 0; i < 3; i++) yield return Tap(GamepadButton.DpadDown);
            Assert.AreEqual("menu.edit_code", Selected());
            yield return Tap(GamepadButton.South);
            Assert.AreEqual("CodeKey_A", Selected());

            EventSystem.current.SetSelectedGameObject(GameObject.Find("menu.code_confirm"));
            yield return Tap(GamepadButton.South);
            Assert.AreEqual(Loc.T("error.invalid"), GameObject.Find("CodeKeyboard/Message/Error").GetComponent<Text>().text);
            Assert.AreEqual("", menu.ErrorShown);

            EventSystem.current.SetSelectedGameObject(GameObject.Find("CodeKey_A"));
            for (int i = 0; i < 6; i++) yield return Tap(GamepadButton.South);
            EventSystem.current.SetSelectedGameObject(GameObject.Find("menu.code_confirm"));
            yield return Tap(GamepadButton.South);
            Assert.AreEqual("button.join", Selected());
            var input = GameObject.Find("Code/Box").GetComponent<InputField>();
            Assert.AreEqual("AAAAAA", input.text);

            EventSystem.current.SetSelectedGameObject(GameObject.Find("menu.edit_code"));
            yield return Tap(GamepadButton.South);
            EventSystem.current.SetSelectedGameObject(GameObject.Find("menu.code_delete"));
            yield return Tap(GamepadButton.South);
            yield return Tap(GamepadButton.East);
            Assert.AreEqual(FrontendMenu.Page.Friends, menu.Current);
            Assert.AreEqual("menu.edit_code", Selected());
            Assert.AreEqual("AAAAAA", input.text);
        }

        [UnityTest]
        public IEnumerator Gamepad_TogglesCaptureView_AndRestoresResultsFocus()
        {
            yield return OpenMenu();
            var menu = Object.FindAnyObjectByType<FrontendMenu>();
            menu.Show(FrontendMenu.Page.Solo);
            GameObject.Find("button.start").GetComponent<Button>().onClick.Invoke();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(PlayerControls.Local);
            var round = Object.FindAnyObjectByType<RoundDirector>();
            typeof(RoundDirector).GetField("elapsed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(round, round.TimeLimit - 0.2f);
            var flow = Object.FindAnyObjectByType<GameFlowUI>();
            for (float t = 0; t < 3f && !flow.ResultsVisible; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(flow.ResultsVisible);
            EventSystem.current.SetSelectedGameObject(GameObject.Find("Snapshot"));
            foreach (var dismiss in new[] { GamepadButton.East, GamepadButton.South })
            {
                yield return Tap(GamepadButton.South);
                Assert.IsTrue(flow.CleanViewOn);
                Assert.AreEqual("(nada)", Selected());
                yield return Tap(dismiss);
                Assert.IsFalse(flow.CleanViewOn);
                Assert.AreEqual("Snapshot", Selected());
            }
            NetSession.LeaveToMenu();
            for (float t = 0; t < 5f && SceneManager.GetActiveScene().name != "Frontend"; t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual("Frontend", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator FriendsPage_OnlineAndLan_AreSeparate_AndBadCodeExplains()
        {
            yield return OpenMenu();
            var menu = Object.FindAnyObjectByType<FrontendMenu>();
            menu.Show(FrontendMenu.Page.Friends); yield return null;
            Assert.IsNotNull(GameObject.Find("menu.host_online"), "crear sala por internet");
            Assert.IsNotNull(GameObject.Find("button.join"), "unirse con codigo");
            Assert.IsNull(GameObject.Find("menu.host_lan"), "la red local va en su propia pagina");
            if (!RelayConnector.ProjectLinked)
                Assert.IsTrue(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text == Loc.T("menu.online_pending")), "sin vinculo, el menu lo dice antes de intentar");
            GameObject.Find("button.join").GetComponent<Button>().onClick.Invoke(); yield return null; yield return null;
            Assert.AreEqual("error.invalid", menu.ErrorShown, "codigo vacio: dialogo con motivo, sin cambiar de escena");
            Assert.AreEqual("Frontend", SceneManager.GetActiveScene().name);
            yield return Tap(GamepadButton.East);
            Assert.AreEqual("", menu.ErrorShown, "el dialogo se cierra con B");
            Assert.AreEqual(FrontendMenu.Page.Friends, menu.Current, "y se vuelve a donde estaba");
            menu.Show(FrontendMenu.Page.Lan); yield return null;
            Assert.IsNotNull(GameObject.Find("menu.host_lan")); Assert.IsNotNull(GameObject.Find("menu.join_lan"));
            yield return Tap(GamepadButton.East);
            Assert.AreEqual(FrontendMenu.Page.Friends, menu.Current, "desde la red local se vuelve a amigos");
        }

        [UnityTest]
        public IEnumerator HostLost_ShowsKitDialogWithDetail()
        {
            yield return OpenMenu();
            var menu = Object.FindAnyObjectByType<FrontendMenu>();
            NetSession.Say("error.host_left", 10f); yield return null; yield return null;
            Assert.AreEqual("error.host_left", menu.ErrorShown);
            var texts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).ToList();
            CollectionAssert.Contains(texts, Loc.T("error.host_left"));
            CollectionAssert.Contains(texts, Loc.T("error.host_left_detail"));
            Assert.AreEqual("Ok", Selected(), "el boton de salida tiene el foco");
            GameObject.Find("Ok").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.AreEqual("", menu.ErrorShown);
        }
    }
}
