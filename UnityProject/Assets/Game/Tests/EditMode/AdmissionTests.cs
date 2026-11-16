using NUnit.Framework;

namespace UncleExtraction.Tests
{
    /// Regla de admision del anfitrion (especificacion 07; UI_SPEC 7): errores diferenciados, sin incorporacion en curso.
    public class AdmissionTests
    {
        const string V = "0.1/1";
        [Test] public void InLobby_WithRoom_SameVersion_IsAdmitted() => Assert.AreEqual("", NetSession.Decide(2, 4, V, V, true));
        [Test] public void Full_IsRejected_WithFullReason() => Assert.AreEqual("error.full", NetSession.Decide(4, 4, V, V, true));
        [Test] public void OtherVersion_IsRejected_WithVersionReason() => Assert.AreEqual("error.version", NetSession.Decide(1, 4, "0.0/1", V, true));
        [Test] public void RoundStarted_IsRejected_WithAlreadyStartedReason() => Assert.AreEqual("error.already_started", NetSession.Decide(1, 4, V, V, false));
        [Test] public void Reasons_AreLocalized()
        {
            foreach (var k in new[] { "error.full", "error.version", "error.already_started", "error.host_left" })
                Assert.IsTrue(Loc.Has(k), $"falta la clave {k} en el catalogo");
        }
    }
}
