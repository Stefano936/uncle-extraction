using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Sesion 21 (UE-16): el entorno de la ruta se monta solo como capa visual. Seis piezas en su lugar (carrito, puerta,
    /// carroceria, dos hojas, cuadra), colgadas de lo que se mueve (carrito, hoja de la puerta, bisagras), sin colisiones y sin
    /// cambiar la masa ni la inercia de los cuerpos que visten.
    public class RouteDressingTests
    {
        [UnityTest]
        public IEnumerator Bar_MountsTheRouteAsVisualOnly()
        {
            SceneManager.LoadScene("Bar_LastRound");
            yield return null; yield return new WaitForFixedUpdate();
            Assert.IsTrue(RouteDressing.Enabled);
            Assert.AreEqual(6, RouteDressing.Applied, "carrito, puerta, carroceria, dos hojas y cuadra");

            var cart = Object.FindAnyObjectByType<TransportCart>();
            var door = Object.FindAnyObjectByType<ExitDoor>();
            var doors = Object.FindAnyObjectByType<VanDoors>();
            Assert.IsNotNull(cart.transform.Find("Route_route_cart"), "el modelo va con el carrito");
            Assert.IsNotNull(door.transform.Find("Route_route_exit_door"), "el modelo va con la hoja de la puerta");
            Assert.IsNotNull(doors.leftLeaf.Find("Route_route_van_leaf_l"));
            Assert.IsNotNull(doors.rightLeaf.Find("Route_route_van_leaf_r"));

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name.StartsWith("Route_"))
                {
                    Assert.IsEmpty(t.GetComponentsInChildren<Collider>(true), $"{t.name} no debe chocar");
                    Assert.IsNotEmpty(t.GetComponentsInChildren<Renderer>(), $"{t.name} se ve");
                }

            // los cuerpos vestidos conservan masa e inercia (solo colliders las definen)
            var rb = cart.GetComponent<Rigidbody>();
            Assert.AreEqual(30f, rb.mass, 1e-3f);
            Assert.AreEqual(25f, door.GetComponent<Rigidbody>().mass, 1e-3f);

            // el carrito visible esta donde esta la geometria funcional (piso a 0,35 m, asa a 1,0 m)
            var b = cart.transform.Find("Route_route_cart").GetComponentInChildren<Renderer>().bounds;
            Assert.AreEqual(cart.transform.position.y + 1.03f, b.max.y, 0.05f);
            Assert.AreEqual(cart.transform.position.y, b.min.y, 0.05f);
        }
    }
}
