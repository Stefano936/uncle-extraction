using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Sesion 20 (UE-14/UE-15): cabeza de produccion del tio. Solo presentacion: no cambia el contrato de agarre ni la
    /// fisica, oculta la cabeza del kit y su expresion se lee distinta al preparar un golpe.
    [Timeout(120000)]
    public class Session20HeadTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            LabInput.ClearSimulated();
            SceneManager.LoadScene("Bar_LastRound");
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator Head_IsPresentationOnly_AndHidesTheKitHead()
        {
            var brain = NpcBrain.Instance; var head = brain.GetComponent<UncleHead>();
            Assert.IsNotNull(head, "el tio articulado tiene cabeza de produccion"); Assert.IsTrue(head.Built);
            Assert.IsTrue(head.fitted, "ajustada a la cabeza del kit por el constructor");
            var vis = brain.GetComponentsInChildren<Transform>(true).First(t => t.name == "UncleHead_Visual");
            Assert.AreEqual(0, vis.GetComponentsInChildren<Collider>(true).Length, "sin colliders nuevos");
            Assert.AreEqual(0, vis.GetComponentsInChildren<Rigidbody>(true).Length, "sin cuerpos nuevos");
            Assert.AreEqual(0, vis.GetComponentsInChildren<GrabSocket>(true).Length, "sin puntos de agarre nuevos");
            Assert.Greater(vis.GetComponentsInChildren<Renderer>().Length, 20, "cara completa (ojos, parpados, cejas, bigote, pelo)");
            // los puntos de agarre del tio siguen siendo los 8 de siempre
            var ids = brain.GetComponentsInChildren<GrabSocket>(true).Select(s => s.socketId).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(new[] { "ankle_l", "ankle_r", "belt", "belt_alt", "forearm_l", "forearm_r", "shoulder_l", "shoulder_r" }, ids);
            var smr = brain.GetComponentInChildren<SkinnedMeshRenderer>();
            var headBone = smr.bones.First(b => b && b.name.ToLowerInvariant().StartsWith("head") && !b.name.Contains("_end"));
            yield return null;
            Assert.Less(headBone.localScale.x, 0.01f, "la cabeza del kit queda colapsada dentro de la nueva");
            Assert.Less(Vector3.Distance(vis.position, brain.GetComponent<NpcBody>().head.position), 0.35f, "la cabeza nueva esta donde estaba la del kit");
        }

        [UnityTest]
        public IEnumerator Expression_ChangesFromDrunkToAnticipation_WhenWindingUp()
        {
            var brain = NpcBrain.Instance; var head = brain.GetComponent<UncleHead>();
            brain.brainEnabled = false;
            for (float t = 0; t < 1.5f; t += Time.deltaTime) yield return null;
            Assert.AreNotEqual("anticipa", head.Mood);
            float idleLid = head.Lid01;
            Assert.IsTrue(brain.ForceAttack(AttackId.Slap), "fuerza la preparacion de un manotazo");
            float t0 = Time.time; while (brain.State != NpcState.Windup && Time.time - t0 < 2f) yield return null;
            Assert.AreEqual(NpcState.Windup, brain.State);
            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual("anticipa", head.Mood, "la cara anticipa el golpe");
            Assert.Less(head.Lid01, idleLid - 0.15f, "abre los ojos al preparar el golpe (se ve venir)");
        }
    }
}
