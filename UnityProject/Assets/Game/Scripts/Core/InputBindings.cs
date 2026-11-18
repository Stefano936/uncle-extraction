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
    /// Direcciones de movimiento remapeables (las flechas quedan siempre como alternativa fija).
    public enum MoveKey { Forward, Back, Left, Right }

    /// Controles remapeables de teclado y raton (GDD 04; UI_SPEC 6). Cada entrada guarda la ruta de un control del Input
    /// System ("<Keyboard>/e", "<Mouse>/leftButton"); el mando conserva su asignacion fija. Se guarda en PlayerPrefs.
    /// Decision: un mapa propio y no un InputActionAsset, porque LabInput (y el autopiloto/pruebas que lo simulan) ya
    /// centraliza la lectura por accion; el asset agregaria una segunda via de entrada sin cambiar el juego.
    /// Contrato (sesion 13):
    /// - Movimiento (WASD por defecto) y acciones principales son remapeables; las flechas son alternativa fija de movimiento.
    /// - Conflictos: si una tecla ya estaba en otra entrada remapeable, se intercambian (nunca quedan dos en la misma tecla).
    ///   Teclas reservadas que no se pueden asignar: Escape (pausa/cancelar), Tab (depuracion) y las flechas.
    /// - Los cambios son un borrador: Set no guarda; SaveAll guarda (Aplicar) y Restore vuelve a una instantanea (Descartar).
    public static class InputBindings
    {
        public static readonly Act[] Remappable = { Act.GrabLeft, Act.GrabRight, Act.Interact, Act.ReleaseAll, Act.Effort, Act.Crouch, Act.Dodge, Act.Mark };
        public static readonly MoveKey[] MoveKeys = { MoveKey.Forward, MoveKey.Back, MoveKey.Left, MoveKey.Right };
        static readonly Dictionary<string, string> defaults = new Dictionary<string, string>
        {
            { "GrabLeft", "<Mouse>/leftButton" }, { "GrabRight", "<Mouse>/rightButton" }, { "Interact", "<Keyboard>/e" },
            { "ReleaseAll", "<Keyboard>/q" }, { "Effort", "<Keyboard>/leftShift" }, { "Crouch", "<Keyboard>/leftCtrl" },
            { "Dodge", "<Keyboard>/space" }, { "Restart", "<Keyboard>/r" }, { "ToggleCursor", "<Keyboard>/tab" }, { "Pause", "<Keyboard>/escape" }, { "Mark", "<Mouse>/middleButton" },
            { "MoveForward", "<Keyboard>/w" }, { "MoveBack", "<Keyboard>/s" }, { "MoveLeft", "<Keyboard>/a" }, { "MoveRight", "<Keyboard>/d" },
        };
        public static readonly string[] Reserved = { "<Keyboard>/escape", "<Keyboard>/tab", "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow" };
        static readonly Dictionary<string, string> paths = new Dictionary<string, string>();
        static bool loaded;
        public static event Action Changed;

        public static string Id(Act a) => a.ToString();
        public static string Id(MoveKey m) => "Move" + m;
        /// Entradas que muestra Opciones, en orden: movimiento y acciones principales.
        public static IEnumerable<string> RemappableIds { get { foreach (var m in MoveKeys) yield return Id(m); foreach (var a in Remappable) yield return Id(a); } }
        static bool IsRemappable(string id) { foreach (var r in RemappableIds) if (r == id) return true; return false; }

        static void Load()
        {
            if (loaded) return; loaded = true;
            foreach (var kv in defaults)
            {
                string p = kv.Value;
                try { p = PlayerPrefs.GetString("ue.bind." + kv.Key, kv.Value); } catch { }
                paths[kv.Key] = Valid(kv.Key, p) ? p : kv.Value; // vacio, reservado o con otro formato: el de fabrica
            }
        }

        /// Una ruta guardada vale si es de teclado o raton, con nombre, y no es reservada (salvo en su propia entrada fija).
        static bool Valid(string id, string p)
        {
            if (string.IsNullOrEmpty(p) || p.Length > 64) return false;
            if (!(p.StartsWith("<Keyboard>/") || p.StartsWith("<Mouse>/")) || p.EndsWith("/")) return false;
            if (IsRemappable(id) && Array.IndexOf(Reserved, p) >= 0) return false;
            return true;
        }

        /// Olvida lo cargado y vuelve a leer lo guardado (equivale a reiniciar el juego; pruebas de persistencia).
        public static void ReloadFromDisk() { loaded = false; paths.Clear(); Load(); }

        public static string Path(Act a) => Path(Id(a));
        public static string Path(MoveKey m) => Path(Id(m));
        public static string Path(string id) { Load(); return paths.TryGetValue(id, out var p) ? p : ""; }

#if UE_INPUT_SYSTEM
        /// Control del teclado o raton EN USO (Keyboard.current / Mouse.current), no el primero que encuentre el sistema:
        /// con dos teclados conectados, la busqueda global devolvia el que no se estaba usando.
        static ButtonControl Control(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            int slash = path.IndexOf('/'); if (slash < 0) return null;
            string layout = path.Substring(0, slash), name = path.Substring(slash + 1);
            InputDevice dev = layout == "<Keyboard>" ? (InputDevice)Keyboard.current : layout == "<Mouse>" ? Mouse.current : null;
            return dev != null ? dev.TryGetChildControl<ButtonControl>(name) : InputSystem.FindControl(path) as ButtonControl;
        }
        /// Teclado o raton de la accion presionado ahora.
        public static bool KeyHeld(Act a) { var c = Control(Path(a)); return c != null && c.isPressed; }
        public static bool KeyHeld(MoveKey m) { var c = Control(Path(m)); return c != null && c.isPressed; }
#endif

        /// Nombre legible del control asignado ("E", "Clic izquierdo"...), para prompts que no dicen una tecla fija.
        /// Sesion 17: con el MANDO como ultimo dispositivo usado, las ayudas muestran el boton del mando (cambia en vivo al
        /// alternar). Opciones sigue mostrando la asignacion de teclado (usa Display(string)).
        public static string Display(Act a) => PadActive && PadName(a) != null ? PadName(a) : Display(Id(a));
        public static string PadName(Act a) => a switch
        {
            Act.GrabLeft => "LT", Act.GrabRight => "RT", Act.Interact => "A", Act.ReleaseAll => "B", Act.Effort => "L3",
            Act.Crouch => "R3", Act.Dodge => "X", Act.Pause => "Start", Act.Mark => "D-pad ↑", _ => null
        };
        /// Ultimo dispositivo con actividad: mando (sticks, gatillos, botones) o teclado/raton. Se evalua una vez por cuadro.
        public static bool PadActive { get { Tick(); return padActive; } }
        public static bool ForcePad; // pruebas y capturas
        static bool padActive; static int tickFrame = -1;
        static void Tick()
        {
            if (ForcePad) { padActive = true; return; }
#if UE_INPUT_SYSTEM
            if (tickFrame == Time.frameCount) return; tickFrame = Time.frameCount;
            var g = Gamepad.current;
            if (g != null && (g.leftStick.ReadValue().sqrMagnitude > 0.09f || g.rightStick.ReadValue().sqrMagnitude > 0.09f || g.leftTrigger.isPressed || g.rightTrigger.isPressed
                || g.buttonSouth.isPressed || g.buttonEast.isPressed || g.buttonWest.isPressed || g.buttonNorth.isPressed || g.startButton.isPressed || g.dpad.ReadValue().sqrMagnitude > 0.1f)) padActive = true;
            var k = Keyboard.current; var m = Mouse.current;
            if ((k != null && k.anyKey.isPressed) || (m != null && (m.delta.ReadValue().sqrMagnitude > 9f || m.leftButton.isPressed || m.rightButton.isPressed))) padActive = false;
#endif
        }
        public static string Display(MoveKey m) => Display(Id(m));
        public static string Display(string id)
        {
            string p = Path(id);
            if (p.StartsWith("<Mouse>/")) { string k = "input.mouse_" + p.Substring(8); if (Loc.Has(k)) return Loc.T(k); }
#if UE_INPUT_SYSTEM
            var c = Control(p); if (c != null) return c.displayName;
#endif
            int i = p.LastIndexOf('/'); return i >= 0 ? p.Substring(i + 1) : p;
        }
        /// Teclas de movimiento para textos de ayuda: «WASD» si son las de fabrica, si no las cuatro asignadas.
        public static string MoveDisplay()
        {
            if (PadActive) return Loc.T("input.pad_move");
            bool def = true; foreach (var m in MoveKeys) if (Path(m) != defaults[Id(m)]) def = false;
            return def ? "WASD" : $"{Display(MoveKey.Forward)}{Display(MoveKey.Left)}{Display(MoveKey.Back)}{Display(MoveKey.Right)}";
        }

        public static bool IsReserved(string path) => Array.IndexOf(Reserved, path) >= 0;

        /// Asigna un control (borrador: no guarda). Si otra entrada remapeable lo usaba, intercambia. Devuelve la entrada
        /// intercambiada o null. Una tecla reservada no se asigna (devuelve el id propio para avisar).
        public static string Set(string id, string path)
        {
            Load();
            if (!IsRemappable(id) || IsReserved(path)) return null;
            string swapped = null;
            foreach (var other in RemappableIds)
                if (other != id && paths[other] == path) { paths[other] = paths[id]; swapped = other; }
            paths[id] = path;
            Changed?.Invoke();
            return swapped;
        }
        public static Act? Set(Act a, string path) { var s = Set(Id(a), path); return s != null && Enum.TryParse<Act>(s, out var r) ? r : (Act?)null; }

        public static void ResetAll() { Load(); foreach (var kv in defaults) paths[kv.Key] = kv.Value; Changed?.Invoke(); }

        /// Instantanea del borrador (para Descartar) y restauracion.
        public static Dictionary<string, string> Snapshot() { Load(); return new Dictionary<string, string>(paths); }
        public static void Restore(Dictionary<string, string> snap) { if (snap == null) return; Load(); foreach (var kv in snap) paths[kv.Key] = kv.Value; Changed?.Invoke(); }
        public static bool DiffersFrom(Dictionary<string, string> snap) { if (snap == null) return false; Load(); foreach (var kv in snap) if (!paths.TryGetValue(kv.Key, out var p) || p != kv.Value) return true; return false; }

        /// Guarda todas las asignaciones (Aplicar).
        public static void SaveAll() { Load(); try { foreach (var kv in paths) PlayerPrefs.SetString("ue.bind." + kv.Key, kv.Value); PlayerPrefs.Save(); } catch { } }

        /// Escucha el proximo boton de teclado o raton (no el mando) y lo asigna al borrador; Escape cancela y deja todo como
        /// estaba. Una tecla reservada se rechaza con aviso. Devuelve un objeto para cancelar la escucha. Mientras escucha,
        /// GameFlowUI bloquea la entrada de juego.
        public static IDisposable Listen(string id, Action<string, string> done)
        {
#if UE_INPUT_SYSTEM
            Listening = true;
            var sub = InputSystem.onAnyButtonPress.CallOnce(ctrl =>
            {
                Listening = false; ListenEndedFrame = Time.frameCount;
                if (ctrl.device is Gamepad) { done?.Invoke("", null); return; }
                if (ctrl.path.EndsWith("/escape")) { done?.Invoke("", null); return; }
                string p = "<" + (ctrl.device is Mouse ? "Mouse" : "Keyboard") + ">/" + ctrl.name;
                if (IsReserved(p)) { done?.Invoke("reserved", null); return; }
                var sw = Set(id, p); done?.Invoke(p, sw);
            });
            return new Stop(() => { Listening = false; sub.Dispose(); });
#else
            done?.Invoke("", null); return new Stop(() => { });
#endif
        }
        public static bool Listening { get; private set; }
        /// Cuadro en que termino la ultima escucha: la tecla que la cerro (Escape) no debe ademas abrir/cerrar la pausa.
        public static int ListenEndedFrame { get; private set; } = -10;
        public static bool JustListened => Time.frameCount - ListenEndedFrame <= 1;
        class Stop : IDisposable { readonly Action a; public Stop(Action a) { this.a = a; } public void Dispose() => a(); }
    }
}
