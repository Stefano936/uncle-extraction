using System;

namespace UncleExtraction
{
    /// Argumentos de diagnostico. La conexion por IP o codigo se lee aparte.
    internal static class QaArgs
    {
#if UE_PLAYER_BUILD
        internal static readonly string[] All = Array.Empty<string>();
#else
        internal static readonly string[] All = Environment.GetCommandLineArgs();
#endif
        internal static bool Has(string flag) => Array.IndexOf(All, flag) >= 0;
    }
}
