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
        protected virtual string Scene => "PhysicsLab";
        NpcBrain npc; NpcBody body; PlayerMotor player; PlayerHands hands; RoundDirector round; LabForceRig rig;

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false;
            SceneManager.LoadScene(Scene);
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
            body.TeleportTo(pel - lying * pelvisLocal, lying); // cuerpo entero (central o articulado) en reposo
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
            body.TeleportTo(c, body.transform.rotation);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(cart.Strap(), $"deberia poder cargarlo: {cart.LastFailure}");
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(cart.IsLoading);
            round.Restart();
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(cart.IsLoading); Assert.IsFalse(cart.IsStrapped);
            foreach (var b in body.AllBodies) Assert.IsFalse(b.isKinematic, $"el tio vuelve a ser dinamico ({b.name})");
            Assert.AreEqual(0, RoundDirector.CountGameplayJoints(), "sin uniones huerfanas");
            foreach (var uc in body.Colliders)
                foreach (var cc in cart.GetComponentsInChildren<Collider>()) Assert.IsFalse(Physics.GetIgnoreCollision(uc, cc), $"sin colisiones ignoradas residuales ({uc.name})");
            foreach (var uc in body.Colliders)
                foreach (var pc in player.GetComponentsInChildren<Collider>()) Assert.IsFalse(Physics.GetIgnoreCollision(uc, pc), $"tio-jugador sin colisiones ignoradas residuales ({uc.name})");
        }

        [UnityTest]
        public IEnumerator Watchdog_RescuesUncleFallenBelowMap_AndReleasesGrabs()
        {
            var dog = Object.FindAnyObjectByType<PhysicsWatchdog>();
            Assert.IsNotNull(dog, "PhysicsLab incluye el rescate tecnico");
            PlacePlayer(new Vector3(0, 0, 6.0f), 0f);
            yield return new WaitForSeconds(1.2f); // registra una pose segura
            hands.DebugHoldLeft = true;
            Assert.AreEqual(GrabResult.Ok, hands.ForceGrab(0, Sock("belt")));
            body.TeleportTo(new Vector3(0f, -20f, 7f), body.transform.rotation); // condicion tecnica: bajo el mapa
            PlacePlayer(new Vector3(4f, 0f, 2f), 0f); // fuera de alcance: lo que se suelte no puede volver a agarrarse enseguida
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, dog.Rescues, dog.LastRescue);
            Assert.Greater(body.Body.position.y, -1f, "vuelve a la ultima pose segura");
            Assert.AreEqual(0, body.GrabCount(), "suelta los vinculos del cuerpo rescatado");
            foreach (var b in body.AllBodies) Assert.Greater(b.position.y, -1f, $"{b.name} vuelve con el cuerpo");
            hands.DebugHoldLeft = false;
        }

        [UnityTest]
        public IEnumerator Uncle_Escapes_BackInsideThroughTheDoor()
        {
            var L = LevelLayout.Current;
            npc.brainEnabled = true;
            PlacePlayer(new Vector3(-5f, 0f, 6f), 0f); // lejos: sin agarres ni ataques
            Vector3 start = new Vector3(L.doorX + 3f, 0f, L.doorZ - 3f); // en la vereda, fuera del eje de la puerta
            body.TeleportTo(start, Quaternion.identity);
            float t0 = Time.time;
            while (body.transform.position.z < L.doorZ + 0.5f && Time.time - t0 < 15f) yield return null;
            Assert.Greater(body.transform.position.z, L.doorZ + 0.5f, $"el tio deberia volver a entrar por la puerta (quedo en {body.transform.position})");
            Assert.Less(Mathf.Abs(body.transform.position.x - L.doorX), 1.5f, "entra por la abertura, no atraviesa el muro");
        }

        [UnityTest]
        public IEnumerator Cart_StrappedUncle_SignalsAndTriesToWriggleFree()
        {
            // Asegurado en el carrito, el tio anuncia (Windup "se quiere soltar") y, si nadie lo impide, rompe la correa.
            npc.brainEnabled = true;
            var cart = Object.FindAnyObjectByType<TransportCart>();
            Vector3 c = cart.transform.position + cart.transform.right * 0.9f; c.y = 0f;
            body.TeleportTo(c, body.transform.rotation);
            PlacePlayer(new Vector3(-4f, 0f, 1f), 0f);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(cart.Strap(), cart.LastFailure);
            for (float t = 0; t < 1f && !cart.IsStrapped; t += Time.deltaTime) yield return null;
            Assert.IsTrue(cart.IsStrapped);
            int w0 = npc.WrigglesStarted; bool sawSignal = false;
            for (float t = 0; t < 14f && cart.IsStrapped; t += Time.deltaTime) { if (npc.WriggleAttempt && npc.State == NpcState.Windup) sawSignal = true; yield return null; }
            Assert.Greater(npc.WrigglesStarted, w0, "deberia intentar soltarse");
            Assert.IsTrue(sawSignal, "el intento se anuncia (preparacion visible) antes de soltarse");
            Assert.IsFalse(cart.IsStrapped, "sin oposicion, la correa se suelta");
        }


        [UnityTest]
        public IEnumerator FrontSockets_AreAimableFromTheFront()
        {
            // Parado frente al tio (punto de carrera), el cinturon tiene que poder apuntarse sin que nada lo tape.
            var L = LevelLayout.Current;
            PlacePlayer(L.raceStandL, 0f);
            yield return new WaitForSeconds(0.5f);
            var cam = player.GetComponentInChildren<Camera>();
            foreach (var id in new[] { "belt" })
            {
                var sk = Sock(id);
                Vector3 o = cam.transform.position, to = sk.Position - o;
                string blockers = string.Join(", ", Physics.RaycastAll(o, to.normalized, to.magnitude - 0.05f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => h.collider.attachedRigidbody != player.Body && !body.Owns(h.collider.attachedRigidbody)).Select(h => h.collider.name));
                Assert.IsEmpty(blockers, $"{id} tapado desde el frente por: {blockers}");
                Assert.LessOrEqual(to.magnitude, LabBalance.GrabReach, $"{id} al alcance ({to.magnitude:0.00} m)");
            }
            player.SetYawPitch(0f, 0f);
            var belt = Sock("belt"); Vector3 dir = belt.Position - cam.transform.position;
            player.SetYawPitch(Quaternion.LookRotation(dir).eulerAngles.y, Quaternion.LookRotation(dir).eulerAngles.x);
            yield return null; yield return new WaitForFixedUpdate();
            Assert.AreEqual("belt", hands.Candidate ? hands.Candidate.socketId : "-", "mirando el cinturon, es el candidato");
        }


        [UnityTest]
        public IEnumerator Fallen_AgainstTheBarRail_StillGetsUp()
        {
            // Caido con el centro debajo de la baranda de la barra: tiene que encontrar lugar al lado y pararse.
            PlacePlayer(new Vector3(-4f, 0f, 1f), 0f);
            var rail = GameObject.Find("Anchor_Rail").transform.position;
            Quaternion lying = Quaternion.LookRotation(Vector3.up, Vector3.right); // acostado de costado, a lo largo de la barra
            body.TeleportTo(new Vector3(rail.x + 0.6f, 0.05f, rail.z - 0.05f), lying);
            body.SetFallen("prueba");
            yield return new WaitForSeconds(0.5f);
            for (float t = 0; t < body.standUpDelay + 3f && body.Posture != NpcPosture.Upright; t += Time.deltaTime) { body.TryStandUp(); yield return null; }
            Assert.AreEqual(NpcPosture.Upright, body.Posture, $"se levanta junto a la baranda (centro en {body.Body.worldCenterOfMass})");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(NpcPosture.Upright, body.Posture, "y queda de pie");
        }


        [UnityTest]
        public IEnumerator Uncle_WithPlayerStandingInFront_DoesNotFallOnHisOwn()
        {
            // Cerebro encendido, un jugador parado enfrente sin agarrarlo: el tio protesta/ataca pero no se cae solo.
            npc.brainEnabled = true;
            var L = LevelLayout.Current;
            PlacePlayer(L.raceStandL, 0f);
            int falls0 = body.FallCount;
            for (float t = 0; t < 12f; t += Time.deltaTime) { player.Body.linearVelocity = Vector3.zero; yield return null; }
            Assert.AreEqual(0, body.GrabCount(), "nadie lo agarro");
            Assert.AreEqual(falls0, body.FallCount, $"no deberia caerse solo (cayo {body.FallCount - falls0} veces; estado {npc.State})");
        }


        [UnityTest]
        public IEnumerator Uncle_LightShoveAtChest_DoesNotKnockHimOver()
        {
            // Un jugador que lo choca caminando (~60 N*s en el pecho, de costado y de frente) no lo tumba; tirar fuerte si.
            PlacePlayer(new Vector3(-4f, 0f, 1f), 0f);
            yield return new WaitForSeconds(0.5f);
            foreach (var dir in new[] { Vector3.right, Vector3.back })
            {
                int falls0 = body.FallCount;
                var chestRb = body.torso.GetComponentInParent<Rigidbody>();
                for (float t = 0; t < 0.3f; t += Time.fixedDeltaTime) { chestRb.AddForceAtPosition(dir * 200f, body.torso.position, ForceMode.Force); yield return new WaitForFixedUpdate(); }
                yield return new WaitForSeconds(2f);
                Assert.AreEqual(falls0, body.FallCount, $"empujon leve hacia {dir} lo tumbo (inclinacion {body.TiltDeg:0})");
            }
        }

        [Test]
        public void Uncle_TotalMass_Is110kg_RegardlessOfVariant()
        {
            Assert.AreEqual(110f, body.TotalMass, 0.5f, "la masa total del tio no depende de la variante (ni escala con jugadores)");
            Assert.IsNotEmpty(GrabSocket.All.Where(s => body.Owns(s.body)).Select(s => s.socketId).Distinct().ToList());
            foreach (var id in new[] { "forearm_l", "forearm_r", "ankle_l", "ankle_r", "shoulder_l", "shoulder_r", "belt", "belt_alt" })
                Assert.IsTrue(GrabSocket.All.Any(s => s.socketId == id && body.Owns(s.body)), $"socket {id} sobre el cuerpo del tio");
        }

        [UnityTest]
        public IEnumerator Uncle_StandsStill_AndFallsLimp_WithoutExploding()
        {
            // Parado 4 s: no se cae solo, no tiembla (velocidades bajas) y las uniones no se estiran.
            PlacePlayer(new Vector3(-4f, 0f, 1f), 0f);
            float maxV = 0f, maxStretch = 0f;
            for (float t = 0; t < 4f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                if (t > 1f) foreach (var b in body.AllBodies) maxV = Mathf.Max(maxV, b.linearVelocity.magnitude);
                maxStretch = Mathf.Max(maxStretch, JointStretch());
            }
            Assert.AreEqual(NpcPosture.Upright, body.Posture, "de pie sin perturbaciones");
            Assert.Less(maxV, 0.6f, "sin temblor sostenido");
            Assert.Less(maxStretch, 0.08f, "uniones sin estirarse");
            // Derribado: queda flojo en el piso sin salir disparado y se vuelve a levantar.
            body.SetFallen("prueba");
            float maxVf = 0f;
            for (float t = 0; t < 2f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                foreach (var b in body.AllBodies) maxVf = Mathf.Max(maxVf, b.linearVelocity.magnitude);
                maxStretch = Mathf.Max(maxStretch, JointStretch());
            }
            Assert.Less(maxVf, 8f, "la caida no lo lanza");
            Assert.Less(maxStretch, 0.1f, "uniones sin estirarse al caer");
            foreach (var b in body.AllBodies) Assert.Greater(b.position.y, -0.3f, $"{b.name} no atraviesa el piso");
            for (float t = 0; t < body.standUpDelay + 3f && body.Posture != NpcPosture.Upright; t += Time.deltaTime) { body.TryStandUp(); yield return null; }
            Assert.AreEqual(NpcPosture.Upright, body.Posture, "se recupera");
        }

        float JointStretch()
        {
            float m = 0f;
            foreach (var j in body.GetComponentsInChildren<ConfigurableJoint>())
            {
                if (!j.connectedBody || !body.Owns(j.connectedBody)) continue; // solo uniones internas (no agarres)
                Vector3 a = j.transform.TransformPoint(j.anchor), c = j.connectedBody.transform.TransformPoint(j.connectedAnchor);
                m = Mathf.Max(m, Vector3.Distance(a, c));
            }
            return m;
        }
    }

    /// Las mismas pruebas de laboratorio con el tio articulado (ragdoll reducido de 11 cuerpos).
    public class PhysicsLabPlayTests_Articulated : PhysicsLabPlayTests
    {
        protected override string Scene => "PhysicsLab_Articulated";

        [Test]
        public void Variant_IsReallyArticulated()
        {
            var b = Object.FindAnyObjectByType<NpcBody>();
            Assert.IsTrue(b.Articulated, "la escena usa el tio articulado");
            Assert.AreEqual(11, b.AllBodies.Count, "pelvis + 10 partes");
            Assert.AreEqual(11, b.Colliders.Count);
            Assert.IsNotNull(b.GetComponent<RagdollVisual>(), "malla del kit ligada a las partes");
            Assert.GreaterOrEqual(b.GetComponent<RagdollVisual>().links.Count, 10, "huesos del kit ligados");
        }
    }
}
