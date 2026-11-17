using System;
using System.Collections.Generic;
using UnityEngine;
#if UE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
#endif

namespace UncleExtraction
{
    /// Controles remapeables de teclado y raton (GDD 04; UI_SPEC 6). Cada accion guarda la ruta de un control del Input
    /// System ("<Keyboard>/e", "<Mouse>/leftButton"); el mando conserva su asignacion fija. Se guarda en PlayerPrefs.
    /// Decision: un mapa propio y no un InputActionAsset, porque LabInput (y el autopiloto/pruebas que lo simulan) ya
    /// centraliza la lectura por accion; el asset agregaria una segunda via de entrada sin cambiar el juego.
    public static class InputBindings
    {
        public static readonly Act[] Remappable = { Act.GrabLeft, Act.GrabRight, Act.Interact, Act.ReleaseAll, Act.Effort, Act.Crouch, Act.Dodge };
        static readonly Dictionary<Act, string> defaults = new Dictionary<Act, string>
        {
            { Act.GrabLeft, "<Mouse>/leftButton" }, { Act.GrabRight, "<Mouse>/rightButton" }, { Act.Interact, "<Keyboard>/e" },
            { Act.ReleaseAll, "<Keyboard>/q" }, { Act.Effort, "<Keyboard>/leftShift" }, { Act.Crouch, "<Keyboard>/leftCtrl" },
            { Act.Dodge, "<Keyboard>/space" }, { Act.Restart, "<Keyboard>/r" }, { Act.ToggleCursor, "<Keyboard>/tab" }, { Act.Pause, "<Keyboard>/escape" },
        };
        static readonly Dictionary<Act, string> paths = new Dictionary<Act, string>();
        static bool loaded;
        public static event Action Changed;

        static void Load()
        {
            if (loaded) return; loaded = true;
            foreach (var kv in defaults)
            {
                string p = kv.Value;
                try { p = PlayerPrefs.GetString("ue.bind." + kv.Key, kv.Value); } catch { }
                paths[kv.Key] = string.IsNullOrEmpty(p) ? kv.Value : p;
            }
        }

        /// Olvida lo cargado y vuelve a leer lo guardado (equivale a reiniciar el juego; pruebas de persistencia).
        public static void ReloadFromDisk() { loaded = false; paths.Clear(); Load(); }

        public static string Path(Act a) { Load(); return paths.TryGetValue(a, out var p) ? p : ""; }

#if UE_INPUT_SYSTEM
        static readonly Dictionary<string, ButtonControl> cache = new Dictionary<string, ButtonControl>();
        static ButtonControl Control(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (cache.TryGetValue(path, out var c) && c != null && c.device != null && c.device.added) return c;
            c = InputSystem.FindControl(path) as ButtonControl; cache[path] = c; return c;
        }
        /// Teclado o raton de la accion presionado ahora.
        public static bool KeyHeld(Act a) { var c = Control(Path(a)); return c != null && c.isPressed; }
#endif

        /// Nombre legible del control asignado ("E", "Clic izquierdo"...), para prompts que no dicen una tecla fija.
        public static string Display(Act a)
        {
            string p = Path(a);
            if (p.StartsWith("<Mouse>/")) { string k = "input.mouse_" + p.Substring(8); if (Loc.Has(k)) return Loc.T(k); }
#if UE_INPUT_SYSTEM
            var c = Control(p); if (c != null) return c.displayName;
#endif
            int i = p.LastIndexOf('/'); return i >= 0 ? p.Substring(i + 1) : p;
        }

        /// Asigna un control; si otra accion remapeable lo usaba, intercambia (no quedan dos acciones en la misma tecla).
        /// Devuelve la accion intercambiada o null.
        public static Act? Set(Act a, string path)
        {
            Load();
            Act? swapped = null;
            foreach (var other in Remappable)
                if (other != a && paths[other] == path) { paths[other] = paths[a]; swapped = other; Save(other); }
            paths[a] = path; Save(a);
            Changed?.Invoke();
            return swapped;
        }

        public static void ResetAll()
        {
            Load();
            foreach (var kv in defaults) { paths[kv.Key] = kv.Value; Save(kv.Key); }
            Changed?.Invoke();
        }

        static void Save(Act a) { try { PlayerPrefs.SetString("ue.bind." + a, paths[a]); PlayerPrefs.Save(); } catch { } }

        /// Escucha el proximo boton de teclado o raton (no el mando) y lo asigna; Escape cancela. Devuelve un objeto para
        /// cancelar la escucha. Mientras escucha, GameFlowUI bloquea la entrada de juego.
        public static IDisposable Listen(Act a, Action<string, Act?> done)
        {
#if UE_INPUT_SYSTEM
            Listening = true;
            var sub = InputSystem.onAnyButtonPress.CallOnce(ctrl =>
            {
                Listening = false;
                if (ctrl.device is Gamepad) { done?.Invoke("", null); return; }
                if (ctrl.path.EndsWith("/escape")) { done?.Invoke("", null); return; }
                string p = "<" + (ctrl.device is Mouse ? "Mouse" : "Keyboard") + ">/" + ctrl.name;
                var sw = Set(a, p); done?.Invoke(p, sw);
            });
            return new Stop(() => { Listening = false; sub.Dispose(); });
#else
            done?.Invoke("", null); return new Stop(() => { });
#endif
        }
        public static bool Listening { get; private set; }
        class Stop : IDisposable { readonly Action a; public Stop(Action a) { this.a = a; } public void Dispose() => a(); }
    }
}
