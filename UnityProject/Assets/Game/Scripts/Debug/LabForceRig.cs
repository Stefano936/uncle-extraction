using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// "Botones para 1/2/4 fuerzas de agarre" (especificacion 03): jugadores virtuales que tiran del tio hacia un objetivo.
    /// Usan el mismo registro y la misma fuerza limitada que un jugador real (sin cuerpo propio: no reciben reaccion).
    public class LabForceRig : MonoBehaviour, IResettable
    {
        public NpcBody npc;
        public Transform pullTarget;
        public float pullLead = 0.6f;

        class Virtual : IGrabOwner
        {
            public string name; public LabForceRig rig;
            public string OwnerName => name;
            public bool CanGrab => true;
            public Vector3 HandOrigin(int hand)
            {
                var l = GrabRegistry.Find(this, hand);
                return l != null ? l.socket.Position : rig.npc.ChestPosition;
            }
        }

        readonly List<Virtual> pullers = new List<Virtual>();
        readonly List<GrabLink> broken = new List<GrabLink>();
        public int ActivePullers => pullers.Count;

        public void SetVirtualPullers(int count, bool grab = true)
        {
            foreach (var p in pullers) GrabRegistry.ReleaseAllOf(p, ReleaseReason.Debug);
            pullers.Clear();
            if (!npc || count <= 0 || !grab) return;
            var free = new List<GrabSocket>();
            foreach (var s in GrabSocket.All) if (s.body == npc.Body && GrabRegistry.OccupancyOf(s) < s.capacity) free.Add(s);
            int si = 0;
            for (int i = 0; i < count; i++)
            {
                var v = new Virtual { name = $"Virtual {i + 1}", rig = this };
                pullers.Add(v);
                for (int hand = 0; hand < 2 && si < free.Count; hand++)
                {
                    // el origen de la mano virtual se coloca en el socket para validar alcance
                    var s = free[si++];
                    GrabRegistry.TryCreate(v, hand, s, out _);
                }
            }
            Debug.Log($"[ForceRig] pullers={count} grabs={GrabRegistry.Count}");
        }

        void FixedUpdate()
        {
            if (!NetAuth.IsServer || pullers.Count == 0 || !pullTarget) return;
            foreach (var v in pullers)
            {
                broken.Clear();
                GrabSpring.ApplyOwner(v, null, hand =>
                {
                    var l = GrabRegistry.Find(v, hand);
                    if (l == null) return Vector3.zero;
                    Vector3 dir = pullTarget.position - l.socket.Position; dir.y = 0f;
                    return l.socket.Position + dir.normalized * pullLead;
                }, false, Time.fixedDeltaTime, broken);
                foreach (var l in broken) GrabRegistry.Release(l.id, ReleaseReason.Distance);
            }
        }

        public void ResetState() => SetVirtualPullers(0, false);
    }
}
