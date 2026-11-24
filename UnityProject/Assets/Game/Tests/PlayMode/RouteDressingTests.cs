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
            Assert.AreEqual(11, RouteDressing.BarApplied, "barra, 3 banquetas, 4 mesas, tarima, silla y vaso");

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

            // muebles: la barra visible cubre su colision; la silla y el vaso van con su cuerpo fisico y sin deformarse
            var counter = GameObject.Find("Bar_Counter").GetComponent<BoxCollider>().bounds;
            var counterVis = GameObject.Find("Route_bar_counter").GetComponentInChildren<Renderer>().bounds;
            Assert.AreEqual(counter.max.y, 1.08f, 0.01f);
            Assert.AreEqual(counter.center.x, counterVis.center.x, 0.15f, "barra centrada sobre su colision");
            Assert.LessOrEqual(counterVis.min.x, counter.min.x + 0.02f); Assert.GreaterOrEqual(counterVis.max.x, counter.max.x - 0.02f);
            var chair = GameObject.Find("Chair_Light").transform; var chairVis = chair.Find("Route_bar_chair");
            Assert.IsNotNull(chairVis);
            Assert.AreEqual(0.6f, chairVis.GetComponentInChildren<Renderer>().bounds.size.y, 0.05f, "silla de 0,60 m de alto (la caja fisica)");
            var pint = GameObject.Find("Decoy_Glass").transform.Find("Route_bar_pint");
            Assert.IsNotNull(pint); Assert.AreEqual(0.16f, pint.GetComponentInChildren<Renderer>().bounds.size.y, 0.02f, "vaso de 16 cm");

            // el carrito visible esta donde esta la geometria funcional (piso a 0,35 m, asa a 1,0 m)
            var b = cart.transform.Find("Route_route_cart").GetComponentInChildren<Renderer>().bounds;
            Assert.AreEqual(cart.transform.position.y + 1.03f, b.max.y, 0.05f);
            Assert.AreEqual(cart.transform.position.y, b.min.y, 0.05f);
        }
    }
}
