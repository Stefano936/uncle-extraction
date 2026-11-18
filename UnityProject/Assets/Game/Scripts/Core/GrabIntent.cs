using UnityEngine;

namespace UncleExtraction
{
    /// Politica de agarre del jugador LOCAL (sesion 13). Traduce los clics reales a «quiero sostener esta mano»; el resultado
    /// viaja igual al anfitrion (anfitrion local o intencion del cliente), asi que mantener y alternar son equivalentes en
    /// ambos. El anfitrion sigue decidiendo si el agarre existe (candidato, ocupado, distancia).
    /// - Mantener: la mano agarra mientras el boton esta apretado (y reintenta al pasar sobre un punto valido). Despues de
    ///   soltar con Q o de una liberacion forzada (distancia, golpe, reinicio), hay que soltar el clic y volver a apretar:
    ///   el mismo clic sostenido no crea otro vinculo.
    /// - Alternar: una pulsacion arma la mano y otra la suelta. Si en 0,6 s no hubo agarre (nada que agarrar o rechazo), se
    ///   desarma y lo avisa: no queda una accion armada que agarre despues de forma inesperada. Una liberacion forzada o Q
    ///   la desarman.
    /// - Menus, pausa, campos de texto, reasignacion o ventana sin foco: las manos se sueltan y hay que SOLTAR y volver a
    ///   hacer clic (en los dos modos: un boton que sigue apretado al volver no agarra ni arma).
    /// Las entradas simuladas (autopiloto, pruebas con LabInput.Simulated) no pasan por esta politica.
    public static class GrabIntent
    {
        public const float ArmWindow = 0.6f;
        static int frame = -1;
        static readonly bool[] held = new bool[2], latched = new bool[2], needRelease = new bool[2], prevRaw = new bool[2], wasHolding = new bool[2];
        static readonly float[] armedAt = new float[2];
        public static string Feedback = ""; public static float FeedbackUntil;

        public static bool Toggle => GameSettings.GrabToggle;
        public static bool Held(int hand) { Step(); return held[hand]; }
        public static bool Armed(int hand) { Step(); return latched[hand]; }
        public static void Reset() { for (int h = 0; h < 2; h++) { held[h] = latched[h] = prevRaw[h] = wasHolding[h] = false; needRelease[h] = true; } frame = -1; }

        static void Step()
        {
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            var pc = PlayerControls.Local; var hands = pc ? pc.GetComponent<PlayerHands>() : null;
            bool blocked = GameFlowUI.BlockingInput || !LabInput.Focused || (!LabInput.TestFocus && Cursor.lockState != CursorLockMode.Locked);
            bool release = !blocked && LabInput.RawPressedForGameplay(Act.ReleaseAll);
            for (int h = 0; h < 2; h++)
            {
                bool raw = !blocked && LabInput.RawHeld(h == 0 ? Act.GrabLeft : Act.GrabRight);
                bool rawDown = raw && !prevRaw[h]; prevRaw[h] = raw;
                bool holding = hands && hands.HeldSocketId(h) != "";
                bool lost = wasHolding[h] && !holding; wasHolding[h] = holding;
                if (blocked || release) { latched[h] = false; needRelease[h] = raw || blocked; held[h] = false; continue; }
                if (!raw) needRelease[h] = false; // rearmado comun a los dos modos: el boton tiene que soltarse de verdad
                if (Toggle)
                {
                    if (lost) latched[h] = false;
                    // un boton que seguia apretado al cerrar la pausa, Opciones o al recuperar el foco no cuenta como
                    // pulsacion nueva (hallazgo B, sesion 14): la mano queda libre hasta soltar y volver a apretar
                    if (rawDown && !needRelease[h])
                    {
                        if (latched[h] || holding) latched[h] = false;
                        else { latched[h] = true; armedAt[h] = Time.unscaledTime; }
                    }
                    if (latched[h] && !holding && Time.unscaledTime - armedAt[h] > ArmWindow)
                    { latched[h] = false; Feedback = "hud.nothing_to_grab"; FeedbackUntil = Time.time + 1.6f; }
                    held[h] = latched[h];
                }
                else
                {
                    if (lost && raw) needRelease[h] = true;
                    held[h] = raw && !needRelease[h];
                }
            }
        }
    }
}
