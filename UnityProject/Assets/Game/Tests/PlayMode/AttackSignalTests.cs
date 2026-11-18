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

        public enum Hold { RightArm, LeftArm, RightAnkle, LeftAnkle, BothArms, BothAnkles }
        class Holder : IGrabOwner { public Vector3 at; public string OwnerName => "prueba"; public bool CanGrab => true; public Vector3 HandOrigin(int h) => at; }

        /// Sujeta (logicamente, como un jugador) los puntos de esos miembros, por lado ANATOMICO del tio.
        int HoldLimbs(bool arms, bool? right)
        {
            int n = 0;
            foreach (var s in GrabSocket.All.ToList())
            {
                if (!npc.LimbOf(s, out bool arm, out bool r) || arm != arms || (right.HasValue && r != right.Value)) continue;
                if (GrabRegistry.TryCreate(new Holder { at = s.Position }, 0, s, out _) == GrabResult.Ok) n++;
            }
            return n;
        }
        void HoldCase(Hold h)
        {
            int n = h == Hold.RightArm ? HoldLimbs(true, true) : h == Hold.LeftArm ? HoldLimbs(true, false) : h == Hold.RightAnkle ? HoldLimbs(false, true)
                  : h == Hold.LeftAnkle ? HoldLimbs(false, false) : h == Hold.BothArms ? HoldLimbs(true, null) : HoldLimbs(false, null);
            Assert.Greater(n, 0, $"{h}: hay puntos de ese miembro");
        }
        IEnumerator PlayerAround(float angleFromFace = 0f, float dist = 1.1f)
        {
            Vector3 dir = Quaternion.Euler(0, angleFromFace, 0) * body.Forward;
            Vector3 p = body.Body.position + dir * dist; p.y = 0f;
            player.Body.position = p; player.transform.position = p; player.Body.linearVelocity = Vector3.zero;
            var toU = body.Body.position - p; player.SetYawPitch(Mathf.Atan2(toU.x, toU.z) * Mathf.Rad2Deg, 15f);
            yield return new WaitForSeconds(0.4f);
        }

        /// Hallazgo C (sesion 14): con las reglas NORMALES de seleccion (no ForceAttack), el tio solo usa miembros libres y
        /// el lado elegido es el libre.
        [UnityTest]
        public IEnumerator NaturalSelection_UsesOnlyFreeLimbs([Values] Hold h)
        {
            yield return PlayerAround();
            HoldCase(h); yield return new WaitForFixedUpdate();
            var seen = new System.Collections.Generic.Dictionary<string, int>();
            for (int i = 0; i < 60; i++)
            {
                if (!npc.TryNaturalAttackForTest()) { npc.CancelAttackForTest(); continue; }
                var id = npc.CurrentAttack.id; var side = npc.Side; string k = id + "/" + side;
                seen[k] = seen.TryGetValue(k, out int c) ? c + 1 : 1;
                switch (h)
                {
                    case Hold.RightArm: Assert.AreNotEqual(AttackId.Push, id); if (id == AttackId.Slap) Assert.AreEqual(AttackSide.Left, side, "brazo derecho sujeto: manotazo con el izquierdo"); break;
                    case Hold.LeftArm: Assert.AreNotEqual(AttackId.Push, id); if (id == AttackId.Slap) Assert.AreEqual(AttackSide.Right, side, "brazo izquierdo sujeto: manotazo con el derecho"); break;
                    case Hold.RightAnkle: if (id == AttackId.Kick) Assert.AreEqual(AttackSide.Left, side, "tobillo derecho sujeto: patea con la izquierda"); break;
                    case Hold.LeftAnkle: if (id == AttackId.Kick) Assert.AreEqual(AttackSide.Right, side, "tobillo izquierdo sujeto: patea con la derecha"); break;
                    case Hold.BothArms: Assert.IsFalse(id == AttackId.Slap || id == AttackId.Push, "sin brazos libres: ni manotazo ni empujon"); break;
                    case Hold.BothAnkles: Assert.AreNotEqual(AttackId.Kick, id, "sin piernas libres: sin patada"); break;
                }
                npc.CancelAttackForTest(); yield return null;
            }
            Debug.Log($"[Test] {h}: " + string.Join(", ", seen.Select(kv => kv.Key + "=" + kv.Value)));
            Assert.Greater(seen.Count, 0, "hubo ataques posibles");
            if (h == Hold.RightArm) Assert.IsTrue(seen.ContainsKey("Slap/Left"), "el brazo libre se usa");
            if (h == Hold.LeftAnkle) Assert.IsTrue(seen.ContainsKey("Kick/Right"), "la pierna libre se usa");
            GrabRegistry.ReleaseEverything(ReleaseReason.Reset);
        }

        /// El puno o el pie elegido se mueve hacia la zona anunciada; el arco del piso apunta a la misma direccion que el
        /// impacto; el golpe alcanza al jugador que estaba dentro del arco. Eleccion por reglas normales.
        [UnityTest]
        public IEnumerator Strike_MovesTheChosenLimbIntoTheAnnouncedZone([Values(AttackId.Slap, AttackId.Kick)] AttackId want)
        {
            if (!body.Articulated) { Assert.Pass("solo el tio articulado tiene poses por miembro"); yield break; }
            yield return PlayerAround();
            // unica opcion de miembro la buscada: manotazo = piernas sujetas + brazo izquierdo (sin empujon); patada = brazos sujetos
            if (want == AttackId.Slap) { HoldLimbs(false, null); HoldLimbs(true, false); } else HoldLimbs(true, null);
            yield return new WaitForFixedUpdate();
            bool started = false;
            for (int i = 0; i < 80 && !started; i++)
            {
                if (npc.TryNaturalAttackForTest() && npc.CurrentAttack.id == want) started = true;
                else { npc.CancelAttackForTest(); yield return null; }
            }
            Assert.IsTrue(started, $"la seleccion normal llego a elegir {want}");
            var side = npc.Side; Assert.AreNotEqual(AttackSide.None, side);
            GrabSocket mine = null, other = null;
            foreach (var s in GrabSocket.All)
                if (npc.LimbOf(s, out bool arm, out bool r) && arm == (want == AttackId.Slap) && (s.socketId.StartsWith("forearm") || s.socketId.StartsWith("ankle")))
                { if (r == (side == AttackSide.Right)) mine = s; else other = s; }
            Assert.IsNotNull(mine);
            var tg = Object.FindAnyObjectByType<AttackTelegraph>();
            float windup = LabBalance.Windup(want, RoundDirector.Instance ? RoundDirector.Instance.difficulty : Difficulty.Normal);
            yield return new WaitForSeconds(windup * 0.9f);
            Assert.AreEqual(NpcState.Windup, npc.State, "no se corto: el miembro elegido esta libre");
            Assert.Less(Vector3.Angle(tg.AimDirection, npc.AttackDirection), 3f, "el arco del piso apunta a donde va el impacto");
            Vector3 dirAtk = npc.AttackDirection; Vector3 a0 = mine.Position - body.Body.position, o0 = other ? other.Position - body.Body.position : Vector3.zero;
            int hits0 = npc.HitsLanded;
            float until = Time.time + windup * 0.1f + LabBalance.Attack(want).active + 0.2f; float best = -9f, bestOther = -9f;
            while (Time.time < until)
            {
                best = Mathf.Max(best, Vector3.Dot(mine.Position - body.Body.position - a0, dirAtk));
                if (other) bestOther = Mathf.Max(bestOther, Vector3.Dot(other.Position - body.Body.position - o0, dirAtk));
                yield return new WaitForFixedUpdate();
            }
            Debug.Log($"[Test] {want} lado={side}: avance del miembro elegido hacia el golpe {best:0.00} m, del otro {bestOther:0.00} m; impactos +{npc.HitsLanded - hits0}");
            Assert.Greater(best, 0.08f, "el miembro elegido avanza hacia la zona anunciada");
            Assert.Greater(best, bestOther, "y es el que se mueve, no el del otro lado");
            Assert.Greater(npc.HitsLanded, hits0, "el jugador dentro del arco recibe el golpe");
            GrabRegistry.ReleaseEverything(ReleaseReason.Reset);
        }

        /// Jugador delante, a los lados y detras de la cara: el tio gira hacia el y arco e impacto coinciden.
        [UnityTest]
        public IEnumerator Windup_FromAnyAngle_ArcAndImpactAgree([Values(0f, 90f, 180f, 270f)] float angle)
        {
            yield return PlayerAround(angle);
            bool ok = false; for (int i = 0; i < 40 && !ok; i++) { ok = npc.TryNaturalAttackForTest(); if (!ok) yield return null; }
            Assert.IsTrue(ok);
            float windup = LabBalance.Windup(npc.CurrentAttack.id, RoundDirector.Instance ? RoundDirector.Instance.difficulty : Difficulty.Normal);
            yield return new WaitForSeconds(windup * 0.95f);
            Vector3 toP = player.transform.position - body.Body.position; toP.y = 0f;
            var tg = Object.FindAnyObjectByType<AttackTelegraph>();
            Assert.Less(Vector3.Angle(npc.AttackDirection, toP), 10f, "el golpe va hacia el jugador");
            Assert.Less(Vector3.Angle(tg.AimDirection, npc.AttackDirection), 3f, "el arco del piso marca la misma direccion");
            Assert.Less(Vector3.Angle(body.Forward, toP), 60f, "la cara gira hacia el jugador");
        }

        /// Sesion 13: la malla del tio articulado mira hacia -z de la raiz; antes giraba la NUCA hacia el objetivo al
        /// preparar el golpe. Con el jugador detras de la cara, al terminar la preparacion la cara tiene que mirarlo.
        [UnityTest]
        public IEnumerator Windup_TurnsTheFaceTowardsThePlayer()
        {
            if (body.Articulated && body.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Kit_uncle_prototype"))
                Assert.AreEqual(-1f, body.FaceSign, "la cara del kit mira hacia atras de la raiz");
            Vector3 behind = body.Body.position - body.Forward * 1.2f; behind.y = 0f;
            player.Body.position = behind; player.transform.position = behind; player.Body.linearVelocity = Vector3.zero;
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(npc.ForceAttack(AttackId.Slap));
            float windup = LabBalance.Windup(AttackId.Slap, RoundDirector.Instance ? RoundDirector.Instance.difficulty : Difficulty.Normal);
            yield return new WaitForSeconds(windup * 0.95f);
            Vector3 toP = player.transform.position - body.Body.position; toP.y = 0f;
            float facing = Vector3.Angle(body.Forward, toP);
            Debug.Log($"[Test] cara vs jugador al final de la preparacion: {facing:0}°");
            Assert.Less(facing, 60f, "al preparar el golpe la cara del tio mira al jugador");
        }

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
