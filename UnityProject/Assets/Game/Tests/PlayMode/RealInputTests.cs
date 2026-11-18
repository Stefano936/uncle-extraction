using IDisposable = System.IDisposable;
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
        HudProfile hud0;
        int savedHud;
        string lang0;

        [UnitySetUp]
        public IEnumerator Load()
        {
            LabInput.ClearSimulated(); LabInput.TestFocus = true; GameSettings.GrabToggle = false; GrabIntent.Reset();
            // editor en lote sin foco: que el Input System no desactive teclado y raton en segundo plano
            bg0 = InputSystem.settings.backgroundBehavior; ed0 = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            kb = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>(); kb.MakeCurrent(); mouse.MakeCurrent();
            bool probe = TestContext.CurrentContext.Test.Name.StartsWith("S18Probe_");
            if (probe)
            {
                NetSession.Mode = NetMode.Host;
                NetSession.Port = 7890;
                NetSession.ListenAddress = "127.0.0.1";
            }
            SceneManager.LoadScene("Bar_LastRound");
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            if (probe)
            {
                PlayerControls.Local.SetReady(true);
                yield return null;
                Assert.IsTrue(RoundDirector.Instance.StartFromLobby());
                yield return null;
            }
            hud0 = GameSettings.Hud;
            savedHud = PlayerPrefs.GetInt("ue.hudProfile", -1);
            lang0 = Loc.Language;
            player = Object.FindAnyObjectByType<PlayerMotor>(); hands = player.GetComponent<PlayerHands>();
            npc = Object.FindAnyObjectByType<NpcBrain>(); npc.brainEnabled = false;
            yield return null;
        }

        [TearDown]
        public void Clear()
        {
            LabInput.ClearSimulated(); LabInput.TestFocus = false; LabInput.TestFocusLost = false; GameSettings.GrabToggle = false; GrabIntent.Reset();
            if (GameFlowUI.Instance) GameFlowUI.Instance.SetPaused(false);
            GameSettings.Hud = hud0;
            if (savedHud < 0) PlayerPrefs.DeleteKey("ue.hudProfile");
            else PlayerPrefs.SetInt("ue.hudProfile", savedHud);
            if (lang0 != null && Loc.Language != lang0) Loc.SetLanguage(lang0);
            if (TestContext.CurrentContext.Test.Name.StartsWith("S18Probe_"))
            {
                NetSession.Mode = NetMode.Solo;
                NetSession.Port = 7777;
                NetSession.ListenAddress = "0.0.0.0";
            }
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
            // pick > 0: el punto mas alto que NO sostiene ya una mano (los dos hombros estan a la misma altura y su orden variaba
            // segun la pose: a veces «el segundo» era el que ya sostenia la otra mano, que el juego excluye a proposito)
            var all = Object.FindObjectsByType<GrabSocket>(FindObjectsSortMode.None).Where(s => s.body && s.body.GetComponentInParent<NpcBody>()).OrderByDescending(s => s.Position.y);
            var socket = pick == 0 ? all.First() : all.First(s => s.socketId != hands.HeldSocketId(0) && s.socketId != hands.HeldSocketId(1));
            Vector3 target = socket.Position, at = target + (npc.transform.forward * 0.9f); at.y = 0f;
            if (pick == 0) { player.GetComponent<Rigidbody>().position = at; player.transform.position = at; }
            yield return new WaitForFixedUpdate();
            for (int i = 0; i < 6; i++)
            {
                Vector3 eye = player.EyePosition, to = target - eye, f = new Vector3(to.x, 0, to.z);
                player.SetYawPitch(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, -Mathf.Atan2(to.y, f.magnitude) * Mathf.Rad2Deg);
                yield return new WaitForFixedUpdate(); yield return null;
            }
            if (!hands.Candidate)
            {
                var cam = Camera.main.transform; Vector3 d = socket.Position - cam.position;
                string hits = string.Join(",", Physics.RaycastAll(cam.position, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore).Select(h => $"{h.collider.name}@{h.distance:0.00}"));
                Assert.Fail($"deberia apuntar a un punto agarrable del tio: objetivo={socket.socketId} dist={d.magnitude:0.00} ang={Vector3.Angle(cam.forward, d):0} golpes={hits} sostiene={hands.HeldSocketId(0)}/{hands.HeldSocketId(1)}");
            }
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
        public IEnumerator ToggleAudio_DistinguishesReleaseWhilePressed_FromLossWithButtonUp()
        {
            GameSettings.GrabToggle = true;
            GrabIntent.Reset();
            yield return FaceUncle();
            var audio = GameAudio.Instance;
            int Count(string id) => audio.Played.TryGetValue(id, out int n) ? n : 0;
            int contact = Count("grab"), release = Count("release"), slip = Count("slip"), tries = Count("grab_try");

            yield return Buttons(false, true);
            yield return Buttons(false, false);
            Assert.AreEqual(tries + 1, Count("grab_try"));
            Assert.AreEqual(contact + 1, Count("grab"));
            Assert.IsNotEmpty(hands.HeldSocketId(1));
            yield return Buttons(false, true);
            Assert.IsEmpty(hands.HeldSocketId(1));
            Assert.AreEqual(release + 1, Count("release"));
            Assert.AreEqual(slip, Count("slip"));
            Assert.AreEqual(tries + 1, Count("grab_try"), "soltar con otro toque no vuelve a intentar agarrar");

            yield return Buttons(false, false);
            yield return FaceUncle();
            yield return Buttons(false, true);
            yield return Buttons(false, false);
            var link = GrabRegistry.Find(hands, 1);
            Assert.IsNotNull(link);
            GrabRegistry.Release(link.id, ReleaseReason.Distance);
            yield return Frames(3);
            Assert.IsEmpty(hands.HeldSocketId(1));
            Assert.AreEqual(slip + 1, Count("slip"));
            Assert.AreEqual(release + 1, Count("release"));
        }

        [UnityTest]
        public IEnumerator HudProfile_ChangesFromPause_AndAimUsesOnePromptWithLiveBindings()
        {
            GameSettings.Hud = HudProfile.Contextual;
            Loc.SetLanguage(Loc.En);
            npc.ReleaseAnchor("test");
            yield return FaceUncle();

            var hud = GameHud.Instance;
            yield return Keys(Key.F); yield return Keys(); // el dispositivo activo vuelve a teclado/raton
            var root = GameObject.Find("HUD/Root").transform;
            var left = root.Find("LeftHand");
            Assert.IsFalse(left.gameObject.activeSelf);
            Assert.IsFalse(root.Find("Energy").gameObject.activeInHierarchy, $"energía en reposo: {player.Energy}");
            Assert.IsTrue(root.Find("Reticle").gameObject.activeInHierarchy);
            Assert.IsNull(GameObject.Find("Grab_Aim_Label"));
            Assert.IsTrue(hud.AimPromptVisible, hud.PromptText);
            StringAssert.Contains(InputBindings.Display(Act.GrabLeft), hud.PromptText);
            InputBindings.Set(Act.GrabLeft, "<Keyboard>/f");
            yield return Frames(2);
            StringAssert.Contains(InputBindings.Display(Act.GrabLeft), hud.PromptText);

            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                pad.MakeCurrent();
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South));
                Loc.SetLanguage(Loc.EsUY);
                yield return Frames(2);
                StringAssert.Contains("Mantené LT / RT", hud.PromptText);
                InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1f });
                yield return Frames(3);
                Assert.IsNotEmpty(hands.HeldSocketId(1), "RT agarra por la via real");
                Assert.IsNull(GameObject.Find("Tension_R"));
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return Frames(3);
                Assert.IsEmpty(hands.HeldSocketId(1));
            }
            finally { InputSystem.RemoveDevice(pad); }

            GameFlowUI.Instance.SetPaused(true);
            yield return Frames(2);
            Assert.IsFalse(hud.Visible);
            GameObject.Find("FlowUI/Pause/Settings").GetComponent<Button>().onClick.Invoke();
            yield return Frames(2);
            GameObject.Find("FlowUI/Options/settings.hud").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(HudProfile.Contextual, GameSettings.Hud, "el cambio es un borrador hasta Aplicar");
            GameObject.Find("FlowUI/Options/Apply").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(HudProfile.Detailed, GameSettings.Hud);
            GameObject.Find("FlowUI/Options/Back").GetComponent<Button>().onClick.Invoke();
            GameFlowUI.Instance.SetPaused(false);
            yield return FaceUncle();
            Assert.IsTrue(left.gameObject.activeInHierarchy);
            var worldLabel = GameObject.Find("Grab_Aim_Label").GetComponent<TextMesh>();
            StringAssert.Contains(InputBindings.Display(Act.GrabLeft), worldLabel.text);
            StringAssert.DoesNotContain("{key}", worldLabel.text);
            Assert.IsFalse(hud.AimPromptVisible);
        }

        [UnityTest]
        public IEnumerator PendingToggle_IsCancelledByPauseBeforeConfirmation()
        {
            GameSettings.GrabToggle = true;
            GrabIntent.Reset();
            player.SetYawPitch(player.Yaw, -80f);
            yield return Frames(3);
            Assert.IsNull(hands.Candidate);
            yield return Buttons(true, false);
            Assert.IsTrue(GrabIntent.Armed(0));

            GameFlowUI.Instance.SetPaused(true);
            yield return Frames(3);
            GameFlowUI.Instance.SetPaused(false);
            yield return Frames(3);
            Assert.IsFalse(GrabIntent.Armed(0));
            Assert.IsFalse(hands.IsGrabPending(0));
            Assert.IsEmpty(hands.HeldSocketId(0));
        }

        void PlaceProbePlayer(Vector3 position)
        {
            player.Body.detectCollisions = false;
            player.Body.useGravity = false;
            player.Body.linearVelocity = Vector3.zero;
            player.Body.position = player.transform.position = position;
            player.SetYawPitch(0f, 0f);
        }

        static GrabSocket ProbeSocket(string id, Vector3 position)
        {
            var probe = new GameObject(id);
            probe.transform.position = position;
            probe.AddComponent<Rigidbody>().isKinematic = true;
            var socket = probe.AddComponent<GrabSocket>();
            socket.socketId = id;
            return socket;
        }

        [UnityTest]
        public IEnumerator S18Probe_LoadedSpeedOnStraightLine()
        {
            PlaceProbePlayer(Vector3.zero);
            GameSettings.Hud = HudProfile.Detailed;
            float oldReaction = GrabSpring.ReactionFactor;
            GrabSpring.ReactionFactor = 0f; // mide el limite del motor sin la reaccion del resorte
            GrabSocket socket = null;
            try
            {
                player.DebugMove = Vector2.up;
                for (int i = 0; i < 35; i++) yield return new WaitForFixedUpdate();
                float before = player.Body.linearVelocity.z;
                socket = ProbeSocket("s18_speed", hands.HandTarget(0));
                hands.DebugHoldLeft = true;
                Assert.AreEqual(GrabResult.Ok, hands.ForceGrab(0, socket));
                var samples = new System.Collections.Generic.List<float>();
                for (int i = 0; i < 16; i++)
                {
                    socket.transform.position = hands.HandTarget(0);
                    yield return new WaitForFixedUpdate();
                    Assert.AreEqual(socket.socketId, hands.HeldSocketId(0));
                    samples.Add(player.Body.linearVelocity.z);
                }
                int firstNearLimit = samples.FindIndex(v => v <= LabBalance.LoadedSpeed + 0.05f);
                Debug.Log(System.FormattableString.Invariant($"[S18Probe] SPEED free={before:F3} first={samples[0]:F3} loaded={samples.Last():F3} ticks={firstNearLimit + 1} dt={Time.fixedDeltaTime:F4}"));
                Assert.That(before, Is.EqualTo(LabBalance.WalkSpeed).Within(0.05f));
                Assert.That(samples.Last(), Is.EqualTo(LabBalance.LoadedSpeed).Within(0.05f));
                Assert.GreaterOrEqual(firstNearLimit, 0);
            }
            finally
            {
                hands.DebugHoldLeft = null;
                player.DebugMove = null;
                GrabSpring.ReactionFactor = oldReaction;
                if (socket) Object.Destroy(socket.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator S18Probe_AimSweepAndBoundary()
        {
            PlaceProbePlayer(new Vector3(0f, 5f, 0f));
            GameSettings.Hud = HudProfile.Detailed;
            yield return Frames(3);
            Vector3 origin = hands.cam.transform.position;
            var left = ProbeSocket("s18_left", origin + Quaternion.Euler(0f, -4f, 0f) * (Vector3.forward * 1.2f));
            var right = ProbeSocket("s18_right", origin + Quaternion.Euler(0f, 4f, 0f) * (Vector3.forward * 1.2f));
            try
            {
                string Pick() => hands.Candidate == left ? "L" : hands.Candidate == right ? "R" : "-";
                var sweep = new System.Collections.Generic.List<string>();
                for (int angle = -6; angle <= 6; angle++)
                {
                    player.SetYawPitch(angle, 0f);
                    yield return Frames(2);
                    sweep.Add(Pick());
                }
                var jitter = new System.Collections.Generic.List<string>();
                for (int i = 0; i < 12; i++)
                {
                    player.SetYawPitch(i % 2 == 0 ? -0.35f : 0.35f, 0f);
                    yield return Frames(2);
                    jitter.Add(Pick());
                }

                int switches = jitter.Skip(1).Where((s, i) => s != jitter[i]).Count();
                Assert.That(sweep.First(), Is.EqualTo("L"));
                Assert.That(sweep.Last(), Is.EqualTo("R"));
                yield return Buttons(true, false);
                Assert.That(hands.HeldSocketId(0), Is.EqualTo(right.socketId));
                Debug.Log($"[S18Probe] AIM sweep={string.Concat(sweep)} jitter={string.Concat(jitter)} switches={switches} confirmed={hands.HeldSocketId(0)}");
                yield return Buttons(false, false);
            }
            finally
            {
                Object.Destroy(left.gameObject);
                Object.Destroy(right.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator S18Probe_FallCamera([Values(false, true)] bool crouch, [Values(false, true)] bool reduced)
        {
            bool oldMotion = GameSettings.ReduceMotion;
            GameSettings.ReduceMotion = reduced;
            LabInput.Simulated[(int)Act.Crouch] = crouch;
            player.SetYawPitch(player.Yaw, 0f);
            try
            {
                yield return new WaitForSeconds(0.4f);
                Assert.AreEqual(crouch, player.Crouching);
                var pivot = player.cameraPivot;
                float startHeight = pivot.localPosition.y;
                Assert.IsTrue(player.ApplyImpact(Vector3.zero, LabBalance.ComposureMax, "s18-probe"));
                float start = Time.time;
                float maxRoll = 0f;
                float rollAt100 = -1;
                while (Time.time - start <= 0.55f)
                {
                    yield return null;
                    float roll = Mathf.Abs(Mathf.DeltaAngle(0f, pivot.localEulerAngles.z));
                    maxRoll = Mathf.Max(maxRoll, roll);
                    if (Time.time - start >= 0.1f && rollAt100 < 0f) rollAt100 = roll;
                }

                Debug.Log(System.FormattableString.Invariant($"[S18Probe] CAMERA crouch={crouch} reduced={reduced} before={startHeight:F2} fallen={pivot.localPosition.y:F2} roll100={rollAt100:F1} peak={maxRoll:F1}"));
                Assert.AreEqual(PlayerState.Fallen, player.State);
                Assert.That(pivot.localPosition.y, Is.EqualTo(0.45f).Within(0.01f));
                Assert.That(maxRoll, Is.GreaterThan(0f));
            }
            finally
            {
                LabInput.Simulated[(int)Act.Crouch] = false;
                GameSettings.ReduceMotion = oldMotion;
            }
        }

        public enum Blocker { PauseClosedWithEscape, OptionsFromPause, Rebinding, FocusLost, RoundRestart }

        /// Hallazgo B (sesion 14): con el boton sostenido durante un bloqueo, al volver la mano queda libre hasta una
        /// pulsacion NUEVA, en los dos modos y con las dos manos; Q y la otra mano siguen funcionando.
        [UnityTest]
        public IEnumerator HeldButtonAcrossBlocking_DoesNotGrabOrArm_UntilANewPress(
            [Values(0, 1)] int hand, [Values(false, true)] bool toggle,
            [Values(Blocker.PauseClosedWithEscape, Blocker.OptionsFromPause, Blocker.Rebinding, Blocker.FocusLost, Blocker.RoundRestart)] Blocker blocker)
        {
            GameSettings.GrabToggle = toggle; GrabIntent.Reset();
            bool l = hand == 0, r = hand == 1;
            yield return FaceUncle(); yield return Frames(3);
            yield return Buttons(l, r); yield return Frames(4);
            if (toggle) { Assert.AreNotEqual("", hands.HeldSocketId(hand), "alternar: un clic agarra"); }
            else Assert.AreNotEqual("", hands.HeldSocketId(hand), "mantener: agarra con el boton apretado");
            // el boton sigue FISICAMENTE apretado durante todo el bloqueo
            IDisposable listen = null;
            switch (blocker)
            {
                case Blocker.PauseClosedWithEscape: GameFlowUI.Instance.SetPaused(true); break;
                case Blocker.OptionsFromPause:
                    GameFlowUI.Instance.SetPaused(true); yield return Frames(2);
                    GameObject.Find("FlowUI/Pause/Settings")?.GetComponent<Button>()?.onClick.Invoke(); break;
                case Blocker.Rebinding: listen = InputBindings.Listen(InputBindings.Id(Act.Interact), (a, b) => { }); break;
                case Blocker.FocusLost: LabInput.TestFocusLost = true; break;
                case Blocker.RoundRestart: RoundDirector.Instance.Restart(); break;
            }
            yield return Frames(6);
            Assert.AreEqual("", hands.HeldSocketId(hand), $"{blocker}: la mano se suelta");
            switch (blocker)
            {
                case Blocker.PauseClosedWithEscape:
                    InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Escape)); yield return Frames(2); yield return Keys(); break;
                case Blocker.OptionsFromPause:
                    GameObject.Find("FlowUI/Options/Back")?.GetComponent<Button>()?.onClick.Invoke(); yield return Frames(2);
                    GameFlowUI.Instance.SetPaused(false); break;
                case Blocker.Rebinding:
                    InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Escape)); yield return Frames(2); yield return Keys(); listen?.Dispose(); break;
                case Blocker.FocusLost: LabInput.TestFocusLost = false; break;
                case Blocker.RoundRestart: yield return FaceUncle(); break;
            }
            yield return Frames(3);
            Assert.IsFalse(GameFlowUI.BlockingInput, $"{blocker}: el bloqueo termino");
            yield return Frames(10);
            Assert.AreEqual("", hands.HeldSocketId(hand), $"{blocker}/{(toggle ? "alternar" : "mantener")}: con el mismo boton sostenido la mano queda libre");
            Assert.IsFalse(GrabIntent.Armed(hand), "y no queda armada");
            yield return FaceUncle(); yield return Frames(2);
            yield return Buttons(false, false); yield return Buttons(l, r); yield return Frames(4);
            Assert.AreNotEqual("", hands.HeldSocketId(hand), "una pulsacion nueva agarra");
            if (toggle) yield return Buttons(false, false);
            InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Q)); yield return Frames(3); yield return Keys(); yield return Frames(3);
            Assert.AreEqual("", hands.HeldSocketId(hand), "Q suelta");
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
