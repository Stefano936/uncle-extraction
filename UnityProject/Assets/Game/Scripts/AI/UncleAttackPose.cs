using UnityEngine;

namespace UncleExtraction
{
    /// Pose de preparacion y de golpe de los cuatro ataques sobre el tio articulado (senal anticipable, GDD 06). Mueve los
    /// objetivos de las articulaciones (slerp drive) en el ANFITRION: la pose es fisica y llega a los clientes con las
    /// partes replicadas, asi que se ve igual en todos y nunca va adelantada o atrasada respecto del golpe, que sigue
    /// decidiendo NpcBrain con su reloj (la pose no decide impactos). Empujon y patada salen de los clips del kit
    /// (Push_Prototype, Kick_Prototype) muestreados al construir; manotazo y sacudida, de poses autorizadas a mano.
    /// Solo de pie: caido o cargado, las articulaciones vuelven a reposo (cuerpo flojo).
    public class UncleAttackPose : MonoBehaviour, IResettable
    {
        [System.Serializable] public class Pose { public string name = ""; public Quaternion[] targets = new Quaternion[0]; }

        public NpcBrain brain; public NpcBody body;
        public Pose slapWindup = new Pose(), slapStrike = new Pose(), pushWindup = new Pose(), pushStrike = new Pose();
        public Pose kickWindup = new Pose(), kickStrike = new Pose(), shakeWindup = new Pose(), shakeStrike = new Pose();
        /// Variantes del lado IZQUIERDO del tio (sesion 14): las de arriba son del derecho. El cerebro elige el miembro libre.
        public Pose slapWindupL = new Pose(), slapStrikeL = new Pose(), kickWindupL = new Pose(), kickStrikeL = new Pose();
        static Pose Pick(Pose right, Pose left, AttackSide s) => s == AttackSide.Left && left != null && left.targets.Length > 0 ? left : right;
        public string pushSource = "", kickSource = "";

        Quaternion[] current;
        /// Pose que se esta aplicando ("" = reposo). Para pruebas y diagnostico.
        public string ActivePose { get; private set; } = "";

        void Awake() { if (!brain) brain = GetComponent<NpcBrain>(); if (!body) body = GetComponent<NpcBody>(); }

        Pose Target(out float rate)
        {
            rate = 4f; // vuelta a reposo
            if (!brain || !body || body.Posture != NpcPosture.Upright) { rate = 20f; return null; }
            var id = brain.CurrentAttack.id;
            if (brain.State == NpcState.Windup)
            {
                rate = 1f / Mathf.Max(0.2f, brain.CurrentAttack.windup * 0.45f); // llega a la pose antes de la mitad de la preparacion
                return id == AttackId.Slap ? Pick(slapWindup, slapWindupL, brain.Side) : id == AttackId.Push ? pushWindup : id == AttackId.Kick ? Pick(kickWindup, kickWindupL, brain.Side) : shakeWindup;
            }
            if (brain.State == NpcState.Attack)
            {
                rate = 14f; // golpe rapido (ventana activa ~0,2 s)
                return id == AttackId.Slap ? Pick(slapStrike, slapStrikeL, brain.Side) : id == AttackId.Push ? pushStrike : id == AttackId.Kick ? Pick(kickStrike, kickStrikeL, brain.Side) : shakeStrike;
            }
            return null;
        }

        void FixedUpdate()
        {
            if (!NetAuth.IsServer || body == null || !body.Articulated) return;
            var parts = body.parts;
            if (current == null || current.Length != parts.Length) { current = new Quaternion[parts.Length]; for (int i = 0; i < current.Length; i++) current[i] = Quaternion.identity; }
            var pose = Target(out float rate);
            bool shaking = pose == shakeStrike && pose != null;
            float k = 1f - Mathf.Exp(-rate * Time.fixedDeltaTime);
            ActivePose = pose != null ? pose.name : "";
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i]; if (!p || !p.joint) continue;
                Quaternion goal = pose != null && pose.targets.Length == parts.Length ? pose.targets[i] : Quaternion.identity;
                if (shaking)
                {   // sacudida: el torso y los brazos alternan entre la pose y su opuesta (~6 Hz)
                    float s = Mathf.Sin(Time.time * 38f);
                    goal = Quaternion.SlerpUnclamped(Quaternion.identity, goal, s);
                    current[i] = goal;
                }
                else current[i] = Quaternion.Slerp(current[i], goal, k);
                p.joint.targetRotation = current[i];
            }
        }

        /// Deja todas las articulaciones en reposo (reinicio de ronda).
        public void Relax()
        {
            if (body == null || !body.Articulated) return;
            foreach (var p in body.parts) if (p && p.joint) p.joint.targetRotation = Quaternion.identity;
            if (current != null) for (int i = 0; i < current.Length; i++) current[i] = Quaternion.identity;
            ActivePose = "";
        }

        public void ResetState() => Relax();
    }
}
