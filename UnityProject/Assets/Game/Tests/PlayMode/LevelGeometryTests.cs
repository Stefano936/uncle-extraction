using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Coherencia fisica de cada escenario: sin apariciones dentro de solidos, sin objetos dinamicos atravesados al empezar
    /// y sin tramos de la ruta principal bloqueados por geometria fija (recorridos imposibles).
    public class LevelGeometryTests
    {
        static IEnumerable<string> Scenes() { yield return "Bar_LastRound"; yield return "PhysicsLab"; }

        static bool IsStatic(Collider c) => c.attachedRigidbody == null && !c.isTrigger;

        [UnityTest]
        public IEnumerator Layout_SpawnsRouteAndPropsAreClear([ValueSource(nameof(Scenes))] string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null; yield return new WaitForFixedUpdate();
            var L = LevelLayout.Current;
            Assert.IsNotNull(L, $"{scene} debe tener LevelLayout");
            var problems = new List<string>();

            // Apariciones de jugadores: capsula libre de solidos fijos
            foreach (var p in L.playerSpawns)
                foreach (var c in Physics.OverlapCapsule(p + Vector3.up * 0.35f, p + Vector3.up * 1.45f, 0.3f, ~0, QueryTriggerInteraction.Ignore))
                    if (IsStatic(c)) problems.Add($"aparicion {p} dentro de {c.name}");

            // Ruta del tio: cada tramo barrido con una capsula de su ancho no choca con solidos fijos
            // (la puerta y los objetos dinamicos no cuentan: se apartan; la rampa y el piso de la camioneta se suben)
            var path = L.uncleDragPath;
            for (int i = 0; i + 1 < path.Length; i++)
            {
                Vector3 a = path[i] + Vector3.up * 0.75f, b = path[i + 1] + Vector3.up * 0.75f;
                Vector3 d = b - a; float len = d.magnitude;
                foreach (var h in Physics.SphereCastAll(a, 0.33f, d / len, len, ~0, QueryTriggerInteraction.Ignore))
                    if (IsStatic(h.collider) && !h.collider.name.StartsWith("Van_Ramp") && !h.collider.name.StartsWith("Van_Floor") && h.collider.name != "Floor")
                        problems.Add($"tramo {i}->{i + 1} ({path[i]} -> {path[i + 1]}) choca con {h.collider.name}");
            }

            // Objetos dinamicos: no empiezan superpuestos con solidos fijos
            foreach (var rb in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (rb.isKinematic || rb.GetComponent<PlayerMotor>()) continue;
                foreach (var own in rb.GetComponentsInChildren<Collider>())
                {
                    if (own.isTrigger) continue;
                    var bb = own.bounds; bb.Expand(-0.04f);
                    foreach (var c in Physics.OverlapBox(bb.center, bb.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                        if (IsStatic(c) && c.name != "Floor" && Physics.ComputePenetration(own, own.transform.position, own.transform.rotation, c, c.transform.position, c.transform.rotation, out _, out float dist) && dist > 0.03f)
                            problems.Add($"{rb.name}/{own.name} atravesado con {c.name} ({dist:0.00} m)");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
