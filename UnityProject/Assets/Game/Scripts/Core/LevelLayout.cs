using UnityEngine;

namespace UncleExtraction
{
    /// Datos propios de cada escenario (los escribe el constructor de la escena; nada de coordenadas fijas en los sistemas):
    /// apariciones, puerta de salida, camioneta, rutas de prueba del autopiloto y puntos de interaccion.
    /// Convencion de ambos escenarios: el bar queda hacia +z, la puerta de salida en el muro frontal (z = doorZ) y la
    /// vereda y la camioneta hacia -z (abertura trasera en z = vanRearZ, mampara de cabina en z = vanCabZ).
    public class LevelLayout : MonoBehaviour
    {
        public static LevelLayout Current { get; private set; }

        public string displayName = "Escenario";
        [Header("Apariciones")]
        public Vector3[] playerSpawns = { new Vector3(0, 0, 2f) };
        public float playerSpawnYaw = 0f;
        [Header("Salida y camioneta")]
        public float doorX = 0f, doorZ = -2f;
        public float vanAxisX = 0f, vanRearZ = -9.25f, vanCabZ = -12.4f;
        [Header("Rutas del autopiloto (pruebas)")]
        public Vector3[] uncleDragPath;      // ruta del TIO, z decreciente hasta adentro de la camioneta
        public Vector3 dragFinalStand;       // donde termina el jugador que arrastra (junto a la mampara)
        public Vector3[] cartPullPath;       // ruta del JUGADOR tirando del carrito
        public int cartPathDoorIndex = 3;    // primer punto de cartPullPath que queda pasada la puerta
        public Vector3 cartOpenSpot;         // lugar despejado para cargar al tio en el carrito
        public Vector3 raceStandL, raceStandR; // carrera por el cinturon (dos jugadores)

        void Awake() { Current = this; }
        void OnDestroy() { if (Current == this) Current = null; }

        public Vector3 Spawn(ulong index) => playerSpawns.Length == 0 ? Vector3.zero : playerSpawns[(int)(index % (ulong)playerSpawns.Length)];
        /// Punto frente a la rampa, sobre el eje de la camioneta, donde se pararia alguien para cerrar.
        public Vector3 CloseStand(float side) => new Vector3(vanAxisX + side * 0.5f, 0f, vanRearZ + 2.0f);
    }
}
