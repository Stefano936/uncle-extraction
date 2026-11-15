using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Dos manos independientes. Mantener boton = agarrar/mantener (GDD 04). Las uniones las valida GrabRegistry.
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerHands : MonoBehaviour, IGrabOwner
    {
        public Camera cam;
        public Transform leftHandVisual, rightHandVisual;
        public float aimConeDeg = 22f;
        public Vector3 holdOffset = new Vector3(0.28f, -0.3f, 0.75f); // x se refleja por mano

        PlayerMotor motor; Rigidbody rb;
        readonly List<GrabLink> broken = new List<GrabLink>();
        float effortTime;
        Renderer leftR, rightR;

        public string OwnerName => motor ? motor.playerName : name;
        public bool CanGrab => motor && motor.ControlsEnabled;
        public bool AnyHeld => GrabRegistry.Find(this, 0) != null || GrabRegistry.Find(this, 1) != null;
        public bool EffortActive { get; private set; }
        public GrabSocket Candidate { get; private set; }
        public string LastFeedback { get; private set; } = "";
        public float FeedbackUntil { get; private set; }
        public float InteractProgress01 { get; private set; }
        public string InteractLabel { get; private set; } = "";

        /// Botones simulados para pruebas (null = entrada real).
        public bool? DebugHoldLeft, DebugHoldRight; public bool DebugEffort;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>(); rb = GetComponent<Rigidbody>();
            if (!cam) cam = GetComponentInChildren<Camera>();
            if (leftHandVisual) leftR = leftHandVisual.GetComponent<Renderer>();
            if (rightHandVisual) rightR = rightHandVisual.GetComponent<Renderer>();
        }

        public Vector3 HandOrigin(int hand) => cam ? cam.transform.position : transform.position + Vector3.up * 1.6f;

        public Vector3 HandTarget(int hand)
        {
            var t = cam ? cam.transform : transform;
            Vector3 o = holdOffset; if (hand == 0) o.x = -o.x;
            return t.TransformPoint(o);
        }

        void Update()
        {
            Candidate = FindCandidate();
            if (!motor.ControlsEnabled) { InteractProgress01 = 0f; return; }

            if (LabInput.Pressed(Act.ReleaseAll)) ReleaseAll(ReleaseReason.ReleaseAll);
            bool live = Cursor.lockState == CursorLockMode.Locked; // con el panel abierto, los clics son para la UI
            HandleHand(0, DebugHoldLeft ?? (live && LabInput.Held(Act.GrabLeft)));
            HandleHand(1, DebugHoldRight ?? (live && LabInput.Held(Act.GrabRight)));
            HandleInteract();
            UpdateVisuals();
        }

        void HandleHand(int hand, bool held)
        {
            var link = GrabRegistry.Find(this, hand);
            if (!held) { if (link != null) GrabRegistry.Release(link.id, ReleaseReason.Released); return; }
            if (link != null) return;
            // Mantener sin agarre: reintenta cada frame (agarre al pasar sobre un punto valido)
            if (Candidate == null) return;
            var r = GrabRegistry.TryCreate(this, hand, Candidate, out _);
            if (r != GrabResult.Ok && r != GrabResult.HandBusy) Feedback(r == GrabResult.SocketBusy ? "Ocupado" : r == GrabResult.OutOfReach ? "Fuera de alcance" : r.ToString());
        }

        /// API directa (pruebas y depuracion): agarra un socket concreto con una mano.
        public GrabResult ForceGrab(int hand, GrabSocket s) => GrabRegistry.TryCreate(this, hand, s, out _);

        void Feedback(string msg) { LastFeedback = msg; FeedbackUntil = Time.time + 1.2f; }

        GrabSocket FindCandidate()
        {
            if (!cam) return null;
            Vector3 o = cam.transform.position, f = cam.transform.forward;
            GrabSocket best = null; float bestScore = float.MaxValue;
            foreach (var s in GrabSocket.All)
            {
                Vector3 to = s.Position - o; float d = to.magnitude;
                if (d > LabBalance.GrabReach || d < 0.05f) continue;
                float ang = Vector3.Angle(f, to);
                if (ang > aimConeDeg) continue;
                if (Blocked(o, s)) continue;
                float score = ang + d * 4f;
                if (score < bestScore) { bestScore = score; best = s; }
            }
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
                return true;
            }
            return false;
        }

        void FixedUpdate()
        {
            bool wantsEffort = (DebugEffort || LabInput.Held(Act.Effort)) && AnyHeld && motor.ControlsEnabled;
            if (wantsEffort && effortTime < LabBalance.EffortMax && motor.ConsumeEffort(Time.fixedDeltaTime)) { effortTime += Time.fixedDeltaTime; EffortActive = true; }
            else { EffortActive = false; if (!wantsEffort) effortTime = 0f; }

            broken.Clear();
            GrabSpring.ApplyOwner(this, rb, HandTarget, EffortActive, Time.fixedDeltaTime, broken);
            foreach (var l in broken) { GrabRegistry.Release(l.id, ReleaseReason.Distance); Feedback("Se solto (distancia)"); }
        }

        void HandleInteract()
        {
            InteractLabel = ""; 
            var npc = NpcBrain.Instance; var cart = TransportCart.Instance;
            // 1) Desanclar: mantener E sobre la mano anclada del tio 0,9 s
            if (npc && npc.IsAnchored && npc.AnchorHandSocket)
            {
                Vector3 to = npc.AnchorHandSocket.Position - cam.transform.position;
                bool aiming = to.magnitude <= LabBalance.GrabReach + 0.3f && Vector3.Angle(cam.transform.forward, to) < 30f;
                if (aiming)
                {
                    InteractLabel = "Mantener E: soltarle la mano";
                    if (LabInput.Held(Act.Interact))
                    {
                        InteractProgress01 += Time.deltaTime / LabBalance.AnchorReleaseHold;
                        if (InteractProgress01 >= 1f) { npc.ReleaseAnchor("jugador"); InteractProgress01 = 0f; }
                    }
                    else InteractProgress01 = 0f;
                    return;
                }
            }
            InteractProgress01 = 0f;
            // 2) Carrito: E para asegurar / liberar al tio en la caja
            if (cart && Vector3.Distance(transform.position, cart.transform.position) < 2.2f)
            {
                if (cart.IsStrapped) { InteractLabel = "E: soltar correa del carrito"; if (LabInput.Pressed(Act.Interact)) cart.Unstrap("jugador"); }
                else if (cart.CanStrap) { InteractLabel = "E: asegurar al tio en el carrito"; if (LabInput.Pressed(Act.Interact)) cart.Strap(); }
            }
        }

        public void ReleaseAll(ReleaseReason r) { GrabRegistry.ReleaseAllOf(this, r); effortTime = 0f; EffortActive = false; }

        void OnDisable() => GrabRegistry.ReleaseAllOf(this, ReleaseReason.Disabled);

        static readonly Color free = new Color(0.95f, 0.8f, 0.65f), holding = new Color(0.2f, 0.85f, 0.75f), strained = new Color(1f, 0.55f, 0.1f);
        void UpdateVisuals()
        {
            SetHand(leftHandVisual, leftR, 0); SetHand(rightHandVisual, rightR, 1);
        }
        void SetHand(Transform t, Renderer r, int hand)
        {
            if (!t) return;
            var l = GrabRegistry.Find(this, hand);
            Vector3 p = l != null ? Vector3.Lerp(HandTarget(hand), l.socket.Position, 0.85f) : HandTarget(hand);
            t.position = Vector3.Lerp(t.position, p, 0.5f);
            if (r) r.material.color = l == null ? free : Color.Lerp(holding, strained, l.tension01);
        }
    }
}
