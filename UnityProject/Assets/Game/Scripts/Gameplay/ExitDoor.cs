using UnityEngine;

namespace UncleExtraction
{
    /// Puerta de salida del bar (GDD 01: «puerta con bisagra y topes»; biblia visual: «manija visible»). Se cierra sola con
    /// un resorte suave (medido: 25 N en el borde a 45 grados, 64 N a 90) y tiene una manija agarrable ("door_handle") para
    /// abrirla, sostenerla para otro o cerrarla. Al reiniciar la ronda vuelve cerrada (antes quedaba como estaba).
    /// Se evaluo un reten de apertura y se descarto: la reproduccion controlada mostro que el carrito cargado sale tirando
    /// recto del asa aun con la hoja apoyada (StuckRecoveryTests); mantener la puerta como obstaculo es parte del diseno.
    [RequireComponent(typeof(HingeJoint))]
    public class ExitDoor : MonoBehaviour, IResettable
    {
        HingeJoint hinge; Rigidbody rb; Quaternion closedRot; Vector3 closedPos;
        public float Angle => hinge ? hinge.angle : 0f;

        void Awake() { hinge = GetComponent<HingeJoint>(); rb = GetComponent<Rigidbody>(); closedRot = transform.rotation; closedPos = transform.position; }

        public void ResetState()
        {
            if (!rb || !NetAuth.IsServer) return;
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            rb.position = closedPos; rb.rotation = closedRot; transform.SetPositionAndRotation(closedPos, closedRot);
        }
    }
}
