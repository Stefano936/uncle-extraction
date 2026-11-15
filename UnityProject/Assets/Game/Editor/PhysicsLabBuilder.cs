using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        public static void Build()
        {
            Directory.CreateDirectory("Assets/Game/Scenes"); Directory.CreateDirectory(MatDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var floorM = Mat("Floor", new Color(0.35f, 0.3f, 0.27f));
            var wallM = Mat("Wall", new Color(0.62f, 0.52f, 0.4f));
            var woodM = Mat("Wood", new Color(0.45f, 0.27f, 0.15f));
            var metalM = Mat("Metal", new Color(0.6f, 0.62f, 0.65f));
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
            Box("Floor", env, new Vector3(0, -0.1f, 4), new Vector3(30, 0.2f, 36), floorM);
            Box("Wall_Back", env, new Vector3(0, 1.5f, 10.1f), new Vector3(14.4f, 3, 0.2f), wallM);
            Box("Wall_L", env, new Vector3(-7.1f, 1.5f, 4), new Vector3(0.2f, 3, 12.4f), wallM);
            Box("Wall_R", env, new Vector3(7.1f, 1.5f, 4), new Vector3(0.2f, 3, 12.4f), wallM);
            Box("Wall_Front_L", env, new Vector3(-3.875f, 1.5f, -2), new Vector3(6.25f, 3, 0.2f), wallM);
            Box("Wall_Front_R", env, new Vector3(3.875f, 1.5f, -2), new Vector3(6.25f, 3, 0.2f), wallM);
            Box("Door_Lintel", env, new Vector3(0, 2.6f, -2), new Vector3(1.5f, 0.8f, 0.2f), wallM);
            NoCollider(Box("Sidewalk", env, new Vector3(0, 0.01f, -5.5f), new Vector3(14, 0.02f, 6), walkM));
            Box("Low_Obstacle", env, new Vector3(3, 0.15f, 3.5f), new Vector3(1.5f, 0.3f, 0.6f), woodM);
            Box("Ramp", env, new Vector3(-5, 0.3f, 0.5f), new Vector3(2, 0.1f, 3), woodM).transform.rotation = Quaternion.Euler(-12, 0, 0);
            Box("Table_Fixed", env, new Vector3(-3.5f, 0.4f, 4), new Vector3(1.2f, 0.8f, 1.2f), woodM);
            Box("Bar_Counter", env, new Vector3(0, 0.55f, 8.6f), new Vector3(6, 1.1f, 0.8f), woodM);
            var rail = Prim(PrimitiveType.Cylinder, "Anchor_Rail", env, new Vector3(0, 0.95f, 8.12f), new Vector3(0.06f, 3f, 0.06f), metalM);
            rail.transform.rotation = Quaternion.Euler(0, 0, 90);
            NoCollider(Prim(PrimitiveType.Cylinder, "Stool_Home", env, new Vector3(0.9f, 0.37f, 7.4f), new Vector3(0.4f, 0.37f, 0.4f), metalM));

            // Silla liviana (objeto dinamico)
            var chair = Box("Chair_Light", env, new Vector3(-2.3f, 0.3f, 4), new Vector3(0.45f, 0.6f, 0.45f), woodM);
            var chairRb = chair.AddComponent<Rigidbody>(); chairRb.mass = 6f;
            Socket(chair.transform, "chair", "silla", Vector3.zero, chairRb, 1);

            // Puerta con bisagra (origen en la bisagra, abertura 1,5 m)
            var door = new GameObject("Exit_Door"); door.transform.SetParent(env); door.transform.position = new Vector3(-0.75f, 0, -2);
            var doorRb = door.AddComponent<Rigidbody>(); doorRb.mass = 25f; doorRb.angularDamping = 2f;
            var panel = Box("Door_Panel", door.transform, new Vector3(0, 0, 0), new Vector3(1.46f, 2.1f, 0.06f), woodM);
            panel.transform.localPosition = new Vector3(0.75f, 1.06f, 0);
            var hinge = door.AddComponent<HingeJoint>();
            hinge.anchor = Vector3.zero; hinge.axis = Vector3.up; hinge.useLimits = true;
            hinge.limits = new JointLimits { min = -100, max = 100 };
            hinge.useSpring = true; hinge.spring = new JointSpring { spring = 15, damper = 4, targetPosition = 0 };

            // Camioneta fija y zona de extraccion (detras de las puertas traseras)
            Box("Passenger_Van", env, new Vector3(0, 1.15f, -11.5f), new Vector3(2.2f, 2.3f, 4.5f), vanM);
            var zoneGo = new GameObject("Extraction_Zone"); zoneGo.transform.position = new Vector3(0, 1f, -8.45f);
            var zoneBox = zoneGo.AddComponent<BoxCollider>(); zoneBox.size = new Vector3(2.2f, 2f, 1.6f);
            NoCollider(Box("Zone_Marker", zoneGo.transform, new Vector3(0, 0.01f, -8.45f), new Vector3(2.2f, 0.02f, 1.6f), zoneM));
            var zone = zoneGo.AddComponent<ExtractionZone>();

            // Carrito
            var cartGo = new GameObject("Transport_Cart"); cartGo.transform.position = new Vector3(2.5f, 0, 1f);
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
            var npcGo = BuildUncle(uncleM, skinM, unclePM, out var body, out var brain, out var pres);
            brain.anchorRail = rail.GetComponent<Collider>();
            var home = new GameObject("Uncle_Home").transform; home.position = new Vector3(0, 0, 7.0f); brain.home = home;
            cart.npc = body; zone.npc = body;

            // Jugador
            var player = BuildPlayer(handM, playerPM, out var motor, out var hands);

            // Sistemas
            var sys = new GameObject("Systems");
            var round = sys.AddComponent<RoundDirector>();
            var rig = sys.AddComponent<LabForceRig>(); rig.npc = body; rig.pullTarget = zoneGo.transform;
            var hud = sys.AddComponent<LabDebugHud>();
            hud.player = motor; hud.hands = hands; hud.npc = brain; hud.npcBody = body; hud.cart = cart; hud.zone = zone; hud.round = round; hud.rig = rig;
            var auto = sys.AddComponent<LabAutopilot>();
            auto.player = motor; auto.hands = hands; auto.npc = brain; auto.body = body; auto.cart = cart; auto.zone = zone; auto.round = round;
            hud.autopilot = auto;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[LabBuilder] PhysicsLab saved: {ScenePath} sockets={UnityEngine.Object.FindObjectsByType<GrabSocket>(FindObjectsSortMode.None).Length}");
        }

        static GameObject BuildUncle(Material uncleM, Material skinM, PhysicsMaterial pm, out NpcBody body, out NpcBrain brain, out NpcPresentation pres)
        {
            var go = new GameObject("Uncle");
            go.transform.SetPositionAndRotation(new Vector3(0, 0, 7.0f), Quaternion.Euler(0, 180, 0));
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
            var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 85f; cam.nearClipPlane = 0.05f; // FOV vertical (convencion de Unity)
            camGo.AddComponent<AudioListener>();
            var hl = NoCollider(LocalPrim(PrimitiveType.Cube, "Hand_L", camGo.transform, new Vector3(-0.28f, -0.3f, 0.75f), new Vector3(0.09f, 0.07f, 0.16f), handM));
            var hr = NoCollider(LocalPrim(PrimitiveType.Cube, "Hand_R", camGo.transform, new Vector3(0.28f, -0.3f, 0.75f), new Vector3(0.09f, 0.07f, 0.16f), handM));
            motor = go.AddComponent<PlayerMotor>(); motor.cameraPivot = pivot;
            hands = go.AddComponent<PlayerHands>(); hands.cam = cam; hands.leftHandVisual = hl.transform; hands.rightHandVisual = hr.transform;
            return go;
        }

        // ---------- utilidades ----------
        static GrabSocket Socket(Transform parent, string id, string label, Vector3 localPos, Rigidbody body, int capacity)
        {
            var go = new GameObject("Socket_" + id); go.transform.SetParent(parent, false); go.transform.localPosition = localPos;
            var s = go.AddComponent<GrabSocket>(); s.socketId = id; s.label = label; s.body = body; s.capacity = capacity;
            return s;
        }

        static GameObject Box(string n, Transform parent, Vector3 worldPos, Vector3 size, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = n; g.transform.SetParent(parent, true);
            g.transform.position = worldPos; g.transform.localScale = size; g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }
        static GameObject LocalBox(string n, Transform parent, Vector3 localPos, Vector3 size, Material m) => LocalPrim(PrimitiveType.Cube, n, parent, localPos, size, m);
        static GameObject Prim(PrimitiveType p, string n, Transform parent, Vector3 worldPos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(p); g.name = n; g.transform.SetParent(parent, true);
            g.transform.position = worldPos; g.transform.localScale = scale; g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }
        static GameObject LocalPrim(PrimitiveType p, string n, Transform parent, Vector3 localPos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(p); g.name = n; g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos; g.transform.localScale = scale; g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }
        static GameObject NoCollider(GameObject g) { var c = g.GetComponent<Collider>(); if (c) UnityEngine.Object.DestroyImmediate(c); return g; }

        static Material Mat(string name, Color c)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(DefaultShader()); AssetDatabase.CreateAsset(m, path); }
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
