using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Punto agarrable deterministico (socket). No necesita collider: la seleccion es por alcance y angulo.
    public class GrabSocket : MonoBehaviour
    {
        public string socketId = "socket";
        public string label = "punto";
        public Rigidbody body;
        [Min(1)] public int capacity = 1;

        static readonly List<GrabSocket> all = new List<GrabSocket>();
        public static IReadOnlyList<GrabSocket> All => all;

        public Vector3 Position => transform.position;

        /// Busca por id estable (los ids de PhysicsLab son unicos). Para estado replicado: nunca se envian referencias de Unity.
        public static GrabSocket ById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < all.Count; i++) if (all[i].socketId == id) return all[i];
            return null;
        }

        void Awake() { if (!body) body = GetComponentInParent<Rigidbody>(); }
        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable()
        {
            all.Remove(this);
            GrabRegistry.ReleaseSocket(this, ReleaseReason.TargetGone);
        }
    }
}
