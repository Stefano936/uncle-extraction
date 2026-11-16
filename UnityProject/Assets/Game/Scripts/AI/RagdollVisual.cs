using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// La malla con rig del kit (uncle_prototype.fbx) sigue al cuerpo articulado: cada hueso copia la pose de su parte
    /// fisica con el desfase medido al construir (ambos en la misma pose de reposo). Solo presentacion: corre en todos
    /// los pares y no toca la fisica.
    public class RagdollVisual : MonoBehaviour
    {
        [System.Serializable] public struct Link { public Transform bone; public Transform part; public Vector3 posOffset; public Quaternion rotOffset; }
        public List<Link> links = new List<Link>();

        /// Registra el desfase hueso/parte en la pose actual (se llama desde el constructor).
        public void Bind(Transform bone, Transform part)
        {
            links.Add(new Link { bone = bone, part = part, posOffset = part.InverseTransformPoint(bone.position), rotOffset = Quaternion.Inverse(part.rotation) * bone.rotation });
        }

        void LateUpdate()
        {
            for (int i = 0; i < links.Count; i++)
            {
                var l = links[i]; if (!l.bone || !l.part) continue;
                l.bone.SetPositionAndRotation(l.part.TransformPoint(l.posOffset), l.part.rotation * l.rotOffset);
            }
        }
    }
}
