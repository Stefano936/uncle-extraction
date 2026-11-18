using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace UncleExtraction
{
    /// Dos manos independientes. Mantener boton = agarrar/mantener (GDD 04). Las uniones las valida GrabRegistry (solo anfitrion).
    /// Red: el anfitrion decide agarres, esfuerzo, correa y desanclaje con la intencion del jugador; el estado de cada mano
    /// se replica para HUD, manos visibles y autopiloto de clientes. El dueno calcula localmente el punto apuntado (solo UI).
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerHands : NetworkBehaviour, IGrabOwner
    {
        public Camera cam;
        public Transform leftHandVisual, rightHandVisual;
        public float aimConeDeg = 22f;
        public Vector3 holdOffset = new Vector3(0.28f, -0.3f, 0.75f); // x se refleja por mano

        readonly NetworkVariable<FixedString32Bytes> nHeldL = new NetworkVariable<FixedString32Bytes>();
        readonly NetworkVariable<FixedString32Bytes> nHeldR = new NetworkVariable<FixedString32Bytes>();
        readonly NetworkVariable<float> nTensionL = new NetworkVariable<float>(), nTensionR = new NetworkVariable<float>();
        readonly NetworkVariable<bool> nEffort = new NetworkVariable<bool>();
        readonly NetworkVariable<FixedString64Bytes> nInteract = new NetworkVariable<FixedString64Bytes>();
        readonly NetworkVariable<float> nInteractProgress = new NetworkVariable<float>();

        PlayerMotor motor; Rigidbody rb; PlayerControls controls;
        readonly List<GrabLink> broken = new List<GrabLink>();
        float effortTime;
        Renderer leftR, rightR; readonly HandRig[] rigs = new HandRig[2];
        public HandRig Rig(int hand) => rigs[hand];
        public PlayerControls Controls => controls;
        bool effortActive; string interactLabel = ""; float interactProgress;

        bool Auth => NetAuth.IsServer;
        bool Mirror => IsSpawned && !IsServer;

        public string OwnerName => motor ? motor.playerName : name;
        public bool CanGrab => motor && motor.ControlsEnabled;
        public bool AnyHeld => HeldSocketId(0) != "" || HeldSocketId(1) != "";
        public bool EffortActive => Mirror ? nEffort.Value : effortActive;
        public GrabSocket Candidate { get; private set; }
        /// Punto apuntado (en el cono y a la vista) pero fuera de alcance, hasta 1,5 m mas alla: solo para el aviso «acercate».
        public GrabSocket AimedOutOfReach { get; private set; }
        public Transform HandVisual(int hand) => hand == 0 ? leftHandVisual : rightHandVisual;

        /// Ocupacion de un punto: en el anfitrion, el registro; en un cliente, las manos replicadas de todos los jugadores.
        public static int OccupancyOf(GrabSocket s)
        {
            if (!s) return 0;
            if (NetAuth.IsServer) return GrabRegistry.OccupancyOf(s);
            int n = 0;
            foreach (var pm in PlayerMotor.All) { if (!pm) continue; var h = pm.GetComponent<PlayerHands>(); if (!h) continue; for (int k = 0; k < 2; k++) if (h.HeldSocketId(k) == s.socketId) n++; }
            return n;
        }
        /// Nombre de OTRO jugador que tiene ese punto (para «lo tiene Ana»); "" si no hay o es uno mismo. Vale en anfitrion y
        /// clientes (manos replicadas). Sesion 17.
        public static string HolderName(GrabSocket s, PlayerHands except)
        {
            if (!s) return "";
            foreach (var pm in PlayerMotor.All) { if (!pm) continue; var h = pm.GetComponent<PlayerHands>(); if (!h || h == except) continue; for (int k = 0; k < 2; k++) if (h.HeldSocketId(k) == s.socketId) return h.OwnerName; }
            return "";
        }
        public string LastFeedback { get; private set; } = "";
        public float FeedbackUntil { get; private set; }
        public float InteractProgress01 => Mirror ? nInteractProgress.Value : interactProgress;
        public string InteractLabel => Mirror ? nInteract.Value.ToString() : interactLabel;

        /// Botones simulados para pruebas en el anfitrion (null = controles del jugador).
        public bool? DebugHoldLeft, DebugHoldRight; public bool DebugEffort;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>(); rb = GetComponent<Rigidbody>(); controls = GetComponent<PlayerControls>();
            if (!cam) cam = GetComponentInChildren<Camera>();
            if (leftHandVisual) leftR = leftHandVisual.GetComponent<Renderer>();
            if (rightHandVisual) rightR = rightHandVisual.GetComponent<Renderer>();
            // manos con forma (sesion 13): palma, dedos y puño de manga; el cubo del prefab queda oculto
            if (leftHandVisual && !leftHandVisual.GetComponentInChildren<HandRig>()) rigs[0] = HandRig.Build(leftHandVisual, 0);
            if (rightHandVisual && !rightHandVisual.GetComponentInChildren<HandRig>()) rigs[1] = HandRig.Build(rightHandVisual, 1);
        }

        /// Id del punto que sostiene una mano ("" = libre). En clientes viene replicado del anfitrion.
        public string HeldSocketId(int hand)
        {
            if (Mirror) return (hand == 0 ? nHeldL : nHeldR).Value.ToString();
            var l = GrabRegistry.Find(this, hand);
            return l != null ? l.socket.socketId : "";
        }
        public GrabSocket HeldSocket(int hand) => GrabSocket.ById(HeldSocketId(hand));
        public float Tension01(int hand)
        {
            if (Mirror) return (hand == 0 ? nTensionL : nTensionR).Value;
            var l = GrabRegistry.Find(this, hand); return l != null ? l.tension01 : 0f;
        }

        public Vector3 HandOrigin(int hand) => cam ? cam.transform.position : transform.position + Vector3.up * 1.6f;

        public Vector3 HandTarget(int hand)
        {
            Vector3 o = holdOffset; if (hand == 0) o.x = -o.x;
            // Desde la mirada ordenada y no desde el transform de la camara: la camara se orienta una vez por cuadro y, con
            // dos pasos de fisica en un mismo cuadro (menos de 60 FPS), la mano apuntaba mas alla del giro (medido a 30 FPS).
            if (motor) return motor.EyePosition + motor.LookRotation * o;
            var t = cam ? cam.transform : transform;
            return t.TransformPoint(o);
        }

        bool Ctrl(Act a) => controls != null ? controls.Held(a) : LabInput.HeldForGameplay(a);
        bool CtrlPressed(Act a) => controls != null ? controls.Pressed(a) : LabInput.Pressed(a);

        void Update()
        {
            Candidate = FindCandidate();
            if (Auth)
            {
                if (motor.ControlsEnabled)
                {
                    if (CtrlPressed(Act.ReleaseAll)) ReleaseAll(ReleaseReason.ReleaseAll);
                    HandleHand(0, DebugHoldLeft ?? Ctrl(Act.GrabLeft));
                    HandleHand(1, DebugHoldRight ?? Ctrl(Act.GrabRight));
                    HandleInteract();
                }
                else interactProgress = 0f;
                Publish();
            }
            UpdateVisuals();
        }

        void Publish()
        {
            if (!IsSpawned || !IsServer) return;
            var l = new FixedString32Bytes(HeldSocketId(0)); if (!nHeldL.Value.Equals(l)) nHeldL.Value = l;
            var r = new FixedString32Bytes(HeldSocketId(1)); if (!nHeldR.Value.Equals(r)) nHeldR.Value = r;
            float tl = Tension01(0), tr = Tension01(1);
            if (Mathf.Abs(nTensionL.Value - tl) > 0.05f) nTensionL.Value = tl;
            if (Mathf.Abs(nTensionR.Value - tr) > 0.05f) nTensionR.Value = tr;
            if (nEffort.Value != effortActive) nEffort.Value = effortActive;
            var il = new FixedString64Bytes(interactLabel); if (!nInteract.Value.Equals(il)) nInteract.Value = il;
            if (Mathf.Abs(nInteractProgress.Value - interactProgress) > 0.02f || (interactProgress == 0f && nInteractProgress.Value != 0f)) nInteractProgress.Value = interactProgress;
        }

        void HandleHand(int hand, bool held)
        {
            var link = GrabRegistry.Find(this, hand);
            if (!held) { if (link != null) GrabRegistry.Release(link.id, ReleaseReason.Released); return; }
            if (link != null) return;
            // Mantener sin agarre: reintenta cada frame (agarre al pasar sobre un punto valido).
            // El anfitrion recalcula el candidato con la mirada recibida: no confia en un socket elegido por el cliente.
            var c = Candidate;
            if (c == null) return;
            var r = GrabRegistry.TryCreate(this, hand, c, out _);
            if (r != GrabResult.Ok && r != GrabResult.HandBusy) Feedback(r == GrabResult.SocketBusy ? "hud.grip_busy" : r == GrabResult.OutOfReach ? "hud.too_far" : r.ToString());
        }

        /// API directa (pruebas y depuracion en el anfitrion): agarra un socket concreto con una mano.
        public GrabResult ForceGrab(int hand, GrabSocket s) => GrabRegistry.TryCreate(this, hand, s, out _);

        void Feedback(string msg)
        {
            LastFeedback = msg; FeedbackUntil = Time.time + 1.2f;
            if (IsSpawned && IsServer && !IsOwner) FeedbackRpc(msg);
        }

        [Rpc(SendTo.Owner)]
        void FeedbackRpc(FixedString64Bytes msg) { LastFeedback = msg.ToString(); FeedbackUntil = Time.time + 1.2f; }

        GrabSocket FindCandidate()
        {
            if (!cam) return null;
            Vector3 o = cam.transform.position, f = cam.transform.forward;
            GrabSocket best = null; float bestScore = float.MaxValue;
            GrabSocket far = null; float farScore = float.MaxValue;
            string h0 = HeldSocketId(0), h1 = HeldSocketId(1);
            foreach (var s in GrabSocket.All)
            {
                // un punto que ya sostiene la otra mano no compite: sin esto no se podia elegir un segundo punto cercano
                // (el hombro ya tomado le ganaba al otro hombro). El asa del carrito (capacidad 2) sigue disponible.
                if (s.capacity < 2 && (s.socketId == h0 || s.socketId == h1)) continue;
                Vector3 to = s.Position - o; float d = to.magnitude;
                if (d > LabBalance.GrabReach + 1.5f || d < 0.05f) continue;
                float ang = Vector3.Angle(f, to);
                if (ang > aimConeDeg) continue;
                if (Blocked(o, s)) continue;
                float score = ang + d * 4f;
                if (d > LabBalance.GrabReach) { if (score < farScore) { farScore = score; far = s; } continue; }
                if (score < bestScore) { bestScore = score; best = s; }
            }
            AimedOutOfReach = best ? null : far;
            return best;
        }

        static readonly RaycastHit[] hits = new RaycastHit[8];
        bool Blocked(Vector3 o, GrabSocket s)
        {
            Vector3 to = s.Position - o; float d = to.magnitude;
            int n = Physics.RaycastNonAlloc(o, to / d, hits, d - 0.05f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var hrb = hits[i].collider.attachedRigidbody;
                if (hrb == rb || hrb == s.body) continue;
                var owner = NpcBody.Of(s.body); if (owner && owner.Owns(hrb)) continue; // otras partes del mismo tio no tapan
                return true;
            }
            return false;
        }

        void FixedUpdate()
        {
            if (!Auth) return;
            bool wantsEffort = (DebugEffort || Ctrl(Act.Effort)) && AnyHeld && motor.ControlsEnabled;
            if (wantsEffort && effortTime < LabBalance.EffortMax && motor.ConsumeEffort(Time.fixedDeltaTime)) { effortTime += Time.fixedDeltaTime; effortActive = true; }
            else { effortActive = false; if (!wantsEffort) effortTime = 0f; }

            broken.Clear();
            GrabSpring.ApplyOwner(this, rb, HandTarget, effortActive, Time.fixedDeltaTime, broken);
            foreach (var l in broken) { GrabRegistry.Release(l.id, ReleaseReason.Distance); Feedback("grab.lost_distance"); }
        }

        public enum InteractKind { None, HelpUp, Unanchor, Van, Cart, Bell }
        /// Objetivo de E de este cuadro (pruebas y aviso).
        public InteractKind InteractTarget { get; private set; }
        InteractKind sticky; PlayerMotor stickyMate;

        /// Regla de prioridad de E (sesion 14; probada en EditMode). Orden: compañero caido que se mira > mano anclada que
        /// se mira > camioneta (tio adentro, desde afuera) > carrito cercano. Mientras se MANTIENE E sobre un objetivo que sigue
        /// valido, no se cambia a otro aunque aparezca uno de mas prioridad: el progreso no se corta por mirar a otro lado.
        public static InteractKind ChooseInteract(bool helpUp, bool unanchor, bool van, bool cart, InteractKind held, bool holdingE)
        {
            if (holdingE && held != InteractKind.None)
            {
                bool still = held == InteractKind.HelpUp ? helpUp : held == InteractKind.Unanchor ? unanchor : held == InteractKind.Van ? van : held == InteractKind.Cart && cart;
                if (still) return held;
            }
            return helpUp ? InteractKind.HelpUp : unanchor ? InteractKind.Unanchor : van ? InteractKind.Van : cart ? InteractKind.Cart : InteractKind.None;
        }
        public static InteractKind ChooseInteract(bool helpUp, bool unanchor, bool van, bool cart, bool bell, InteractKind held, bool holdingE)
        {   // la campana va ultima: nunca le gana a ayudar, soltar, cerrar ni atar
            var k = ChooseInteract(helpUp, unanchor, van, cart, held, holdingE);
            return k == InteractKind.None && bell ? InteractKind.Bell : k;
        }

        /// Linea de vision desde la camara: no se interactua a traves de paredes, barra o puertas.
        bool Visible(Vector3 target, Rigidbody targetBody)
        {
            Vector3 o = cam.transform.position, d = target - o; float len = d.magnitude; if (len < 0.05f) return true;
            var hits = Physics.RaycastAll(o, d / len, Mathf.Max(0f, len - 0.25f), ~0, QueryTriggerInteraction.Ignore); // los ultimos 25 cm no cuentan: la mano apoyada en la barra, el compañero sobre el piso
            foreach (var h in hits)
            {
                var rb = h.rigidbody;
                if (rb == null) return false;                                   // geometria estatica en el medio
                if (rb == this.rb || rb == targetBody) continue;
                if (NpcBody.Of(rb) && targetBody && NpcBody.Of(targetBody) == NpcBody.Of(rb)) continue; // otra parte del mismo tio
                if (rb.GetComponent<PlayerMotor>()) continue;                   // otros jugadores no tapan
                if (!rb.isKinematic && rb.mass < 15f) continue;                  // objetos sueltos chicos (vasos, banquetas)
                return false;
            }
            return true;
        }

        void HandleInteract()
        {
            interactLabel = "";
            var npc = NpcBrain.Instance; var cart = TransportCart.Instance;
            var van = VanDoors.Instance; var zone = ExtractionZone.Instance;
            bool holdingE = Ctrl(Act.Interact);
            // candidatos validos este cuadro
            PlayerMotor mate = null;
            foreach (var other in PlayerMotor.All)
            {
                if (!other || other == motor || other.State != PlayerState.Fallen) continue;
                Vector3 to = other.transform.position + Vector3.up * 0.4f - cam.transform.position;
                if (to.magnitude > 1.9f || Vector3.Angle(cam.transform.forward, to) > 40f) continue;
                if (!Visible(other.transform.position + Vector3.up * 0.4f, other.Body)) continue;
                if (mate == null || (sticky == InteractKind.HelpUp && other == stickyMate)) mate = other;
            }
            bool unanchor = false;
            if (npc && npc.IsAnchored && npc.AnchorHandSocket)
            {
                Vector3 to = npc.AnchorHandSocket.Position - cam.transform.position;
                unanchor = to.magnitude <= LabBalance.GrabReach + 0.3f && Vector3.Angle(cam.transform.forward, to) < 30f && Visible(npc.AnchorHandSocket.Position, npc.AnchorHandSocket.body);
            }
            bool vanOk = van && zone && !zone.Secured && zone.AllPointsInside && van.InRange(transform.position);
            bool cartOk = cart && Vector3.Distance(transform.position, cart.transform.position) < 2.2f && (cart.IsStrapped || cart.CanStrap);
            var props = BarProps.Instance;
            bool bellOk = props && props.HasBell && Vector3.Distance(cam.transform.position, BarProps.BellPos) < 1.9f && Vector3.Angle(cam.transform.forward, BarProps.BellPos - cam.transform.position) < 28f;
            var kind = ChooseInteract(mate != null, unanchor, vanOk, cartOk, bellOk, sticky, holdingE);
            if (kind != InteractTarget && InteractTarget != InteractKind.None) interactProgress = 0f; // otro objetivo: el progreso empieza de cero
            InteractTarget = kind; sticky = holdingE ? kind : InteractKind.None; stickyMate = kind == InteractKind.HelpUp ? mate : null;
            switch (kind)
            {
                case InteractKind.HelpUp:
                    interactLabel = Loc.Code("interact.help_up", ("player", mate.playerName));
                    if (holdingE) RoundStats.Assisting(this, mate);
                    if (holdingE && mate.AssistGetUp(Time.deltaTime)) interactProgress = Mathf.Clamp01(1f - mate.StateTimeLeft / LabBalance.FallLockMax);
                    else if (!holdingE) interactProgress = 0f;
                    break;
                case InteractKind.Unanchor:
                    interactLabel = "interact.unanchor";
                    if (holdingE)
                    {
                        interactProgress += Time.deltaTime / (LabBalance.AnchorReleaseHold * (npc.BellWindow ? BarProps.BellReleaseFactor : 1f));
                        if (interactProgress >= 1f) { npc.ReleaseAnchor("jugador"); RoundStats.Unanchored(this); interactProgress = 0f; }
                    }
                    else interactProgress = 0f;
                    break;
                case InteractKind.Van:
                {   // con el tio adentro, mantener E 1 s desde afuera para cerrar las puertas (GDD 03)
                    string why = van.BlockReason();
                    if (why == "") { interactLabel = "van.hold_close"; if (holdingE) RoundStats.Closing(this); interactProgress = holdingE ? van.HoldClose(this, Time.deltaTime) : 0f; }
                    else { interactLabel = why; interactProgress = 0f; }
                    break;
                }
                case InteractKind.Cart:
                    interactProgress = 0f;
                    if (cart.IsStrapped) { interactLabel = "cart.unstrap"; if (CtrlPressed(Act.Interact)) cart.Unstrap("jugador"); }
                    else { interactLabel = "cart.strap"; if (CtrlPressed(Act.Interact)) { if (cart.Strap()) RoundStats.Strapped(this); else Feedback(cart.LastFailure); } }
                    break;
                case InteractKind.Bell:
                    interactProgress = 0f;
                    {
                        string wait = props.BellBlock();
                        interactLabel = wait != "" ? wait : "interact.bell";
                        if (wait == "" && CtrlPressed(Act.Interact)) props.RingBell(this);
                    }
                    break;
                default: interactProgress = 0f; break;
            }
        }

        public void ReleaseAll(ReleaseReason r) { GrabRegistry.ReleaseAllOf(this, r); effortTime = 0f; effortActive = false; }

        void OnDisable() { if (Auth) GrabRegistry.ReleaseAllOf(this, ReleaseReason.Disabled); }

        public override void OnNetworkDespawn()
        {   // desconexion mientras agarra: el anfitrion libera todo de ese jugador (especificacion 05)
            if (IsServer) GrabRegistry.ReleaseAllOf(this, ReleaseReason.Disconnect);
        }

        static readonly Color sleeveFree = new Color(0.22f, 0.3f, 0.36f);
        static readonly Color free = new Color(0.95f, 0.8f, 0.65f), holding = new Color(0.2f, 0.85f, 0.75f), strained = new Color(1f, 0.55f, 0.1f);
        void UpdateVisuals()
        {
            SetHand(leftHandVisual, leftR, 0); SetHand(rightHandVisual, rightR, 1);
        }
        void SetHand(Transform t, Renderer r, int hand)
        {
            if (!t) return;
            var s = HeldSocket(hand);
            Vector3 p = s ? Vector3.Lerp(HandTarget(hand), s.Position, 0.85f) : HandTarget(hand);
            // sesion 14: caido o levantandose, las manos bajan abiertas (sin control); con esfuerzo, el puño que sostiene tiembla
            bool down = motor && motor.State != PlayerState.Active;
            if (down && cam) p = cam.transform.TransformPoint(new Vector3(hand == 0 ? -0.3f : 0.3f, -0.55f, 0.55f));
            bool straining = s && EffortActive;
            if (straining) p += (cam ? cam.transform.right : Vector3.right) * (Mathf.Sin(Time.time * 47f + hand * 1.7f) * 0.006f) + Vector3.up * (Mathf.Sin(Time.time * 39f + hand) * 0.004f);
            t.position = Vector3.Lerp(t.position, p, Smooth.Factor(down ? 0.15f : 0.5f));
            Color c = s == null ? free : Color.Lerp(holding, strained, Mathf.Max(Tension01(hand), straining ? 0.6f : 0f));
            var rig = rigs[hand];
            if (rig && controls && controls.IsLocal && !down && controls.Pressed(hand == 0 ? Act.GrabLeft : Act.GrabRight))
            {   // cada clic se ve y se oye en el acto (la confirmacion del agarre llega despues, con su propio sonido)
                rig.Pulse(); if (GameAudio.Instance) GameAudio.Instance.Play2D("grab_try", 0.45f);
            }
            if (rig)
            {
                bool pending = s == null && Candidate && Ctrl(hand == 0 ? Act.GrabLeft : Act.GrabRight) && !down;
                rig.Set(down ? 0.05f : s ? 1f : pending ? 0.55f : 0.15f, s == null ? sleeveFree : c);
            }
            else if (r) r.material.color = c;
        }
    }
}
