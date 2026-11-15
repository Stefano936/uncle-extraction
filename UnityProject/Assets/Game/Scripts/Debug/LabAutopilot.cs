using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UncleExtraction
{
    public enum AutopilotMode { Drag, Cart }

    /// Jugador automatico de PhysicsLab para pruebas y demostraciones. Usa solo las entradas de un jugador real:
    /// mirar (yaw/pitch), moverse, mantener clic izquierdo/derecho, E, Q, Espacio y agacharse (via LabInput.Simulated).
    /// Nunca llama a Strap(), Secure() ni mueve cuerpos a mano. La geometria de rutas es la de PhysicsLabBuilder.
    public class LabAutopilot : MonoBehaviour, IResettable
    {
        public PlayerMotor player; public PlayerHands hands;
        public NpcBrain npc; public NpcBody body;
        public TransportCart cart; public ExtractionZone zone; public RoundDirector round;

        public bool Running { get; private set; }
        public AutopilotMode Mode { get; private set; }
        public string Status { get; private set; } = "";
        public readonly List<string> Timeline = new List<string>();

        // Ruta del TIO hacia la camioneta: barra -> centro -> puerta (centrado) -> vereda -> costado de la zona.
        static readonly Vector3[] DragPath = { new Vector3(0, 0, 6.0f), new Vector3(0, 0, 3.0f), new Vector3(0, 0, 1.0f), new Vector3(0, 0, -1.0f), new Vector3(0, 0, -3.5f), new Vector3(-1.6f, 0, -6.0f), new Vector3(-1.6f, 0, -7.8f), new Vector3(0.4f, 0, -8.4f) };
        static readonly Vector3 DragFinalStand = new Vector3(1.8f, 0, -8.45f); // el tio entra de costado, con los pies primero
        // Ruta del JUGADOR tirando del asa (el carrito va detras): recto por el centro de la puerta y,
        // al final, de costado para que el carrito quede atravesado dentro de la zona.
        static readonly Vector3[] CartPullPath = { new Vector3(0, 0, 1.6f), new Vector3(0, 0, -0.4f), new Vector3(0, 0, -3.5f), new Vector3(-2.2f, 0, -6.0f), new Vector3(-2.6f, 0, -8.45f), new Vector3(2.6f, 0, -8.45f) };

        Coroutine routine; HingeJoint door; Vector3 stuckRef; float stuckSince; bool zigLogged;
        NpcState lastNpc; int lastFalls;
        float handsOffUntil = -1f, aimSince, nextReport;
        GrabSocket lastTarget; bool switchedToAnkles;

        void Awake()
        {
            if (!player) player = FindAnyObjectByType<PlayerMotor>();
            if (player && !hands) hands = player.GetComponent<PlayerHands>();
            if (!npc) npc = FindAnyObjectByType<NpcBrain>();
            if (npc && !body) body = npc.GetComponent<NpcBody>();
            if (!cart) cart = FindAnyObjectByType<TransportCart>();
            if (!zone) zone = FindAnyObjectByType<ExtractionZone>();
            if (!round) round = FindAnyObjectByType<RoundDirector>();
            door = FindAnyObjectByType<HingeJoint>();
        }

        bool quitOnEnd;

        /// Linea de comandos de la build: -ueAutopilot drag|cart [-ueQuitOnEnd] (prueba de humo de un intento completo).
        void Start()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-ueQuitOnEnd") quitOnEnd = true;
                if (args[i] == "-ueAutopilot" && i + 1 < args.Length)
                    StartRun(args[i + 1].Equals("cart", StringComparison.OrdinalIgnoreCase) ? AutopilotMode.Cart : AutopilotMode.Drag);
            }
        }

        void Update()
        {
            if (quitOnEnd && round && round.State != RoundState.Running)
            {
                Debug.Log($"[Autopilot] build: ronda {round.State} en {round.Elapsed:0.0}s; saliendo");
                quitOnEnd = false; Application.Quit(round.State == RoundState.Success ? 0 : 3);
            }
        }

        public void StartRun(AutopilotMode mode)
        {
            StopRun();
            Mode = mode; Running = true; Timeline.Clear();
            lastNpc = npc.State; lastFalls = player.FallCount; handsOffUntil = -1f; switchedToAnkles = false; lastTarget = null;
            aimSince = Time.time; nextReport = Time.time + 10f;
            Log($"inicio modo {mode}: tio en {body.Body.position}, jugador en {player.transform.position}");
            routine = StartCoroutine(mode == AutopilotMode.Drag ? DragRun() : CartRun());
        }

        public void StopRun()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null; Running = false; Status = "";
            LabInput.ClearSimulated();
            if (hands) { hands.DebugHoldLeft = hands.DebugHoldRight = null; hands.DebugEffort = false; }
            if (player) player.DebugMove = null;
        }

        public void ResetState() => StopRun();
        void OnDisable() => StopRun();

        void Log(string s) { string l = $"t={Time.time:0.0} {s}"; Timeline.Add(l); Debug.Log("[Autopilot] " + l); }

        // ---------- utilidades ----------
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        Vector3 Eye => hands.cam.transform.position;
        Vector3 Me => Flat(player.transform.position);
        bool Holding(int hand) => GrabRegistry.Find(hands, hand) != null;
        GrabSocket Held(int hand) => GrabRegistry.Find(hands, hand)?.socket;
        bool Finished => round.State != RoundState.Running;

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

        void Idle() { hands.DebugHoldLeft = hands.DebugHoldRight = false; hands.DebugEffort = false; player.DebugMove = Vector2.zero; }

        IEnumerator Tap(Act a) { LabInput.Simulated[(int)a] = true; yield return null; LabInput.Simulated[(int)a] = false; }

        /// De pie: antebrazos (no puede aferrarse a la barra). En el piso: tobillos (el torso queda detras al entrar en la zona).
        GrabSocket BestFreeUncleSocket(GrabSocket exclude, bool nearestOnly = false)
        {
            string pref = body.Posture == NpcPosture.Upright ? "forearm" : "ankle";
            return GrabSocket.All.Where(s => s.body == body.Body && s != exclude && GrabRegistry.OccupancyOf(s) < s.capacity)
                                 .OrderBy(s => (!nearestOnly && s.socketId.StartsWith(pref) ? 0f : 3f) + Vector3.Distance(s.Position, Eye))
                                 .FirstOrDefault();
        }

        void Observe()
        {
            if (npc.State != lastNpc) { Log($"tio {lastNpc}->{npc.State} '{npc.Intent}'"); lastNpc = npc.State; }
            if (player.FallCount > lastFalls) { Log($"jugador derribado (caidas={player.FallCount})"); lastFalls = player.FallCount; }
            if (Time.time >= nextReport)
            {
                nextReport = Time.time + 10f;
                Log($"posicion: jugador {Me} pelvis {body.pelvis.position} postura={body.Posture} correa={cart.IsStrapped} dentro={zone.AllPointsInside} " +
                    $"carrito {Flat(cart.transform.position)} yaw={cart.transform.eulerAngles.y:0} manos={Held(0)?.socketId ?? "-"}/{Held(1)?.socketId ?? "-"} puerta={(door ? door.angle : 0f):0}deg");
            }
        }

        /// Pasos comunes de cada frame. Devuelve true si consumio el frame (caido, desanclando).
        bool CommonStep()
        {
            Observe();
            if (player.State != PlayerState.Active)
            {   // caido o levantandose: soltar botones y esperar
                Idle(); LabInput.ClearSimulated(); Status = "caido"; return true;
            }
            if (npc.IsAnchored && npc.AnchorHandSocket)
            {   // desanclar: mirar la mano del tio en la barra y mantener E
                Status = "desanclando";
                Vector3 hp = npc.AnchorHandSocket.Position; LookAt(hp);
                bool near = Vector3.Distance(Eye, hp) <= LabBalance.GrabReach;
                MoveWorld(near ? Vector3.zero : hp - player.transform.position);
                LabInput.Simulated[(int)Act.Interact] = near;
                return true;
            }
            LabInput.Simulated[(int)Act.Interact] = false;
            return false;
        }

        /// Acercarse y agarrar un punto con una mano, como un jugador: mantener el clic solo si el punto apuntado es el elegido.
        void ApproachAndGrab(int hand, GrabSocket target, float stopAt)
        {
            LabInput.Simulated[(int)Act.Crouch] = target.Position.y < 0.7f;
            LookAt(target.Position);
            float d = Vector3.Distance(Eye, target.Position);
            float flat = Vector3.Distance(Flat(Eye), Flat(target.Position));
            if (d > LabBalance.GrabReach || target != lastTarget) aimSince = Time.time;
            lastTarget = target;
            MoveWorld(flat > stopAt ? target.Position - player.transform.position : Vector3.zero);
            bool press = d <= LabBalance.GrabReach && (hands.Candidate == target || (hands.Candidate && Time.time - aimSince > 1.5f));
            if (hand == 0) hands.DebugHoldLeft = press; else hands.DebugHoldRight = press;
        }

        // ---------- arrastre del tio por una ruta ----------
        int wp;

        /// Un frame de arrastre. 'path' es la ruta del tio; 'finalStand' donde se para el jugador en el ultimo tramo.
        IEnumerator DragStep(Vector3[] path, Vector3 finalStand, bool anklesOnLastLeg)
        {
            bool holdingAny = Holding(0) || Holding(1);
            if (!holdingAny)
            {   // fase de acercamiento y agarre (mano izquierda primero)
                Status = "agarrando al tio";
                hands.DebugHoldRight = false;
                var target = BestFreeUncleSocket(null);
                if (target) ApproachAndGrab(0, target, LabBalance.GrabReach - 0.35f);
                yield return null;
                if (Holding(0)) Log($"agarra {Held(0).socketId} con la izquierda");
                yield break;
            }
            if (!Holding(1))
            {   // con una mano no alcanza para arrastrarlo caido: acercarse y agarrar con la otra
                var held0 = Held(0);
                var second = BestFreeUncleSocket(held0, held0 == null || !held0.socketId.StartsWith("ankle"));
                if (second)
                {
                    Status = "segunda mano";
                    LabInput.Simulated[(int)Act.Crouch] = second.Position.y < 0.7f;
                    LookAt(second.Position);
                    bool near = Vector3.Distance(Flat(Eye), Flat(second.Position)) <= 0.9f;
                    MoveWorld(near ? Vector3.zero : second.Position - player.transform.position);
                    hands.DebugHoldRight = near && hands.Candidate == second;
                    yield return null;
                    if (Holding(1)) Log($"agarra {Held(1).socketId} con la derecha"); else hands.DebugHoldRight = false;
                    yield break;
                }
            }

            LabInput.Simulated[(int)Act.Crouch] = false;
            Status = "arrastrando";
            Vector3 uFlat = Flat(body.pelvis.position);
            LookAt(body.Body.worldCenterOfMass);
            // progreso segun donde esta el tio (puede retroceder si se escapa): primer punto todavia por delante en z
            int nwp = path.Length - 1;
            for (int i = 0; i < path.Length - 1; i++) if (path[i].z < uFlat.z - 0.5f) { nwp = i; break; }
            Vector3 pen = path[path.Length - 2];
            if (nwp == path.Length - 2 && (Vector3.Distance(uFlat, pen) < 0.9f || uFlat.z < pen.z + 0.5f)) nwp = path.Length - 1;
            if (nwp != wp) { Log($"tio hacia el punto {nwp}/{path.Length - 1} desde {uFlat}"); wp = nwp; }

            // Tramo final con el tio en el piso: Q y volver a agarrar de los tobillos para que el torso quede dentro.
            bool anklesHeld = Held(0)?.socketId.StartsWith("ankle") == true || Held(1)?.socketId.StartsWith("ankle") == true;
            if (anklesOnLastLeg && wp == path.Length - 1 && body.Posture != NpcPosture.Upright && !anklesHeld && !switchedToAnkles)
            {
                switchedToAnkles = true; Log("suelta (Q) para tomarlo de los tobillos");
                Idle(); yield return Tap(Act.ReleaseAll); yield break;
            }

            Vector3 next = path[wp];
            Vector3 goal = wp == path.Length - 1 ? finalStand : next + (next - uFlat).normalized * 1.1f;
            goal.z = Mathf.Max(goal.z, -8.85f); // la camioneta
            float gap = Vector3.Distance(Me, uFlat);
            hands.DebugEffort = gap > 1.5f;
            if (gap > 1.8f || Vector3.Distance(Me, goal) < 0.3f) player.DebugMove = Vector2.zero; // dejar que el resorte lo traiga
            else MoveWorld(goal - Me);

            // Esquivar ataques directos anunciados (la sacudida solo se evita soltando).
            bool dodge = npc.State == NpcState.Windup && npc.CurrentAttack.id != AttackId.Shake && npc.StateTimeLeft < 0.35f
                         && Vector3.Distance(Me, Flat(body.Body.position)) < 1.9f;
            LabInput.Simulated[(int)Act.Dodge] = dodge && player.DodgeCooldownLeft <= 0f;
            yield return null;
        }

        /// Ya esta dentro: soltar (Q) y esperar el conteo de la zona, como haria un jugador.
        IEnumerator HandsOffInZone()
        {
            Status = "esperando en la zona";
            if (Holding(0) || Holding(1)) { Log("tio dentro de la zona: suelta (Q) y espera"); handsOffUntil = Time.time + 2.5f; }
            Idle(); yield return Tap(Act.ReleaseAll);
        }

        IEnumerator DragRun()
        {
            wp = 0;
            while (!Finished)
            {
                if (CommonStep()) { yield return null; continue; }
                if (zone.AllPointsInside || Time.time < handsOffUntil) { yield return HandsOffInZone(); continue; }
                yield return DragStep(DragPath, DragFinalStand, true);
            }
            Finish();
        }

        // ---------- modo carrito ----------
        IEnumerator CartRun()
        {
            int cartWp = 0;
            while (!Finished)
            {
                if (CommonStep()) { yield return null; continue; }
                if (zone.AllPointsInside || Time.time < handsOffUntil) { yield return HandsOffInZone(); continue; }

                if (!cart.IsStrapped)
                {
                    // 1) Llevar al tio junto al carrito y 2) asegurarlo con E.
                    if (cart.CanStrap && Vector3.Distance(Me, Flat(cart.transform.position)) < 2.0f)
                    {
                        Status = "asegurando con E";
                        Idle(); yield return Tap(Act.ReleaseAll);
                        LookAt(cart.transform.position + Vector3.up * 0.6f);
                        yield return Tap(Act.Interact);
                        yield return null;
                        if (cart.IsStrapped) Log($"asegura al tio en el carrito (correas={cart.StrapCount})");
                        continue;
                    }
                    if (HoldingCart()) { Idle(); yield return Tap(Act.ReleaseAll); continue; }
                    Vector3 c = Flat(cart.transform.position), pel = Flat(body.pelvis.position);
                    Vector3 fromCart = (pel - c).sqrMagnitude > 0.01f ? (pel - c).normalized : Vector3.forward;
                    Vector3 uncleTarget = c + fromCart * 0.9f;                                   // junto a la caja, del lado del tio
                    Vector3 stand = c + Vector3.Cross(Vector3.up, fromCart).normalized * 1.3f;     // el jugador, al costado del carrito
                    Status = "llevando al tio al carrito";
                    yield return DragToPoint(uncleTarget, stand);
                    continue;
                }

                // 3) Empujar el carrito por el asa hasta la camioneta. Si el tio se suelta, volver a 1.
                var handle = GrabSocket.All.First(s => s.socketId == "cart_handle");
                if (!HoldingCart())
                {
                    if (Holding(0) || Holding(1)) { Idle(); yield return Tap(Act.ReleaseAll); continue; }
                    Status = "tomando el asa";
                    Vector3 behind = cart.transform.TransformPoint(new Vector3(0, 0, -1.5f));
                    if (Vector3.Distance(Me, Flat(behind)) > 0.5f && Vector3.Distance(Eye, handle.Position) > LabBalance.GrabReach - 0.2f)
                    { LookAt(handle.Position); MoveWorld(behind - player.transform.position); hands.DebugHoldLeft = hands.DebugHoldRight = false; yield return null; continue; }
                    LookAt(handle.Position); player.DebugMove = Vector2.zero;
                    bool aimed = hands.Candidate == handle;
                    hands.DebugHoldLeft = aimed; hands.DebugHoldRight = aimed;
                    yield return null;
                    if (HoldingCart()) Log("toma el asa");
                    continue;
                }
                Status = "tirando del carrito";
                Vector3 cc = Flat(cart.transform.position);
                if (!(Held(0)?.socketId == "cart_handle" && Held(1)?.socketId == "cart_handle"))
                {   // las dos manos en el asa
                    LookAt(handle.Position); player.DebugMove = Vector2.zero;
                    bool aimed = hands.Candidate == handle;
                    if (!Holding(0)) hands.DebugHoldLeft = aimed; if (!Holding(1)) hands.DebugHoldRight = aimed;
                    yield return null; continue;
                }
                while (cartWp < CartPullPath.Length - 1 && Vector3.Distance(Me, CartPullPath[cartWp]) < 0.6f) { cartWp++; Log($"jugador hacia el punto {cartWp}/{CartPullPath.Length - 1} con el carrito en {Flat(cart.transform.position)}"); }
                Vector3 goal = CartPullPath[cartWp];
                // Mientras el carrito no cruzo la puerta (z = -2), tirar recto por el centro: en diagonal se traba en el marco.
                if (cc.z > -2.9f && Me.z < 0.8f)
                {   // antes de la puerta, recentrar el carrito en x; dentro del marco, tirar recto
                    float gx = cc.z > -0.6f ? Mathf.Clamp(-cc.x, -0.6f, 0.6f) : 0f;
                    goal = new Vector3(gx, 0f, Mathf.Max(Me.z - 1.0f, -6.0f));
                }
                else if (cartWp == CartPullPath.Length - 1)
                {   // tramo final de costado por el frente de la camioneta: avanzar al este hasta que la pelvis pase el centro
                    Vector3 zc = Flat(zone.transform.position), pel = Flat(body.pelvis.position);
                    goal = new Vector3(pel.x < zc.x + 0.1f ? 2.6f : Me.x, 0f, zc.z);
                }
                float gap = Vector3.Distance(Me, Flat(handle.Position));
                LookAt(handle.Position);
                hands.DebugEffort = gap > 1.1f;
                // de espaldas hacia el punto; si el carrito se queda atras, esperar a que el resorte lo traiga
                // Destrabar: si el carrito no avanza en 2 s, zigzaguear de costado (como sacudir un changuito trabado).
                if (Vector3.Distance(cc, stuckRef) > 0.15f) { stuckRef = cc; stuckSince = Time.time; zigLogged = false; }
                if (Time.time - stuckSince > 2f)
                {
                    float phase = Mathf.Repeat(Time.time - stuckSince - 2f, 2.4f);
                    if (!zigLogged) { zigLogged = true; Log($"carrito trabado en {cc}: zigzag"); }
                    Vector3 side = Vector3.Cross(Vector3.up, Flat(cc - Me)).normalized * (phase < 1.2f ? 1f : -1f);
                    MoveWorld(side + Flat(Me - cc).normalized * 0.5f);
                    if (Time.time - stuckSince > 12f) { stuckSince = Time.time; stuckRef = cc; }
                }
                else if (gap > 1.25f || Vector3.Distance(Me, goal) < 0.2f) player.DebugMove = Vector2.zero;
                else MoveWorld(goal - Me);
                yield return null;
            }
            Finish();
        }

        bool HoldingCart() => Held(0)?.socketId == "cart_handle" || Held(1)?.socketId == "cart_handle";

        /// Arrastra al tio hasta un punto; en el ultimo tramo el jugador se para en 'stand'.
        IEnumerator DragToPoint(Vector3 uncleTarget, Vector3 stand) => DragStep(new[] { uncleTarget, uncleTarget }, stand, false);

        void Finish()
        {
            Log($"fin: ronda={round.State} en {round.Elapsed:0.0}s; tio en {body.Body.position}; ataques={npc.AttacksStarted} golpes={npc.HitsLanded} " +
                $"caidas jugador={player.FallCount} caidas tio={body.FallCount} correas={cart.StrapCount}");
            Debug.Log("[Autopilot] TIMELINE\n" + string.Join("\n", Timeline));
            StopRun();
        }
    }
}
