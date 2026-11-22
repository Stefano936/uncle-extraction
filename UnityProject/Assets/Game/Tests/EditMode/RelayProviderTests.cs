using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
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
            var h = RelayConnector.Host(3);
            Assert.IsTrue(h.IsCompleted, "sin proyecto no debe intentar la red");
            Assert.IsFalse(h.Result.ok); Assert.AreEqual("error.online_unlinked", h.Result.error);
            var j = RelayConnector.Join("ABC123");
            Assert.IsTrue(j.IsCompleted); Assert.AreEqual("error.online_unlinked", j.Result.error);
            Assert.AreEqual("error.invalid", RelayConnector.Join("??").Result.error, "codigo mal formado: motivo de codigo, no de servicio");
        }

        [TestCase(NetMode.Host)]
        [TestCase(NetMode.Client)]
        public void LateResponse_IsIgnoredWhenRetriedInSameMode(NetMode mode)
        {
            var go = new GameObject("Relay attempt");
            var oldMode = NetSession.Mode;
            var oldOnline = NetSession.Online;
            var type = typeof(NetSession);
            var instance = type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            var generation = type.GetField("sessionAttempt", BindingFlags.Static | BindingFlags.NonPublic);
            var wait = type.GetMethod("WaitForRelay", BindingFlags.Instance | BindingFlags.NonPublic);
            var oldInstance = instance.GetValue(null);
            var oldGeneration = generation.GetValue(null);
            try
            {
                var session = go.AddComponent<NetSession>();
                instance.SetValue(null, session);
                NetSession.Mode = mode;
                NetSession.Online = true;
                var stale = new TaskCompletionSource<RelayConnector.Result>();
                var active = new TaskCompletionSource<RelayConnector.Result>();
                string accepted = "";
                Action<RelayConnector.Result> accept = result => accepted += result.code;
                IEnumerator Begin(Task<RelayConnector.Result> task, int attempt) =>
                    (IEnumerator)wait.Invoke(session, new object[] { task, attempt, mode, accept });

                generation.SetValue(null, 1);
                var first = Begin(stale.Task, 1);
                Assert.IsTrue(first.MoveNext());
                generation.SetValue(null, 2);
                var retry = Begin(active.Task, 2);
                Assert.IsTrue(retry.MoveNext());

                stale.SetResult(new RelayConnector.Result { ok = true, code = "STALE1" });
                Assert.IsFalse(first.MoveNext());
                Assert.AreEqual("", accepted);

                active.SetResult(new RelayConnector.Result { ok = true, code = "ACTIVE" });
                Assert.IsFalse(retry.MoveNext());
                Assert.AreEqual("ACTIVE", accepted);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                instance.SetValue(null, oldInstance);
                generation.SetValue(null, oldGeneration);
                NetSession.Mode = oldMode;
                NetSession.Online = oldOnline;
            }
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
