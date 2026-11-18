namespace UncleExtraction
{
    /// Unica fuente de la version del juego y del protocolo de red (sesion 17). La usan la admision (NetSession.VersionTag),
    /// los registros, el menu y BuildTools (PlayerSettings.bundleVersion y VERSION.txt).
    /// Subir Protocol cuando cambie de forma incompatible el estado replicado (variables de red), el orden o tipo de campos,
    /// los RPC o su serializacion. Historial:
    ///   1 = hasta la demo de la sesion 16 (1ca19d7; en realidad cambiaba entre demos sin subir: se corrige aca).
    ///   2 = sesion 17: parte de extraccion estructurado (RPC con texto JSON), torre de vasos con numero de ronda en el RPC.
    public static class GameVersion
    {
        public const string Number = "0.17.0";
        public const ushort Protocol = 2;
        public static string Tag => Number + "/" + Protocol;
    }
}
