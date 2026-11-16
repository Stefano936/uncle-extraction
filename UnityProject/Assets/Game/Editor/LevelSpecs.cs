using System;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Especificacion de un escenario para PhysicsLabBuilder.BuildLevel. Convencion comun: bar hacia +z, puerta en el
    /// muro frontal (z = doorZ), vereda y camioneta hacia -z (la camioneta y su volumen de carga los arma el constructor).
    public class LevelSpec
    {
        public string name, scenePath;
        public float halfX = 7f, backZ = 10f, doorX = 0f, doorZ = -2f;
        public bool kitVisuals;
        public bool articulatedUncle;                // variante de ragdoll reducido (11 cuerpos) en lugar del cuerpo central
        public Vector3 home, chairPos, glassPos, cartPos; public float cartYaw;
        public Vector3 overviewPos, overviewEuler;
        public Func<Transform, GameObject> furnish;  // mobiliario fijo; devuelve la baranda de anclaje
        public Action<LevelLayout> layout;           // apariciones y rutas propias del escenario
    }

    public static class LevelSpecs
    {
        static GameObject Box(string n, Transform p, Vector3 pos, Vector3 size, Material m) => PhysicsLabBuilder.Box(n, p, pos, size, m);

        static GameObject Rail(Transform env, Vector3 center, float length)
        {
            var rail = PhysicsLabBuilder.Prim(PrimitiveType.Cylinder, "Anchor_Rail", env, center, new Vector3(0.06f, length * 0.5f, 0.06f), PhysicsLabBuilder.metalM);
            rail.transform.rotation = Quaternion.Euler(0, 0, 90);
            return rail;
        }

        /// PhysicsLab: laboratorio de diagnostico (sin cambios de distribucion respecto de las versiones anteriores).
        public static LevelSpec Lab() => new LevelSpec
        {
            name = "PhysicsLab", scenePath = PhysicsLabBuilder.ScenePath, halfX = 7f, backZ = 10f, kitVisuals = false,
            home = new Vector3(0, 0, 7f), chairPos = new Vector3(-2.3f, 0.3f, 4), glassPos = new Vector3(-3.5f, 0.9f, 4f), cartPos = new Vector3(2.5f, 0, 1f),
            overviewPos = new Vector3(6f, 6f, -9f), overviewEuler = new Vector3(28f, -35f, 0f),
            furnish = env =>
            {
                Box("Low_Obstacle", env, new Vector3(3, 0.15f, 3.5f), new Vector3(1.5f, 0.3f, 0.6f), PhysicsLabBuilder.woodM);
                Box("Ramp", env, new Vector3(-5, 0.3f, 0.5f), new Vector3(2, 0.1f, 3), PhysicsLabBuilder.woodM).transform.rotation = Quaternion.Euler(-12, 0, 0);
                Box("Table_Fixed", env, new Vector3(-3.5f, 0.4f, 4), new Vector3(1.2f, 0.8f, 1.2f), PhysicsLabBuilder.woodM);
                Box("Bar_Counter", env, new Vector3(0, 0.55f, 8.6f), new Vector3(6, 1.1f, 0.8f), PhysicsLabBuilder.woodM);
                PhysicsLabBuilder.NoCollider(PhysicsLabBuilder.Prim(PrimitiveType.Cylinder, "Stool_Home", env, new Vector3(0.9f, 0.37f, 7.4f), new Vector3(0.4f, 0.37f, 0.4f), PhysicsLabBuilder.metalM));
                return Rail(env, new Vector3(0, 0.95f, 8.12f), 6f);
            },
            layout = L =>
            {
                L.displayName = "Laboratorio";
                L.playerSpawns = new[] { new Vector3(0, 0, 2f), new Vector3(1.1f, 0, 2f), new Vector3(-1.1f, 0, 2f), new Vector3(2.2f, 0, 2f) };
                L.uncleDragPath = new[] { new Vector3(0, 0, 6.0f), new Vector3(0, 0, 3.0f), new Vector3(0, 0, 1.0f), new Vector3(0, 0, -1.0f), new Vector3(0, 0, -3.5f), new Vector3(0, 0, -6.5f), new Vector3(0, 0, -8.3f), new Vector3(0, 0, -10.6f) };
                L.dragFinalStand = new Vector3(0f, 0, -11.95f);
                L.cartPullPath = new[] { new Vector3(0, 0, 2.8f), new Vector3(0, 0, 1.2f), new Vector3(0, 0, -0.4f), new Vector3(0, 0, -3.5f), new Vector3(0, 0, -6.5f), new Vector3(0, 0, -8.2f), new Vector3(0, 0, -11.95f) };
                L.cartPathDoorIndex = 3; L.cartOpenSpot = new Vector3(1.1f, 0f, 2.6f); // a 1,1 m de la ruta del tio: al alcance de la correa
                L.raceStandL = new Vector3(-0.35f, 0, 5.85f); L.raceStandR = new Vector3(0.35f, 0, 5.85f);
            },
        };

        /// PhysicsLab_Articulated: el mismo laboratorio con el tio articulado (ragdoll reducido). Variante de prueba; el
        /// cuerpo central queda como respaldo en PhysicsLab y Bar_LastRound hasta verificar estabilidad e interacciones.
        public static LevelSpec LabArticulated()
        {
            var s = Lab(); s.name = "PhysicsLab_Articulated"; s.scenePath = PhysicsLabBuilder.ArticulatedLabScenePath; s.articulatedUncle = true;
            return s;
        }

        /// Bar_LastRound (GDD 13): salon de 18 x 12 m, barra al fondo a la derecha con baranda (anclaje) y banco del tio,
        /// mesas y banquetas fuera del corredor principal, rincon de karaoke, salida de 1,5 m, vereda de 14 m y camioneta.
        /// Visuales: FBX del kit (barra, mesas, banquetas, puerta); colisiones: cajas simples del tamano medido del kit.
        public static LevelSpec Bar() => new LevelSpec
        {
            name = "Bar_LastRound", scenePath = PhysicsLabBuilder.BarScenePath, halfX = 9f, backZ = 10f, kitVisuals = true,
            home = new Vector3(4.5f, 0, 7.2f), chairPos = new Vector3(-2.6f, 0.3f, 5.6f), glassPos = new Vector3(-4.2f, 0.88f, 6.2f),
            cartPos = new Vector3(5.6f, 0, 2.4f), cartYaw = 0f, // en el salon, lejos de esquinas y paredes (en la esquina quedaba encajado)
            overviewPos = new Vector3(9f, 7f, -10f), overviewEuler = new Vector3(30f, -32f, 0f),
            furnish = env =>
            {
                var wood = PhysicsLabBuilder.woodM;
                // Barra (kit bar_counter: 4,12 x 1,08 x 0,90 m) al fondo a la derecha; colision del mismo tamano
                var counter = Box("Bar_Counter", env, new Vector3(4.5f, 0.54f, 8.85f), new Vector3(4.12f, 1.08f, 0.9f), wood);
                if (PhysicsLabBuilder.KitVisual("bar_counter", env, new Vector3(4.5f, 0f, 8.85f), Quaternion.identity)) PhysicsLabBuilder.HideRenderer(counter);
                // Banquetas del kit (0,54 x 0,83 m): obstaculos fijos a los costados del banco del tio, no en su camino
                foreach (var x in new[] { 2.8f, 6.3f })
                {
                    var st = Box("Stool", env, new Vector3(x, 0.41f, 7.75f), new Vector3(0.42f, 0.83f, 0.42f), wood);
                    if (PhysicsLabBuilder.KitVisual("stool", env, new Vector3(x, 0f, 7.75f), Quaternion.identity)) PhysicsLabBuilder.HideRenderer(st);
                }
                PhysicsLabBuilder.NoCollider(PhysicsLabBuilder.Prim(PrimitiveType.Cylinder, "Stool_Home", env, new Vector3(4.5f, 0.37f, 7.75f), new Vector3(0.4f, 0.37f, 0.4f), PhysicsLabBuilder.metalM));
                // Mesas del kit (1,40 x 0,82 x 0,90 m): salon izquierdo y una junto a la pared derecha, lejos del corredor
                foreach (var p in new[] { new Vector3(-4.2f, 0, 6.2f), new Vector3(-4.5f, 0, 2.6f), new Vector3(-6.8f, 0, 4.4f), new Vector3(7.6f, 0, 4.2f) })
                {
                    var t = Box("Table", env, p + new Vector3(0, 0.41f, 0), new Vector3(1.4f, 0.82f, 0.9f), wood);
                    if (PhysicsLabBuilder.KitVisual("table", env, p, Quaternion.identity)) PhysicsLabBuilder.HideRenderer(t);
                }
                // Rincon de karaoke: tarima baja y pie de microfono decorativo
                Box("Karaoke_Stage", env, new Vector3(-7.4f, 0.12f, 8.6f), new Vector3(3.0f, 0.24f, 2.4f), PhysicsLabBuilder.Mat("Stage", new Color(0.3f, 0.12f, 0.35f)));
                PhysicsLabBuilder.NoCollider(PhysicsLabBuilder.Prim(PrimitiveType.Cylinder, "Mic_Stand", env, new Vector3(-7.2f, 0.95f, 8.4f), new Vector3(0.04f, 0.7f, 0.04f), PhysicsLabBuilder.metalM));
                // Baranda de anclaje a lo largo del frente de la barra (a ~1 m del banco: alcanzable por sus manos)
                return Rail(env, new Vector3(4.5f, 0.95f, 8.36f), 4.0f);
            },
            layout = L =>
            {
                L.displayName = "The Last Round";
                L.playerSpawns = new[] { new Vector3(1.0f, 0, 3.0f), new Vector3(2.1f, 0, 3.0f), new Vector3(-0.1f, 0, 3.0f), new Vector3(3.2f, 0, 3.0f) };
                L.playerSpawnYaw = 20f; // mirando hacia la barra; la salida queda a la espalda (se ve al girar)
                L.uncleDragPath = new[] { new Vector3(4.4f, 0, 6.4f), new Vector3(3.2f, 0, 4.6f), new Vector3(1.6f, 0, 2.8f), new Vector3(0.3f, 0, 1.0f), new Vector3(0, 0, -1.0f), new Vector3(0, 0, -3.5f), new Vector3(0, 0, -6.5f), new Vector3(0, 0, -8.3f), new Vector3(0, 0, -10.6f) };
                L.dragFinalStand = new Vector3(0f, 0, -11.95f);
                L.cartPullPath = new[] { new Vector3(3.0f, 0, 2.8f), new Vector3(0, 0, 2.8f), new Vector3(0, 0, 1.2f), new Vector3(0, 0, -0.4f), new Vector3(0, 0, -3.5f), new Vector3(0, 0, -6.5f), new Vector3(0, 0, -8.2f), new Vector3(0, 0, -11.95f) };
                L.cartPathDoorIndex = 4; L.cartOpenSpot = new Vector3(2.2f, 0f, 2.6f);
                L.raceStandL = new Vector3(4.15f, 0, 6.05f); L.raceStandR = new Vector3(4.85f, 0, 6.05f);
            },
        };
    }
}
