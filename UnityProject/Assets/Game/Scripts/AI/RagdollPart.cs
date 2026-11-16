using UnityEngine;

namespace UncleExtraction
{
    /// Una parte del tio articulado (especificacion 04: ragdoll reducido de 11 cuerpos). La une a su padre una
    /// ConfigurableJoint con limites angulares, proyeccion (sin estiramientos) y un motor (slerp drive) hacia la pose de
    /// reposo. NpcBody decide la rigidez: firme de pie, floja caido. La raiz (pelvis) no tiene RagdollPart: es NpcBody.Body.
    [RequireComponent(typeof(Rigidbody))]
    public class RagdollPart : MonoBehaviour
    {
        public string role;                 // chest, head, upper_arm.L, forearm.L, thigh.L, calf.L, ...
        public ConfigurableJoint joint;     // hacia la parte padre
        public float uprightSpring = 900f, limpSpring = 60f, damper = 60f;
        [System.NonSerialized] public Vector3 restLocalPos;     // relativo a la raiz del tio
        [System.NonSerialized] public Quaternion restLocalRot;
        public Rigidbody Body { get; private set; }

        void Awake() { Body = GetComponent<Rigidbody>(); }

        public void SetStrength(bool upright)
        {
            if (!joint) return;
            float k = upright ? uprightSpring : limpSpring;
            joint.slerpDrive = new JointDrive { positionSpring = k, positionDamper = damper, maximumForce = upright ? 4000f : 600f };
        }
    }
}
