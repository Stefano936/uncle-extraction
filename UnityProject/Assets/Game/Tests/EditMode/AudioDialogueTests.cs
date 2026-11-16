using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UncleExtraction.Tests
{
    /// Sonido sintetizado y subtitulos del catalogo de dialogos (sin voz grabada).
    public class AudioDialogueTests
    {
        [Test]
        public void Sfx_AllClipsAudible_AndAttackCuesAreDistinct()
        {
            foreach (var id in SfxSynth.Ids)
            {
                var c = SfxSynth.Get(id); var d = new float[c.samples]; c.GetData(d, 0);
                Assert.Greater(c.length, 0.02f, id);
                Assert.Greater(d.Max(Mathf.Abs), 0.05f, $"{id} deberia oirse");
                Assert.LessOrEqual(d.Max(Mathf.Abs), 1.0f, $"{id} sin saturar");
            }
            var cues = new[] { "cue_slap", "cue_push", "cue_kick", "cue_shake" }.Select(i => SfxSynth.Get(i)).ToArray();
            for (int a = 0; a < cues.Length; a++) for (int b = a + 1; b < cues.Length; b++)
            {
                var x = new float[cues[a].samples]; var y = new float[cues[b].samples]; cues[a].GetData(x, 0); cues[b].GetData(y, 0);
                bool same = x.Length == y.Length && x.Zip(y, (p, q) => Mathf.Abs(p - q)).Max() < 1e-3f;
                Assert.IsFalse(same, $"{cues[a].name} y {cues[b].name} deberian distinguirse");
            }
        }

        [Test]
        public void Dialogue_LoadsAlcoholProfile_FilterUsesCleanColumns_AndCooldown()
        {
            var lines = UncleDialogue.Load();
            Assert.Greater(lines.Count, 20, "catalogo cargado");
            Assert.IsFalse(lines.Any(l => l.ev.StartsWith("cannabis")), "solo el perfil del alcance inicial (alcohol)");
            var dirty = lines.First(l => l.en != l.cleanEn);
            string lang0 = Loc.Language;
            try
            {
                Loc.SetLanguage(Loc.En);
                UncleDialogue.ProfanityFilter = false; Assert.AreEqual(dirty.en, UncleDialogue.Text(dirty));
                UncleDialogue.ProfanityFilter = true; Assert.AreEqual(dirty.cleanEn, UncleDialogue.Text(dirty));
                Loc.SetLanguage(Loc.EsUY); Assert.AreEqual(dirty.cleanEs, UncleDialogue.Text(dirty));
            }
            finally { UncleDialogue.ProfanityFilter = false; Loc.SetLanguage(lang0); }
            var go = new GameObject("dlg"); var d = go.AddComponent<UncleDialogue>();
            try
            {
                string single = lines.GroupBy(l => l.ev).First(g => g.Count() == 1 && g.First().cooldown > 1f).Key;
                Assert.IsTrue(d.Say(single));
                Assert.IsFalse(d.Say(single), "la misma linea respeta su enfriamiento");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
