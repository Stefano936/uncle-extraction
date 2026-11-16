using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UncleExtraction
{
    public enum AutopilotMode { Drag, Cart, Race }

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

        // Todas las posiciones vienen del escenario (LevelLayout): nada de coordenadas fijas de PhysicsLab.
        LevelLayout L => LevelLayout.Current;
        Vector3[] DragPath => L.uncleDragPath;
        Vector3 DragFinalStand => L.dragFinalStand;
        Vector3[] CartPullPath => L.cartPullPath;
        Vector3 OpenSpot => L.cartOpenSpot;

        // Ruta del TIO hacia la camioneta: barra -> centro -> puerta (centrado) -> vereda -> costado de la zona.
        // Ruta del TIO: barra -> centro -> puerta (centrado) -> vereda -> pie de la rampa -> dentro de la camioneta.
        // Ruta del JUGADOR tirando del asa (el carrito va detras): recto por el centro de la puerta y,
        // al final, de costado para que el carrito quede atravesado dentro de la zona.

        Coroutine routine; HingeJoint door; Vector3 stuckRef; float stuckSince; bool zigLogged, cartClearedDoor;
        NpcState lastNpc; int lastFalls;
        float handsOffUntil = -1f, aimSince, nextReport;
        GrabSocket lastTarget; bool switchedToAnkles;

        void Awake()
        {
            if (!player && PlayerControls.Local) player = PlayerControls.Local.GetComponent<PlayerMotor>();
            if (player && !hands) hands = player.GetComponent<PlayerHands>();
            if (!npc) npc = FindAnyObjectByType<NpcBrain>();
            if (npc && !body) body = npc.GetComponent<NpcBody>();
            if (!cart) cart = FindAnyObjectByType<TransportCart>();
            if (!zone) zone = FindAnyObjectByType<ExtractionZone>();
            if (!round) round = FindAnyObjectByType<RoundDirector>();
            door = FindAnyObjectByType<HingeJoint>();
        }

        string dropAt = ""; bool noClose, menuAtEnd;
        bool cartTargetsSet; Vector3 cartRef, cartUncleTarget, cartStand;
        long raceAtUnixMs; int repeatLeft; AutopilotMode lastMode; bool quitOnEnd; float quitAt = -1f; AutopilotMode? pendingMode; float pendingDelay; bool cartNeedsSpace;

        /// Linea de comandos de la build: -ueAutopilot drag|cart [-ueQuitOnEnd] (prueba de humo de un intento completo).
        void Start()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-ueQuitOnEnd") quitOnEnd = true;
                if (args[i] == "-ueAutopilot" && i + 1 < args.Length)
                    pendingMode = args[i + 1].Equals("cart", StringComparison.OrdinalIgnoreCase) ? AutopilotMode.Cart
                                : args[i + 1].Equals("race", StringComparison.OrdinalIgnoreCase) ? AutopilotMode.Race : AutopilotMode.Drag;
                if (args[i] == "-ueRaceAt" && i + 1 < args.Length) long.TryParse(args[i + 1], out raceAtUnixMs);
                if (args[i] == "-ueRepeat" && i + 1 < args.Length) int.TryParse(args[i + 1], out repeatLeft);
                if (args[i] == "-ueDropAt" && i + 1 < args.Length) dropAt = args[i + 1];   // prueba: salir del juego en "strap" o "closing"
                if (args[i] == "-ueNoClose") noClose = true;
                if (args[i] == "-ueMenuAtEnd") menuAtEnd = true;                           // prueba: al terminar, volver al menu                               // prueba: este jugador nunca cierra la camioneta
                if (args[i] == "-ueAutopilotDelay" && i + 1 < args.Length) float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pendingDelay);
            }
        }

        void Update()
        {
            // El jugador local aparece cuando la red lo genera (en un cliente, al conectarse).
            // Ronda nueva: esperar 0,5 s de ronda para tener las posiciones reiniciadas replicadas (en un cliente)
            if (pendingMode.HasValue && PlayerControls.Local && Time.timeSinceLevelLoad >= pendingDelay && round && round.State == RoundState.Running && (NetAuth.IsServer || round.Elapsed > 0.5f))
            { var m = pendingMode.Value; pendingMode = null; lastMode = m; quitAt = -1f; StartRun(m); }
            if ((quitOnEnd || repeatLeft > 0 || menuAtEnd) && round && round.State != RoundState.Running && !pendingMode.HasValue)
            {   // 3 s de "resultados": los clientes reciben el final antes de que el anfitrion cierre o reinicie
                if (quitAt < 0f)
                {   // el cliente sale antes que el anfitrion: si el anfitrion cierra primero, el cliente veria una desconexion
                    quitAt = Time.unscaledTime + (NetAuth.IsServer ? 3f : 1f); NetSession.LogWorldNow("final");
                    Debug.Log($"[Autopilot] build: ronda {round.State} en {round.Elapsed:0.0}s; {(repeatLeft > 0 ? "reinicia en 3 s" : "saliendo en 3 s")}");
                }
                else if (Time.unscaledTime >= quitAt)
                {
                    quitAt = -1f;
                    if (repeatLeft > 0)
                    {   // reintento despues de ganar o perder: el anfitrion reinicia; el cliente espera la ronda nueva
                        repeatLeft--; StopRun();
                        if (NetAuth.IsServer) round.Restart();
                        pendingMode = lastMode; pendingDelay = 0f;
                        Debug.Log($"[Autopilot] reintento (quedan {repeatLeft})");
                    }
                    else if (menuAtEnd) { quitOnEnd = false; Debug.Log("[Autopilot] vuelve al menu"); NetSession.LeaveToMenu(); }
                    else { quitOnEnd = false; Application.Quit(round.State == RoundState.Success ? 0 : 3); }
                }
            }
        }

        public void StartRun(AutopilotMode mode)
        {
            StopRun();
            if (PlayerControls.Local) { player = PlayerControls.Local.GetComponent<PlayerMotor>(); hands = PlayerControls.Local.GetComponent<PlayerHands>(); }
            if (!player || !hands) { Debug.LogWarning("[Autopilot] sin jugador local todavia"); return; }
            Mode = mode; Running = true; Timeline.Clear();
            lastNpc = npc.State; lastFalls = player.FallCount; handsOffUntil = -1f; switchedToAnkles = false; lastTarget = null; cartClearedDoor = false; cartNeedsSpace = false; cartTargetsSet = false; zigLogged = false; stuckSince = Time.time; stuckRef = Vector3.zero;
            aimSince = Time.time; nextReport = Time.time + 10f;
            Log($"inicio modo {mode}: tio en {body.Body.position}, jugador en {player.transform.position}");
            routine = StartCoroutine(mode == AutopilotMode.Drag ? DragRun() : mode == AutopilotMode.Cart ? CartRun() : RaceRun());
        }

        public void StopRun()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null; Running = false; Status = "";
            LabInput.ClearSimulated();
        }

        public void ResetState() => StopRun();
        void OnDisable() => StopRun();

        void Log(string s) { string l = $"t={Time.time:0.0} {s}"; Timeline.Add(l); Debug.Log("[Autopilot] " + l); }

        // ---------- utilidades ----------
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        Vector3 Eye => hands.cam.transform.position;
        Vector3 Me => Flat(player.transform.position);
        // Estado de manos replicado: funciona igual en el anfitrion y en un cliente conectado.
        bool Holding(int hand) => hands.HeldSocketId(hand) != "";
        GrabSocket Held(int hand) => hands.HeldSocket(hand);
        /// Ocupacion de un punto: en el anfitrion, el registro; en un cliente, las manos replicadas de todos los jugadores.
        static int Occupancy(GrabSocket s)
        {
            if (NetAuth.IsServer) return GrabRegistry.OccupancyOf(s);
            int n = 0;
            foreach (var h in FindObjectsByType<PlayerHands>(FindObjectsSortMode.None))
                for (int k = 0; k < 2; k++) if (h.HeldSocketId(k) == s.socketId) n++;
            return n;
        }
        static void SetHold(int hand, bool on) => LabInput.Simulated[hand == 0 ? (int)Act.GrabLeft : (int)Act.GrabRight] = on;
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
            dir = Flat(dir); if (dir.sqrMagnitude < 1e-4f) { LabInput.SimulatedMove = Vector2.zero; return; }
            Vector3 local = Quaternion.Euler(0, -player.transform.eulerAngles.y, 0) * dir.normalized;
            LabInput.SimulatedMove = new Vector2(local.x, local.z);
        }

        void Idle() { SetHold(0, false); SetHold(1, false); LabInput.Simulated[(int)Act.Effort] = false; LabInput.SimulatedMove = Vector2.zero; }

        IEnumerator Tap(Act a) { LabInput.Simulated[(int)a] = true; yield return null; LabInput.Simulated[(int)a] = false; }

        /// De pie: antebrazos (no puede aferrarse a la barra). En el piso: tobillos (el torso queda detras al entrar en la zona).
        GrabSocket BestFreeUncleSocket(GrabSocket exclude, bool nearestOnly = false)
        {
            string pref = pushingIn ? "shoulder" : body.Posture == NpcPosture.Upright ? "forearm" : "ankle"; // acomodar: tirar del torso hacia la cabina
            return GrabSocket.All.Where(s => s.body == body.Body && s != exclude && Occupancy(s) < s.capacity && !(unreachable.TryGetValue(s, out float until) && Time.time < until))
                                 .OrderBy(s => (!nearestOnly && s.socketId.StartsWith(pref) ? 0f : 3f) + Vector3.Distance(s.Position, Eye))
                                 .FirstOrDefault();
        }

        Vector3 lastMe; float lastMeAt;
        void Observe()
        {
            // Diagnostico: salto de posicion del jugador (tunel fisico o recolocacion) o cruce de la pared del frente
            if (lastMeAt > 0f && Vector3.Distance(Me, lastMe) > 0.8f) Log($"SALTO jugador {lastMe} -> {Me} en {Time.time - lastMeAt:0.000}s");
            bool wasIn = lastMe.z > L.doorZ, isIn = Me.z > L.doorZ;
            if (lastMeAt > 0f && wasIn != isIn && Mathf.Abs(Me.x - L.doorX) > 0.75f) Log($"CRUCE de pared fuera de la puerta: {lastMe} -> {Me}");
            lastMe = Me; lastMeAt = Time.time;
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
            if (!player || !hands) { Log("sin jugador local (desconexion o reinicio): se detiene"); StopRun(); return true; }
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
        readonly Dictionary<GrabSocket, float> unreachable = new Dictionary<GrabSocket, float>();
        float detourUntil; Vector3 detourGoal;

        void ApproachAndGrab(int hand, GrabSocket target, float stopAt)
        {
            // Atascado apuntando a un punto que no se puede tomar (puerta o pared en la linea de vision): descartarlo un rato y rodear
            if (target == lastTarget && Time.time - aimSince > 2.5f)
            {
                unreachable[target] = Time.time + 12f; aimSince = Time.time;
                Vector3 pel = Flat(body.pelvis.position), side = Vector3.Cross(Vector3.up, Flat(pel - Me)).normalized;
                detourGoal = pel + side * (UnityEngine.Random.value < 0.5f ? 1.2f : -1.2f); detourUntil = Time.time + 1.5f;
                Log($"no alcanza {target.socketId}: lo descarta 12 s y rodea");
            }
            if (Time.time < detourUntil) { LookAt(target.Position); MoveWorld(detourGoal - Me); SetHold(hand, false); return; }
            LabInput.Simulated[(int)Act.Crouch] = target.Position.y < 0.7f;
            LookAt(target.Position);
            float d = Vector3.Distance(Eye, target.Position);
            float flat = Vector3.Distance(Flat(Eye), Flat(target.Position));
            if (d > LabBalance.GrabReach || target != lastTarget) aimSince = Time.time;
            lastTarget = target;
            MoveWorld(flat > stopAt ? target.Position - player.transform.position : Vector3.zero);
            bool press = d <= LabBalance.GrabReach && (hands.Candidate == target || (hands.Candidate && Time.time - aimSince > 1.5f));
            if (hand == 0) SetHold(0, press); else SetHold(1, press);
        }

        // ---------- arrastre del tio por una ruta ----------
        int wp;

        /// Un frame de arrastre. 'path' es la ruta del tio; 'finalStand' donde se para el jugador en el ultimo tramo.
        bool pushingIn;
        IEnumerator DragStep(Vector3[] path, Vector3 finalStand, bool anklesOnLastLeg)
        {
            pushingIn = zone && zone.AllPointsInside && NeedsPushingIn();
            if (pushingIn) anklesOnLastLeg = false;
            bool holdingAny = Holding(0) || Holding(1);
            if (!holdingAny)
            {   // fase de acercamiento y agarre (mano izquierda primero)
                Status = "agarrando al tio";
                SetHold(1, false);
                var target = BestFreeUncleSocket(null);
                if (!target && unreachable.Count > 0) { unreachable.Clear(); target = BestFreeUncleSocket(null); } // todos descartados: volver a intentar, no quedarse quieto
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
                    SetHold(1, near && hands.Candidate == second);
                    yield return null;
                    if (Holding(1)) Log($"agarra {Held(1).socketId} con la derecha"); else SetHold(1, false);
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
            if (anklesOnLastLeg && wp >= path.Length - 3 && body.Posture != NpcPosture.Upright && !anklesHeld && !switchedToAnkles)
            {
                switchedToAnkles = true; Log("suelta (Q) para tomarlo de los tobillos");
                Idle(); yield return Tap(Act.ReleaseAll); yield break;
            }

            Vector3 next = path[wp];
            Vector3 goal = wp == path.Length - 1 ? finalStand : next + (next - uFlat).normalized * 1.1f;
            if (uFlat.z < L.vanRearZ + 1.25f && Mathf.Abs(uFlat.x - L.vanAxisX) > 1.3f)
            {   // quedo al costado de la camioneta: primero volver al eje frente a la rampa
                next = new Vector3(L.vanAxisX, 0f, L.vanRearZ + 1.85f); goal = next + (next - uFlat).normalized * 1.1f;
            }
            goal.z = Mathf.Max(goal.z, L.vanCabZ + 0.4f); // mampara de la cabina
            float gap = Vector3.Distance(Me, uFlat);
            LabInput.Simulated[(int)Act.Effort] = gap > 1.5f;
            if (gap > 1.8f || Vector3.Distance(Me, goal) < 0.3f) LabInput.SimulatedMove = Vector2.zero; // dejar que el resorte lo traiga
            else MoveWorld(goal - Me);

            // Esquivar ataques directos anunciados (la sacudida solo se evita soltando).
            bool dodge = npc.State == NpcState.Windup && npc.CurrentAttack.id != AttackId.Shake && npc.StateTimeLeft < 0.35f
                         && Vector3.Distance(Me, Flat(body.Body.position)) < 1.9f;
            LabInput.Simulated[(int)Act.Dodge] = dodge && player.DodgeCooldownLeft <= 0f;
            yield return null;
        }

        /// Ya esta adentro: soltar (Q), salir por un costado, pararse fuera de la franja de las puertas y mantener E (GDD 03).
        IEnumerator CloseStep()
        {
            Status = "cerrando la camioneta";
            if (Holding(0) || Holding(1)) { Log("tio adentro de la camioneta: suelta (Q) y sale a cerrar"); Idle(); yield return Tap(Act.ReleaseAll); yield break; }
            var van = VanDoors.Instance;
            float side = body.pelvis.position.x > L.vanAxisX ? -0.8f : 0.8f;
            LabInput.Simulated[(int)Act.Crouch] = false;
            float laneX = L.vanAxisX + side;
            if (Me.z < L.vanRearZ + 0.25f && Mathf.Abs(Me.x - laneX) > 0.25f) { LabInput.Simulated[(int)Act.Interact] = false; LookAt(new Vector3(laneX, 1.5f, Me.z)); MoveWorld(new Vector3(laneX, 0, Me.z) - Me); yield return null; yield break; }
            if (Me.z < L.vanRearZ + 1.8f) { LabInput.Simulated[(int)Act.Interact] = false; LookAt(new Vector3(laneX, 1.5f, L.vanRearZ + 3.25f)); MoveWorld(L.CloseStand(side) - Me); yield return null; yield break; }
            LabInput.SimulatedMove = Vector2.zero;
            LookAt(van.closePoint);
            if (noClose) { Status = "esperando (no cierra)"; LabInput.Simulated[(int)Act.Interact] = false; yield return null; yield break; }
            LabInput.Simulated[(int)Act.Interact] = true;
            if (dropAt == "closing" && hands.InteractProgress01 > 0.4f) { Drop($"durante el cierre ({hands.InteractProgress01 * 100f:0}%)"); yield break; }
            if (Time.time >= nextCloseLog) { nextCloseLog = Time.time + 2f; Log($"cierre: '{hands.InteractLabel}' {hands.InteractProgress01 * 100f:0}%"); }
            yield return null;
        }
        float nextCloseLog;

        /// El tio esta "adentro" pero algo sobresale por la abertura o esta trabado: hay que acomodarlo antes de cerrar.
        bool NeedsPushingIn()
        {
            var van = VanDoors.Instance; if (!van) return false;
            string why = van.BlockReason();
            return why.Contains("afuera") || why.Contains("trabado");
        }

        /// Carrera por un mismo punto (especificacion 05): dos jugadores apuntan al cinturon y aprietan en el mismo instante
        /// de reloj (-ueRaceAt, ms Unix). El anfitrion debe crear un solo agarre y responder SocketBusy al otro.
        IEnumerator RaceRun()
        {
            var belt = GrabSocket.ById("belt");
            bool second = player.Controls && player.Controls.IsSpawned && player.Controls.OwnerClientId % 2 == 1;
            Vector3 stand = second ? L.raceStandR : L.raceStandL;
            while (Vector3.Distance(Me, stand) > 0.15f) { Status = "yendo al punto de carrera"; MoveWorld(stand - Me); LookAt(belt.Position); yield return null; }
            LabInput.SimulatedMove = Vector2.zero;
            long now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            while (now() < raceAtUnixMs) { LookAt(belt.Position); Status = "esperando la senal"; yield return null; }
            Log($"aprieta en {now()} (senal {raceAtUnixMs}); candidato={(hands.Candidate ? hands.Candidate.socketId : "-")}");
            for (float t = 0; t < 2f; t += Time.deltaTime) { LookAt(belt.Position); SetHold(0, true); yield return null; }
            Log($"resultado carrera: mano izq = '{hands.HeldSocketId(0)}' aviso='{hands.LastFeedback}'");
            SetHold(0, false); yield return null;
            StopRun();
        }

        IEnumerator DragRun()
        {
            wp = 0;
            while (!Finished)
            {
                if (CommonStep()) { yield return null; continue; }
                if (zone.AllPointsInside && !zone.Secured && !NeedsPushingIn()) { yield return CloseStep(); continue; }
                yield return DragStep(DragPath, DragFinalStand, true);
            }
            Finish();
        }

        // ---------- modo carrito ----------
        IEnumerator CartRun()
        {
            cartNeedsSpace = Vector3.Distance(Flat(cart.transform.position), OpenSpot) > 0.6f; // primero llevar el carrito vacio al punto de carga
            int cartWp = 0;
            while (!Finished)
            {
                if (CommonStep()) { yield return null; continue; }
                if (zone.AllPointsInside && !zone.Secured && !NeedsPushingIn()) { yield return CloseStep(); continue; }

                if (!cart.IsStrapped && cartNeedsSpace)
                {   // llevar el carrito vacio a un lugar despejado del bar y volver a intentarlo
                    var h = GrabSocket.All.First(s => s.socketId == "cart_handle");
                    Vector3 cc0 = Flat(cart.transform.position);
                    if (Vector3.Distance(cc0, OpenSpot) < 0.6f)
                    {
                        Log($"carrito en lugar abierto {cc0}"); cartNeedsSpace = false;
                        Idle(); yield return Tap(Act.ReleaseAll); continue;
                    }
                    Status = "moviendo el carrito a un lugar abierto";
                    if (!HoldingCart())
                    {
                        if (Holding(0) || Holding(1)) { Idle(); yield return Tap(Act.ReleaseAll); continue; }
                        LookAt(h.Position);
                        bool reach = Vector3.Distance(Eye, h.Position) <= LabBalance.GrabReach - 0.2f;
                        MoveWorld(reach ? Vector3.zero : h.Position - player.transform.position);
                        SetHold(0, reach && hands.Candidate == h); SetHold(1, reach && hands.Candidate == h);
                        yield return null; continue;
                    }
                    // tirar del asa caminando hacia mas alla del lugar abierto
                    Vector3 dirO = Flat(OpenSpot - cc0).normalized;
                    LookAt(h.Position);
                    float gapO = Vector3.Distance(Me, Flat(h.Position));
                    LabInput.SimulatedMove = Vector2.zero;
                    if (gapO <= 1.25f) MoveWorld(OpenSpot + dirO * 1.4f - Me);
                    yield return null; continue;
                }

                if (!cart.IsStrapped)
                {
                    // 1) Llevar al tio junto al carrito y 2) asegurarlo con E.
                    if (cart.CanStrap && Vector3.Distance(Me, Flat(cart.transform.position)) < 2.0f)
                    {
                        Status = "asegurando con E";
                        Idle(); yield return Tap(Act.ReleaseAll);
                        LookAt(cart.transform.position + Vector3.up * 0.6f);
                        yield return Tap(Act.Interact);
                        if (dropAt == "strap") { yield return null; Drop("durante la carga del carrito"); yield break; }
                        for (float w = 0; w < 0.8f && !cart.IsStrapped; w += Time.deltaTime) yield return null; // la subida dura 0,45 s
                        if (cart.IsStrapped) Log($"asegura al tio en el carrito (correas={cart.StrapCount})");
                        else
                        {   // sin lugar (por ejemplo, el jugador esta en el volumen): apartarse y reintentar
                            Log($"no pudo asegurarlo: '{hands.LastFeedback}'; se aparta");
                            if (!hands.LastFeedback.Contains("jugador")) cartNeedsSpace = true; // pared u objeto: mover el carrito a un lugar abierto
                            Vector3 away = Flat(player.transform.position - cart.transform.position).normalized;
                            for (float w = 0; w < 0.6f; w += Time.deltaTime) { MoveWorld(away); yield return null; }
                            LabInput.SimulatedMove = Vector2.zero;
                        }
                        continue;
                    }
                    if (HoldingCart()) { Idle(); yield return Tap(Act.ReleaseAll); continue; }
                    Vector3 c = Flat(cart.transform.position), pel = Flat(body.pelvis.position);
                    // Objetivos fijados al empezar (o si el carrito se movio de verdad): recalcularlos cada frame con la
                    // posicion actual del carrito hacia que el jugador lo fuera empujando con el cuerpo hasta una pared.
                    if (!cartTargetsSet || Vector3.Distance(c, cartRef) > 0.6f)
                    {
                        Vector3 fromCart = (pel - c).sqrMagnitude > 0.01f ? (pel - c).normalized : Vector3.forward;
                        cartRef = c; cartTargetsSet = true;
                        cartUncleTarget = c + fromCart * 0.9f;                                         // junto a la caja, del lado del tio
                        cartStand = c + fromCart * 0.9f + Vector3.Cross(Vector3.up, fromCart).normalized * 1.6f; // al costado, sin tocar el carrito
                    }
                    Vector3 uncleTarget = cartUncleTarget, stand = cartStand;
                    Status = "llevando al tio al carrito";
                    // Estrategia de un jugador solo: el carrito ya espera en un punto abierto junto a la ruta; arrastrar al tio por
                    // la ruta normal y cargarlo con E apenas pasa al lado (el control de arriba lo detecta cada frame).
                    if (Vector3.Distance(pel, c) < 3.0f) yield return DragToPoint(uncleTarget, stand); // ultimos metros: al costado de la caja (frenada)
                    else yield return DragStep(DragPath, DragFinalStand, false);
                    continue;
                }

                // 3) Empujar el carrito por el asa hasta la camioneta. Si el tio se suelta, volver a 1.
                var handle = GrabSocket.All.First(s => s.socketId == "cart_handle");
                if (!HoldingCart())
                {
                    if (Holding(0) || Holding(1)) { Idle(); yield return Tap(Act.ReleaseAll); continue; }
                    Status = "tomando el asa";
                    Vector3 behind = cart.transform.TransformPoint(new Vector3(0, 0, -1.5f));
                    if (Vector3.Distance(Eye, handle.Position) > LabBalance.GrabReach - 0.2f)
                    {   // acercarse por detras del asa si ese lado esta libre; si no (pared), directo al asa
                        bool behindFree = !Physics.CheckCapsule(behind + Vector3.up * 0.4f, behind + Vector3.up * 1.4f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
                        Vector3 dest = behindFree && Vector3.Distance(Me, Flat(behind)) > 0.5f ? behind : handle.Position;
                        LookAt(handle.Position); MoveWorld(dest - player.transform.position); SetHold(0, false); SetHold(1, false); yield return null; continue; }
                    LookAt(handle.Position); LabInput.SimulatedMove = Vector2.zero;
                    bool aimed = hands.Candidate == handle;
                    SetHold(0, aimed); SetHold(1, aimed);
                    yield return null;
                    if (HoldingCart()) Log("toma el asa");
                    continue;
                }
                Status = "tirando del carrito";
                Vector3 cc = Flat(cart.transform.position);
                if (!(Held(0)?.socketId == "cart_handle" && Held(1)?.socketId == "cart_handle"))
                {   // las dos manos en el asa
                    LookAt(handle.Position); LabInput.SimulatedMove = Vector2.zero;
                    bool aimed = hands.Candidate == handle;
                    if (!Holding(0)) SetHold(0, aimed); if (!Holding(1)) SetHold(1, aimed);
                    yield return null; continue;
                }
                while (cartWp < CartPullPath.Length - 1 && (cartWp < L.cartPathDoorIndex || cartClearedDoor) && Vector3.Distance(Me, CartPullPath[cartWp]) < 0.6f) { cartWp++; Log($"jugador hacia el punto {cartWp}/{CartPullPath.Length - 1} con el carrito en {Flat(cart.transform.position)}"); }
                Vector3 goal = CartPullPath[cartWp];
                // Mientras el carrito no cruzo la puerta (z = -2), tirar recto por el centro: en diagonal se traba en el marco.
                if (cc.z < L.doorZ - 2.0f) cartClearedDoor = true;          // carrito + tio que sobresale, del todo afuera
                else if (cc.z > L.doorZ + 1.0f) cartClearedDoor = false;
                if (!cartClearedDoor && Me.z < L.doorZ + 2.8f)
                {   // antes de la puerta, recentrar el carrito en x; dentro del marco, tirar recto
                    float gx = L.doorX + (cc.z > L.doorZ + 1.4f ? Mathf.Clamp(L.doorX - cc.x, -0.6f, 0.6f) : 0f);
                    goal = new Vector3(gx, 0f, Mathf.Max(Me.z - 1.0f, L.doorZ - 4.5f));
                }

                float gap = Vector3.Distance(Me, Flat(handle.Position));
                LookAt(handle.Position);
                LabInput.Simulated[(int)Act.Effort] = gap > 1.1f;
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
                else if (gap > 1.25f || Vector3.Distance(Me, goal) < 0.2f) LabInput.SimulatedMove = Vector2.zero;
                else MoveWorld(goal - Me);
                yield return null;
            }
            Finish();
        }

        bool HoldingCart() => Held(0)?.socketId == "cart_handle" || Held(1)?.socketId == "cart_handle";

        /// Arrastra al tio hasta un punto; en el ultimo tramo el jugador se para en 'stand'.
        IEnumerator DragToPoint(Vector3 uncleTarget, Vector3 stand) => DragStep(new[] { uncleTarget, uncleTarget }, stand, false);

        /// Prueba de desconexion en un momento preciso: registra el estado y cierra el proceso (el anfitrion ve la salida).
        void Drop(string when)
        {
            Log($"DROP {when}"); NetSession.LogWorldNow("drop");
            Application.Quit(5);
        }

        void Finish()
        {
            Log($"fin: ronda={round.State} en {round.Elapsed:0.0}s; tio en {body.Body.position}; ataques={npc.AttacksStarted} golpes={npc.HitsLanded} " +
                $"caidas jugador={player.FallCount} caidas tio={body.FallCount} correas={cart.StrapCount}");
            Debug.Log("[Autopilot] TIMELINE\n" + string.Join("\n", Timeline));
            StopRun();
            // Cliente con -ueRepeat: espera la ronda nueva que reinicia el anfitrion y vuelve a jugar
            if (repeatLeft > 0 && !NetAuth.IsServer) { repeatLeft--; pendingMode = lastMode; pendingDelay = 0f; Debug.Log($"[Autopilot] cliente espera el reintento (quedan {repeatLeft})"); }
        }
    }
}
