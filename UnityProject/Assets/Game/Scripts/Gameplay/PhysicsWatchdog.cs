using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Rescate tecnico (especificacion 08): solo ante condiciones TECNICAS verificables —posicion no finita, caida bajo el
    /// mapa o velocidad fuera de limites sostenida—, nunca por «jugar mal». Suelta los vinculos del cuerpo afectado, cancela
    /// su movimiento y lo coloca en la ultima pose segura registrada. Solo en el anfitrion.
    public class PhysicsWatchdog : MonoBehaviour, IResettable
    {
        public float floorY = -1.5f, maxSpeed = 25f, overSpeedGrace = 0.5f, safeInterval = 1f;

        class Track { public NpcBody npc; public Rigidbody rb; public Vector3 safePos; public Quaternion safeRot; public float overSpeedFor, nextSafe; }
        readonly List<Track> tracks = new List<Track>();
        public int Rescues { get; private set; }
        public string LastRescue { get; private set; } = "";

        void FixedUpdate()
        {
            if (!NetAuth.IsServer) return;
            Collect();
            foreach (var t in tracks)
            {
                if (!t.rb || t.rb.isKinematic) continue;
                // El tio articulado se vigila parte por parte (una extremidad que explota o atraviesa el piso cuenta)
                bool finite = true, below = false; float maxV = 0f;
                foreach (var b in t.npc ? t.npc.AllBodies : (IReadOnlyList<Rigidbody>)new[] { t.rb })
                {
                    Vector3 bp = b.position, bv = b.linearVelocity;
                    if (!(float.IsFinite(bp.x) && float.IsFinite(bp.y) && float.IsFinite(bp.z) && float.IsFinite(bv.x) && float.IsFinite(bv.y) && float.IsFinite(bv.z))) finite = false;
                    else { if (bp.y < floorY) below = true; maxV = Mathf.Max(maxV, bv.magnitude); }
                }
                Vector3 p = t.rb.position; Vector3 v = t.rb.linearVelocity;
                t.overSpeedFor = finite && maxV > maxSpeed ? t.overSpeedFor + Time.fixedDeltaTime : 0f;
                string why = !finite ? "posicion no finita" : below ? "bajo el mapa" : t.overSpeedFor > overSpeedGrace ? "velocidad fuera de limites" : "";
                if (why != "") { Rescue(t, why); continue; }
                if (Time.time >= t.nextSafe && p.y > -0.5f && v.magnitude < 3f)
                { t.safePos = p; t.safeRot = t.rb.rotation; t.nextSafe = Time.time + safeInterval; }
            }
        }

        void Collect()
        {
            tracks.RemoveAll(t => !t.rb);
            void Add(Rigidbody rb) { if (rb && !tracks.Exists(t => t.rb == rb)) tracks.Add(new Track { rb = rb, safePos = rb.position, safeRot = rb.rotation }); }
            if (NpcBrain.Instance) { var nb = NpcBrain.Instance.GetComponent<NpcBody>(); Add(nb.Body); var tr = tracks.Find(x => x.rb == nb.Body); if (tr != null) tr.npc = nb; }
            if (TransportCart.Instance) Add(TransportCart.Instance.GetComponent<Rigidbody>());
            foreach (var p in PlayerMotor.All) if (p) Add(p.Body);
        }

        void Rescue(Track t, string why)
        {
            Rescues++; LastRescue = $"{t.rb.name}: {why}";
            Debug.LogWarning($"[Watchdog] rescate tecnico de {t.rb.name} ({why}) -> {t.safePos}");
            if (t.npc) t.npc.ReleaseAllGrabs(ReleaseReason.Reset); else GrabRegistry.ReleaseAllOnBody(t.rb, ReleaseReason.Reset);
            var hands = t.rb.GetComponent<PlayerHands>(); if (hands) hands.ReleaseAll(ReleaseReason.Reset);
            if (TransportCart.Instance && (t.rb == TransportCart.Instance.GetComponent<Rigidbody>() || (NpcBrain.Instance && t.rb == NpcBrain.Instance.GetComponent<Rigidbody>())))
                TransportCart.Instance.Unstrap("rescate tecnico");
            if (t.npc) t.npc.TeleportTo(t.safePos, t.safeRot); // el cuerpo entero, en su pose de reposo
            else
            {
                t.rb.linearVelocity = Vector3.zero; t.rb.angularVelocity = Vector3.zero;
                t.rb.position = t.safePos; t.rb.rotation = t.safeRot; t.rb.transform.SetPositionAndRotation(t.safePos, t.safeRot);
            }
            t.overSpeedFor = 0f;
        }

        public void ResetState() { tracks.Clear(); }
    }
}
