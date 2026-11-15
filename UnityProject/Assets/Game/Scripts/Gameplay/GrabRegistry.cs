using System;
using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    public enum GrabResult { Ok, NoTarget, OutOfReach, SocketBusy, HandBusy, OwnerInactive, GlobalLimit }
    public enum ReleaseReason { Released, ReleaseAll, Fell, Distance, Disconnect, Reset, TargetGone, Secured, Attack, Disabled, Debug }

    /// Dueno de un agarre: un jugador o una fuerza virtual de depuracion.
    public interface IGrabOwner
    {
        string OwnerName { get; }
        bool CanGrab { get; }
        Vector3 HandOrigin(int hand);
    }

    public sealed class GrabLink
    {
        public int id;
        public IGrabOwner owner;
        public int hand;
        public GrabSocket socket;
        public float overBreakTime;
        public float tension01;
        public bool alive;
        public override string ToString() => $"#{id} {owner?.OwnerName}/{(hand == 0 ? "L" : "R")} -> {socket?.socketId}";
    }

    /// Registro unico de agarres (autoridad: host / instancia local en individual).
    /// Liberar es idempotente. Cada liberacion tiene motivo y se cuenta para detectar fugas.
    public static class GrabRegistry
    {
        static readonly List<GrabLink> links = new List<GrabLink>();
        static int nextId = 1;
        public static IReadOnlyList<GrabLink> Links => links;
        public static int Count => links.Count;
        public static int TotalCreated { get; private set; }
        public static int TotalReleased { get; private set; }
        public static readonly Dictionary<ReleaseReason, int> ReleasesByReason = new Dictionary<ReleaseReason, int>();
        public static event Action<GrabLink, ReleaseReason> OnReleased;
        public static event Action<GrabLink> OnCreated;
        public static bool Verbose = true;
        static float lastBusyLog = -99f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStatic()
        {
            links.Clear(); nextId = 1; TotalCreated = 0; TotalReleased = 0; ReleasesByReason.Clear();
            OnReleased = null; OnCreated = null;
        }

        public static int OccupancyOf(GrabSocket s)
        {
            int n = 0; foreach (var l in links) if (l.socket == s) n++; return n;
        }
        public static GrabLink Find(IGrabOwner owner, int hand)
        {
            foreach (var l in links) if (l.owner == owner && l.hand == hand) return l; return null;
        }
        public static int CountOnBody(Rigidbody body)
        {
            int n = 0; foreach (var l in links) if (l.socket && l.socket.body == body) n++; return n;
        }
        public static void OwnersOnBody(Rigidbody body, List<IGrabOwner> result)
        {
            result.Clear();
            foreach (var l in links) if (l.socket && l.socket.body == body && !result.Contains(l.owner)) result.Add(l.owner);
        }

        public static GrabResult TryCreate(IGrabOwner owner, int hand, GrabSocket socket, out GrabLink link)
        {
            link = null;
            if (owner == null || !owner.CanGrab) return GrabResult.OwnerInactive;
            if (socket == null || socket.body == null) return GrabResult.NoTarget;
            if (Find(owner, hand) != null) return GrabResult.HandBusy;
            if (OccupancyOf(socket) >= socket.capacity)
            {   // carrera por el mismo punto: gana el primero en el orden autoritativo del anfitrion; el otro recibe SocketBusy
                if (Verbose && Time.time - lastBusyLog > 0.5f) { lastBusyLog = Time.time; Debug.Log($"[Grab] reject {owner.OwnerName} hand={hand} socket={socket.socketId}: SocketBusy"); }
                return GrabResult.SocketBusy;
            }
            if (links.Count >= LabBalance.MaxPlayerGrabs) return GrabResult.GlobalLimit;
            float d = Vector3.Distance(owner.HandOrigin(hand), socket.Position);
            if (d > LabBalance.GrabReach + 0.25f) return GrabResult.OutOfReach; // tolerancia pequena: el origen es la mano virtual
            link = new GrabLink { id = nextId++, owner = owner, hand = hand, socket = socket, alive = true };
            links.Add(link);
            TotalCreated++;
            if (Verbose) Debug.Log($"[Grab] create {link}");
            OnCreated?.Invoke(link);
            return GrabResult.Ok;
        }

        /// Idempotente: devuelve false si el id ya no existe.
        public static bool Release(int grabId, ReleaseReason reason)
        {
            for (int i = 0; i < links.Count; i++)
            {
                if (links[i].id != grabId) continue;
                var l = links[i];
                links.RemoveAt(i);
                l.alive = false;
                TotalReleased++;
                ReleasesByReason.TryGetValue(reason, out int c); ReleasesByReason[reason] = c + 1;
                if (Verbose) Debug.Log($"[Grab] release {l} reason={reason}");
                OnReleased?.Invoke(l, reason);
                return true;
            }
            return false;
        }

        static readonly List<int> tmp = new List<int>();
        static void ReleaseWhere(Predicate<GrabLink> p, ReleaseReason r)
        {
            tmp.Clear();
            foreach (var l in links) if (p(l)) tmp.Add(l.id);
            foreach (var id in tmp) Release(id, r);
        }
        public static void ReleaseAllOf(IGrabOwner owner, ReleaseReason r) => ReleaseWhere(l => l.owner == owner, r);
        public static void ReleaseHand(IGrabOwner owner, int hand, ReleaseReason r) => ReleaseWhere(l => l.owner == owner && l.hand == hand, r);
        public static void ReleaseAllOnBody(Rigidbody body, ReleaseReason r) => ReleaseWhere(l => l.socket && l.socket.body == body, r);
        public static void ReleaseSocket(GrabSocket s, ReleaseReason r) => ReleaseWhere(l => l.socket == s, r);
        public static void ReleaseEverything(ReleaseReason r) => ReleaseWhere(l => true, r);
    }
}
