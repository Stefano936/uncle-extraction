using System;
using NUnit.Framework;
using UnityEngine;

namespace UncleExtraction.Tests
{
    /// Proveedor de internet (Relay) sin red: lo que se puede comprobar sin cuenta. La conexion real por Relay se verifica
    /// con net_smoke.ps1 -Scenario relay (BLOCKED mientras el proyecto no este vinculado) y a mano entre dos equipos.
    public class RelayProviderTests
    {
        [TestCase("abc123", "ABC123")]
        [TestCase("  AB C1-23 ", "ABC123")]
        [TestCase("q7xk2m", "Q7XK2M")]
        public void Code_IsNormalized(string typed, string expected) => Assert.AreEqual(expected, RelayConnector.NormalizeCode(typed));

        [TestCase("")]
        [TestCase(null)]
        [TestCase("ABC")]            // corto
        [TestCase("ABCDEFGHIJKLM")]  // largo
        [TestCase("ABC<b>1")]        // simbolos
        [TestCase("ÑANDÚ1")]         // fuera de A-Z0-9
        public void BadCode_IsRejectedWithoutNetwork(string typed) => Assert.AreEqual("", RelayConnector.NormalizeCode(typed));

        [Test]
        public void Errors_MapToLocalizedReasons()
        {
            Assert.AreEqual("error.timeout", RelayConnector.MapError(new TimeoutException()));
            Assert.AreEqual("error.service", RelayConnector.MapError(new InvalidOperationException("x")));
        }

        [Test]
        public void WithoutLinkedProject_HostAndJoinFailWithClearReason_NoFakeCredentials()
        {
            if (RelayConnector.ProjectLinked) Assert.Ignore("el proyecto ya esta vinculado: este caso no aplica");
            var h = RelayConnector.Host(null, 3);
            Assert.IsTrue(h.IsCompleted, "sin proyecto no debe intentar la red");
            Assert.IsFalse(h.Result.ok); Assert.AreEqual("error.online_unlinked", h.Result.error);
            var j = RelayConnector.Join(null, "ABC123");
            Assert.IsTrue(j.IsCompleted); Assert.AreEqual("error.online_unlinked", j.Result.error);
            Assert.AreEqual("error.invalid", RelayConnector.Join(null, "??").Result.error, "codigo mal formado: motivo de codigo, no de servicio");
        }

        [Test]
        public void OnlineTexts_ExistInBothLanguages()
        {
            foreach (var k in new[] { "menu.host_online", "menu.lan", "relay.hosting", "error.online_unlinked", "lobby.code", "button.copy_code", "lobby.copied", "button.join", "error.invalid", "error.service", "error.timeout", "lobby.creating" })
            {
                foreach (var lang in new[] { "en", "es_UY" })
                {
                    Loc.SetLanguage(lang);
                    Assert.IsTrue(Loc.Has(k), $"{k} falta");
                    Assert.AreNotEqual(k, Loc.T(k, ("code", "ABC123")), $"{k} sin texto en {lang}");
                }
            }
            Loc.SetLanguage("es_UY");
        }
    }
}
