using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Tio articulado (especificacion 04: 11 cuerpos) construido sobre el rig del kit (uncle_prototype.fbx), girado 180
    /// grados para mirar hacia +z local como el tio central. Posiciones de reposo medidas en Unity (Kit3DImporter.ReportUncleRig):
    /// pelvis 0,85 m, pecho 1,00, cabeza 1,40, hombros +-0,37/1,39, codos +-0,61/1,13, caderas +-0,17/0,85, rodillas +-0,18/0,47.
    /// Masa total 110 kg repartida (no 110 por parte). La raiz del GameObject queda a nivel del piso como en el tio central.
    public static class ArticulatedUncleBuilder
    {
        struct Seg { public string name, parent; public Vector3 a, b; public float r, mass, spring; public Vector3 limX; public float limY, limZ; }

        // Coordenadas locales del tio (frente +z). Los nombres son los del rig del kit; tras el giro de 180 grados, ".L" queda en +x.
        static readonly Seg[] Segs =
        {
            new Seg { name = "chest",       parent = "pelvis",      a = new Vector3(0, 1.00f, 0),      b = new Vector3(0, 1.40f, 0),        r = 0.21f, mass = 30f,  spring = 3000f, limX = new Vector3(-20, 30, 0), limY = 20, limZ = 20 },
            new Seg { name = "head",        parent = "chest",       a = new Vector3(0, 1.40f, 0),      b = new Vector3(0, 1.90f, 0),        r = 0.15f, mass = 7f,   spring = 600f,  limX = new Vector3(-30, 30, 0), limY = 40, limZ = 20 },
            new Seg { name = "upper_arm.L", parent = "chest",       a = new Vector3(0.37f, 1.39f, 0),  b = new Vector3(0.61f, 1.13f, 0),    r = 0.07f, mass = 4f,   spring = 400f,  limX = new Vector3(-80, 100, 0), limY = 60, limZ = 80 },
            new Seg { name = "forearm.L",   parent = "upper_arm.L", a = new Vector3(0.61f, 1.13f, 0),  b = new Vector3(0.78f, 0.88f, 0),    r = 0.06f, mass = 3f,   spring = 200f,  limX = new Vector3(-120, 120, 0), limY = 10, limZ = 10 },
            new Seg { name = "upper_arm.R", parent = "chest",       a = new Vector3(-0.37f, 1.39f, 0), b = new Vector3(-0.61f, 1.13f, 0),   r = 0.07f, mass = 4f,   spring = 400f,  limX = new Vector3(-80, 100, 0), limY = 60, limZ = 80 },
            new Seg { name = "forearm.R",   parent = "upper_arm.R", a = new Vector3(-0.61f, 1.13f, 0), b = new Vector3(-0.78f, 0.88f, 0),   r = 0.06f, mass = 3f,   spring = 200f,  limX = new Vector3(-120, 120, 0), limY = 10, limZ = 10 },
            new Seg { name = "thigh.L",     parent = "pelvis",      a = new Vector3(0.17f, 0.85f, 0),  b = new Vector3(0.18f, 0.47f, 0),    r = 0.10f, mass = 11f,  spring = 4000f, limX = new Vector3(-90, 40, 0), limY = 20, limZ = 30 },
            new Seg { name = "calf.L",      parent = "thigh.L",     a = new Vector3(0.18f, 0.47f, 0),  b = new Vector3(0.18f, 0.06f, 0),    r = 0.08f, mass = 8.5f, spring = 4000f, limX = new Vector3(-120, 120, 0), limY = 10, limZ = 10 },
            new Seg { name = "thigh.R",     parent = "pelvis",      a = new Vector3(-0.17f, 0.85f, 0), b = new Vector3(-0.18f, 0.47f, 0),   r = 0.10f, mass = 11f,  spring = 4000f, limX = new Vector3(-90, 40, 0), limY = 20, limZ = 30 },
            new Seg { name = "calf.R",      parent = "thigh.R",     a = new Vector3(-0.18f, 0.47f, 0), b = new Vector3(-0.18f, 0.06f, 0),   r = 0.08f, mass = 8.5f, spring = 4000f, limX = new Vector3(-120, 120, 0), limY = 10, limZ = 10 },
        };
        const float PelvisMass = 20f; // 20 + 30 + 7 + 2*4 + 2*3 + 2*11 + 2*8,5 = 110 kg

        public static GameObject Build(Vector3 at, Material uncleM, Material skinM, PhysicsMaterial pm, out NpcBody body, out NpcBrain brain, out NpcPresentation pres)
        {
            var go = new GameObject("Uncle");
            go.transform.SetPositionAndRotation(at, Quaternion.identity); // se construye mirando a +z y se gira al final
            var rb = go.AddComponent<Rigidbody>(); rb.mass = PelvisMass;
            var pelvisCol = go.AddComponent<BoxCollider>(); pelvisCol.center = new Vector3(0, 0.92f, 0); pelvisCol.size = new Vector3(0.42f, 0.30f, 0.30f); pelvisCol.sharedMaterial = pm;
            var t = go.transform;
            var map = new Dictionary<string, Transform> { { "pelvis", t } };
            var partList = new List<RagdollPart>();

            foreach (var s in Segs)
            {
                var pgo = new GameObject("Part_" + s.name); pgo.transform.SetParent(map[s.parent], false);
                Vector3 dir = (s.b - s.a); float len = dir.magnitude;
                pgo.transform.SetPositionAndRotation(t.TransformPoint(s.a), t.rotation * Quaternion.FromToRotation(Vector3.up, dir / len));
                var prb = pgo.AddComponent<Rigidbody>(); prb.mass = s.mass; prb.interpolation = RigidbodyInterpolation.Interpolate;
                prb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                if (s.name == "head") { var sc = pgo.AddComponent<SphereCollider>(); sc.center = new Vector3(0, 0.26f, 0); sc.radius = s.r; sc.sharedMaterial = pm; }
                else if (s.name == "chest") { var bc = pgo.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 0.2f, 0); bc.size = new Vector3(0.52f, 0.40f, 0.32f); bc.sharedMaterial = pm; }
                else { var cc = pgo.AddComponent<CapsuleCollider>(); cc.direction = 1; cc.center = new Vector3(0, len * 0.5f, 0); cc.height = len + s.r * 2f * 0.6f; cc.radius = s.r; cc.sharedMaterial = pm; }

                var j = pgo.AddComponent<ConfigurableJoint>();
                j.connectedBody = map[s.parent].GetComponent<Rigidbody>();
                j.xMotion = j.yMotion = j.zMotion = ConfigurableJointMotion.Locked;
                j.angularXMotion = j.angularYMotion = j.angularZMotion = ConfigurableJointMotion.Limited;
                j.lowAngularXLimit = new SoftJointLimit { limit = s.limX.x }; j.highAngularXLimit = new SoftJointLimit { limit = s.limX.y };
                j.angularYLimit = new SoftJointLimit { limit = s.limY }; j.angularZLimit = new SoftJointLimit { limit = s.limZ };
                j.projectionMode = JointProjectionMode.PositionAndRotation; j.projectionDistance = 0.03f; j.projectionAngle = 5f; // sin estiramientos
                j.enableCollision = false; j.enablePreprocessing = false;
                j.rotationDriveMode = RotationDriveMode.Slerp; j.targetRotation = Quaternion.identity; // reposo = pose del rig

                var part = pgo.AddComponent<RagdollPart>(); part.role = s.name; part.joint = j; part.uprightSpring = s.spring; part.limpSpring = 50f; part.damper = s.spring * 0.08f;
                partList.Add(part);
                map[s.name] = pgo.transform;
            }

            body = go.AddComponent<NpcBody>(); body.parts = partList.ToArray();
            Transform Marker(string n, Transform parent, Vector3 localUncle) { var m = new GameObject(n).transform; m.SetParent(parent, true); m.position = t.TransformPoint(localUncle); return m; }
            body.pelvis = Marker("Pelvis", t, new Vector3(0, 0.95f, 0));
            body.torso = Marker("Torso", map["chest"], new Vector3(0, 1.3f, 0));
            body.shoulderL = Marker("Shoulder_L", map["chest"], new Vector3(-0.25f, 1.45f, 0));
            body.shoulderR = Marker("Shoulder_R", map["chest"], new Vector3(0.25f, 1.45f, 0));
            body.head = Marker("Head", map["head"], new Vector3(0, 1.7f, 0));

            // Sockets: mismos ids que el tio central (lado "l" = -x), sobre la parte que corresponde (marcadores Grab_* del kit)
            GrabSocket S(string id, string label, Transform part, Vector3 localUncle)
            {
                var sgo = new GameObject("Socket_" + id); sgo.transform.SetParent(part, true); sgo.transform.position = t.TransformPoint(localUncle);
                var sk = sgo.AddComponent<GrabSocket>(); sk.socketId = id; sk.label = label; sk.body = part.GetComponent<Rigidbody>(); sk.capacity = 1; return sk;
            }
            brain = go.AddComponent<NpcBrain>();
            brain.forearmL = S("forearm_l", "antebrazo izq", map["forearm.R"], new Vector3(-0.71f, 1.00f, 0.03f));
            brain.forearmR = S("forearm_r", "antebrazo der", map["forearm.L"], new Vector3(0.71f, 1.00f, 0.03f));
            S("ankle_l", "tobillo izq", map["calf.R"], new Vector3(-0.18f, 0.17f, 0));
            S("ankle_r", "tobillo der", map["calf.L"], new Vector3(0.18f, 0.17f, 0));
            S("shoulder_l", "asa hombro izq", map["chest"], new Vector3(-0.30f, 1.39f, -0.10f));
            S("shoulder_r", "asa hombro der", map["chest"], new Vector3(0.30f, 1.39f, -0.10f));
            S("belt", "cinturon", t, new Vector3(0, 0.91f, 0.17f));
            S("belt_alt", "cinturon (atras)", t, new Vector3(0, 0.91f, -0.19f));

            // Red: la raiz la replica Networked() (NetworkObject + NetworkTransform + NetworkRigidbody); cada parte, su NetworkTransform
            foreach (var p in partList)
            {
                var nt = p.gameObject.AddComponent<NetworkTransform>();
                nt.AuthorityMode = NetworkTransform.AuthorityModes.Server; nt.Interpolate = true; nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;
            }

            // Visual: malla con rig del kit girada 180 grados; si falta, capsulas de color de respaldo
            pres = go.AddComponent<NpcPresentation>(); pres.brain = brain;
            var kit = PhysicsLabBuilder.KitVisual("uncle_prototype", t, Vector3.zero, Quaternion.Euler(0, 180, 0));
            RagdollVisual visRef = null;
            if (kit)
            {
                var vis = go.AddComponent<RagdollVisual>(); visRef = vis;
                var bones = new Dictionary<string, Transform>();
                foreach (var b in kit.GetComponentsInChildren<Transform>()) bones[b.name] = b;
                if (bones.TryGetValue("pelvis", out var pb)) vis.Bind(pb, t);
                // El lado de cada hueso se elige por cercania (no por sufijo .L/.R): tras el giro y la conversion de ejes del
                // FBX, el hueso «.L» puede quedar en cualquiera de los dos lados; se liga al segmento fisico que tiene al lado.
                foreach (var s in Segs)
                {
                    string baseName = s.name.Split('.')[0]; Transform best = null; float bd = 0.25f;
                    foreach (var kv in bones)
                        if (kv.Key.StartsWith(baseName) && !kv.Key.Contains("_end"))
                        { float d = Vector3.Distance(kv.Value.position, map[s.name].position); if (d < bd) { bd = d; best = kv.Value; } }
                    if (best) vis.Bind(best, map[s.name]); else Debug.LogWarning($"[ArticulatedUncle] sin hueso del kit para {s.name}");
                }
                var anim = kit.GetComponent<Animator>(); if (anim) Object.DestroyImmediate(anim); // la pose la da la fisica
                Finishing.Uncle(kit, map["head"]); // materiales (tela con trama, piel, cuero) y gorro de fiesta
                pres.bodyRenderers = kit.GetComponentsInChildren<Renderer>().Where(r => r is SkinnedMeshRenderer).ToArray();
            }
            else
            {
                var rs = new List<Renderer>();
                foreach (var s in Segs) { var v = PhysicsLabBuilder.NoCollider(GameObject.CreatePrimitive(PrimitiveType.Capsule)); v.name = s.name + "_Visual"; v.transform.SetParent(map[s.name], false); v.transform.localPosition = new Vector3(0, (s.b - s.a).magnitude * 0.5f, 0); v.transform.localScale = new Vector3(s.r * 2, (s.b - s.a).magnitude * 0.5f, s.r * 2); v.GetComponent<Renderer>().sharedMaterial = uncleM; rs.Add(v.GetComponent<Renderer>()); }
                pres.bodyRenderers = rs.ToArray();
            }
            var ind = PhysicsLabBuilder.NoCollider(GameObject.CreatePrimitive(PrimitiveType.Sphere)); ind.name = "Telegraph_Indicator";
            ind.transform.SetParent(map["head"], false); ind.transform.position = t.TransformPoint(new Vector3(0, 2.42f, 0)); // sobre el gorro ind.transform.localScale = Vector3.one * 0.2f;
            ind.GetComponent<Renderer>().sharedMaterial = PhysicsLabBuilder.Mat("Indicator", Color.yellow);
            pres.indicator = ind.transform; pres.indicatorRenderer = ind.GetComponent<Renderer>(); ind.SetActive(false);

            // Poses de preparacion y golpe (clips del kit muestreados y poses autorizadas), todavia mirando a +z
            AttackPoseBuilder.Build(go, body, brain, visRef, kit, t);
            go.transform.rotation = Quaternion.Euler(0, 180, 0); // mirando a -z en el mundo, como el tio central
            return go;
        }
    }
}
