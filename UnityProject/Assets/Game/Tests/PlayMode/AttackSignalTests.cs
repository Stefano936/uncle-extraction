using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Senales de los cuatro ataques sobre el tio (GDD 06: anticipables). Con el tio articulado, la preparacion y el golpe
    /// tienen que mover las extremidades que corresponden (pose fisica, replicada a los clientes); en todas las variantes,
    /// el aviso en el piso muestra el alcance real y se llena con la preparacion, y arriba se lee el nombre del ataque.
    /// Despues del golpe sigue de pie y sin uniones estiradas. La pose no decide impactos: el reloj es el de NpcBrain.
    [Timeout(120000)]
    public class AttackSignalTests
    {
        protected virtual string Scene => "PhysicsLab_Articulated";
        protected NpcBrain npc; protected NpcBody body; PlayerMotor player; PlayerHands hands;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false; LabInput.ClearSimulated();
            SceneManager.LoadScene(Scene);
            yield return null; yield return new WaitForFixedUpdate();
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(PlayerControls.Local);
            yield return new WaitForFixedUpdate();
            npc = Object.FindAnyObjectByType<NpcBrain>(); body = npc.GetComponent<NpcBody>();
            player = Object.FindAnyObjectByType<PlayerMotor>(); hands = player.GetComponent<PlayerHands>();
            npc.brainEnabled = false;
        }

        public static readonly AttackId[] Ids = { AttackId.Slap, AttackId.Push, AttackId.Kick, AttackId.Shake };

        /// Centro de cada parte en el marco de la raiz del tio (cancela el balanceo del cuerpo entero).
        Vector3[] LocalCenters() => body.parts.Select(p => body.transform.InverseTransformPoint(p.GetComponent<Collider>().bounds.center)).ToArray();

        static float MaxMove(Vector3[] a, Vector3[] b, out int which)
        {
            float m = 0f; which = -1;
            for (int i = 0; i < a.Length; i++) { float d = Vector3.Distance(a[i], b[i]); if (d > m) { m = d; which = i; } }
            return m;
        }

        float JointStretch()
        {
            float worst = 0f;
            foreach (var p in body.parts)
            {
                if (!p || !p.joint || !p.joint.connectedBody) continue;
                Vector3 a = p.transform.TransformPoint(p.joint.anchor), b = p.joint.connectedBody.transform.TransformPoint(p.joint.connectedAnchor);
                worst = Mathf.Max(worst, Vector3.Distance(a, b));
            }
            return worst;
        }

        [UnityTest]
        public IEnumerator Attack_WindupAndStrike_AreReadable([ValueSource(nameof(Ids))] AttackId id)
        {
            // un jugador enfrente, a distancia de golpe (el ataque necesita una direccion); para la sacudida, agarrandolo
            Vector3 front = body.Body.position + body.Forward * 1.1f; front.y = 0f;
            player.Body.position = front; player.transform.position = front; player.Body.linearVelocity = Vector3.zero;
            var toU = body.Body.position - front; player.SetYawPitch(Mathf.Atan2(toU.x, toU.z) * Mathf.Rad2Deg, 15f);
            yield return new WaitForSeconds(1.0f);
            var tg = Object.FindAnyObjectByType<AttackTelegraph>(); var pose = body.GetComponent<UncleAttackPose>();
            Assert.IsNotNull(tg, "el tio deberia tener el aviso de ataque en el piso");
            var rest = LocalCenters();

            Assert.IsTrue(npc.ForceAttack(id));
            var def = LabBalance.Attack(id);
            float windup = LabBalance.Windup(id, RoundDirector.Instance ? RoundDirector.Instance.difficulty : Difficulty.Normal);
            yield return new WaitForSeconds(windup * 0.75f);
            Assert.AreEqual(NpcState.Windup, npc.State);
            Assert.IsTrue(tg.Visible, "aviso visible durante la preparacion");
            Assert.That(tg.Fill01, Is.InRange(0.5f, 0.95f), "el relleno avanza con la preparacion");
            var wind = LocalCenters();
            int wi = -1; float wMove = body.Articulated ? MaxMove(rest, wind, out wi) : 0f;
            string wPart = wi >= 0 ? body.parts[wi].role : "-";

            for (float t = 0; t < windup && npc.State == NpcState.Windup; t += Time.deltaTime) yield return null;
            Assert.AreEqual(NpcState.Attack, npc.State, "despues de la preparacion viene el golpe");
            yield return new WaitForSeconds(Mathf.Min(0.15f, def.active * 0.8f));
            var strike = LocalCenters();
            float sMove = body.Articulated ? MaxMove(wind, strike, out _) : 0f;
            Assert.IsTrue(tg.Visible, "aviso visible en el golpe");
            string active = pose ? pose.ActivePose : "-";
            Debug.Log($"[AttackSignal] {Scene} {id}: preparacion mueve {wMove:0.00} m ({wPart}), golpe mueve {sMove:0.00} m mas; pose activa '{active}'; empujon: {(pose ? pose.pushSource : "-")}; patada: {(pose ? pose.kickSource : "-")}");
            if (body.Articulated)
            {
                Assert.IsNotNull(pose, "el articulado deberia tener poses de ataque");
                Assert.Greater(wMove, 0.12f, $"la preparacion de {id} deberia mover una extremidad de forma visible");
                Assert.Greater(sMove, 0.08f, $"el golpe de {id} deberia verse distinto de la preparacion");
            }

            for (float t = 0; t < 4f && (npc.State == NpcState.Attack || npc.State == NpcState.Recover); t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(NpcPosture.Upright, body.Posture, "el propio ataque no lo tira");
            if (body.Articulated) Assert.Less(JointStretch(), 0.06f, "sin uniones estiradas despues del ataque");
            Assert.IsFalse(tg.Visible, "el aviso se apaga al terminar");
        }
    }

    public class AttackSignalTests_Bar : AttackSignalTests { protected override string Scene => "Bar_LastRound"; }
    public class AttackSignalTests_Central : AttackSignalTests { protected override string Scene => "PhysicsLab"; }
}
