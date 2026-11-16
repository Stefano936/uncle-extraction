using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Pruebas de comportamiento sobre la escena real PhysicsLab (debe estar en Build Settings).
    public class PhysicsLabPlayTests
    {
        NpcBrain npc; NpcBody body; PlayerMotor player; PlayerHands hands; RoundDirector round; LabForceRig rig;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false;
            SceneManager.LoadScene("PhysicsLab");
            yield return null; yield return new WaitForFixedUpdate();
            // El jugador lo genera la red (modo individual = anfitrion en loopback): esperarlo.
            for (float t = 0; t < 5f && PlayerControls.Local == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(PlayerControls.Local, "el anfitrion local deberia generar al jugador");
            yield return new WaitForFixedUpdate();
            npc = Object.FindAnyObjectByType<NpcBrain>(); body = npc.GetComponent<NpcBody>();
            player = Object.FindAnyObjectByType<PlayerMotor>(); hands = player.GetComponent<PlayerHands>();
            round = Object.FindAnyObjectByType<RoundDirector>(); rig = Object.FindAnyObjectByType<LabForceRig>();
            npc.brainEnabled = false;
        }

        static GrabSocket Sock(string id) => GrabSocket.All.First(s => s.socketId == id);

        void PlacePlayer(Vector3 pos, float yaw)
        {
            player.Body.position = pos; player.transform.position = pos; player.Body.linearVelocity = Vector3.zero;
            player.SetYawPitch(yaw, 20f);
        }

        [UnityTest]
        public IEnumerator EightGrabs_ThenTenResets_LeaveNoOrphans()
        {
            rig.SetVirtualPullers(4);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(8, GrabRegistry.Count, "4 jugadores virtuales x 2 manos");
            for (int i = 0; i < 10; i++)
            {
                rig.SetVirtualPullers(4);
                yield return new WaitForSeconds(0.2f);
                round.Restart();
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(0, GrabRegistry.Count, $"agarres huerfanos tras reinicio {i + 1}");
                Assert.AreEqual(0, RoundDirector.CountGameplayJoints(), $"uniones huerfanas tras reinicio {i + 1}");
            }
            Assert.AreEqual(10, round.Restarts);
        }

        [UnityTest]
        public IEnumerator Grab_And_Drag_MovesUncle()
        {
            PlacePlayer(new Vector3(0, 0, 5.9f), 0f);
            yield return new WaitForFixedUpdate();
            hands.DebugHoldLeft = true; hands.DebugHoldRight = true;
            Assert.AreEqual(GrabResult.Ok, hands.ForceGrab(0, Sock("belt")));
            Assert.AreEqual(GrabResult.Ok, hands.ForceGrab(1, Sock("forearm_l")));
            Vector3 start = body.Body.position;
            player.DebugMove = new Vector2(0, -1);
            yield return new WaitForSeconds(3f);
            float moved = start.z - body.Body.position.z;
            Debug.Log($"[Test] drag moved {moved:0.00} m, grabs={GrabRegistry.Count}");
            Assert.Greater(moved, 0.3f, "el tio deberia acercarse al jugador que retrocede");
        }

        [UnityTest]
        public IEnumerator Push_KnocksPlayerDown_ReleasesHands_AndRecovers()
        {
            PlacePlayer(new Vector3(0, 0, 6.0f), 0f);
            yield return new WaitForFixedUpdate();
            hands.DebugHoldLeft = true;
            Assert.AreEqual(GrabResult.Ok, hands.ForceGrab(0, Sock("belt")));
            Assert.IsTrue(npc.ForceAttack(AttackId.Push));
            yield return new WaitForSeconds(LabBalance.Windup(AttackId.Push, Difficulty.Normal) + 0.3f);
            Assert.GreaterOrEqual(player.FallCount, 1, "el empujon en Normal debe derribar");
            Assert.IsFalse(hands.AnyHeld, "caer suelta todo");
            hands.DebugHoldLeft = false;
            yield return new WaitForSeconds(LabBalance.FallLockMax + player.getUpDuration + 0.2f);
            Assert.AreEqual(PlayerState.Active, player.State);
            Assert.IsTrue(player.Invulnerable, "invulnerabilidad al levantarse");
        }

        /// Acuesta al tio dentro de la camioneta (prueba de reglas de la zona, no de transporte).
        void LayUncleInVan(float pelvisZOffset)
        {
            var zone = Object.FindAnyObjectByType<ExtractionZone>();
            Quaternion lying = Quaternion.LookRotation(Vector3.up, Vector3.back); // cabeza hacia la cabina, boca arriba
            Vector3 pelvisLocal = body.transform.InverseTransformPoint(body.pelvis.position);
            Vector3 pel = new Vector3(0f, zone.transform.position.y - 0.85f + 0.36f, zone.transform.position.z + pelvisZOffset); // piso de carga + radio
            body.Body.rotation = lying; body.Body.position = pel - lying * pelvisLocal;
            body.transform.SetPositionAndRotation(body.Body.position, body.Body.rotation);
            body.Body.linearVelocity = Vector3.zero; body.Body.angularVelocity = Vector3.zero;
            body.SetFallen("prueba");
        }

        IEnumerator HoldClose(Vector3 standAt, float seconds)
        {
            var van = Object.FindAnyObjectByType<VanDoors>();
            PlacePlayer(standAt, 180f);
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                Vector3 to = van.closePoint - hands.cam.transform.position;
                player.SetYawPitch(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg);
                LabInput.Simulated[(int)Act.Interact] = true;
                yield return null;
            }
            LabInput.Simulated[(int)Act.Interact] = false;
        }

        [UnityTest]
        public IEnumerator Extraction_NeedsPoseInsideVan_ThenDoorClosedFromOutside()
        {
            var zone = Object.FindAnyObjectByType<ExtractionZone>();
            LayUncleInVan(0f);
            yield return new WaitForSeconds(LabBalance.ExtractionHold + 0.3f);
            Assert.IsTrue(zone.PoseValid, "pose dentro del volumen y sin solapes");
            Assert.IsTrue(zone.PoseReady);
            Assert.AreEqual(RoundState.Running, round.State, "la pose sola no gana: hay que cerrar");

            // Alguien parado en la franja de las puertas: no se puede cerrar
            yield return HoldClose(new Vector3(0f, 0f, -8.6f), 1.6f);
            Assert.AreEqual(RoundState.Running, round.State, "con un jugador en la puerta no se cierra");

            // Desde afuera, mantener E 1 s inicia el cierre; la ronda NO se asegura hasta que las hojas cierran
            var van = Object.FindAnyObjectByType<VanDoors>();
            yield return HoldClose(new Vector3(0.3f, 0f, -7.3f), VanDoors.CloseHold + 0.15f);
            Assert.AreEqual(VanDoors.Phase.Closing, van.CurrentPhase, "tras 1 s de E empieza a cerrar");
            Assert.AreEqual(RoundState.Running, round.State, "no se gana antes del cierre efectivo");
            yield return new WaitForSeconds(VanDoors.SwingTime + 0.2f);
            Assert.IsTrue(zone.Secured);
            Assert.AreEqual(RoundState.Success, round.State);
            Assert.AreEqual(NpcState.Secured, npc.State);
            Assert.AreEqual(0, GrabRegistry.Count);
            yield return new WaitForSeconds(0.7f);
            Assert.Greater(Object.FindAnyObjectByType<VanDoors>().Closed01, 0.95f, "las hojas se cierran");
        }

        [UnityTest]
        public IEnumerator Extraction_HalfOutside_CannotClose()
        {
            var zone = Object.FindAnyObjectByType<ExtractionZone>();
            LayUncleInVan(1.6f); // pelvis cerca de la abertura: hombros/pelvis afuera
            yield return new WaitForSeconds(LabBalance.ExtractionHold + 0.3f);
            Assert.IsFalse(zone.PoseReady, "a medio entrar no hay pose valida");
            yield return HoldClose(new Vector3(0.3f, 0f, -7.3f), VanDoors.CloseHold + 0.5f);
            Assert.AreEqual(RoundState.Running, round.State);
            Assert.IsFalse(zone.Secured);
        }

        [UnityTest]
        public IEnumerator Decoy_ReleasesAnchor_DistractsAndHasCooldown()
        {
            npc.brainEnabled = true;
            var glass = Object.FindAnyObjectByType<DecoyGlass>();
            // Agarrarlo para que proteste y se aferre a la barra
            PlacePlayer(new Vector3(0, 0, 6.0f), 0f);
            yield return new WaitForFixedUpdate();
            hands.DebugHoldLeft = true;
            Assert.AreEqual(GrabResult.Ok, hands.ForceGrab(0, Sock("belt")));
            for (float t = 0; t < 4f && !npc.IsAnchored; t += Time.deltaTime) yield return null;
            Assert.IsTrue(npc.IsAnchored, "el tio deberia aferrarse a la barra al protestar");
            // Apoyar el vaso a la vista
            var grb = glass.GetComponent<Rigidbody>();
            grb.position = new Vector3(1.5f, 0.1f, 5.2f); grb.linearVelocity = Vector3.zero; glass.transform.position = grb.position;
            glass.MarkPlaced(); // equivale a que un jugador lo trajo y lo apoyo
            for (float t = 0; t < 2.5f && npc.State != NpcState.Distracted; t += Time.deltaTime) yield return null;
            Assert.AreEqual(NpcState.Distracted, npc.State, "un vaso apoyado y visible distrae");
            Assert.IsFalse(npc.IsAnchored, "la distraccion suelta el anclaje (GDD 06)");
            yield return new WaitForSeconds(NpcBrain.DistractionDuration + 0.4f);
            Assert.AreNotEqual(NpcState.Distracted, npc.State, "la distraccion dura una ventana acotada");
            // Enfriamiento: el mismo vaso no lo vuelve a distraer enseguida
            hands.DebugHoldLeft = false;
            for (float t = 0; t < 4f; t += Time.deltaTime) { Assert.AreNotEqual(NpcState.Distracted, npc.State, "enfriamiento de 12 s"); yield return null; }
            Assert.AreEqual(1, npc.DistractionUses);
        }

        [UnityTest]
        public IEnumerator Teammate_HelpsFallenPlayer_GetUpFaster()
        {
            // Segundo jugador local sin red (solo para la prueba): cae y el jugador 1 lo ayuda mirando y manteniendo E
            var prefab = Object.FindAnyObjectByType<NetSession>().playerPrefab;
            var other = Object.Instantiate(prefab, new Vector3(-2f, 0f, 0f), Quaternion.identity).GetComponent<PlayerMotor>();
            yield return new WaitForFixedUpdate();
            other.Fall("prueba");
            PlacePlayer(new Vector3(-2f, 0f, 1.0f), 180f);
            float start = Time.time;
            while (other.State == PlayerState.Fallen && Time.time - start < 3f)
            {
                Vector3 to = other.transform.position + Vector3.up * 0.4f - hands.cam.transform.position;
                player.SetYawPitch(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg);
                LabInput.Simulated[(int)Act.Interact] = true;
                yield return null;
            }
            LabInput.Simulated[(int)Act.Interact] = false;
            float fallen = Time.time - start;
            Object.Destroy(other.gameObject);
            Assert.Greater(other.AssistedTime, 0.3f, "la ayuda se aplico");
            Assert.Less(fallen, LabBalance.FallLockMax * 0.75f, $"con ayuda se levanta antes ({fallen:0.00}s vs {LabBalance.FallLockMax}s)");
        }

        [UnityTest]
        public IEnumerator Close_TwoHolders_DoNotAccelerate_AndAbortWhenDoorwayBlocked()
        {
            var zone = Object.FindAnyObjectByType<ExtractionZone>(); var van = Object.FindAnyObjectByType<VanDoors>();
            LayUncleInVan(0f);
            yield return new WaitForSeconds(LabBalance.ExtractionHold + 0.3f);
            Assert.IsTrue(zone.PoseReady);
            // Dos jugadores manteniendo E en el mismo frame: el progreso avanza una sola vez
            PlacePlayer(new Vector3(0.3f, 0f, -7.3f), 180f);
            float t0 = Time.time;
            while (van.CurrentPhase != VanDoors.Phase.Closing && Time.time - t0 < 3f)
            {
                van.HoldClose(hands, Time.deltaTime); van.HoldClose(null, Time.deltaTime);
                yield return null;
            }
            float took = Time.time - t0;
            Assert.GreaterOrEqual(took, VanDoors.CloseHold * 0.9f, $"con dos jugadores no se cierra mas rapido ({took:0.00}s)");
            // Alguien entra en la abertura durante el cierre: se cancela y se reabre
            PlacePlayer(new Vector3(0f, 0f, -8.7f), 180f);
            yield return new WaitForSeconds(VanDoors.SwingTime + 0.3f);
            Assert.AreEqual(1, van.Aborts, "el cierre se cancela");
            Assert.AreEqual(RoundState.Running, round.State);
            Assert.IsFalse(zone.Secured);
            yield return new WaitForSeconds(0.6f);
            Assert.Less(van.Closed01, 0.05f, "las hojas se reabren");
        }

        [UnityTest]
        public IEnumerator Restart_DuringClosingAndCartLoading_LeavesCleanState()
        {
            var zone = Object.FindAnyObjectByType<ExtractionZone>(); var van = Object.FindAnyObjectByType<VanDoors>();
            var cart = Object.FindAnyObjectByType<TransportCart>();
            // 1) reiniciar a mitad del cierre
            LayUncleInVan(0f);
            yield return new WaitForSeconds(LabBalance.ExtractionHold + 0.3f);
            PlacePlayer(new Vector3(0.3f, 0f, -7.3f), 180f);
            for (float t = 0; t < VanDoors.CloseHold + 0.1f; t += Time.deltaTime) { van.HoldClose(hands, Time.deltaTime); yield return null; }
            Assert.AreEqual(VanDoors.Phase.Closing, van.CurrentPhase);
            round.Restart();
            yield return new WaitForSeconds(VanDoors.SwingTime + 0.3f);
            Assert.AreEqual(VanDoors.Phase.Open, van.CurrentPhase); Assert.Less(van.Closed01, 0.05f);
            Assert.IsFalse(zone.Secured); Assert.AreEqual(RoundState.Running, round.State);
            // 2) reiniciar a mitad de la carga del carrito
            Vector3 c = cart.transform.position + cart.transform.right * 0.9f; c.y = 0f;
            body.Body.position = c; body.transform.position = c; body.Body.linearVelocity = Vector3.zero;
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(cart.Strap(), $"deberia poder cargarlo: {cart.LastFailure}");
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(cart.IsLoading);
            round.Restart();
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(cart.IsLoading); Assert.IsFalse(cart.IsStrapped);
            Assert.IsFalse(body.Body.isKinematic, "el tio vuelve a ser dinamico");
            Assert.AreEqual(0, RoundDirector.CountGameplayJoints(), "sin uniones huerfanas");
            var uc = body.GetComponent<Collider>();
            foreach (var cc in cart.GetComponentsInChildren<Collider>()) Assert.IsFalse(Physics.GetIgnoreCollision(uc, cc), "sin colisiones ignoradas residuales");
        }
    }
}
