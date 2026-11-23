using UnityEngine;
using UnityEngine.SceneManagement;

namespace UncleExtraction
{
    /// Sesion 21 (UE-16): entorno de la ruta modelado en Blender (tools/art/blender/build_route_s21.py): carrito, carroceria e
    /// interior de carga de la camioneta con sus hojas traseras, puerta de salida y cuadra de enfrente.
    /// SOLO VISUAL: se monta al cargar la escena sobre la geometria funcional, que no cambia (colisiones, masas, sockets, piso de
    /// carga, rampa, zona de extraccion, bisagras). No lleva colliders ni NetworkObject; cada proceso lo arma por su cuenta.
    /// Se hace en ejecucion y no en el constructor de escenas porque regenerar Bar_LastRound cambia el orden de creacion de los
    /// cuerpos en PhysX y altera el rescate del pie en la camioneta (UE-18); asi la escena queda identica.
    /// -ueOldRoute deja el entorno anterior para comparar.
    public static class RouteDressing
    {
        public const string Folder = "RouteS21/";
        public static bool Enabled => !QaArgs.Has("-ueOldRoute");
        public static int Applied { get; private set; }   // piezas montadas en la ultima escena (pruebas)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; }

        static void OnLoaded(Scene s, LoadSceneMode m) { if (Enabled) Apply(s); }

        public static int Apply(Scene scene)
        {
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var cart in root.GetComponentsInChildren<TransportCart>(true))
                    if (Mount(cart.transform, "route_cart", HideAll(cart.transform))) n++;
                foreach (var door in root.GetComponentsInChildren<ExitDoor>(true))
                    if (Mount(door.transform, "route_exit_door", HideAll(door.transform))) n++;
                foreach (var doors in root.GetComponentsInChildren<VanDoors>(true))
                {
                    if (doors.leftLeaf && Mount(doors.leftLeaf, "route_van_leaf_l", HideAll(doors.leftLeaf))) n++;
                    if (doors.rightLeaf && Mount(doors.rightLeaf, "route_van_leaf_r", HideAll(doors.rightLeaf))) n++;
                }
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Passenger_Van" && Mount(t, "route_van", HideVan(t))) n++;
                    else if (t.name == "Facade" && Mount(null, "route_street", HideBlock(t))) n++;
                }
            }
            Applied = n;
            if (n > 0) Debug.Log($"[Route] entorno S21 montado: {n} piezas (-ueOldRoute para el anterior)");
            return n;
        }

        /// Instancia el modelo (marco local = marco del padre; sin padre, mundo) si se ocultaron las piezas que reemplaza.
        static bool Mount(Transform parent, string model, bool hidOld)
        {
            if (!hidOld) return false;
            var prefab = Resources.Load<GameObject>(Folder + model);
            if (!prefab) { Debug.LogWarning($"[Route] falta {Folder}{model}"); return false; }
            // se conserva la transformacion local del modelo importado (la raiz del FBX lleva la conversion de ejes)
            var go = Object.Instantiate(prefab, parent, false); go.name = "Route_" + model;
            if (!parent) SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            return true;
        }

        static bool HideAll(Transform t)
        {
            bool any = false;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true)) if (!r.name.StartsWith("Route_") && !Under(r.transform, "Route_")) { r.enabled = false; any = true; }
            return any;
        }

        /// Camioneta: se ocultan la carroceria anterior, sus luces y ventanas y las piezas del kit. Quedan el piso, la rampa con
        /// sus bordes, la zona de carga con la cinta y la luz del techo (lo que se usa para jugar y para leer la zona).
        static bool HideVan(Transform van)
        {
            bool any = false;
            foreach (var r in van.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name;
                bool keep = n == "Van_Floor" || n == "Van_Ramp" || n == "Ramp_Edge" || n == "Zone_Marker" || n == "Zone_Tape" || n == "Van_DomeLamp" || Under(r.transform, "Route_");
                if (!keep) { r.enabled = false; any = true; }
            }
            return any;
        }

        /// Cuadra de enfrente: se ocultan los bloques y sus ventanas (z menor que -19). Vecinos del bar, cartel y apliques quedan.
        static bool HideBlock(Transform facade)
        {
            bool any = false;
            foreach (var r in facade.GetComponentsInChildren<Renderer>(true))
                if ((r.name == "Block" || r.name == "Win") && r.bounds.center.z < -19f) { r.enabled = false; any = true; }
            return any;
        }

        static bool Under(Transform t, string prefix) { for (var p = t; p; p = p.parent) if (p.name.StartsWith(prefix)) return true; return false; }
    }
}
