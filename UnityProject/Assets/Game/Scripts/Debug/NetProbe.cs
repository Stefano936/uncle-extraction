using System;
using System.Collections;
using System.Linq;
using UnityEngine;
#if UE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace UncleExtraction
{
    /// Sondas de red para procesos separados (sesion 14; solo pruebas, sin argumentos no hace nada).
    /// Invitado: -ueProbe toggle | mark. Usa un mouse y un teclado VIRTUALES del Input System, asi que recorre el mismo
    /// camino que un jugador (GrabIntent, intencion al anfitrion, confirmacion replicada). Escribe «[Probe] paso=... ok=...»
    /// y «[Probe] fin ...» y cierra el juego.
    /// Anfitrion: -ueProbeStage pone a los invitados frente al tio (con el cerebro quieto) al empezar la ronda;
    /// -ueProbeRestartAt s reinicia la ronda a los s segundos (limpieza de marcadores).
    public class NetProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
#if UE_PLAYER_BUILD
            return; // build de jugadores: sin sondas de QA
#endif
            var a = Environment.GetCommandLineArgs();
            if (a.Contains("-ueProbeCycle") || a.Contains("-ueProbe") || a.Contains("-ueProbeStage") || a.Contains("-ueProbeRestartAt") || a.Contains("-ueQuitAtRound"))
            { var go = new GameObject("NetProbe"); DontDestroyOnLoad(go); go.AddComponent<NetProbe>(); }
        }
        static string Arg(string k) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

        int ok, total;
        void Check(string step, bool pass, string detail = "") { total++; if (pass) ok++; Debug.Log($"[Probe] paso={step} ok={pass} {detail}"); }

        IEnumerator Start()
        {
            while (!RoundDirector.Instance || !PlayerControls.Local || RoundDirector.Instance.InLobby || RoundDirector.Instance.State != RoundState.Running) yield return null;
            var a = Environment.GetCommandLineArgs();
            if (a.Contains("-ueProbeStage") && NetAuth.IsServer) StartCoroutine(Stage());
            var rs = Arg("-ueProbeRestartAt");
            if (a.Contains("-ueProbeCycle") && NetAuth.IsServer) StartCoroutine(HostCycle());
            if (rs != null && NetAuth.IsServer && float.TryParse(rs, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float at)) StartCoroutine(RestartAt(at));
            var qa = Arg("-ueQuitAtRound"); // invitado: salir del juego cuando el reloj replicado de la ronda llega a s
            if (qa != null && float.TryParse(qa, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float qs))
            {
                while (RoundDirector.Instance && RoundDirector.Instance.Elapsed < qs) yield return null;
                Debug.Log($"[Probe] sale de la partida a los {RoundDirector.Instance.Elapsed:0.0} s de ronda"); Application.Quit(); yield break;
            }
            var probe = Arg("-ueProbe");
            if (probe == "cycle") yield return GuestCycle();
            if (probe == "toggle") yield return Toggle();
            else if (probe == "mark") yield return Mark();
        }

        // ---------- anfitrion ----------
        IEnumerator Stage()
        {
            var npc = NpcBrain.Instance; var body = npc ? npc.GetComponent<NpcBody>() : null; if (!body) yield break;
            npc.brainEnabled = false;
            for (float t = 0; t < 4f; t += Time.deltaTime)
            {
                int k = 0;
                foreach (var pm in PlayerMotor.All)
                {
                    if (!pm || !pm.Controls) continue;
                    Vector3 dir = pm.Controls.IsHostPlayer ? Quaternion.Euler(0, 110, 0) * body.Forward : Quaternion.Euler(0, 25 * k++, 0) * body.Forward;
                    Vector3 p = body.Body.position + dir * (pm.Controls.IsHostPlayer ? 2.6f : 0.95f); p.y = 0f;
                    pm.Body.position = p; pm.transform.position = p; pm.Body.linearVelocity = Vector3.zero;
                }
                yield return new WaitForFixedUpdate();
            }
            Debug.Log("[Probe] anfitrion: invitados frente al tio, cerebro quieto");
        }
        IEnumerator RestartAt(float s)
        {
            yield return new WaitForSeconds(s);
            RoundDirector.Instance.Restart();
            yield return null; yield return null;
            Debug.Log($"[Probe] anfitrion: reinicio; marcadores activos={(CoopMarkers.Instance ? CoopMarkers.Instance.ActiveCount : -1)}");
        }

        /// Anfitrion (sesion 17): tres rondas seguidas sin que nadie vuelva a entrar: Sin carrito, Sin carrito y Estandar,
        /// tirando la torre una vez en cada ronda (la decision la toma el anfitrion, como en el juego).
        IEnumerator HostCycle()
        {
            var r = RoundDirector.Instance;
            var modes = new[] { ChallengeMode.NoCart, ChallengeMode.NoCart, ChallengeMode.Standard };
            for (int i = 0; i < modes.Length; i++)
            {
                if (i > 0) { r.challenge = modes[i]; r.Restart(); }
                else if (r.CurrentChallenge != modes[0]) { r.challenge = modes[0]; r.Restart(); }
                if (NpcBrain.Instance) NpcBrain.Instance.brainEnabled = false;
                yield return Wait(4f);
                r.TowerFall(Vector3.right);
                Debug.Log($"[Probe] anfitrion: ronda {r.Restarts} modo {r.CurrentChallenge} torre tirada");
                yield return Wait(4f);
            }
            Debug.Log("[Probe] anfitrion: ciclo terminado");
        }

        /// Invitado (sesion 17): en cada ronda nueva comprueba el carrito segun el modo y que la torre empieza armada y cae una vez.
        IEnumerator GuestCycle()
        {
            var r = RoundDirector.Instance; int seen = -1, rounds = 0;
            for (float t = 0; t < 60f && rounds < 3; t += Time.unscaledDeltaTime)
            {
                if (r.Restarts != seen && r.State == RoundState.Running && !r.InLobby)
                {
                    seen = r.Restarts; rounds++;
                    yield return Wait(1.5f);
                    var cart = TransportCart.Instance; var props = BarProps.Instance;
                    bool visible = false, usable = false;
                    if (cart) { foreach (var rr in cart.GetComponentsInChildren<Renderer>()) visible |= rr.enabled; foreach (var s in cart.GetComponentsInChildren<GrabSocket>()) usable |= s.enabled; }
                    bool nocart = r.CurrentChallenge == ChallengeMode.NoCart;
                    Check($"ronda{rounds}_carrito_{r.CurrentChallenge}", nocart ? (!visible && !usable) : (visible && usable), $"ronda={r.Restarts} visible={visible} agarrable={usable} pos={(cart ? cart.transform.position.ToString() : "-")}");
                    int f0 = props ? props.Falls : -1;
                    Check($"ronda{rounds}_torre_armada", props && !props.Fallen, $"caidas_previas={f0}");
                    for (float w = 0; w < 6f && props && !props.Fallen; w += Time.unscaledDeltaTime) yield return null;
                    yield return Wait(0.5f);
                    Check($"ronda{rounds}_torre_cae_una_vez", props && props.Fallen && props.Falls == f0 + 1, $"caidas={(props ? props.Falls : -1)}");
                }
                yield return null;
            }
            Debug.Log($"[Probe] fin cycle ok={ok}/{total} rondas={rounds}");
            yield return Wait(1f); Application.Quit();
        }

        // ---------- invitado ----------
#if UE_INPUT_SYSTEM
        Mouse mouse; Keyboard kb;
        void Devices()
        {
            LabInput.TestFocus = true; // la ventana de prueba no tiene foco: el foco se simula (los bloqueos se prueban aparte)
            mouse = InputSystem.AddDevice<Mouse>("ProbeMouse"); kb = InputSystem.AddDevice<Keyboard>("ProbeKeyboard"); mouse.MakeCurrent(); kb.MakeCurrent();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }
        IEnumerator Mouse_(bool left, bool middle = false)
        {
            var st = new MouseState(); if (left) st = st.WithButton(MouseButton.Left); if (middle) st = st.WithButton(MouseButton.Middle);
            InputSystem.QueueStateEvent(mouse, st); yield return null; yield return null;
        }
        IEnumerator Key_(params Key[] keys) { InputSystem.QueueStateEvent(kb, new KeyboardState(keys)); yield return null; yield return null; }
#endif
        PlayerHands Hands => PlayerControls.Local ? PlayerControls.Local.GetComponent<PlayerHands>() : null;
        string Held0 => Hands ? Hands.HeldSocketId(0) : "";
        IEnumerator AimAtUncle(float pitchOffset = 0f)
        {
            var npc = NpcBrain.Instance; var motor = PlayerControls.Local.GetComponent<PlayerMotor>();
            var s = GrabSocket.All.Where(x => x && x.body && NpcBody.Of(x.body) && (x.socketId.StartsWith("shoulder") || x.socketId.StartsWith("forearm")))
                                  .OrderBy(x => Vector3.Distance(x.Position, motor.EyePosition)).FirstOrDefault();
            Vector3 target = s ? s.Position : npc.transform.position + Vector3.up * 1.3f;
            for (int i = 0; i < 20; i++)
            {
                Vector3 to = target - motor.EyePosition, f = new Vector3(to.x, 0, to.z);
                motor.SetYawPitch(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, -Mathf.Atan2(to.y, f.magnitude) * Mathf.Rad2Deg + pitchOffset);
                yield return null;
            }
        }
        IEnumerator Wait(float s) { for (float t = 0; t < s; t += Time.unscaledDeltaTime) yield return null; }

        IEnumerator Toggle()
        {
#if UE_INPUT_SYSTEM
            Devices(); GameSettings.GrabToggle = true; GrabIntent.Reset();
            yield return Wait(5f); // el anfitrion acomoda a los invitados
            yield return AimAtUncle();
            Check("candidato", Hands.Candidate != null, $"mira={(Hands.Candidate ? Hands.Candidate.socketId : "-")}");
            // 1) una pulsacion agarra; medir el tiempo pendiente (sin confirmacion del anfitrion)
            int AudioCount(string id) => GameAudio.Instance && GameAudio.Instance.Played.TryGetValue(id, out int n) ? n : 0;
            int contacts = AudioCount("grab"), releases = AudioCount("release"), slips = AudioCount("slip");
            float t0 = Time.realtimeSinceStartup, pending = -1f;
            yield return Mouse_(true); yield return Mouse_(false);
            for (float t = 0; t < 2f && Held0 == ""; t += Time.unscaledDeltaTime) yield return null;
            pending = (Time.realtimeSinceStartup - t0) * 1000f;
            yield return Wait(0.6f);
            Check("alternar_un_clic_agarra", Held0 != "", $"punto={Held0} pendiente_ms={pending:0} armada={GrabIntent.Armed(0)}");
            Check("audio_contacto_unico", AudioCount("grab") == contacts + 1, $"contactos={AudioCount("grab") - contacts}");
            // 2) otra pulsacion suelta
            yield return Mouse_(true); yield return Mouse_(false); yield return Wait(0.8f);
            Check("alternar_otro_clic_suelta", Held0 == "", $"punto='{Held0}'");
            Check("audio_soltar_voluntario", AudioCount("release") == releases + 1 && AudioCount("slip") == slips,
                $"sueltas={AudioCount("release") - releases} resbalones={AudioCount("slip") - slips}");
            // 3) Q con el boton sostenido: suelta y no reagarra
            yield return AimAtUncle(); yield return Mouse_(true); yield return Wait(0.8f);
            bool grabbed = Held0 != "";
            yield return Key_(Key.Q); yield return Key_(); yield return Wait(1.2f);
            Check("q_con_boton_sostenido", grabbed && Held0 == "" && !GrabIntent.Armed(0), $"agarro={grabbed} despues='{Held0}'");
            Check("audio_soltar_con_q", AudioCount("release") == releases + 2 && AudioCount("slip") == slips,
                $"sueltas={AudioCount("release") - releases} resbalones={AudioCount("slip") - slips}");
            yield return Mouse_(false); yield return Wait(0.3f);
            // 4) pausa: suelta; al cerrar con el boton sostenido no vuelve a agarrar
            yield return AimAtUncle(); yield return Mouse_(true); yield return Mouse_(false); yield return Wait(0.8f);
            bool before = Held0 != "";
            yield return Mouse_(true); // boton sostenido durante la pausa
            GameFlowUI.Instance.SetPaused(true); yield return Wait(1.0f);
            bool released = Held0 == "";
            GameFlowUI.Instance.SetPaused(false); yield return Wait(1.2f);
            Check("pausa_bloquea_y_no_reagarra", before && released && Held0 == "" && !GrabIntent.Armed(0), $"antes={before} en_pausa_suelta={released} despues='{Held0}'");
            yield return Mouse_(false); yield return Wait(0.3f);
            // 5) pedido sin objetivo (o rechazado): no queda armada indefinidamente
            var motor = PlayerControls.Local.GetComponent<PlayerMotor>(); motor.SetYawPitch(motor.Yaw + 180f, -70f); yield return Wait(0.3f);
            yield return Mouse_(true); yield return Mouse_(false);
            bool armed = GrabIntent.Armed(0); yield return Wait(GrabIntent.ArmWindow + 0.6f);
            Check("sin_objetivo_se_desarma", !GrabIntent.Armed(0) && Held0 == "", $"armada_al_principio={armed} armada_despues={GrabIntent.Armed(0)} aviso='{GrabIntent.Feedback}'");
            Debug.Log($"[Probe] fin toggle ok={ok}/{total}");
#else
            Debug.Log("[Probe] fin toggle ok=0/1 (sin Input System)"); yield break;
#endif
            yield return Wait(1f); Application.Quit();
        }

        IEnumerator Mark()
        {
#if UE_INPUT_SYSTEM
            Devices();
            yield return Wait(5f);
            yield return AimAtUncle();
            var me = PlayerControls.Local; var cm = CoopMarkers.Instance;
            yield return Mouse_(false, true); yield return Mouse_(false); yield return Wait(0.6f);
            bool got = cm.TryGet(me.OwnerClientId, out var kind, out _);
            Check("marca_tio", got && kind == MarkKind.Uncle, $"tipo={kind} dueño={me.OwnerClientId} color=#{ColorUtility.ToHtmlStringRGB(CoopMarkers.ColorOf(me.OwnerClientId))} nombre={me.GetComponent<PlayerMotor>().playerName}");
            int before = cm.ActiveCount;
            yield return Mouse_(false, true); yield return Mouse_(false); yield return Wait(0.2f);
            me.DebugMarkRaw(me.transform.position + Vector3.forward * 200f); yield return Wait(0.6f);
            Check("limites_frecuencia_y_alcance", cm.ActiveCount == before && cm.TryGet(me.OwnerClientId, out var k2, out _) && k2 == MarkKind.Uncle, $"activos={cm.ActiveCount} (los rechazos quedan en el registro del anfitrion)");
            yield return Wait(CoopMarkers.Lifetime + 0.3f);
            Check("caduca", cm.ActiveCount == 0, $"activos={cm.ActiveCount}");
            // una marca nueva antes del reinicio del anfitrion: el reinicio la limpia
            yield return AimAtUncle(); yield return Mouse_(false, true); yield return Mouse_(false); yield return Wait(0.5f);
            int r0 = RoundDirector.Instance.Restarts; bool marked = cm.ActiveCount == 1;
            for (float t = 0; t < 20f && RoundDirector.Instance.Restarts == r0; t += Time.unscaledDeltaTime) yield return null;
            yield return Wait(0.3f);
            Check("reinicio_limpia", marked && RoundDirector.Instance.Restarts > r0 && cm.ActiveCount == 0, $"marcada={marked} reinicios {r0}->{RoundDirector.Instance.Restarts} activos={cm.ActiveCount}");
            Debug.Log($"[Probe] fin mark ok={ok}/{total}");
#else
            Debug.Log("[Probe] fin mark ok=0/1 (sin Input System)"); yield break;
#endif
            yield return Wait(1f); Application.Quit();
        }
    }
}
