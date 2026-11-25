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
        long raceAtUnixMs; int repeatLeft; AutopilotMode lastMode; bool quitOnEnd; float quitAt = -1f, endSeenAt; bool finalLogged; int endAttempt; RoundState endResult;
        AutopilotMode? pendingMode; float pendingDelay; bool cartNeedsSpace;
        const float FinalMaxWait = 2.0f; // menos que los 3 s en que el anfitrion reinicia o cierra

        /// Linea de comandos de la build: -ueAutopilot drag|cart [-ueQuitOnEnd] (prueba de humo de un intento completo).
        void Start()
        {
#if UE_PLAYER_BUILD
            enabled = false; return; // build de jugadores: sin autopiloto
#endif
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-ueQuitOnEnd") quitOnEnd = true;
                if (args[i] == "-ueHost" || args[i] == "-ueClient" || args[i] == "-ueJoinCode") lobbyBot = true;
                if (args[i] == "-ueExpectPlayers" && i + 1 < args.Length) int.TryParse(args[i + 1], out expectPlayers);
                if (args[i] == "-ueAutopilot" && i + 1 < args.Length)
                    pendingMode = args[i + 1].Equals("cart", StringComparison.OrdinalIgnoreCase) ? AutopilotMode.Cart
                                : args[i + 1].Equals("race", StringComparison.OrdinalIgnoreCase) ? AutopilotMode.Race : AutopilotMode.Drag;
                if (args[i] == "-ueRaceAt" && i + 1 < args.Length) long.TryParse(args[i + 1], out raceAtUnixMs);
                if (args[i] == "-ueRaceNatural") raceNatural = true;
                if (args[i] == "-ueRepeat" && i + 1 < args.Length) int.TryParse(args[i + 1], out repeatLeft);
                if (args[i] == "-ueDropAt" && i + 1 < args.Length) dropAt = args[i + 1];   // prueba: salir del juego en "strap" o "closing"
                if (args[i] == "-ueNoClose") noClose = true;
                if (args[i] == "-ueMenuAtEnd") menuAtEnd = true;                           // prueba: al terminar, volver al menu                               // prueba: este jugador nunca cierra la camioneta
                if (args[i] == "-ueAutopilotDelay" && i + 1 < args.Length) float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pendingDelay);
            }
        }

        /// Procesos de prueba lanzados por linea de comandos (-ueHost/-ueClient): en la sala confirman solos y el anfitrion
        /// inicia cuando estan los jugadores esperados (-ueExpectPlayers, por defecto 2). Con el juego abierto desde el menu
        /// no hace nada: la sala la manejan las personas.
        int expectPlayers = 2; bool lobbyBot; float lobbyLogAt;
        void LobbyStep()
        {
            if (!lobbyBot || !round || !round.InLobby || !PlayerControls.Local) return;
            if (!PlayerControls.Local.Ready) { PlayerControls.Local.SetReady(true); Log("sala: confirma listo"); }
            if (NetAuth.IsServer && RoundDirector.AllReady(out _) && NetAuth.ConnectedPlayers >= expectPlayers && Time.unscaledTime > lobbyLogAt)
            {
                lobbyLogAt = Time.unscaledTime + 1f;
                if (round.StartFromLobby()) Log($"sala: inicia la ronda con {NetAuth.ConnectedPlayers} jugadores");
            }
        }

        void Update()
        {
            CheckFinal(); // tambien despues de un reinicio que llego antes de converger
            LobbyStep();
            // El jugador local aparece cuando la red lo genera (en un cliente, al conectarse).
            // Ronda nueva: esperar 0,5 s de ronda para tener las posiciones reiniciadas replicadas (en un cliente)
            if (pendingMode.HasValue && PlayerControls.Local && Time.timeSinceLevelLoad >= pendingDelay && round && round.State == RoundState.Running && !round.InLobby && (NetAuth.IsServer || round.Elapsed > 0.5f))
            { var m = pendingMode.Value; pendingMode = null; lastMode = m; quitAt = -1f; StartRun(m); }
            if ((quitOnEnd || repeatLeft > 0 || menuAtEnd) && round && round.State != RoundState.Running && !pendingMode.HasValue)
            {   // 3 s de "resultados": los clientes reciben el final antes de que el anfitrion cierre o reinicie
                if (quitAt < 0f)
                {   // el cliente sale antes que el anfitrion: si el anfitrion cierra primero, el cliente veria una desconexion
                    quitAt = Time.unscaledTime + (NetAuth.IsServer ? 3f : 1f); finalLogged = false;
                    endSeenAt = Time.unscaledTime; endAttempt = round.Restarts; endResult = round.State;
                    Debug.Log($"[Autopilot] build: ronda {round.State} en {round.Elapsed:0.0}s; {(repeatLeft > 0 ? "reinicia en 3 s" : "saliendo en 3 s")}");
                    CheckFinal();
                }
                else if (finalLogged && Time.unscaledTime >= quitAt)
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

        /// Estado final por CONVERGENCIA (no por una espera fija): resultado, tio asegurado y puertas coherentes entre si
        /// dentro del mismo intento. Con perdida de paquetes la ronda, la zona y las puertas replican en mensajes distintos.
        /// Si llega el reinicio o vence el plazo, se registra que falto (sin mezclar con la ronda siguiente).
        void CheckFinal()
        {
            if (quitAt < 0f || finalLogged || !round) return;
            string missing = FinalMissing();
            bool restarted = round.Restarts != endAttempt || round.State == RoundState.Running;
            float waited = Time.unscaledTime - endSeenAt;
            if (missing == "" && !restarted) { finalLogged = true; NetSession.LogWorldNow("final", $"convergencia={waited:0.00}s"); }
            else if (restarted) { finalLogged = true; NetSession.LogWorldNow("final", $"convergencia=reinicio({waited:0.00}s) faltan={missing}"); }
            else if (waited >= FinalMaxWait) { finalLogged = true; NetSession.LogWorldNow("final", $"convergencia=TIMEOUT({FinalMaxWait:0.0}s) faltan={missing}"); }
            if (finalLogged && !NetAuth.IsServer) quitAt = Mathf.Max(quitAt, Time.unscaledTime + 0.3f); // el cliente sale despues de registrarlo
        }

        /// Que le falta al estado replicado para ser coherente con el resultado de la ronda ("" = convergio).
        string FinalMissing()
        {
            var faltan = new List<string>();
            var v = VanDoors.Instance;
            if (endResult == RoundState.Success)
            {
                if (!zone || !zone.Secured) faltan.Add("asegurado");
                if (!v || v.CurrentPhase != VanDoors.Phase.Closed) faltan.Add("puertas");
                if (!npc || npc.State != NpcState.Secured) faltan.Add("tio");
            }
            else if (zone && zone.Secured) faltan.Add("asegurado_sin_victoria");
            if (round.State != endResult) faltan.Add("ronda");
            return string.Join(",", faltan);
        }

        public void StartRun(AutopilotMode mode)
        {
            StopRun();
            if (PlayerControls.Local) { player = PlayerControls.Local.GetComponent<PlayerMotor>(); hands = PlayerControls.Local.GetComponent<PlayerHands>(); }
            if (!player || !hands) { Debug.LogWarning("[Autopilot] sin jugador local todavia"); return; }
            Mode = mode; Running = true; Timeline.Clear();
            lastNpc = npc.State; lastFalls = player.FallCount; handsOffUntil = -1f; switchedToAnkles = false; lastTarget = null; cartClearedDoor = false; cartNeedsSpace = false; cartTargetsSet = false; emptyAimFailSince = Time.time; emptyApproachUntil = -1f; cartClearing = false; recenterUntil = -1f; offLineLogged = false; zigLogged = false; stuckSince = Time.time; stuckRef = Vector3.zero;
            aimSince = Time.time; nextReport = Time.time + 10f; idleCheckAt = 0f;
            Log($"inicio modo {mode}: tio en {body.Body.position}, jugador en {player.transform.position}");
            routine = StartCoroutine(mode == AutopilotMode.Drag ? DragRun() : mode == AutopilotMode.Cart ? CartRun() : RaceRun());
        }

        public void StopRun()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null; Running = false; Status = "";
            LabInput.ClearSimulated();
            raceMode = false;
            // sesion 17: la carrera apaga la IA del tio en el anfitrion; al terminar, cancelar o reiniciar se restaura lo que habia
            if (brainSaved) { brainSaved = false; if (npc) npc.brainEnabled = brainBefore; Log($"carrera: IA del tio restaurada ({brainBefore})"); }
        }
        bool brainSaved, brainBefore, raceNatural;

        public void ResetState() => StopRun();

        /// Pruebas: si la ronda sigue en curso al vencer el plazo, detener y volcar la linea de tiempo (sesion 21, UE-18).
        public void DumpIfUnfinished(string what)
        {
            if (!Running) return;
            Log($"sin terminar: {what}");
            Debug.Log($"[Autopilot] TIMELINE (sin terminar: {what})\n" + string.Join("\n", Timeline));
            StopRun();
        }
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
        /// Destino intermedio si el destino esta del otro lado del muro frontal: primero alinearse con la puerta de este lado,
        /// despues cruzarla recto. Sin esto, desde la vereda caminaba en linea recta contra la pared hacia el tio.
        Vector3 Via(Vector3 dest)
        {
            float dz = L.doorZ; bool meOut = Me.z < dz - 0.15f, destOut = dest.z < dz - 0.15f;
            if (meOut == destOut) return dest;
            if (Mathf.Abs(Me.x - L.doorX) > 0.45f) return new Vector3(L.doorX, 0f, dz + (meOut ? -0.9f : 0.9f));
            return new Vector3(L.doorX, 0f, dz + (meOut ? 0.9f : -0.9f));
        }

        /// Desafio «Ruta estrecha»: si el jugador va a cruzar la barricada fuera del paso, primero va al paso (como haria una persona).
        Vector3 ViaNarrowGap(Vector3 dir)
        {
            var ch = RoundDirector.Instance ? RoundDirector.Instance.CurrentChallenge : ChallengeMode.Standard;
            if (ch != ChallengeMode.Narrow || GameObject.Find("Challenge_Narrow") == null) return dir;
            float z = Challenges.BarricadeZ, gx = -2.6f; Vector3 me = Me;
            bool south = me.z < z, crossing = south ? dir.z > 0.05f : dir.z < -0.05f;
            if (!crossing || Mathf.Abs(me.z - z) > 3.5f) return dir;
            if (Mathf.Abs(me.x - gx) < 0.35f) return new Vector3(0f, 0f, south ? 1f : -1f);   // en el paso: derecho
            return new Vector3(gx, 0f, z + (south ? -0.9f : 0.9f)) - me;                     // ir a la boca del paso
        }

        float moveSince, unstickUntil; Vector3 moveFrom, unstickDir; bool raceMode;
        /// Veces que el autopiloto se destrabo en esta corrida (registro de pruebas).
        public int Unsticks { get; private set; }
        void MoveWorld(Vector3 dir)
        {
            dir = Flat(dir); if (dir.sqrMagnitude < 1e-4f) { LabInput.SimulatedMove = Vector2.zero; moveSince = Time.time; moveFrom = Me; return; }
            dir = ViaNarrowGap(dir);
            // sesion 15: destrabarse. Si quiere caminar y en 2,5 s no avanzo 0,15 m (p. ej. contra la hoja entreabierta de la
            // puerta: el fallo intermitente del carrito en el laboratorio articulado), se corre 1 s de costado y reintenta,
            // como haria una persona. Antes se quedaba empujando hasta que se acababa el tiempo.
            if (raceMode) { moveSince = Time.time; moveFrom = Me; } // en la carrera se quedan pegados a proposito
            else if (Time.time < unstickUntil) dir = unstickDir;
            else if (Time.time - moveSince > 2.5f)
            {
                if (Vector3.Distance(Me, moveFrom) < 0.15f)
                {
                    unstickDir = Vector3.Cross(Vector3.up, dir).normalized * (UnityEngine.Random.value < 0.5f ? 1f : -1f) - dir.normalized * 0.4f;
                    unstickUntil = Time.time + 1.0f; Unsticks++; Log($"trabado en {Me} (no avanza): se corre de costado y reintenta");
                }
                moveSince = Time.time; moveFrom = Me;
            }
            Vector3 local = Quaternion.Euler(0, -player.Yaw, 0) * dir.normalized; // relativo a la mirada ordenada (como WASD), no al giro interpolado del cuerpo
            LabInput.SimulatedMove = new Vector2(local.x, local.z);
        }

        void Idle() { SetHold(0, false); SetHold(1, false); LabInput.Simulated[(int)Act.Effort] = false; LabInput.SimulatedMove = Vector2.zero; }

        IEnumerator Tap(Act a) { LabInput.Simulated[(int)a] = true; yield return null; LabInput.Simulated[(int)a] = false; }

        /// De pie: antebrazos (no puede aferrarse a la barra). En el piso: tobillos (el torso queda detras al entrar en la zona).
        GrabSocket BestFreeUncleSocket(GrabSocket exclude, bool nearestOnly = false)
        {
            string pref = pushingIn ? "shoulder" : body.Posture == NpcPosture.Upright ? "forearm" : "ankle"; // acomodar: tirar del torso hacia la cabina
            return GrabSocket.All.Where(s => body.Owns(s.body) && s != exclude && Occupancy(s) < s.capacity && !(unreachable.TryGetValue(s, out float until) && Time.time < until))
                                 .OrderBy(s => (!nearestOnly && s.socketId.StartsWith(pref) ? 0f : 3f) + Vector3.Distance(s.Position, Eye))
                                 .FirstOrDefault();
        }

        Vector3 lastMe, idleMe, idlePel; float lastMeAt, idleCheckAt;
        void Observe()
        {
            // Diagnostico: salto de posicion del jugador (tunel fisico o recolocacion) o cruce de la pared del frente
            if (lastMeAt > 0f && Vector3.Distance(Me, lastMe) > 0.8f) Log($"SALTO jugador {lastMe} -> {Me} en {Time.time - lastMeAt:0.000}s");
            bool wasIn = lastMe.z > L.doorZ, isIn = Me.z > L.doorZ;
            if (lastMeAt > 0f && wasIn != isIn && Mathf.Abs(Me.x - L.doorX) > 0.75f) Log($"CRUCE de pared fuera de la puerta: {lastMe} -> {Me}");
            lastMe = Me; lastMeAt = Time.time;
            if (npc.State != lastNpc) { Log($"tio {lastNpc}->{npc.State} '{npc.Intent}'"); lastNpc = npc.State; }
            if (player.FallCount > lastFalls) { Log($"jugador derribado (caidas={player.FallCount})"); lastFalls = player.FallCount; }
            // sesion 21 (UE-18): diagnostico de quietud. Si en 15 s ni el jugador ni el tio se movieron 0,3 m, registrar en que
            // estaba el autopiloto y que apuntaba (las fallas por tiempo eran 150-250 s quieto sin rastro de por que).
            if (Time.time - idleCheckAt >= 15f)
            {
                if (idleCheckAt > 0f && Vector3.Distance(Me, idleMe) < 0.3f && Vector3.Distance(Flat(body.pelvis.position), idlePel) < 0.3f)
                {
                    var ha = GrabSocket.All.FirstOrDefault(s => s.socketId == "cart_handle");
                    Log($"QUIETO 15 s: estado='{Status}' jugador {Me} pelvis {Flat(body.pelvis.position)} postura={body.Posture} tio={npc.State} " +
                        $"manos={Held(0)?.socketId ?? "-"}/{Held(1)?.socketId ?? "-"} apunta={(hands.Candidate ? hands.Candidate.socketId : "nada")}" +
                        (ha ? $" asa a {Vector3.Distance(Eye, ha.Position):0.00} m" : "") + $" mover={LabInput.SimulatedMove}");
                }
                idleCheckAt = Time.time; idleMe = Me; idlePel = Flat(body.pelvis.position);
            }
            if (Time.time >= nextReport)
            {
                nextReport = Time.time + 10f;
                Log($"posicion: jugador {Me} pelvis {body.pelvis.position} postura={body.Posture} correa={cart.IsStrapped} dentro={zone.AllPointsInside} " +
                    $"carrito {Flat(cart.transform.position)} yaw={cart.transform.eulerAngles.y:0} manos={Held(0)?.socketId ?? "-"}/{Held(1)?.socketId ?? "-"} puerta={(door ? door.angle : 0f):0}deg estado='{Status}'");
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
                MoveWorld(near ? Vector3.zero : Via(hp) - player.transform.position);
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
            MoveWorld(flat > stopAt ? Via(target.Position) - player.transform.position : Vector3.zero);
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
                    MoveWorld(near ? Vector3.zero : Via(second.Position) - player.transform.position);
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
            if (!van.InRange(Me))
            {   // quedo lejos de la abertura (empujado o al salir): acercarse a un punto de cierre libre
                Vector3 st = L.CloseStand(side);
                foreach (var pm in PlayerMotor.All) if (pm && pm != player && Vector3.Distance(Flat(pm.transform.position), st) < 0.7f) st = L.CloseStand(-side);
                LabInput.Simulated[(int)Act.Interact] = false; LookAt(van.closePoint); MoveWorld(st - Me); yield return null; yield break;
            }
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
            return why.StartsWith("van.part_outside") || why == "van.stuck";
        }

        /// Carrera por un mismo punto (especificacion 05): dos jugadores apuntan al cinturon y aprietan en el mismo instante
        /// de reloj (-ueRaceAt, ms Unix). El anfitrion debe crear un solo agarre y responder SocketBusy al otro.
        IEnumerator RaceRun()
        {
            var belt = GrabSocket.ById("belt");
            bool second = player.Controls && player.Controls.IsSpawned && player.Controls.OwnerClientId % 2 == 1;
            // sesion 16: los dos se paran FRENTE AL CINTURON segun hacia donde mira el tio ahora (no en puntos fijos del mapa)
            // y no aprietan hasta tener el cinturon como candidato: asi la prueba mide de verdad la disputa por el mismo punto
            // la prueba mide la disputa por un punto, no la IA: en el anfitrion el tio queda quieto durante la carrera
            // sesion 17 (-ueRaceNatural): caso de JUEGO NORMAL, sin reubicar jugadores y con la IA del tio activa (se corre con
            // latencia simulada). Cada uno se acerca desde donde esta; se registran las aproximaciones fallidas aparte.
            if (NetAuth.IsServer && npc && !raceNatural) { brainBefore = npc.brainEnabled; brainSaved = true; npc.brainEnabled = false; }
            Log($"carrera: modo={(raceNatural ? "natural" : "escenificada")} ia_tio={(npc ? npc.brainEnabled : false)}");
            Vector3 StandNow()
            {
                Vector3 toward = Flat(belt.Position - body.Body.position); if (toward.sqrMagnitude < 1e-3f) toward = body.Forward; toward.Normalize();
                return Flat(belt.Position) + toward * 0.7f + Vector3.Cross(Vector3.up, toward) * (second ? 0.36f : -0.36f);
            }
            Vector3 stand = StandNow();
            raceMode = true;
            long now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            while (now() < raceAtUnixMs)
            {   // hasta la señal: pararse frente al cinturon (recalculado si el cuerpo se mueve) y apuntarle. El ANFITRION, que tiene
                // la autoridad, ubica a los dos jugadores en sus lugares (si no, uno le tapaba el camino al otro y apuntaba a otra parte)
                stand = StandNow();
                if (NetAuth.IsServer && !raceNatural)
                    foreach (var pm in PlayerMotor.All)
                    {
                        if (!pm || !pm.Controls) continue;
                        bool sec = pm.Controls.OwnerClientId % 2 == 1;
                        Vector3 tw = Flat(belt.Position - body.Body.position); if (tw.sqrMagnitude < 1e-3f) tw = body.Forward; tw.Normalize();
                        Vector3 p = Flat(belt.Position) + tw * 0.7f + Vector3.Cross(Vector3.up, tw) * (sec ? 0.36f : -0.36f);
                        pm.Body.position = p; pm.transform.position = p; pm.Body.linearVelocity = Vector3.zero;
                    }
                if (Vector3.Distance(Me, stand) > 0.15f) MoveWorld(stand - Me); else LabInput.SimulatedMove = Vector2.zero;
                LookAt(belt.Position);
                Status = hands.Candidate == belt ? "esperando la senal (apunta al cinturon)" : "yendo al cinturon";
                yield return null;
            }
            LabInput.SimulatedMove = Vector2.zero;
            raceMode = false;
            if (hands.Candidate != belt) Log($"aproximacion fallida: al llegar la senal el candidato era {(hands.Candidate ? hands.Candidate.socketId : "-")} (dist {Vector3.Distance(Eye, belt.Position):0.00})");
            Log($"aprieta en {now()} (senal {raceAtUnixMs}); candidato={(hands.Candidate ? hands.Candidate.socketId : "-")} yo={Me} destino={stand} cinturon={belt.Position} dist={Vector3.Distance(Eye, belt.Position):0.00}");
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
        float unjamUntil;
        float handleBestDist = 99f, handleProgressAt, handleAroundUntil, handleLastSeen = -99f; Vector3 handleAround;
        bool realignLogged; float cartBestDist = 99f, cartProgressAt; float handleAimFailSince, handleSidestepUntil, handlePhaseSeen = -99f, handleSide = 1f; bool handleOccludedLogged; float emptyAimFailSince, emptyApproachUntil = -1f, emptyPhaseSeen = -99f; float emptySide = 1f; Vector3 emptyGoal;
        /// Punto libre a ~0,9 m del asa (a un costado del carrito o frente al asa), del mismo lado de la puerta del bar que el
        /// carrito; prefiere el lado pedido y, si no esta libre, cualquiera de los otros.
        Vector3 HandleSideSpot(GrabSocket h, float side)
        {
            Vector3 hf = Flat(h.Position), across = Flat(cart.transform.right).normalized, back = Flat(-cart.transform.forward).normalized;
            bool cartIn = cart.transform.position.z > L.doorZ;
            bool Ok(Vector3 q) => (q.z > L.doorZ + 0.3f) == cartIn && !Physics.CheckCapsule(q + Vector3.up * 0.4f, q + Vector3.up * 1.4f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var q in new[] { hf + across * 0.9f * side, hf - across * 0.9f * side, hf + back * 0.9f, hf + across * 0.9f * side + back * 0.5f, hf - across * 0.9f * side + back * 0.5f })
                if (Ok(q)) return q;
            return hf + across * 0.9f * side;
        }

        IEnumerator CartRun()
        {
            handleAimFailSince = Time.time;
            cartNeedsSpace = Vector3.Distance(Flat(cart.transform.position), OpenSpot) > 0.6f; // primero llevar el carrito vacio al punto de carga
            int cartWp = 0;
            while (!Finished)
            {
                if (CommonStep()) { yield return null; continue; }
                // Sesion 21 (UE-18): el tio se solto del carrito ya adentro y el carrito vacio quedo en la abertura ("objeto en
                // la puerta"). Antes alternaba entre cerrar y llevar el carrito al bar sin sacarlo nunca (NoCart, 70+ s).
                // Como una persona: tomar el asa, empujarlo hacia afuera hasta pasar la abertura, soltarlo y cerrar.
                if (CartBlocksDoorway())
                {   // vacio: sacarlo hacia afuera; cargado con el tio ya adentro (C3): empujarlo mas adentro de la caja
                    bool loaded = cart.IsStrapped;
                    Status = loaded ? "metiendo el carrito cargado" : "sacando el carrito de la abertura";
                    if (!cartClearing) { cartClearing = true; Log(loaded ? $"el carrito cargado asoma por la abertura en {Flat(cart.transform.position)}: lo empuja mas adentro" : $"el carrito vacio tapa la abertura en {Flat(cart.transform.position)}: lo saca hacia afuera"); }
                    var hClear = GrabSocket.All.First(s => s.socketId == "cart_handle");
                    if (!HoldingCart()) { if (Holding(0) || Holding(1)) { Idle(); yield return Tap(Act.ReleaseAll); continue; } ApproachHandle(hClear); yield return null; continue; }
                    Vector3 cartC = Flat(cart.transform.position), cartOut = new Vector3(L.vanAxisX, 0f, loaded ? L.vanRearZ - 2.0f : L.vanRearZ + 2.6f);
                    LookAt(hClear.Position); LabInput.Simulated[(int)Act.Effort] = true;
                    MoveWorld(cartOut - cartC);
                    yield return null; continue;
                }
                if (cartClearing)
                {
                    cartClearing = false; Log($"carrito fuera de la abertura en {Flat(cart.transform.position)}: lo suelta y vuelve a cerrar");
                    if (HoldingCart()) { Idle(); yield return Tap(Act.ReleaseAll); continue; }
                }
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
                        // sesion 21 (UE-18): al alcance pero sin poder apuntar al asa (el marco de la puerta la tapa) se quedaba
                        // quieto para siempre. Pasado 1 s, usar 3 s el acercamiento del carrito cargado (rodear, correrse de costado).
                        if (!reach || hands.Candidate == h || Time.time - emptyPhaseSeen > 0.25f) emptyAimFailSince = Time.time; // o recien entra en esta fase
                        emptyPhaseSeen = Time.time;
                        if (Time.time - emptyAimFailSince > 1f && Time.time >= emptyApproachUntil)
                        {   // ir a un punto libre al costado del asa, del mismo lado de la puerta que el carrito (desde afuera, con el
                            // asa en el marco, "detras del asa" quedaba afuera y se corria de costado en el umbral para siempre)
                            emptyAimFailSince = Time.time; emptyApproachUntil = Time.time + 4f; emptySide = -emptySide;
                            emptyGoal = HandleSideSpot(h, emptySide);
                            Log($"asa del carrito vacio sin linea de vision desde {Me} (mira: {(hands.Candidate ? hands.Candidate.socketId : "nada")}): va al costado del asa {emptyGoal}");
                        }
                        if (Time.time < emptyApproachUntil && Vector3.Distance(Me, emptyGoal) > 0.3f)
                        { LookAt(h.Position); MoveWorld(Via(emptyGoal) - player.transform.position); SetHold(0, false); SetHold(1, false); yield return null; continue; }
                        if (Time.time < emptyApproachUntil) emptyApproachUntil = Time.time; // llego: volver a apuntar desde ahi
                        MoveWorld(reach ? Vector3.zero : Via(h.Position) - player.transform.position);
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
                    // Sesion 21 (UE-18): si el tio (forcejeando) mando lejos el carrito vacio, la ruta de arrastre ya no pasa
                    // junto a el y el bot terminaba llevando al tio a mano hasta la camioneta: ganaba sin la correa (la prueba
                    // de modo carrito lo detecta). Como un jugador con plan de carrito: soltar, volver a poner el carrito en el
                    // lugar abierto y retomar.
                    if (!cartNeedsSpace && Vector3.Distance(Flat(cart.transform.position), OpenSpot) > 1.8f
                        && Vector3.Distance(Flat(body.pelvis.position), Flat(cart.transform.position)) > 3.0f && Me.z > L.doorZ)
                    {
                        cartNeedsSpace = true; cartTargetsSet = false;
                        Log($"el carrito quedo lejos del lugar abierto en {Flat(cart.transform.position)} (tio en {Flat(body.pelvis.position)}): lo vuelve a acercar");
                        Idle(); yield return Tap(Act.ReleaseAll); continue;
                    }
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
                            if (!hands.LastFeedback.Contains("blocker.player")) cartNeedsSpace = true; // pared u objeto: mover el carrito a un lugar abierto
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
                    // Sin acercarse al objetivo en 6 s (el tio cayo del otro lado del carrito): recalcular desde donde esta ahora.
                    float dTarget = cartTargetsSet ? Vector3.Distance(pel, cartUncleTarget) : 99f;
                    if (dTarget < cartBestDist - 0.3f) { cartBestDist = dTarget; cartProgressAt = Time.time; }
                    bool nearCart = Vector3.Distance(pel, c) < 3.0f;                 // solo en el tramo final junto al carrito
                    if (!nearCart) cartProgressAt = Time.time;
                    bool stalled = cartTargetsSet && nearCart && Time.time - cartProgressAt > 6f;
                    if (stalled) Log($"sin avanzar hacia el carrito desde {pel}: recalcula el punto de carga");
                    if (!cartTargetsSet || Vector3.Distance(c, cartRef) > 0.6f || stalled)
                    {
                        cartBestDist = 99f; cartProgressAt = Time.time;
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
                    ApproachHandle(handle);
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
                // Sesion 21 (UE-18): una sacudida mando el carrito cargado a la vereda y el bot fue derecho al ultimo punto (dentro
                // de la camioneta): termino caminando por afuera, al costado de la caja, 120 s. Fuera de la linea de la camioneta,
                // volver detras de ella y entrar por la rampa.
                bool besideVan = Me.z < L.vanRearZ + 0.8f && Mathf.Abs(Me.x - L.vanAxisX) > 1.35f;
                bool cartOffLine = cartWp == CartPullPath.Length - 1 && cc.z > L.vanRearZ - 0.3f && Mathf.Abs(cc.x - L.vanAxisX) > 0.9f;
                if (besideVan || cartOffLine)
                {
                    if (!offLineLogged) { offLineLogged = true; Log($"fuera de la linea de la camioneta (jugador {Me}, carrito {cc}): vuelve detras de la rampa"); }
                    cartWp = Mathf.Min(cartWp, CartPullPath.Length - 2);
                    goal = besideVan ? new Vector3(Me.x, 0f, L.vanRearZ + 2.2f) : CartPullPath[cartWp];
                }
                else offLineLogged = false;
                // Mientras el carrito no cruzo la puerta (z = -2), tirar recto por el centro: en diagonal se traba en el marco.
                if (cc.z < L.doorZ - 2.0f) cartClearedDoor = true;          // carrito + tio que sobresale, del todo afuera
                else if (cc.z > L.doorZ + 1.0f) cartClearedDoor = false;
                if (!cartClearedDoor && Me.z < L.doorZ + 2.8f)
                {   // antes de la puerta, recentrar el carrito en x; dentro del marco, tirar recto
                    float gx = L.doorX + (cc.z > L.doorZ + 1.4f || Mathf.Abs(cc.x - L.doorX) > 0.35f ? Mathf.Clamp(L.doorX - cc.x, -0.6f, 0.6f) : 0f); // tambien si quedo corrido del eje contra la pared (solo_unjam)
                    goal = new Vector3(gx, 0f, Mathf.Max(Me.z - 1.0f, L.doorZ - 4.5f));
                }

                float gap = Vector3.Distance(Me, Flat(handle.Position));
                LookAt(handle.Position);
                LabInput.Simulated[(int)Act.Effort] = gap > 1.1f;
                // de espaldas hacia el punto; si el carrito se queda atras, esperar a que el resorte lo traiga
                // Destrabar: si el carrito no avanza en 2 s, zigzaguear de costado (como sacudir un changuito trabado).
                if (Vector3.Distance(cc, stuckRef) > 0.15f) { stuckRef = cc; stuckSince = Time.time; zigLogged = false; }
                // Carrito atravesado (la hoja de la puerta o una pared lo giro): realinearlo tirando del asa hacia adelante, en la
                // direccion de la ruta, como un changuito; el zigzag lo terminaba de girar 90 grados contra la pared.
                Vector3 pullDir = Flat(goal - cc); Vector3 cartBack = Flat(-cart.transform.forward);
                float skew = pullDir.sqrMagnitude > 0.25f ? Vector3.Angle(cartBack, pullDir) : 0f;
                // Solo atravesado de verdad (empujarlo de frente tambien sirve), ya fuera del marco y mientras avance.
                if (cc.z < L.doorZ - 1.2f && skew > 50f && skew < 130f && Time.time - stuckSince <= 2f)
                {
                    if (!realignLogged) { realignLogged = true; Log($"carrito atravesado en {cc} ({skew:0} grados): lo realinea"); }
                    Vector3 lead = cc + pullDir.normalized * 1.7f;
                    if (gap > 1.25f) LabInput.SimulatedMove = Vector2.zero; else MoveWorld(lead - Me);
                    yield return null; continue;
                }
                realignLogged = false;
                // Trabado en el marco con el tiro recto (StuckRecoveryTests, pose lab_fail_m28): como un jugador, empujarlo
                // hacia adentro ~1,8 s y volver a tirar alineado con la puerta (el objetivo de la ruta ya recentra en x).
                if (Time.time < unjamUntil)
                {
                    LookAt(handle.Position); LabInput.Simulated[(int)Act.Effort] = true; MoveWorld(Flat(cc - Me));
                    yield return null; continue;
                }
                if (Time.time - stuckSince > 3f && Mathf.Abs(cc.z - L.doorZ) <= 1.6f)
                {
                    unjamUntil = Time.time + 1.8f; stuckSince = Time.time; stuckRef = cc;
                    Log($"carrito trabado en la puerta en {cc} (yaw {cart.transform.eulerAngles.y:0}): lo empuja hacia adentro y vuelve a tirar alineado");
                    yield return null; continue;
                }
                // Sesion 21 (UE-18): entrando en la camioneta corrido del eje, el carrito raspa la pared de la caja y avanza
                // 1 cm por segundo; el zigzag lo trababa mas (NoCart, 150+ s). Recentrarlo: correrse hacia el eje tirando.
                bool inVan = cc.z < L.vanRearZ + 0.6f && Mathf.Abs(cc.x - L.vanAxisX) > 0.3f && Mathf.Abs(cc.x - L.vanAxisX) < 1.2f;
                if (inVan && (Time.time < recenterUntil || Time.time - stuckSince > 2f))
                {
                    if (Time.time >= recenterUntil) { recenterUntil = Time.time + 2f; stuckSince = Time.time; Log($"carrito rozando en la camioneta en {cc}: lo recentra hacia el eje"); }
                    LabInput.Simulated[(int)Act.Effort] = true;
                    MoveWorld(new Vector3(L.vanAxisX - (cc.x - L.vanAxisX) * 0.6f, 0f, Mathf.Max(Me.z - 0.4f, L.vanCabZ + 0.45f)) - Me);
                    yield return null; continue;
                }
                // Junto a la puerta no zigzaguear: tirar recto por el eje la saca aun atravesada (StuckRecoveryTests, poses de
                // 45 y 90 grados); el zigzag era lo que terminaba de girarla contra la pared.
                if (Time.time - stuckSince > 2f && Mathf.Abs(cc.z - L.doorZ) > 1.6f)
                {
                    float phase = Mathf.Repeat(Time.time - stuckSince - 2f, 2.4f);
                    if (!zigLogged) { zigLogged = true; Log($"carrito trabado en {cc}: zigzag"); }
                    Vector3 side = Vector3.Cross(Vector3.up, Flat(cc - Me)).normalized * (phase < 1.2f ? 1f : -1f);
                    MoveWorld(side + Flat(Me - cc).normalized * 0.5f);
                    if (Time.time - stuckSince > 12f) { stuckSince = Time.time; stuckRef = cc; }
                }
                else if (gap > 1.25f && Time.time - stuckSince > 1.5f) MoveWorld(Flat(handle.Position) - Me); // el carrito no viene: acercarse al asa y volver a tirar (pt_ba4: 150 s quieto esperando)
                else if (gap > 1.25f || Vector3.Distance(Me, goal) < 0.2f) LabInput.SimulatedMove = Vector2.zero;
                else MoveWorld(goal - Me);
                yield return null;
            }
            Finish();
        }

        /// Un frame de acercarse al asa del carrito y tomarla con las dos manos, como un jugador: por detras si esta libre,
        /// rodeando si no se acerca, y corriendose de costado si al alcance no la puede apuntar (algo tapa la linea de vision).
        /// Lo usan el traslado del carrito vacio y el tiro del carrito cargado.
        void ApproachHandle(GrabSocket handle)
        {
            Vector3 behind = cart.transform.TransformPoint(new Vector3(0, 0, -1.5f));
            if (Time.time < handleSidestepUntil)
            {
                Vector3 toH = Flat(handle.Position) - Me; Vector3 sideDir = Vector3.Cross(Vector3.up, toH.sqrMagnitude > 1e-4f ? toH.normalized : Vector3.forward) * handleSide;
                LookAt(handle.Position); MoveWorld(sideDir); SetHold(0, false); SetHold(1, false); return;
            }
            // Al alcance pero sin linea de vision al asa (el tio acostado la tapa desde adelante): rodear hasta atras.
            if (Time.time - handlePhaseSeen > 0.25f) handleAimFailSince = Time.time; // recien entra en esta fase
            handlePhaseSeen = Time.time;
            if (hands.Candidate == handle) handleAimFailSince = Time.time;              // la ve: no esta tapada
            bool occluded = Time.time - handleAimFailSince > 0.8f;
            // Sin acercarse al asa en 1,5 s (el carrito en el medio, con el asa contra una pared): rodear hasta un costado
            // libre del asa. Antes caminaba contra el carrito para siempre (pt_s8, 2026-11-16: 280 s quieto).
            float dH = Vector3.Distance(Me, Flat(handle.Position));
            if (dH < handleBestDist - 0.1f || Time.time - handleLastSeen > 0.5f) { handleBestDist = dH; handleProgressAt = Time.time; }
            handleLastSeen = Time.time;
            if (Time.time < handleAroundUntil) { LookAt(handle.Position); MoveWorld(handleAround - Me); SetHold(0, false); SetHold(1, false); return; }
            if (dH > LabBalance.GrabReach - 0.3f && Time.time - handleProgressAt > 1.5f)
            {
                Vector3 hf = Flat(handle.Position), across = Flat(cart.transform.right).normalized;
                Vector3 a = hf + across * 0.9f, b = hf - across * 0.9f;
                bool Free(Vector3 q) => !Physics.CheckCapsule(q + Vector3.up * 0.4f, q + Vector3.up * 1.4f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
                handleAround = Free(a) && (!Free(b) || Vector3.Distance(Me, a) <= Vector3.Distance(Me, b)) ? a : b;
                handleAroundUntil = Time.time + 2.5f; handleProgressAt = Time.time; handleBestDist = 99f;
                Log($"no se acerca al asa desde {Me}: rodea el carrito hasta {handleAround}");
            }
            if (Vector3.Distance(Eye, handle.Position) > LabBalance.GrabReach - 0.2f || occluded)
            {   // acercarse por detras del asa si ese lado esta libre; si no (pared), directo al asa
                bool behindFree = !Physics.CheckCapsule(behind + Vector3.up * 0.4f, behind + Vector3.up * 1.4f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
                Vector3 dest = behindFree && Vector3.Distance(Me, Flat(behind)) > 0.5f ? behind : handle.Position;
                if (!occluded) handleAimFailSince = Time.time; // todavia lejos: el plazo cuenta desde que llega al alcance
                else if (Vector3.Distance(Me, Flat(behind)) < 0.6f || !behindFree)
                {   // ya rodeo (o no se puede) y sigue sin verla: correrse de costado, alternando, y volver a apuntar
                    handleAimFailSince = Time.time; handleSide = -handleSide; handleSidestepUntil = Time.time + 0.7f;
                    Log($"asa sin linea de vision desde {Me} (mira: {(hands.Candidate ? hands.Candidate.socketId : "nada")}): se corre de costado");
                }
                else if (!handleOccludedLogged) { handleOccludedLogged = true; Log($"asa tapada desde {Me}: rodea hasta {Flat(dest)}"); }
                LookAt(handle.Position); MoveWorld(Via(dest) - player.transform.position); SetHold(0, false); SetHold(1, false); return; }
            LookAt(handle.Position); LabInput.SimulatedMove = Vector2.zero;
            bool aimed = hands.Candidate == handle;
            if (aimed) { handleAimFailSince = Time.time; handleOccludedLogged = false; }
            SetHold(0, aimed); SetHold(1, aimed);
        }

        bool cartClearing, offLineLogged; float recenterUntil = -1f;
        /// El carrito vacio impide cerrar: la camioneta lo informa como objeto en la puerta; una vez empezado, seguir hasta que
        /// el carrito pase 1,5 m de la abertura (si no, al soltarlo vuelve a rodar adentro).
        bool CartBlocksDoorway()
        {
            var v = VanDoors.Instance; if (!v || !zone || zone.Secured) return false;
            string why = v.BlockReason();
            if (why.StartsWith("van.object_in_door") && why.Contains(cart.name)) return true;
            Vector3 c = Flat(cart.transform.position);
            return cartClearing && !cart.IsStrapped && c.z < L.vanRearZ + 1.5f && Mathf.Abs(c.x - L.vanAxisX) < 1.6f;
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
