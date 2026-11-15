using UnityEngine;
#if UE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UncleExtraction
{
    public enum Act { GrabLeft, GrabRight, Interact, ReleaseAll, Effort, Crouch, Dodge, Restart, ToggleCursor }

    /// Entrada del laboratorio. Usa Input System si el paquete esta instalado; si no, el Input Manager clasico.
    /// TODO M1+: migrar a un InputActionAsset remapeable (GDD 04).
    public static class LabInput
    {
        /// Movimiento simulado (autopiloto/pruebas). Si tiene valor, reemplaza al teclado.
        public static Vector2? SimulatedMove;

        public static Vector2 Move() => SimulatedMove ?? RealMove();

        static Vector2 RealMove()
        {
#if UE_INPUT_SYSTEM
            Vector2 v = Vector2.zero;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed) v.y += 1; if (k.sKey.isPressed) v.y -= 1;
                if (k.dKey.isPressed) v.x += 1; if (k.aKey.isPressed) v.x -= 1;
            }
            var g = Gamepad.current;
            if (g != null) v += g.leftStick.ReadValue();
            return Vector2.ClampMagnitude(v, 1f);
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
            if ((a == Act.GrabLeft || a == Act.GrabRight) && Cursor.lockState != CursorLockMode.Locked) return false;
            return RealHeld(a);
        }

        public static bool Held(Act a) => Simulated[(int)a] || RealHeld(a);

        static bool RealHeld(Act a)
        {
#if UE_INPUT_SYSTEM
            var k = Keyboard.current; var m = Mouse.current; var g = Gamepad.current;
            switch (a)
            {
                case Act.GrabLeft: return (m != null && m.leftButton.isPressed) || (g != null && g.leftTrigger.isPressed);
                case Act.GrabRight: return (m != null && m.rightButton.isPressed) || (g != null && g.rightTrigger.isPressed);
                case Act.Interact: return (k != null && k.eKey.isPressed) || (g != null && g.buttonSouth.isPressed);
                case Act.ReleaseAll: return (k != null && k.qKey.isPressed) || (g != null && g.buttonEast.isPressed);
                case Act.Effort: return (k != null && k.leftShiftKey.isPressed) || (g != null && g.leftStickButton.isPressed);
                case Act.Crouch: return (k != null && k.leftCtrlKey.isPressed) || (g != null && g.rightStickButton.isPressed);
                case Act.Dodge: return (k != null && k.spaceKey.isPressed) || (g != null && g.buttonWest.isPressed);
                case Act.Restart: return k != null && k.rKey.isPressed;
                case Act.ToggleCursor: return k != null && k.tabKey.isPressed;
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
                for (int i = 0; i <= (int)Act.ToggleCursor; i++)
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
