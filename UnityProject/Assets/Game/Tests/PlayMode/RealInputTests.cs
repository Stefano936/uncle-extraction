using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UncleExtraction.Tests
{
    /// Controles por la via real (sesion 13): teclado y raton simulados del Input System -> InputBindings -> GrabIntent ->
    /// manos del anfitrion local. Cubre los hallazgos A (flechas), B (movimiento remapeable), C (alternar), D (Q con clics
    /// sostenidos), E (campos de texto y menus), G (borrador de controles) y H (tamano de texto durante la partida).
    [Timeout(120000)]
    public class RealInputTests
    {
        Keyboard kb; Mouse mouse;
        InputSettings.BackgroundBehavior bg0; InputSettings.EditorInputBehaviorInPlayMode ed0;
        PlayerMotor player; PlayerHands hands; NpcBrain npc;

        [UnitySetUp]
        public IEnumerator Load()
        {
            LabInput.ClearSimulated(); LabInput.TestFocus = true; GameSettings.GrabToggle = false; GrabIntent.Reset();
            // editor en lote sin foco: que el Input System no desactive teclado y raton en segundo plano
            bg0 = InputSystem.settings.backgroundBehavior; ed0 = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            kb = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>(); kb.MakeCurrent(); mouse.MakeCurrent();
            SceneManager.LoadScene("Bar_LastRound");
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            player = Object.FindAnyObjectByType<PlayerMotor>(); hands = player.GetComponent<PlayerHands>();
            npc = Object.FindAnyObjectByType<NpcBrain>(); npc.brainEnabled = false;
            yield return null;
        }

        [TearDown]
        public void Clear()
        {
            LabInput.ClearSimulated(); LabInput.TestFocus = false; GameSettings.GrabToggle = false; GrabIntent.Reset();
            if (kb != null) InputSystem.RemoveDevice(kb); if (mouse != null) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = bg0; InputSystem.settings.editorInputBehaviorInPlayMode = ed0;
            InputBindings.ResetAll(); InputBindings.SaveAll();
        }

        IEnumerator Keys(params Key[] keys) { InputSystem.QueueStateEvent(kb, new KeyboardState(keys)); yield return null; yield return null; }
        IEnumerator Buttons(bool left, bool right)
        {
            var st = new MouseState(); if (left) st = st.WithButton(MouseButton.Left); if (right) st = st.WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(mouse, st); yield return null; yield return null;
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// Frente al tio, mirando su hombro (punto agarrable).
        IEnumerator FaceUncle(int pick = 0)
        {
            var socket = Object.FindObjectsByType<GrabSocket>(FindObjectsSortMode.None).Where(s => s.body && s.body.GetComponentInParent<NpcBody>()).OrderByDescending(s => s.Position.y).ElementAt(pick);
            Vector3 target = socket.Position, at = target + (npc.transform.forward * 0.9f); at.y = 0f;
            if (pick == 0) { player.GetComponent<Rigidbody>().position = at; player.transform.position = at; }
            yield return new WaitForFixedUpdate();
            for (int i = 0; i < 6; i++)
            {
                Vector3 eye = player.EyePosition, to = target - eye, f = new Vector3(to.x, 0, to.z);
                player.SetYawPitch(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, -Mathf.Atan2(to.y, f.magnitude) * Mathf.Rad2Deg);
                yield return new WaitForFixedUpdate(); yield return null;
            }
            Assert.IsNotNull(hands.Candidate, "deberia apuntar a un punto agarrable del tio");
        }

        [UnityTest]
        public IEnumerator Move_WasdAndArrows_DiagonalNormalized_NoDoubleSpeed()
        {
            yield return Keys(Key.W); Assert.AreEqual(new Vector2(0, 1), LabInput.Move(), "W");
            yield return Keys(Key.UpArrow); Assert.AreEqual(new Vector2(0, 1), LabInput.Move(), "flecha arriba (hallazgo A)");
            yield return Keys(Key.W, Key.UpArrow); Assert.AreEqual(1f, LabInput.Move().magnitude, 1e-4f, "W y flecha juntas no suman");
            yield return Keys(Key.W, Key.D); Assert.AreEqual(1f, LabInput.Move().magnitude, 1e-4f, "diagonal normalizada");
            yield return Keys(Key.UpArrow, Key.RightArrow, Key.W, Key.D); Assert.AreEqual(1f, LabInput.Move().magnitude, 1e-4f, "todo junto");
            yield return Keys(Key.LeftArrow); Assert.AreEqual(new Vector2(-1, 0), LabInput.Move());
            yield return Keys();
            // movimiento remapeable (hallazgo B): avanzar en I
            InputBindings.Set(InputBindings.Id(MoveKey.Forward), "<Keyboard>/i");
            yield return Keys(Key.I); Assert.AreEqual(new Vector2(0, 1), LabInput.Move(), "avanzar reasignado a I");
            yield return Keys(Key.W); Assert.AreEqual(Vector2.zero, LabInput.Move(), "W ya no avanza");
            yield return Keys(Key.UpArrow); Assert.AreEqual(new Vector2(0, 1), LabInput.Move(), "la flecha sigue como alternativa");
            yield return Keys();
        }

        [UnityTest]
        public IEnumerator Hold_HandsIndependent_Q_ReleasesAndDoesNotRegrabWithClicksHeld()
        {
            yield return FaceUncle();
            yield return Buttons(false, true); yield return Frames(4);
            Assert.AreNotEqual("", hands.HeldSocketId(1), "clic derecho agarra con la mano derecha");
            Assert.AreEqual("", hands.HeldSocketId(0), "la izquierda sigue libre");
            yield return FaceUncle(1); // otro punto: el que sostiene la derecha admite una sola mano
            yield return Buttons(true, true); yield return Frames(4);
            Assert.AreNotEqual("", hands.HeldSocketId(0), "clic izquierdo agarra con la izquierda");
            yield return Keys(Key.Q); yield return Frames(6);
            Assert.AreEqual("", hands.HeldSocketId(0) + hands.HeldSocketId(1), "Q suelta las dos (hallazgo D)");
            yield return Frames(20);
            Assert.AreEqual("", hands.HeldSocketId(0) + hands.HeldSocketId(1), "con los clics todavia apretados no vuelve a agarrar");
            yield return Keys(); yield return Buttons(false, false); yield return Buttons(false, true); yield return Frames(4);
            Assert.AreNotEqual("", hands.HeldSocketId(1), "soltar el clic y volver a apretar habilita otra vez");
            yield return Buttons(false, false); yield return Frames(4);
            Assert.AreEqual("", hands.HeldSocketId(1), "en Mantener, soltar el boton suelta la mano");
        }

        [UnityTest]
        public IEnumerator Toggle_TapGrabs_TapReleases_EmptyTapDisarms()
        {
            GameSettings.GrabToggle = true; GrabIntent.Reset();
            // pulsacion sin objetivo: se desarma y avisa; despues no agarra sola (hallazgo C)
            player.SetYawPitch(player.Yaw, -80f); yield return Frames(3);
            yield return Buttons(false, true); yield return Buttons(false, false);
            yield return new WaitForSeconds(GrabIntent.ArmWindow + 0.2f);
            Assert.IsFalse(GrabIntent.Armed(1), "sin objetivo se desarma");
            Assert.AreEqual("hud.nothing_to_grab", GrabIntent.Feedback);
            yield return FaceUncle(); yield return Frames(10);
            Assert.AreEqual("", hands.HeldSocketId(1), "no queda un agarre armado que se dispare despues");
            yield return Buttons(false, true); yield return Buttons(false, false); yield return Frames(6);
            Assert.AreNotEqual("", hands.HeldSocketId(1), "una pulsacion agarra y sigue agarrando sin mantener");
            yield return Frames(20);
            Assert.AreNotEqual("", hands.HeldSocketId(1), "sigue agarrado");
            yield return Buttons(false, true); yield return Buttons(false, false); yield return Frames(6);
            Assert.AreEqual("", hands.HeldSocketId(1), "otra pulsacion suelta");
        }

        [UnityTest]
        public IEnumerator Pause_ReleasesGrabs_AndHeldClickDoesNotRegrabAfterClosing()
        {
            yield return FaceUncle();
            yield return Buttons(false, true); yield return Frames(4);
            Assert.AreNotEqual("", hands.HeldSocketId(1));
            GameFlowUI.Instance.SetPaused(true); yield return Frames(4);
            Assert.AreEqual("", hands.HeldSocketId(1), "con la pausa abierta no se sostiene");
            GameFlowUI.Instance.SetPaused(false); yield return Frames(10);
            Assert.AreEqual("", hands.HeldSocketId(1), "al cerrar la pausa con el clic apretado no agarra solo");
            yield return Buttons(false, false); yield return Buttons(false, true); yield return Frames(4);
            Assert.AreNotEqual("", hands.HeldSocketId(1), "un clic nuevo agarra");
        }

        [UnityTest]
        public IEnumerator TypingInField_DoesNotTriggerGameActions()
        {
            var canvas = Ui.Canvas("TestField", 50);
            var go = new GameObject("Field", typeof(RectTransform)); go.transform.SetParent(canvas.transform, false);
            var img = go.AddComponent<Image>(); var txt = Ui.Label(go.transform, "T", "", 20, Color.white);
            var field = go.AddComponent<InputField>(); field.textComponent = txt; field.targetGraphic = img;
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(go); field.ActivateInputField(); yield return Frames(3);
            Assert.IsTrue(Ui.Typing, "el campo tiene foco");
            Assert.IsTrue(GameFlowUI.BlockingInput, "escribir bloquea la entrada de juego (hallazgo E)");
            int restarts = RoundDirector.Instance.Restarts; Vector3 p0 = player.transform.position;
            yield return Keys(Key.R); yield return Keys(Key.E); yield return Keys(Key.Space); yield return Keys(Key.W); yield return Frames(20);
            Assert.AreEqual(restarts, RoundDirector.Instance.Restarts, "R escrita no reinicia");
            Assert.AreEqual(0f, hands.InteractProgress01, 1e-4f, "E escrita no interactua");
            Assert.Less(Vector3.Distance(p0, player.transform.position), 0.05f, "W y Espacio escritas no mueven ni esquivan");
            yield return Keys(); Object.Destroy(canvas.gameObject);
        }

        [UnityTest]
        public IEnumerator Options_RebindIsDraft_DiscardRestores_ApplySaves()
        {
            var canvas = Ui.Canvas("TestOptions", 60);
            string before = InputBindings.Path(Act.Interact);
            var ov = new OptionsView(canvas.transform, () => { });
            InputBindings.Set(Act.Interact, "<Keyboard>/f");
            Assert.IsTrue(ov.Dirty, "una reasignacion es un cambio pendiente (hallazgo G)");
            ov.Close(); yield return null;
            Assert.AreEqual(before, InputBindings.Path(Act.Interact), "salir sin aplicar descarta la reasignacion");
            ov = new OptionsView(canvas.transform, () => { });
            InputBindings.Set(Act.Interact, "<Keyboard>/f"); ov.ApplyChanges();
            Assert.IsFalse(ov.Dirty);
            InputBindings.ReloadFromDisk();
            Assert.AreEqual("<Keyboard>/f", InputBindings.Path(Act.Interact), "aplicar guarda");
            yield return Keys(Key.F); Assert.IsTrue(LabInput.Held(Act.Interact), "la tecla nueva funciona en juego");
            yield return Keys(Key.E); Assert.IsFalse(LabInput.Held(Act.Interact), "la vieja ya no");
            yield return Keys(); ov.Close(); Object.Destroy(canvas.gameObject);
        }

        [UnityTest]
        public IEnumerator FirstSteps_AdvanceWithRealActions_AndCanBeSkipped()
        {
            var fs = FirstSteps.Instance; Assert.IsNotNull(fs, "la guia existe en la partida");
            fs.Begin(); yield return Frames(2);
            Assert.AreEqual("tut.move", fs.CurrentKey);
            InputSystem.QueueStateEvent(kb, new KeyboardState(Key.UpArrow)); yield return new WaitForSeconds(1.2f); yield return Keys();
            Assert.AreNotEqual("tut.move", fs.CurrentKey, "caminar con las flechas completa el primer paso");
            yield return FaceUncle(); yield return Frames(3);
            Assert.AreEqual("tut.grab", fs.CurrentKey, "apuntar al tio pasa a «agarrar»");
            yield return Buttons(false, true); yield return Frames(4);
            Assert.AreEqual("tut.release", fs.CurrentKey, "agarrar pasa a «soltar»");
            yield return Buttons(false, false); yield return Frames(4);
            Assert.AreEqual("tut.twohands", fs.CurrentKey, "soltar pasa a «dos manos»");
            fs.Skip(); yield return Frames(2);
            Assert.IsFalse(fs.Active, "se puede omitir");
            Assert.IsTrue(FirstSteps.Completed, "y no vuelve a aparecer sola");
            PlayerPrefs.DeleteKey("ue.firstSteps");
        }

        [UnityTest]
        public IEnumerator Mark_MiddleClickOnUncle_ShowsOneMarkerThatExpires()
        {
            yield return FaceUncle(); yield return Frames(3);
            var cm = CoopMarkers.Instance; Assert.IsNotNull(cm, "marcadores presentes en la partida");
            var st = new MouseState().WithButton(MouseButton.Middle); InputSystem.QueueStateEvent(mouse, st); yield return Frames(3);
            InputSystem.QueueStateEvent(mouse, new MouseState()); yield return Frames(3);
            Assert.AreEqual(1, cm.ActiveCount, "un marcador por jugador");
            Assert.IsTrue(cm.TryGet(PlayerControls.Local.OwnerClientId, out var kind, out _));
            Assert.AreEqual(MarkKind.Uncle, kind, "apuntando al tio marca al tio");
            var label = GameObject.Find("CoopMarkers/Mark" + PlayerControls.Local.OwnerClientId + "/Text")?.GetComponent<Text>();
            Assert.IsNotNull(label); StringAssert.Contains(Loc.T("mark.uncle"), label.text);
            // un segundo clic enseguida no crea otro (frecuencia limitada) y el marcador sigue siendo uno
            InputSystem.QueueStateEvent(mouse, st); yield return Frames(3); InputSystem.QueueStateEvent(mouse, new MouseState()); yield return Frames(3);
            Assert.AreEqual(1, cm.ActiveCount);
            yield return new WaitForSeconds(CoopMarkers.Lifetime + 0.3f);
            Assert.AreEqual(0, cm.ActiveCount, "vence solo");
        }

        [UnityTest]
        public IEnumerator TextSizeAndLanguage_ChangeDuringMatch_UpdateHud()
        {
            float s0 = Ui.TextScale; string l0 = Loc.Language;
            Text Objective() => GameObject.Find("HUD/Root/Objective/Title")?.GetComponent<Text>();
            yield return Frames(3);
            var t = Objective(); Assert.IsNotNull(t);
            int size100 = t.fontSize;
            Ui.TextScale = 1.5f; GameSettings.TextScale = 1.5f; yield return Frames(4);
            t = Objective(); Assert.Greater(t.fontSize, size100, "el HUD se reconstruye con el tamano nuevo (hallazgo H)");
            Loc.SetLanguage(Loc.En); yield return Frames(3);
            string en = Objective().text;
            Loc.SetLanguage(Loc.EsUY); yield return Frames(3);
            Assert.AreNotEqual(en, Objective().text, "el idioma cambia en la partida");
            Ui.TextScale = s0; GameSettings.TextScale = s0; Loc.SetLanguage(l0); yield return Frames(3);
        }
    }
}
