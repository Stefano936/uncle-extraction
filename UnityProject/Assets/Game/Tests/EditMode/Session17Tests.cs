using System.Text;
using NUnit.Framework;

namespace UncleExtraction.Tests
{
    /// Sesion 17: nombres por capacidad UTF-8 real, mensajes con «|» y «=», parte estructurado y version unica.
    public class Session17Tests
    {
        static int Bytes(string s) => Encoding.UTF8.GetByteCount(s);

        [TestCase("Ana", "Ana")]
        [TestCase("  José  ", "José")]
        [TestCase("<b>Ana</b>", "bAna/b")]
        [TestCase("a|b=c", "a|b=c")]
        public void Name_KeepsValidText(string input, string expected) => Assert.AreEqual(expected, PlayerMotor.Sanitize(input));

        [Test] public void Name_NeverExceeds29Utf8Bytes_NorSplitsCharacters()
        {
            foreach (var input in new[] { new string('x', 40), "ñandúñandúñandúñandúñandú", "😀😀😀😀😀😀😀😀😀😀", "Ana👨‍👩‍👧‍👦Beto", "日本語の名前はとても長いです" })
            {
                string s = PlayerMotor.Sanitize(input);
                Assert.LessOrEqual(Bytes(s), PlayerMotor.MaxNameBytes, input);
                Assert.IsFalse(s.Length > 0 && char.IsHighSurrogate(s[s.Length - 1]), "no corta un emoji a la mitad: " + input);
                var fs = new Unity.Collections.FixedString32Bytes(s); Assert.AreEqual(s, fs.ToString(), "entra completo en el tipo replicado");
            }
            Assert.AreEqual("", PlayerMotor.Sanitize("   "), "vacio = nombre por defecto");
        }

        [Test] public void CodedMessages_SurviveNamesWithSeparators()
        {
            const string tricky = @"a|b=c\d";
            string code = Loc.Code("interact.help_up", ("player", tricky));
            Loc.SetLanguage(Loc.EsUY);
            StringAssert.Contains(tricky, Loc.Coded(code), "el nombre llega entero");
        }

        [Test] public void Report_IsStructured_AndRoundTripsWeirdNames()
        {
            var r = new RoundStats.Report { round = 3, win = true, time = 75f, left = 9f, falls = 2, challenge = "NoCart", featured = "close_call", featuredName = "a|b=c 😀", featuredValue = 9 };
            r.moments.Add(new RoundStats.Moment { t = 5f, key = "first_grab", name = "a|b=c 😀", who = 0 });
            r.best.Add(new RoundStats.Best { key = "hold", name = "José=1|2", v = 30, seconds = true });
            RoundStats.Receive(UnityEngine.JsonUtility.ToJson(r));
            var back = RoundStats.Last;
            Assert.AreEqual(3, back.round); Assert.AreEqual("a|b=c 😀", back.moments[0].name); Assert.AreEqual("José=1|2", back.best[0].name);
            Loc.SetLanguage(Loc.EsUY); string es = RoundStats.Render(back); Loc.SetLanguage(Loc.En); string en = RoundStats.Render(back);
            StringAssert.Contains("José=1|2", es); StringAssert.Contains("a|b=c 😀", en); Assert.AreNotEqual(es, en, "se traduce en cada cliente");
            StringAssert.Contains("9", RoundStats.Featured(back));
        }

        [Test] public void Bell_IsTheLowestPriorityInteraction()
        {
            var K = PlayerHands.InteractKind.None;
            Assert.AreEqual(PlayerHands.InteractKind.Bell, PlayerHands.ChooseInteract(false, false, false, false, true, K, false));
            Assert.AreEqual(PlayerHands.InteractKind.Unanchor, PlayerHands.ChooseInteract(false, true, false, false, true, K, false));
            Assert.AreEqual(PlayerHands.InteractKind.HelpUp, PlayerHands.ChooseInteract(true, false, false, false, true, K, false));
            Assert.AreEqual(PlayerHands.InteractKind.Cart, PlayerHands.ChooseInteract(false, false, false, true, true, K, false));
        }

        [Test] public void Hints_FollowTheDeviceInUse()
        {
            Loc.SetLanguage(Loc.EsUY);
            InputBindings.ForcePad = true;
            try { Assert.AreEqual("LT", InputBindings.Display(Act.GrabLeft)); Assert.AreEqual("A", InputBindings.Display(Act.Interact)); Assert.AreEqual("Stick izquierdo", InputBindings.MoveDisplay()); }
            finally { InputBindings.ForcePad = false; }
            Assert.AreNotEqual("LT", InputBindings.Display(Act.GrabLeft), "con teclado y raton vuelve a la asignacion");
        }

        /// Sesion 20: regenerar escenas (UiKitImporter) pisaba los catalogos con la fuente del kit y borraba textos del juego.
        [Test] public void Catalogs_KeepGameOwnedLines()
        {
            var dlg = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Localization/dialogue_en_es");
            StringAssert.Contains("last_call", dlg.text, "la linea de la campana sigue en el catalogo de dialogos");
            var ui = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Localization/ui_strings");
            StringAssert.Contains("actualizá y volvé a intentar desde la sala", ui.text, "el mensaje de version completo sigue");
        }

        [Test] public void Version_HasASingleSource()
        {
            Assert.AreEqual(GameVersion.Tag, NetSession.VersionTag);
            Assert.AreEqual(GameVersion.Protocol, NetSession.ProtocolVersion);
            Assert.AreEqual("error.version", NetSession.Decide(1, 4, "0.16.0/1", GameVersion.Tag, true), "una build del protocolo anterior se rechaza con motivo");
        }
    }
}
