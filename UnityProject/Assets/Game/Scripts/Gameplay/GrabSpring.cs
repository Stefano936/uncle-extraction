using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Unica implementacion autoritativa de la fuerza de agarre (especificacion 04):
    /// F = clampMagnitude(k * errorPos - c * errorVel, Fmax), con tope global por dueno.
    public static class GrabSpring
    {
        public static float K = 1400f;   // N/m   (ajustable en el HUD de PhysicsLab)
        public static float C = 160f;    // N*s/m
        public static float ReactionFactor = 0.6f; // fraccion de la reaccion que recibe el jugador (tira de el)

        struct Pending { public GrabLink link; public Vector3 force; public Vector3 point; }
        static readonly List<Pending> pending = new List<Pending>(4);

        /// Aplica fuerzas de todas las manos de un dueno. Devuelve la fuerza neta aplicada sobre objetivos.
        public static Vector3 ApplyOwner(IGrabOwner owner, Rigidbody ownerBody, System.Func<int, Vector3> handTarget,
                                         bool effort, float dt, List<GrabLink> brokenOut)
        {
            pending.Clear();
            float handMax = effort ? LabBalance.HandEffortForce : LabBalance.HandForce;
            float ownerCap = effort ? LabBalance.PlayerEffortForceCap : LabBalance.PlayerForceCap;
            float sum = 0f;
            var links = GrabRegistry.Links;
            for (int i = 0; i < links.Count; i++)
            {
                var l = links[i];
                if (l.owner != owner || l.socket == null || l.socket.body == null) continue;
                Vector3 p = l.socket.Position;
                Vector3 target = handTarget(l.hand);
                Vector3 err = target - p;
                float e = err.magnitude;
                l.tension01 = Mathf.InverseLerp(0.2f, LabBalance.GrabWarnError, e);
                if (e > LabBalance.GrabBreakError) { l.overBreakTime += dt; if (l.overBreakTime >= LabBalance.GrabBreakGrace) { brokenOut?.Add(l); continue; } }
                else l.overBreakTime = 0f;

                Vector3 ownerVel = ownerBody ? ownerBody.linearVelocity : Vector3.zero;
                Vector3 relVel = l.socket.body.GetPointVelocity(p) - ownerVel;
                Vector3 f = Vector3.ClampMagnitude(K * err - C * relVel, handMax);
                pending.Add(new Pending { link = l, force = f, point = p });
                sum += f.magnitude;
            }
            float scale = sum > ownerCap ? ownerCap / sum : 1f;
            Vector3 net = Vector3.zero;
            foreach (var pd in pending)
            {
                Vector3 f = pd.force * scale;
                if (!pd.link.socket.body.isKinematic) pd.link.socket.body.AddForceAtPosition(f, pd.point, ForceMode.Force);
                net += f;
            }
            if (ownerBody && !ownerBody.isKinematic)
            {
                Vector3 reaction = -net * ReactionFactor; reaction.y = 0f;
                ownerBody.AddForce(reaction, ForceMode.Force);
            }
            return net;
        }
    }
}
