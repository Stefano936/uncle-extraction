using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Prioridad de E en la partida (sesion 14): el aviso corresponde a la accion ejecutada; mirar a otro lado corta el
    /// progreso; mantener E completa la accion anunciada. La combinacion de los cuatro objetivos se prueba en EditMode.
    public class InteractPriorityTests
    {
        PlayerMotor player; PlayerHands hands; NpcBrain npc;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false; LabInput.ClearSimulated();
            SceneManager.LoadScene("Bar_LastRound");
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            player = PlayerControls.Local.GetComponent<PlayerMotor>(); hands = player.GetComponent<PlayerHands>();
            npc = Object.FindAnyObjectByType<NpcBrain>(); npc.brainEnabled = false;
        }
        [TearDown] public void Clear() { LabInput.ClearSimulated(); }

        IEnumerator Aim(Vector3 target, float yawOffset = 0f)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 to = target - player.EyePosition, f = new Vector3(to.x, 0, to.z);
                player.SetYawPitch(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg + yawOffset, -Mathf.Atan2(to.y, f.magnitude) * Mathf.Rad2Deg);
                yield return new WaitForFixedUpdate(); yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AnchoredHand_LabelMatchesAction_LookingAwayResetsProgress_HoldingCompletes()
        {
            Assert.IsTrue(npc.AnchorForTest(), "el tio se aferra a la barra");
            var hand = npc.AnchorHandSocket;
            // al costado del tio, del lado de la mano anclada (sin superponerse con su cuerpo)
            Vector3 side = Vector3.ProjectOnPlane(hand.Position - npc.transform.position, Vector3.up).normalized;
            Vector3 at = hand.Position + side * 0.75f; at.y = 0f;
            player.Body.position = at; player.transform.position = at; player.Body.linearVelocity = Vector3.zero;
            yield return new WaitForSeconds(0.3f);
            yield return Aim(hand.Position);
            var cam = Camera.main.transform; Vector3 d = hand.Position - cam.position;
            string diag = $"dist={d.magnitude:0.00} ang={Vector3.Angle(cam.forward, d):0} hits=" + string.Join(",", System.Linq.Enumerable.Select(Physics.RaycastAll(cam.position, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore), h => $"{h.collider.name}@{h.distance:0.00}"));
            Debug.Log("[Test] mano anclada: " + diag);
            Assert.AreEqual(PlayerHands.InteractKind.Unanchor, hands.InteractTarget, diag);
            Assert.AreEqual("interact.unanchor", hands.InteractLabel, "el aviso es el de la accion que hara E");
            LabInput.Simulated[(int)Act.Interact] = true;
            yield return new WaitForSeconds(LabBalance.AnchorReleaseHold * 0.5f);
            Assert.Greater(hands.InteractProgress01, 0.3f);
            yield return Aim(hand.Position, 90f);                 // mira a otro lado con E apretada
            Assert.AreNotEqual(PlayerHands.InteractKind.Unanchor, hands.InteractTarget);
            Assert.AreEqual(0f, hands.InteractProgress01, 1e-4f, "cambiar de objetivo empieza de cero");
            Assert.IsTrue(npc.IsAnchored, "no se solto por un progreso a medias");
            yield return Aim(hand.Position);
            yield return new WaitForSeconds(LabBalance.AnchorReleaseHold + 0.3f);
            LabInput.Simulated[(int)Act.Interact] = false;
            Assert.IsFalse(npc.IsAnchored, "mantener E sobre la mano la suelta");
        }
    }
}
