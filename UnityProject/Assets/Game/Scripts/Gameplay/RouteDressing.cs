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
        public static int Applied { get; private set; }   // piezas de la ruta montadas en la ultima escena (pruebas)
        public static int BarApplied { get; private set; } // muebles del bar montados en la ultima escena (pruebas)

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
            BarApplied = ApplyBar(scene);
            if (BarApplied > 0) Debug.Log($"[Route] muebles del bar S21 montados: {BarApplied} piezas");
            return n + BarApplied;
        }

        // ---------- muebles del bar (build_bar_s21.py): barra y mueble del fondo, banquetas, mesas, tarima, silla y vaso.
        // Solo en la escena que tiene la barra (Bar_LastRound); las colisiones de cada mueble quedan como estaban.
        static int ApplyBar(Scene scene)
        {
            Transform counter = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Bar_Counter" && t.GetComponent<BoxCollider>()) counter = t;
            if (!counter) return 0;
            int n = 0; var env = counter.parent;
            bool hid = false;
            foreach (var t in env.GetComponentsInChildren<Transform>(true))
            {
                string nm = t.name;
                if (nm == "Kit_bar_counter" || nm == "Bar_Trims") hid |= HideAll(t);
                else if (nm == "Anchor_Rail" || nm == "BackBar_Panel" || nm == "Shelf" || nm == "Bottle" || nm == "Neck") hid |= HideOwn(t);
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Bar_Identity") foreach (var c in t.GetComponentsInChildren<Transform>(true))
                            if (c.name == "BackBar_Panel" || c.name == "Shelf" || c.name == "Bottle" || c.name == "Neck") hid |= HideOwn(c);
            if (MountAt(env, "bar_counter", Floor(counter.position), hid)) n++;
            foreach (var t in env.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Kit_stool" && MountAt(env, "bar_stool", Floor(t.position), HideAll(t))) n++;
                else if (t.name == "Kit_table" && MountAt(env, "bar_table", Floor(t.position), HideAll(t))) n++;
                else if (t.name == "Karaoke_Stage")
                {
                    bool h = HideOwn(t); var mic = env.Find("Mic_Stand"); if (mic) h |= HideOwn(mic);
                    if (MountAt(env, "bar_stage", Floor(t.position), h)) n++;
                }
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Chair_Light" && MountUnscaled(t, "bar_chair", HideOwn(t))) n++;
                    else if (t.name == "Decoy_Glass" && MountUnscaled(t, "bar_pint", HideOwn(t))) n++;
                }
            return n;
        }

        static Vector3 Floor(Vector3 p) => new Vector3(p.x, 0f, p.z);

        /// Mueble fijo: hijo del entorno, en una posicion de mundo (giro de la raiz del modelo conservado).
        static bool MountAt(Transform parent, string model, Vector3 worldPos, bool hidOld)
        {
            var go = Spawn(parent, model, hidOld); if (!go) return false;
            go.position = worldPos; return true;
        }

        /// Objeto dinamico hecho con una primitiva escalada (silla, vaso): el modelo va de hijo y compensa la escala del padre,
        /// asi se mueve con el cuerpo fisico sin deformarse.
        static bool MountUnscaled(Transform parent, string model, bool hidOld)
        {
            if (!hidOld) return false;
            // contenedor alineado con el padre que anula su escala; el modelo conserva su propio giro (conversion de ejes)
            var holder = new GameObject("Route_" + model).transform; holder.SetParent(parent, false);
            var ls = parent.lossyScale; holder.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
            var go = Spawn(holder, model, true); if (!go) { Object.Destroy(holder.gameObject); return false; }
            go.name = "Model"; return true;
        }

        static bool HideOwn(Transform t)
        {
            var r = t.GetComponent<Renderer>(); if (!r || !r.enabled) return false;
            r.enabled = false; return true;
        }

        /// Instancia el modelo (marco local = marco del padre; sin padre, mundo) si se ocultaron las piezas que reemplaza.
        static bool Mount(Transform parent, string model, bool hidOld) => Spawn(parent, model, hidOld) != null;

        static Transform Spawn(Transform parent, string model, bool hidOld)
        {
            if (!hidOld) return null;
            var prefab = Resources.Load<GameObject>(Folder + model);
            if (!prefab) { Debug.LogWarning($"[Route] falta {Folder}{model}"); return null; }
            // se conserva la transformacion local del modelo importado (la raiz del FBX lleva la conversion de ejes)
            var go = Object.Instantiate(prefab, parent, false); go.name = "Route_" + model;
            if (!parent) SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            return go.transform;
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
