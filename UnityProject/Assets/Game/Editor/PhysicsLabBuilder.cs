using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;

namespace UncleExtraction.EditorTools
{
    /// Construye PhysicsLab de forma reproducible (especificacion 03): suelo, paredes, puerta con bisagra, obstaculo bajo,
    /// rampa, barra con anclaje, mesa fija, silla liviana, carrito, tio, camioneta fija y zona de extraccion.
    /// Arte: primitivas de color. Los FBX del kit se integran despues (preproduction/uncle_extraction_kit_3D).
    public static class PhysicsLabBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/PhysicsLab.unity";
        public const string PlayerPrefabPath = "Assets/Game/Prefabs/Player.prefab";
        const string MatDir = "Assets/Game/Materials";

        [MenuItem("Uncle Extraction/Reconstruir PhysicsLab")]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static void BuildFromCommandLine()
        {
            int code = 1;
            try { Build(); code = 0; } catch (Exception e) { Debug.LogException(e); }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        public const string BarScenePath = "Assets/Game/Scenes/Bar_LastRound.unity";
        public const string ArticulatedLabScenePath = "Assets/Game/Scenes/PhysicsLab_Articulated.unity";

        /// Reconstruye TODO lo reproducible: prefab del jugador, PhysicsLab (diagnostico), Bar_LastRound (principal),
        /// menu y Build Settings (Frontend, Bar_LastRound, PhysicsLab).
        public static void Build()
        {
            UrpSetup.Ensure(); // pipeline de render antes de crear materiales y escenas
            UiKitImporter.Import(); // sprites, fuentes y catalogos EN/es_UY del kit de UI
            Directory.CreateDirectory("Assets/Game/Scenes"); Directory.CreateDirectory(MatDir); Directory.CreateDirectory("Assets/Game/Prefabs");
            builtPlayerPrefab = null;
            BuildLevel(LevelSpecs.Lab());
            BuildLevel(LevelSpecs.Bar());
            BuildLevel(LevelSpecs.LabArticulated());
            FrontendBuilder.Build();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(FrontendBuilder.ScenePath, true), new EditorBuildSettingsScene(BarScenePath, true), new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene(ArticulatedLabScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[LabBuilder] escenas: Frontend, Bar_LastRound, PhysicsLab, PhysicsLab_Articulated");
        }

        static GameObject builtPlayerPrefab;
        internal static Material floorM, wallM, woodM, metalM;

        static void BuildLevel(LevelSpec spec)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            floorM = Mat("Floor", new Color(0.35f, 0.3f, 0.27f));
            wallM = Mat("Wall", new Color(0.62f, 0.52f, 0.4f));
            woodM = Mat("Wood", new Color(0.45f, 0.27f, 0.15f));
            metalM = Mat("Metal", new Color(0.6f, 0.62f, 0.65f));
            var cartM = Mat("Cart", new Color(0.15f, 0.45f, 0.85f));
            var vanM = Mat("Van", new Color(0.9f, 0.9f, 0.85f));
            var walkM = Mat("Sidewalk", new Color(0.55f, 0.55f, 0.55f));
            var zoneM = Mat("Zone", new Color(0.2f, 0.9f, 0.4f));
            var skinM = Mat("Skin", new Color(0.95f, 0.75f, 0.6f));
            var uncleM = Mat("Uncle", new Color(0.55f, 0.35f, 0.75f));
            var handM = Mat("Hand", new Color(0.95f, 0.8f, 0.65f));

            var slippery = PhysMat("Wheels", 0.02f, 0.02f, PhysicsMaterialCombine.Minimum);
            var playerPM = PhysMat("Player", 0f, 0f, PhysicsMaterialCombine.Minimum);
            var unclePM = PhysMat("Uncle", 0.25f, 0.3f, PhysicsMaterialCombine.Average);

            // Luz
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f; light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

            // Escenario: interior x[-7,7] z[-2,10]; vereda y camioneta hacia -z
            var env = new GameObject("Environment").transform;
            Box("Floor", env, new Vector3(0, -0.1f, 0), new Vector3(30, 0.2f, 44), floorM); // z -22..22: el suelo pasa de largo la camioneta
            // Limites del area jugable (vereda y calle): muros invisibles; nadie se cae por un borde del mapa
            var bounds = new GameObject("Play_Bounds").transform; bounds.SetParent(env);
            NoRenderer(Box("Bound_W", bounds, new Vector3(-8.5f, 1.5f, -9f), new Vector3(0.2f, 3f, 14f), wallM));
            NoRenderer(Box("Bound_E", bounds, new Vector3(8.5f, 1.5f, -9f), new Vector3(0.2f, 3f, 14f), wallM));
            NoRenderer(Box("Bound_S", bounds, new Vector3(0f, 1.5f, -16.2f), new Vector3(17f, 3f, 0.2f), wallM));
            // Paredes del bar: interior x[-halfX, halfX], z[doorZ, backZ]; abertura de 1,5 m en el muro frontal (GDD: puertas de 1,5 x 2,2 m)
            float hx = spec.halfX, bz = spec.backZ, dz = spec.doorZ, dx = spec.doorX, depth = bz - dz, midZ = (bz + dz) * 0.5f;
            Box("Wall_Back", env, new Vector3(0, 1.5f, bz + 0.1f), new Vector3(hx * 2f + 0.4f, 3, 0.2f), wallM);
            Box("Wall_L", env, new Vector3(-hx - 0.1f, 1.5f, midZ), new Vector3(0.2f, 3, depth + 0.4f), wallM);
            Box("Wall_R", env, new Vector3(hx + 0.1f, 1.5f, midZ), new Vector3(0.2f, 3, depth + 0.4f), wallM);
            float leftLen = (dx - 0.75f) + hx, rightLen = hx - (dx + 0.75f);
            Box("Wall_Front_L", env, new Vector3(-hx + leftLen * 0.5f, 1.5f, dz), new Vector3(leftLen, 3, 0.2f), wallM);
            Box("Wall_Front_R", env, new Vector3(hx - rightLen * 0.5f, 1.5f, dz), new Vector3(rightLen, 3, 0.2f), wallM);
            Box("Door_Lintel", env, new Vector3(dx, 2.6f, dz), new Vector3(1.5f, 0.8f, 0.2f), wallM);
            NoCollider(Box("Sidewalk", env, new Vector3(0, 0.01f, dz - 3.5f), new Vector3(14, 0.02f, 6), walkM));
            var rail = spec.furnish(env);

            // Silla liviana (objeto dinamico)
            var chair = Box("Chair_Light", env, spec.chairPos, new Vector3(0.45f, 0.6f, 0.45f), woodM);
            var chairRb = chair.AddComponent<Rigidbody>(); chairRb.mass = 6f;
            Socket(chair.transform, "chair", "silla", Vector3.zero, chairRb, 1);

            // Vaso senuelo sobre la mesa fija (GDD 09): se lleva con una mano y distrae al tio si esta apoyado y a la vista
            var glass = Prim(PrimitiveType.Cylinder, "Decoy_Glass", env, spec.glassPos, new Vector3(0.09f, 0.08f, 0.09f), Mat("Beer", new Color(0.95f, 0.7f, 0.15f)));
            var glassRb = glass.AddComponent<Rigidbody>(); glassRb.mass = 0.4f; glassRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Socket(glass.transform, "glass", "vaso senuelo", Vector3.zero, glassRb, 1);
            glass.AddComponent<DecoyGlass>();

            // Puerta con bisagra (origen en la bisagra, abertura 1,5 m)
            var door = new GameObject("Exit_Door"); door.transform.SetParent(env); door.transform.position = new Vector3(dx - 0.75f, 0, dz);
            var doorRb = door.AddComponent<Rigidbody>(); doorRb.mass = 25f; doorRb.angularDamping = 2f;
            var panel = Box("Door_Panel", door.transform, new Vector3(0, 0, 0), new Vector3(1.46f, 2.1f, 0.06f), woodM);
            panel.transform.localPosition = new Vector3(0.75f, 1.06f, 0);
            var hinge = door.AddComponent<HingeJoint>();
            hinge.anchor = Vector3.zero; hinge.axis = Vector3.up; hinge.useLimits = true;
            hinge.limits = new JointLimits { min = -100, max = 100 };
            hinge.useSpring = true; hinge.spring = new JointSpring { spring = 15, damper = 4, targetPosition = 0 };
            bool kitDoor = spec.kitVisuals && KitVisual("exit_door", door.transform, Vector3.zero, Quaternion.Euler(0, 180, 0));
            if (kitDoor) HideRenderer(panel); // visual del kit (origen en la bisagra, hoja hacia -x: girada 180 grados); colision: el panel primitivo
            // Manija agarrable cerca del borde libre (abrir, sostener o cerrar la hoja) y reten de apertura (ExitDoor)
            Socket(door.transform, "door_handle", "manija de la puerta", new Vector3(1.3f, 1.0f, 0f), doorRb, 1);
            if (!kitDoor) foreach (var side in new[] { -1f, 1f })
                NoCollider(LocalPrim(PrimitiveType.Cube, "Door_Knob", door.transform, new Vector3(1.3f, 1.0f, side * 0.06f), new Vector3(0.12f, 0.03f, 0.05f), metalM));
            door.AddComponent<ExitDoor>();

            // Camioneta fija y zona de extraccion (detras de las puertas traseras)
            // Camioneta de pasajeros hueca (kit 3D: 2,52 x 5,38 x 2,33 m). Abertura trasera en z = -9,25 con dos hojas;
            // piso de carga a 0,45 m con rampa corta (decision de prototipo); mampara de cabina en z = -12,4.
            var van = new GameObject("Passenger_Van").transform; van.SetParent(env);
            const float rearZ = -9.25f, frontZ = -14.65f, floorY = 0.30f, roofY = 2.3f, halfW = 1.25f, cabZ = -12.4f;
            float vanLen = rearZ - frontZ, vanMidZ = (rearZ + frontZ) * 0.5f;
            Box("Van_Floor", van, new Vector3(0, floorY * 0.5f, vanMidZ), new Vector3(halfW * 2f, floorY, vanLen), vanM);
            Box("Van_Side_L", van, new Vector3(-halfW + 0.05f, (floorY + roofY) * 0.5f, vanMidZ), new Vector3(0.1f, roofY - floorY, vanLen), vanM);
            Box("Van_Side_R", van, new Vector3(halfW - 0.05f, (floorY + roofY) * 0.5f, vanMidZ), new Vector3(0.1f, roofY - floorY, vanLen), vanM);
            Box("Van_Roof", van, new Vector3(0, roofY - 0.05f, vanMidZ), new Vector3(halfW * 2f, 0.1f, vanLen), vanM);
            Box("Van_Cab_Partition", van, new Vector3(0, (floorY + roofY) * 0.5f, cabZ), new Vector3(halfW * 2f, roofY - floorY, 0.1f), vanM);
            Box("Van_Front", van, new Vector3(0, (floorY + roofY) * 0.5f, frontZ + 0.05f), new Vector3(halfW * 2f, roofY - floorY, 0.1f), vanM);
            Box("Van_Rear_Lintel", van, new Vector3(0, roofY - 0.08f, rearZ + 0.05f), new Vector3(halfW * 2f, 0.16f, 0.1f), vanM);
            // Rampa de 2,4 m (7 grados): subir al tio arrastrando cuesta ~570 N (pendiente + friccion), dentro del tope de un jugador (700 N)
            const float rampLen = 2.4f;
            var ramp = Box("Van_Ramp", van, new Vector3(0, floorY * 0.5f - 0.03f, rearZ + rampLen * 0.5f), new Vector3(1.7f, 0.06f, rampLen + 0.06f), metalM);
            ramp.transform.rotation = Quaternion.Euler(Mathf.Atan2(floorY, rampLen) * Mathf.Rad2Deg, 0, 0); // alto del lado de la camioneta (-z)

            // Puertas traseras (red): las hojas giran hacia afuera; cerradas tapan la abertura.
            var doorsGo = new GameObject("Van_Rear_Doors"); doorsGo.transform.position = Vector3.zero;
            var doors = doorsGo.AddComponent<VanDoors>();
            Transform Leaf(string n, float hingeX, float dir)
            {
                var hinge = new GameObject(n).transform; hinge.SetParent(doorsGo.transform, false); hinge.position = new Vector3(hingeX, 0, rearZ + 0.02f);
                var hrb = hinge.gameObject.AddComponent<Rigidbody>(); hrb.isKinematic = true; // colisionador que se mueve: cinematico, no estatico
                var panel = LocalBox(n + "_Panel", hinge, new Vector3(dir * 0.6f, floorY + 0.9f, 0.03f), new Vector3(1.18f, 1.75f, 0.05f), vanM);
                return hinge;
            }
            doors.leftLeaf = Leaf("Van_Door_L", -halfW + 0.03f, 1f);
            doors.rightLeaf = Leaf("Van_Door_R", halfW - 0.03f, -1f);
            doors.closePoint = new Vector3(0, 1.2f, rearZ); doors.doorwayCenter = new Vector3(0, floorY + 0.9f, rearZ + 0.62f);
            doors.doorwayHalf = new Vector3(halfW - 0.1f, 0.85f, 0.6f);

            // Volumen de carga (interior, entre la abertura y la mampara)
            var zoneGo = new GameObject("Extraction_Zone"); zoneGo.transform.position = new Vector3(0, floorY + 0.85f, (rearZ + cabZ) * 0.5f);
            var zoneBox = zoneGo.AddComponent<BoxCollider>(); zoneBox.size = new Vector3(halfW * 2f - 0.25f, 1.7f, (rearZ - cabZ) - 0.1f);
            NoCollider(Box("Zone_Marker", zoneGo.transform, new Vector3(0, floorY + 0.01f, (rearZ + cabZ) * 0.5f), new Vector3(halfW * 2f - 0.3f, 0.02f, (rearZ - cabZ) - 0.2f), zoneM));
            var zone = zoneGo.AddComponent<ExtractionZone>();

            // Carrito
            var cartGo = new GameObject("Transport_Cart"); cartGo.transform.SetPositionAndRotation(spec.cartPos, Quaternion.Euler(0, spec.cartYaw, 0));
            var cartRb = cartGo.AddComponent<Rigidbody>(); cartRb.mass = 30f; cartRb.linearDamping = 0.3f; cartRb.angularDamping = 3f;
            cartRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            cartRb.interpolation = RigidbodyInterpolation.Interpolate;
            var ct = cartGo.transform;
            LocalBox("Cart_Base", ct, new Vector3(0, 0.3f, 0), new Vector3(0.9f, 0.1f, 1.4f), cartM);
            LocalBox("Cart_Side_L", ct, new Vector3(-0.45f, 0.45f, 0), new Vector3(0.05f, 0.2f, 1.4f), cartM);
            LocalBox("Cart_Side_R", ct, new Vector3(0.45f, 0.45f, 0), new Vector3(0.05f, 0.2f, 1.4f), cartM);
            LocalBox("Cart_Front", ct, new Vector3(0, 0.45f, 0.7f), new Vector3(0.9f, 0.2f, 0.05f), cartM);
            LocalBox("Cart_Post_L", ct, new Vector3(-0.35f, 0.65f, -0.72f), new Vector3(0.05f, 0.7f, 0.05f), metalM);
            LocalBox("Cart_Post_R", ct, new Vector3(0.35f, 0.65f, -0.72f), new Vector3(0.05f, 0.7f, 0.05f), metalM);
            var handle = LocalBox("Cart_Handle", ct, new Vector3(0, 1.0f, -0.74f), new Vector3(0.75f, 0.05f, 0.05f), metalM);
            foreach (var x in new[] { -0.38f, 0.38f }) foreach (var z in new[] { -0.55f, 0.55f })
            {
                var w = new GameObject("Wheel"); w.transform.SetParent(ct, false); w.transform.localPosition = new Vector3(x, 0.12f, z);
                var sc = w.AddComponent<SphereCollider>(); sc.radius = 0.12f; sc.sharedMaterial = slippery;
            }
            var bedGo = new GameObject("Cart_Bed"); bedGo.transform.SetParent(ct, false);
            var bed = bedGo.AddComponent<BoxCollider>(); bed.isTrigger = true; bed.center = new Vector3(0, 0.7f, 0); bed.size = new Vector3(0.9f, 0.7f, 1.4f);
            var strapPt = new GameObject("Strap_Point").transform; strapPt.SetParent(ct, false); strapPt.localPosition = new Vector3(0, 0.6f, 0);
            Socket(handle.transform, "cart_handle", "asa del carrito", Vector3.zero, cartRb, 2);
            var cart = cartGo.AddComponent<TransportCart>(); cart.bed = bed; cart.strapPoint = strapPt;

            // Tio
            NpcBody body; NpcBrain brain; NpcPresentation pres;
            var npcGo = spec.articulatedUncle
                ? ArticulatedUncleBuilder.Build(spec.home, uncleM, skinM, unclePM, out body, out brain, out pres)
                : BuildUncle(uncleM, skinM, unclePM, spec.home, out body, out brain, out pres);
            brain.anchorRail = rail.GetComponent<Collider>();
            // Aviso de ataque en el piso y nombre sobre la cabeza (ambas variantes del tio)
            var telegraph = npcGo.AddComponent<AttackTelegraph>(); telegraph.brain = brain; telegraph.body = body; telegraph.material = AttackPoseBuilder.TelegraphMaterial();
            var home = new GameObject("Uncle_Home").transform; home.position = spec.home; brain.home = home;
            cart.npc = body; zone.npc = body;

            // Jugador
            // Jugador: prefab de red que el anfitrion genera por cada conexion (no vive en la escena).
            if (!builtPlayerPrefab)
            {
                var playerGo = BuildPlayer(handM, playerPM, out var motor, out var hands);
                builtPlayerPrefab = PrefabUtility.SaveAsPrefabAsset(playerGo, PlayerPrefabPath);
                UnityEngine.Object.DestroyImmediate(playerGo);
            }
            var playerPrefab = builtPlayerPrefab;

            // Objetos de escena replicados (fisica del anfitrion; clientes cinematicos e interpolados)
            Networked(npcGo); Networked(cartGo); Networked(door); Networked(chair); Networked(glass);
            zoneGo.AddComponent<NetworkObject>(); doorsGo.AddComponent<NetworkObject>();

            // Camara general mientras no hay jugador local (conectando)
            var overview = new GameObject("Overview_Camera").AddComponent<Camera>();
            overview.transform.SetPositionAndRotation(spec.overviewPos, Quaternion.Euler(spec.overviewEuler)); overview.depth = -10;

            // Sistemas
            var sys = new GameObject("Systems");
            var layout = sys.AddComponent<LevelLayout>(); spec.layout(layout);
            layout.doorX = dx; layout.doorZ = dz; layout.vanAxisX = 0f; layout.vanRearZ = rearZ; layout.vanCabZ = cabZ;
            sys.AddComponent<NetworkObject>();
            var session = sys.AddComponent<NetSession>(); session.playerPrefab = playerPrefab;
            var round = sys.AddComponent<RoundDirector>();
            var rig = sys.AddComponent<LabForceRig>(); rig.npc = body; rig.pullTarget = zoneGo.transform;
            sys.AddComponent<PhysicsWatchdog>();
            var flow = sys.AddComponent<GameFlowUI>(); flow.round = round; flow.npc = brain;
            var hud = sys.AddComponent<LabDebugHud>();
            hud.npc = brain; hud.npcBody = body; hud.cart = cart; hud.zone = zone; hud.round = round; hud.rig = rig;
            var auto = sys.AddComponent<LabAutopilot>();
            auto.npc = brain; auto.body = body; auto.cart = cart; auto.zone = zone; auto.round = round;
            hud.autopilot = auto;

            // Materiales del kit (escenario principal): solo visual; el laboratorio conserva colores lisos de diagnostico
            if (spec.kitVisuals) foreach (var rootGo in scene.GetRootGameObjects()) KitMaterials.ApplyToLevel(rootGo.transform, floorM, wallM, woodM);

            EditorSceneManager.SaveScene(scene, spec.scenePath);
            // NGO genera GlobalObjectIdHash en OnValidate, que en modo batch no siempre corre: forzarlo y guardar de nuevo.
            int hashed = ForceNetworkIds(UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            if (spec.scenePath == ScenePath) hashed += ForceNetworkIds(new[] { playerPrefab.GetComponent<NetworkObject>() });
            EditorUtility.SetDirty(playerPrefab); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, spec.scenePath);
            Debug.Log($"[LabBuilder] {spec.name}: network ids {hashed}, sockets={UnityEngine.Object.FindObjectsByType<GrabSocket>(FindObjectsSortMode.None).Length}, guardada en {spec.scenePath}");
        }

        static GameObject BuildUncle(Material uncleM, Material skinM, PhysicsMaterial pm, Vector3 at, out NpcBody body, out NpcBrain brain, out NpcPresentation pres)
        {
            var go = new GameObject("Uncle");
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, 180, 0));
            var rb = go.AddComponent<Rigidbody>();
            var cap = go.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, 0.95f, 0); cap.height = 1.9f; cap.radius = 0.33f; cap.sharedMaterial = pm;
            var t = go.transform;
            var torso = NoCollider(LocalPrim(PrimitiveType.Capsule, "Torso_Visual", t, new Vector3(0, 1.15f, 0), new Vector3(0.66f, 0.42f, 0.5f), uncleM));
            var belly = NoCollider(LocalPrim(PrimitiveType.Sphere, "Belly_Visual", t, new Vector3(0, 1.05f, 0.1f), new Vector3(0.62f, 0.55f, 0.5f), uncleM));
            var headV = NoCollider(LocalPrim(PrimitiveType.Sphere, "Head_Visual", t, new Vector3(0, 1.72f, 0), Vector3.one * 0.3f, skinM));
            var nose = NoCollider(LocalPrim(PrimitiveType.Sphere, "Nose_Visual", t, new Vector3(0, 1.72f, 0.15f), Vector3.one * 0.08f, Mat("Nose", new Color(0.9f, 0.3f, 0.3f))));
            Transform Pivot(string n, Vector3 p) { var x = new GameObject(n).transform; x.SetParent(t, false); x.localPosition = p; return x; }
            var armL = Pivot("Arm_L", new Vector3(-0.36f, 1.46f, 0)); var armR = Pivot("Arm_R", new Vector3(0.36f, 1.46f, 0));
            var legL = Pivot("Leg_L", new Vector3(-0.14f, 0.9f, 0)); var legR = Pivot("Leg_R", new Vector3(0.14f, 0.9f, 0));
            var aLV = NoCollider(LocalPrim(PrimitiveType.Capsule, "Arm_L_Visual", armL, new Vector3(0, -0.32f, 0), new Vector3(0.14f, 0.32f, 0.14f), uncleM));
            var aRV = NoCollider(LocalPrim(PrimitiveType.Capsule, "Arm_R_Visual", armR, new Vector3(0, -0.32f, 0), new Vector3(0.14f, 0.32f, 0.14f), uncleM));
            var lLV = NoCollider(LocalPrim(PrimitiveType.Capsule, "Leg_L_Visual", legL, new Vector3(0, -0.45f, 0), new Vector3(0.18f, 0.45f, 0.18f), uncleM));
            var lRV = NoCollider(LocalPrim(PrimitiveType.Capsule, "Leg_R_Visual", legR, new Vector3(0, -0.45f, 0), new Vector3(0.18f, 0.45f, 0.18f), uncleM));
            NoCollider(LocalPrim(PrimitiveType.Sphere, "Hand_L_Visual", armL, new Vector3(0, -0.66f, 0), Vector3.one * 0.13f, skinM));
            NoCollider(LocalPrim(PrimitiveType.Sphere, "Hand_R_Visual", armR, new Vector3(0, -0.66f, 0), Vector3.one * 0.13f, skinM));

            Transform Marker(string n, Vector3 p) => Pivot(n, p);
            body = go.AddComponent<NpcBody>();
            body.pelvis = Marker("Pelvis", new Vector3(0, 0.95f, 0)); body.torso = Marker("Torso", new Vector3(0, 1.3f, 0));
            body.shoulderL = Marker("Shoulder_L", new Vector3(-0.25f, 1.48f, 0)); body.shoulderR = Marker("Shoulder_R", new Vector3(0.25f, 1.48f, 0));
            body.head = Marker("Head", new Vector3(0, 1.72f, 0));

            brain = go.AddComponent<NpcBrain>();
            brain.forearmL = Socket(armL, "forearm_l", "antebrazo izq", new Vector3(0, -0.5f, 0), rb, 1);
            brain.forearmR = Socket(armR, "forearm_r", "antebrazo der", new Vector3(0, -0.5f, 0), rb, 1);
            Socket(legL, "ankle_l", "tobillo izq", new Vector3(0, -0.82f, 0), rb, 1);
            Socket(legR, "ankle_r", "tobillo der", new Vector3(0, -0.82f, 0), rb, 1);
            Socket(t, "shoulder_l", "asa hombro izq", new Vector3(-0.2f, 1.42f, -0.2f), rb, 1);
            Socket(t, "shoulder_r", "asa hombro der", new Vector3(0.2f, 1.42f, -0.2f), rb, 1);
            Socket(t, "belt", "cinturon", new Vector3(0, 0.95f, 0.3f), rb, 1);
            Socket(t, "belt_alt", "cinturon (atras)", new Vector3(0, 0.95f, -0.3f), rb, 1);

            pres = go.AddComponent<NpcPresentation>();
            pres.brain = brain; pres.armL = armL; pres.armR = armR; pres.legL = legL; pres.legR = legR;
            pres.bodyRenderers = new[] { torso.GetComponent<Renderer>(), belly.GetComponent<Renderer>(), aLV.GetComponent<Renderer>(), aRV.GetComponent<Renderer>(), lLV.GetComponent<Renderer>(), lRV.GetComponent<Renderer>() };
            var ind = NoCollider(LocalPrim(PrimitiveType.Sphere, "Telegraph_Indicator", t, new Vector3(0, 2.15f, 0), Vector3.one * 0.2f, Mat("Indicator", Color.yellow)));
            pres.indicator = ind.transform; pres.indicatorRenderer = ind.GetComponent<Renderer>(); ind.SetActive(false);
            return go;
        }

        static GameObject BuildPlayer(Material handM, PhysicsMaterial pm, out PlayerMotor motor, out PlayerHands hands)
        {
            var go = new GameObject("Player");
            go.transform.SetPositionAndRotation(new Vector3(0, 0, 2f), Quaternion.identity);
            go.AddComponent<Rigidbody>();
            var cap = go.AddComponent<CapsuleCollider>(); cap.sharedMaterial = pm;
            var pivot = new GameObject("CameraPivot").transform; pivot.SetParent(go.transform, false); pivot.localPosition = new Vector3(0, 1.62f, 0);
            var camGo = new GameObject("PlayerCamera"); camGo.tag = "MainCamera"; camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>(); cam.depth = 0; cam.fieldOfView = 85f; cam.nearClipPlane = 0.05f; // FOV vertical (convencion de Unity)
            camGo.AddComponent<AudioListener>();
            var hl = NoCollider(LocalPrim(PrimitiveType.Cube, "Hand_L", camGo.transform, new Vector3(-0.28f, -0.3f, 0.75f), new Vector3(0.09f, 0.07f, 0.16f), handM));
            var hr = NoCollider(LocalPrim(PrimitiveType.Cube, "Hand_R", camGo.transform, new Vector3(0.28f, -0.3f, 0.75f), new Vector3(0.09f, 0.07f, 0.16f), handM));
            go.AddComponent<NetworkObject>();
            var nt = go.AddComponent<NetworkTransform>(); nt.AuthorityMode = NetworkTransform.AuthorityModes.Server; nt.Interpolate = true;
            nt.SyncRotAngleX = false; nt.SyncRotAngleZ = false; nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;
            go.AddComponent<NetworkRigidbody>();
            go.AddComponent<PlayerControls>();
            motor = go.AddComponent<PlayerMotor>(); motor.cameraPivot = pivot;
            hands = go.AddComponent<PlayerHands>(); hands.cam = cam; hands.leftHandVisual = hl.transform; hands.rightHandVisual = hr.transform;
            var feedback = go.AddComponent<GrabFeedback>(); feedback.hands = hands; feedback.material = AttackPoseBuilder.TelegraphMaterial(); // aviso de agarre y tension (solo local)
            return go;
        }

        // ---------- red ----------
        static void Networked(GameObject go)
        {
            go.AddComponent<NetworkObject>();
            var nt = go.AddComponent<NetworkTransform>();
            nt.AuthorityMode = NetworkTransform.AuthorityModes.Server; nt.Interpolate = true;
            go.AddComponent<NetworkRigidbody>();
        }

        static int ForceNetworkIds(NetworkObject[] objs)
        {
            var validate = typeof(NetworkObject).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var hashField = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            int n = 0; var seen = new System.Collections.Generic.HashSet<uint>();
            foreach (var o in objs)
            {
                if (!o) continue;
                validate?.Invoke(o, null);
                EditorUtility.SetDirty(o);
                uint h = hashField != null ? (uint)hashField.GetValue(o) : 0u;
                if (h == 0 || !seen.Add(h)) Debug.LogError($"[LabBuilder] NetworkObject {o.name} hash invalido o duplicado: {h}");
                else n++;
                Debug.Log($"[LabBuilder] {o.name} GlobalObjectIdHash={h}");
            }
            return n;
        }

        // ---------- utilidades ----------
        static GrabSocket Socket(Transform parent, string id, string label, Vector3 localPos, Rigidbody body, int capacity)
        {
            var go = new GameObject("Socket_" + id); go.transform.SetParent(parent, false); go.transform.localPosition = localPos;
            var s = go.AddComponent<GrabSocket>(); s.socketId = id; s.label = label; s.body = body; s.capacity = capacity;
            return s;
        }

        internal static GameObject Box(string n, Transform parent, Vector3 worldPos, Vector3 size, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = n; g.transform.SetParent(parent, true);
            g.transform.position = worldPos; g.transform.localScale = size; g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }
        static GameObject LocalBox(string n, Transform parent, Vector3 localPos, Vector3 size, Material m) => LocalPrim(PrimitiveType.Cube, n, parent, localPos, size, m);
        internal static GameObject Prim(PrimitiveType p, string n, Transform parent, Vector3 worldPos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(p); g.name = n; g.transform.SetParent(parent, true);
            g.transform.position = worldPos; g.transform.localScale = scale; g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }
        static GameObject LocalPrim(PrimitiveType p, string n, Transform parent, Vector3 localPos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(p); g.name = n; g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos; g.transform.localScale = scale; g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }
        internal static GameObject NoRenderer(GameObject g) { var r = g.GetComponent<Renderer>(); if (r) UnityEngine.Object.DestroyImmediate(r); return g; }
        internal static void HideRenderer(GameObject g) { var r = g.GetComponent<Renderer>(); if (r) r.enabled = false; }

        /// Instancia un FBX del kit 3D como visual (sin colisiones: la colision la ponen primitivas simples). null si falta.
        internal static GameObject KitVisual(string id, Transform parent, Vector3 localPos, Quaternion localRot)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{Kit3DImporter.Folder}/{id}.fbx");
            if (!src) { Debug.LogWarning($"[LabBuilder] falta el FBX del kit: {id}"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            go.name = "Kit_" + id; go.transform.SetParent(parent, false); go.transform.localPosition = localPos; go.transform.localRotation = localRot;
            foreach (var c in go.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        internal static GameObject NoCollider(GameObject g) { var c = g.GetComponent<Collider>(); if (c) UnityEngine.Object.DestroyImmediate(c); return g; }

        internal static Material Mat(string name, Color c)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(DefaultShader()); AssetDatabase.CreateAsset(m, path); }
            var sh = DefaultShader(); if (m.shader != sh) m.shader = sh; // materiales creados antes de URP
            m.color = c; EditorUtility.SetDirty(m); return m;
        }
        static Shader DefaultShader()
        {
            var rp = GraphicsSettings.defaultRenderPipeline;
            if (rp && rp.defaultMaterial) return rp.defaultMaterial.shader;
            return Shader.Find("Standard");
        }
        static PhysicsMaterial PhysMat(string name, float dyn, float stat, PhysicsMaterialCombine combine)
        {
            string path = $"{MatDir}/{name}.physicMaterial";
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (!m) { m = new PhysicsMaterial(name); AssetDatabase.CreateAsset(m, path); }
            m.dynamicFriction = dyn; m.staticFriction = stat; m.frictionCombine = combine; EditorUtility.SetDirty(m); return m;
        }
    }
}
