using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    /// Quien decide (especificacion 07): el anfitrion simula reglas, fisica, agarres, ataques y ronda.
    /// Sin red escuchando (pruebas de editor sin sesion) la instancia local es la autoridad.
    public static class NetAuth
    {
        public static bool Listening => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        public static bool IsServer => !Listening || NetworkManager.Singleton.IsServer;
        public static bool IsClientOnly => Listening && !NetworkManager.Singleton.IsServer;
        /// Reloj comun para sellar fines de estados replicados (preparacion de ataques, caidas).
        public static float ServerNow => Listening ? NetworkManager.Singleton.ServerTime.TimeAsFloat : Time.time;
        public static int ConnectedPlayers => Listening && NetworkManager.Singleton.IsServer ? NetworkManager.Singleton.ConnectedClientsIds.Count : 1;
    }
}
