using UnityEngine;
#if UE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UncleExtraction
{
    public enum Act { GrabLeft, GrabRight, Interact, ReleaseAll, Effort, Crouch, Dodge, Restart, ToggleCursor, Pause }

    /// Entrada del laboratorio. Usa Input System si el paquete esta instalado; si no, el Input Manager clasico.
    /// TODO M1+: migrar a un InputActionAsset remapeable (GDD 04).
    public static class LabInput
    {
        /// Movimiento simulado (autopiloto/pruebas). Si tiene valor, reemplaza al teclado.
        public static Vector2? SimulatedMove;
        /// Pruebas con dispositivos simulados en un editor sin ventana: tratar la ventana como enfocada y el cursor como capturado.
        public static bool TestFocus;
        public static bool Focused => TestFocus || Application.isFocused;

        public static Vector2 Move() => SimulatedMove ?? RealMove();

        static Vector2 RealMove()
        {
#if UE_INPUT_SYSTEM
            if (!Focused) return Vector2.zero; // ventana sin foco: no caminar con teclas que quedaron apretadas
            Vector2 v = Vector2.zero;
            var k = Keyboard.current;
            if (k != null)
            {   // teclas asignadas (WASD por defecto) y flechas como alternativa fija: cada direccion vale 0 o 1, asi que usar
                // las dos a la vez no suma velocidad
                float f = InputBindings.KeyHeld(MoveKey.Forward) || k.upArrowKey.isPressed ? 1f : 0f, b = InputBindings.KeyHeld(MoveKey.Back) || k.downArrowKey.isPressed ? 1f : 0f;
                float r = InputBindings.KeyHeld(MoveKey.Right) || k.rightArrowKey.isPressed ? 1f : 0f, l = InputBindings.KeyHeld(MoveKey.Left) || k.leftArrowKey.isPressed ? 1f : 0f;
                v = new Vector2(r - l, f - b);
            }
            var g = Gamepad.current;
            if (g != null) v += g.leftStick.ReadValue();
            return Vector2.ClampMagnitude(v, 1f); // diagonal normalizada
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#else
            return Vector2.zero;
#endif
        }

        /// Delta de mirada en "pixeles de raton" por frame.
        public static Vector2 Look()
        {
#if UE_INPUT_SYSTEM
            Vector2 v = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            var g = Gamepad.current;
            if (g != null) v += g.rightStick.ReadValue() * 900f * Time.unscaledDeltaTime;
            return v;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
#else
            return Vector2.zero;
#endif
        }

        /// Botones simulados (pruebas automaticas y autopiloto). Se suman a la entrada real.
        public static readonly bool[] Simulated = new bool[16];
        public static void ClearSimulated() { System.Array.Clear(Simulated, 0, Simulated.Length); SimulatedMove = null; }

        /// Entrada para el juego: los clics reales de agarre solo cuentan con el cursor capturado
        /// (con el panel de depuracion abierto, los clics son para la UI). Los simulados siempre cuentan.
        public static bool HeldForGameplay(Act a)
        {
            if (Simulated[(int)a]) return true;
            if (a == Act.GrabLeft) return GrabIntent.Held(0);   // mantener/alternar, liberacion por Q y menus (GrabIntent)
            if (a == Act.GrabRight) return GrabIntent.Held(1);
            if (!Focused) return false;
            return RealHeld(a);
        }

        /// Estado real de un boton (sin simulacion ni politica de agarre). Lo usa GrabIntent.
        public static bool RawHeld(Act a) => RealHeld(a);
        static readonly bool[] rawPrev = new bool[16]; static readonly bool[] rawDown = new bool[16]; static int rawFrame = -1;
        /// Flanco real de un boton para el juego (sin simulacion; no cuenta con la ventana sin foco).
        public static bool RawPressedForGameplay(Act a)
        {
            if (rawFrame != Time.frameCount)
            {
                rawFrame = Time.frameCount;
                for (int i = 0; i <= (int)Act.Pause; i++) { bool h = Focused && RealHeld((Act)i); rawDown[i] = h && !rawPrev[i]; rawPrev[i] = h; }
            }
            return rawDown[(int)a];
        }

        public static bool Held(Act a) => Simulated[(int)a] || RealHeld(a);

        static bool RealHeld(Act a)
        {
#if UE_INPUT_SYSTEM
            var k = Keyboard.current; var m = Mouse.current; var g = Gamepad.current;
            switch (a)
            {
                case Act.GrabLeft: return InputBindings.KeyHeld(a) || (g != null && g.leftTrigger.isPressed);
                case Act.GrabRight: return InputBindings.KeyHeld(a) || (g != null && g.rightTrigger.isPressed);
                case Act.Interact: return InputBindings.KeyHeld(a) || (g != null && g.buttonSouth.isPressed);
                case Act.ReleaseAll: return InputBindings.KeyHeld(a) || (g != null && g.buttonEast.isPressed);
                case Act.Effort: return InputBindings.KeyHeld(a) || (g != null && g.leftStickButton.isPressed);
                case Act.Crouch: return InputBindings.KeyHeld(a) || (g != null && g.rightStickButton.isPressed);
                case Act.Dodge: return InputBindings.KeyHeld(a) || (g != null && g.buttonWest.isPressed);
                case Act.Restart: return InputBindings.KeyHeld(a);
                case Act.ToggleCursor: return InputBindings.KeyHeld(a);
                case Act.Pause: return InputBindings.KeyHeld(a) || (g != null && g.startButton.isPressed);
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (a)
            {
                case Act.GrabLeft: return Input.GetMouseButton(0);
                case Act.GrabRight: return Input.GetMouseButton(1);
                case Act.Interact: return Input.GetKey(KeyCode.E);
                case Act.ReleaseAll: return Input.GetKey(KeyCode.Q);
                case Act.Effort: return Input.GetKey(KeyCode.LeftShift);
                case Act.Crouch: return Input.GetKey(KeyCode.LeftControl);
                case Act.Dodge: return Input.GetKey(KeyCode.Space);
                case Act.Restart: return Input.GetKey(KeyCode.R);
                case Act.ToggleCursor: return Input.GetKey(KeyCode.Tab);
                case Act.Pause: return Input.GetKey(KeyCode.Escape);
            }
            return false;
#else
            return false;
#endif
        }

        static readonly bool[] prev = new bool[16];
        static readonly bool[] down = new bool[16];
        static int lastFrame = -1;

        /// Flanco de bajada (true solo en el frame en que se presiona).
        public static bool Pressed(Act a)
        {
            if (lastFrame != Time.frameCount)
            {
                lastFrame = Time.frameCount;
                for (int i = 0; i <= (int)Act.Pause; i++)
                {
                    bool h = Held((Act)i);
                    down[i] = h && !prev[i];
                    prev[i] = h;
                }
            }
            return down[(int)a];
        }
    }
}
