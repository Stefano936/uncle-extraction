using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UncleExtraction.Tests
{
    /// Criterio de salida M1: un intento individual completo, con el cerebro del tio ENCENDIDO y sin teletransportes.
    /// Un "jugador bot" usa solo las entradas de un jugador real: mirar (yaw/pitch), moverse, mantener clic izquierdo/derecho,
    /// E y Espacio (via LabInput.Simulated). No llama a Strap(), Secure() ni mueve cuerpos a mano.
    public class SoloAttemptTests
    {
        const float TimeScale = 4f;          // acelera la simulacion; el paso fisico sigue siendo 1/60 s
        const float SimBudget = 420f;        // segundos de juego; la ronda Normal individual dura 645 s

        NpcBrain npc; NpcBody body; PlayerMotor player; PlayerHands hands; RoundDirector round; ExtractionZone zone;
        readonly List<string> timeline = new List<string>();

        [UnitySetUp]
        public IEnumerator Load()
        {
            GrabRegistry.Verbose = false;
            LabInput.ClearSimulated();
            SceneManager.LoadScene("PhysicsLab");
            yield return null; yield return new WaitForFixedUpdate();
            npc = Object.FindAnyObjectByType<NpcBrain>(); body = npc.GetComponent<NpcBody>();
            player = Object.FindAnyObjectByType<PlayerMotor>(); hands = player.GetComponent<PlayerHands>();
            round = Object.FindAnyObjectByType<RoundDirector>(); zone = Object.FindAnyObjectByType<ExtractionZone>();
            Assert.IsTrue(npc.brainEnabled, "el intento completo exige el cerebro encendido");
            Assert.AreEqual(1, round.partySize); Assert.AreEqual(Difficulty.Normal, round.difficulty);
            timeline.Clear();
            Time.timeScale = TimeScale;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f; LabInput.ClearSimulated();
            if (hands) { hands.DebugHoldLeft = hands.DebugHoldRight = null; hands.DebugEffort = false; }
            if (player) player.DebugMove = null;
            yield return null;
        }

        void Log(string s) { string l = $"t={Time.time:0.0} {s}"; timeline.Add(l); Debug.Log("[Solo] " + l); }

        // ---------- utilidades del bot ----------
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        Vector3 Eye => hands.cam.transform.position;

        void LookAt(Vector3 p)
        {
            Vector3 to = p - Eye; Vector3 f = Flat(to);
            float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(to.y, Mathf.Max(0.05f, f.magnitude)) * Mathf.Rad2Deg; // positivo = mirar abajo
            player.SetYawPitch(yaw, Mathf.Clamp(pitch, -80f, 80f));
        }

        /// Mueve al jugador en direccion mundial 'dir' (sin importar hacia donde mira).
        void MoveWorld(Vector3 dir)
        {
            dir = Flat(dir); if (dir.sqrMagnitude < 1e-4f) { player.DebugMove = Vector2.zero; return; }
            Vector3 local = Quaternion.Euler(0, -player.transform.eulerAngles.y, 0) * dir.normalized;
            player.DebugMove = new Vector2(local.x, local.z);
        }

        bool Holding(int hand) => GrabRegistry.Find(hands, hand) != null;

        /// De pie: antebrazos (no puede aferrarse a la barra). En el piso: tobillos, para que el torso quede detras al entrar en la zona.
        GrabSocket BestFreeSocket(GrabSocket exclude, bool nearestOnly = false)
        {
            return GrabSocket.All.Where(s => s.body == body.Body && s != exclude && GrabRegistry.OccupancyOf(s) < s.capacity)
                                 .OrderBy(s => (!nearestOnly && s.socketId.StartsWith(body.Posture == NpcPosture.Upright ? "forearm" : "ankle") ? 0f : 3f) + Vector3.Distance(s.Position, Eye))
                                 .FirstOrDefault();
        }

        [UnityTest]
        public IEnumerator SoloAttempt_BrainOn_DragUncleIntoVan_Succeeds()
        {
            // Ruta del TIO (no del jugador): barra -> centro -> puerta -> vereda -> zona. El jugador va delante, de espaldas.
            var path = new[] { new Vector3(0, 0, 6.0f), new Vector3(0, 0, 3.0f), new Vector3(0, 0, 1.0f), new Vector3(0, 0, -1.0f), new Vector3(0, 0, -3.5f), new Vector3(-1.6f, 0, -6.0f), new Vector3(-1.6f, 0, -7.8f), new Vector3(0.4f, 0, -8.4f) };
            Vector3 finalStand = new Vector3(1.8f, 0, -8.45f); // al costado de la zona: el tio entra de lado, con los pies primero
            int wp = 0; float start = Time.time, lastProgressAt = Time.time, nextReport = Time.time + 10f; float bestZ = body.Body.position.z;
            float handsOffUntil = -1f; GrabSocket lastTarget = null; bool switchedToAnkles = false; float aimSince = Time.time; int falls0 = player.FallCount, regrabs = 0; NpcState lastNpc = npc.State; bool wasHolding = false;
            Log($"inicio: tio en {body.Body.position}, jugador en {player.transform.position}");

            while (round.State == RoundState.Running && Time.time - start < SimBudget)
            {
                if (npc.State != lastNpc) { Log($"tio {lastNpc}->{npc.State} '{npc.Intent}'"); lastNpc = npc.State; }
                if (player.FallCount > falls0) { Log($"jugador derribado (caidas={player.FallCount})"); falls0 = player.FallCount; }

                if (player.State != PlayerState.Active)
                {   // caido o levantandose: soltar botones y esperar
                    hands.DebugHoldLeft = hands.DebugHoldRight = false; player.DebugMove = Vector2.zero; LabInput.ClearSimulated();
                    yield return null; continue;
                }

                // Desanclar si el tio se aferra a la barra: mirar su mano y mantener E.
                if (npc.IsAnchored && npc.AnchorHandSocket)
                {
                    Vector3 hp = npc.AnchorHandSocket.Position;
                    LookAt(hp);
                    if (Vector3.Distance(Eye, hp) > LabBalance.GrabReach) { MoveWorld(hp - player.transform.position); LabInput.Simulated[(int)Act.Interact] = false; }
                    else { player.DebugMove = Vector2.zero; LabInput.Simulated[(int)Act.Interact] = true; }
                    yield return null; continue;
                }
                LabInput.Simulated[(int)Act.Interact] = false;

                // Ya esta dentro: soltarlo (Q) y esperar el conteo de la zona, como haria un jugador.
                if (zone.AllPointsInside || Time.time < handsOffUntil)
                {
                    if (Holding(0) || Holding(1)) { Log("tio dentro de la zona: suelta (Q) y espera"); handsOffUntil = Time.time + 2.5f; }
                    hands.DebugHoldLeft = hands.DebugHoldRight = false; player.DebugMove = Vector2.zero;
                    LabInput.Simulated[(int)Act.ReleaseAll] = true; yield return null; LabInput.Simulated[(int)Act.ReleaseAll] = false;
                    wasHolding = false; continue;
                }

                bool holdingAny = Holding(0) || Holding(1);
                if (wasHolding && !holdingAny) Log("perdio el agarre");
                wasHolding = holdingAny;

                if (!holdingAny)
                {   // fase de acercamiento y agarre (mano izquierda primero)
                    hands.DebugHoldRight = false;
                    var target = BestFreeSocket(null);
                    if (!target) { yield return null; continue; }
                    LabInput.Simulated[(int)Act.Crouch] = target.Position.y < 0.7f;
                    LookAt(target.Position);
                    float d = Vector3.Distance(Eye, target.Position);
                    MoveWorld(d > LabBalance.GrabReach - 0.35f ? target.Position - player.transform.position : Vector3.zero);
                    if (d > LabBalance.GrabReach || target != lastTarget) aimSince = Time.time;
                    lastTarget = target;
                    // como un jugador: mantener clic solo cuando el punto apuntado es el elegido (o cualquiera tras 1,5 s)
                    hands.DebugHoldLeft = d <= LabBalance.GrabReach && (hands.Candidate == target || (hands.Candidate && Time.time - aimSince > 1.5f));
                    yield return null;
                    if (Holding(0)) { regrabs++; Log($"agarra {GrabRegistry.Find(hands, 0).socket.socketId} con la izquierda"); }
                    continue;
                }

                // Segunda mano: si hay otro punto libre al alcance, agarrarlo (sin soltar la primera).
                if (!Holding(1))
                {
                    var held0 = GrabRegistry.Find(hands, 0)?.socket;
                    var second = BestFreeSocket(held0, held0 == null || !held0.socketId.StartsWith("ankle"));
                    if (second)
                    {   // con una mano no alcanza para arrastrarlo caido: acercarse y agarrar con la otra
                        LabInput.Simulated[(int)Act.Crouch] = second.Position.y < 0.7f;
                        LookAt(second.Position);
                        bool near = Vector3.Distance(Flat(Eye), Flat(second.Position)) <= 0.9f;
                        MoveWorld(near ? Vector3.zero : second.Position - player.transform.position);
                        hands.DebugHoldRight = near && hands.Candidate == second;
                        yield return null;
                        if (Holding(1)) Log($"agarra {GrabRegistry.Find(hands, 1).socket.socketId} con la derecha");
                        else hands.DebugHoldRight = false;
                        continue;
                    }
                }

                LabInput.Simulated[(int)Act.Crouch] = false;
                // Arrastre: mirar al tio y caminar de espaldas por delante de el, sin estirar el agarre hasta romperlo.
                Vector3 uncle = body.Body.worldCenterOfMass; Vector3 uFlat = Flat(body.pelvis.position);
                LookAt(uncle);
                Vector3 me = Flat(player.transform.position);
                // progreso segun donde esta el tio (puede volver atras si se escapa): primer punto todavia por delante en z
                int nwp = path.Length - 1;
                for (int i = 0; i < path.Length - 1; i++) if (path[i].z < uFlat.z - 0.5f) { nwp = i; break; }
                if (nwp == path.Length - 2 && uFlat.z < -7.3f) nwp = path.Length - 1;
                if (nwp != wp) { Log($"tio hacia el punto {nwp}/{path.Length - 1} desde {uFlat}"); wp = nwp; }
                // Tramo final con el tio en el piso: cambiar a los tobillos (Q y volver a agarrar) para que el torso quede dentro.
                bool anklesHeld = new[] { 0, 1 }.Any(h => GrabRegistry.Find(hands, h)?.socket.socketId.StartsWith("ankle") == true);
                if (wp == path.Length - 1 && body.Posture != NpcPosture.Upright && !anklesHeld && !switchedToAnkles)
                {
                    switchedToAnkles = true; Log("suelta (Q) para tomarlo de los tobillos");
                    hands.DebugHoldLeft = hands.DebugHoldRight = false; LabInput.Simulated[(int)Act.ReleaseAll] = true;
                    yield return null; LabInput.Simulated[(int)Act.ReleaseAll] = false; continue;
                }
                Vector3 next = path[wp];
                Vector3 goal = wp == path.Length - 1 ? finalStand : next + (next - uFlat).normalized * 1.1f;
                goal.z = Mathf.Max(goal.z, -8.85f); // la camioneta
                float gap = Vector3.Distance(me, uFlat);
                hands.DebugEffort = gap > 1.5f;
                if (gap > 1.8f || Vector3.Distance(me, goal) < 0.3f) player.DebugMove = Vector2.zero; // dejar que el resorte lo traiga
                else MoveWorld(goal - me);
                if (Time.time >= nextReport) { nextReport = Time.time + 10f; Log($"posicion: jugador {me} tio {uFlat} postura={body.Posture} punto={wp} pelvis={body.pelvis.position} torso={body.torso.position} hombros={body.shoulderL.position}/{body.shoulderR.position} dentro={zone.AllPointsInside}"); }

                // Esquivar ataques directos anunciados (no la sacudida, que solo se evita soltando).
                bool dodge = npc.State == NpcState.Windup && npc.CurrentAttack.id != AttackId.Shake && npc.StateTimeLeft < 0.35f
                             && Vector3.Distance(Flat(player.transform.position), Flat(body.Body.position)) < 1.9f;
                LabInput.Simulated[(int)Act.Dodge] = dodge && player.DodgeCooldownLeft <= 0f;

                if (body.Body.position.z < bestZ - 0.25f) { bestZ = body.Body.position.z; lastProgressAt = Time.time; }
                yield return null;
            }

            float elapsed = Time.time - start;
            Log($"fin: ronda={round.State} en {elapsed:0.0}s; tio en {body.Body.position}; ataques={npc.AttacksStarted} golpes={npc.HitsLanded} " +
                $"caidas jugador={player.FallCount} caidas tio={body.FallCount} agarres iniciados={regrabs} sin progreso desde t={lastProgressAt:0.0}");
            Debug.Log("[Solo] TIMELINE\n" + string.Join("\n", timeline));
            Assert.AreEqual(RoundState.Success, round.State, $"el intento individual no termino (tio en {body.Body.position}, mejor z={bestZ:0.0})");
            Assert.IsTrue(zone.Secured);
            Assert.AreEqual(NpcState.Secured, npc.State);
            Assert.Greater(npc.AttacksStarted, 0, "el tio deberia haber atacado durante el intento");
            Assert.AreEqual(0, GrabRegistry.Count, "asegurar suelta todos los agarres");
        }
    }
}
