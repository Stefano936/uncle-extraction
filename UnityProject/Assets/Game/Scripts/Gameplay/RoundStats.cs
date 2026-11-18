using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UncleExtraction
{
    /// Parte de extraccion. El ANFITRION registra solo hechos de la ronda y al terminar arma un parte ESTRUCTURADO
    /// (campos y eventos, sesion 17): los nombres viajan como valores, nunca dentro de un texto con separadores, asi que un
    /// nombre con «|», «=», tildes o emoji no rompe nada. Cada jugador lo traduce en su idioma. Solo destacados positivos.
    /// Se reinicia con la ronda (tambien en los invitados: el parte trae el numero de ronda).
    public class RoundStats : MonoBehaviour, IResettable
    {
        public static RoundStats Instance { get; private set; }

        [System.Serializable] public class Moment { public float t; public string key; public string name; public int who; } // who: 0 jugador, 1 carrito, 2 tio, 3 nadie
        [System.Serializable] public class Best { public string key; public string name; public int v; public bool seconds; }
        [System.Serializable] public class Report
        {
            public int round; public bool win; public float time, left = -1f; public int falls, attacks, hits, cut;
            public string challenge = "Standard"; public string featured = ""; public string featuredName = ""; public int featuredValue;
            public List<Moment> moments = new List<Moment>(); public List<Best> best = new List<Best>();
        }
        /// Ultimo parte (en los invitados llega por RPC como JSON).
        public static Report Last;
        public static string LastJson = "";

        public class P { public string name = "?"; public float holdUncle, driveCart; public int helps, unanchors, straps, cuts, closes; }
        readonly Dictionary<ulong, P> players = new Dictionary<ulong, P>();
        readonly List<Moment> moments = new List<Moment>();
        readonly HashSet<string> once = new HashSet<string>();
        PlayerHands lastCloser; readonly Dictionary<PlayerMotor, (PlayerHands who, float at)> assisting = new Dictionary<PlayerMotor, (PlayerHands, float)>();
        readonly Dictionary<PlayerMotor, PlayerState> lastState = new Dictionary<PlayerMotor, PlayerState>();
        RoundState lastRound = RoundState.Running; int lastRestarts = -1; float firstUnanchorAt = -1f, coordinatedAt = -1f; string coordinatedName = "";

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        public void ResetState() { players.Clear(); moments.Clear(); once.Clear(); lastCloser = null; assisting.Clear(); lastState.Clear(); Last = null; LastJson = ""; firstUnanchorAt = coordinatedAt = -1f; coordinatedName = ""; }

        static ulong Id(PlayerHands h) => h && h.Controls ? h.Controls.OwnerClientId : 0;
        static string NameOf(PlayerHands h) { var m = h ? h.GetComponent<PlayerMotor>() : null; return m ? m.playerName : ""; }
        P Of(PlayerHands h)
        {
            ulong id = Id(h);
            if (!players.TryGetValue(id, out var p)) players[id] = p = new P();
            var n = NameOf(h); if (n != "") p.name = n;
            return p;
        }
        float T => RoundDirector.Instance ? RoundDirector.Instance.Elapsed : 0f;
        void AddMoment(string key, string name, int who) { if (once.Add(key)) moments.Add(new Moment { t = T, key = key, name = name ?? "", who = who }); }
        void AddMoment(string key, PlayerHands h) => AddMoment(key, NameOf(h), h ? 0 : 3);

        // ---- llamadas desde el juego (solo el anfitrion) ----
        public static void Unanchored(PlayerHands h)
        {
            if (!Instance || !NetAuth.IsServer) return; Instance.Of(h).unanchors++; Instance.AddMoment("unanchor", h);
            if (Instance.firstUnanchorAt < 0f) Instance.firstUnanchorAt = Instance.T;
            // liberar la mano mientras el tio esta distraido (timbre, torre, vaso): jugada coordinada
            var npc = NpcBrain.Instance;
            if (npc && (npc.State == NpcState.Distracted || npc.BellWindow) && Instance.coordinatedAt < 0f) { Instance.coordinatedAt = Instance.T; Instance.coordinatedName = NameOf(h); Instance.AddMoment("coordinated", h); }
        }
        public static void Strapped(PlayerHands h) { if (!Instance || !NetAuth.IsServer) return; if (h) Instance.Of(h).straps++; Instance.AddMoment("strap", h); }
        public static void Closing(PlayerHands h) { if (!Instance || !NetAuth.IsServer) return; Instance.lastCloser = h; }
        public static void Assisting(PlayerHands h, PlayerMotor mate) { if (!Instance || !NetAuth.IsServer || !mate) return; Instance.assisting[mate] = (h, Time.time); }
        /// Torre de vasos: who = 1 carrito, 2 tio, 0 un jugador (name).
        public static void Tower(int who, string name) { if (!Instance || !NetAuth.IsServer) return; Instance.AddMoment("tower", name, who); }
        public static void Bell(PlayerHands h) { if (!Instance || !NetAuth.IsServer) return; Instance.AddMoment("bell", h); }
        public static void AttackCut(PlayerHands h) { if (!Instance || !NetAuth.IsServer || !h) return; Instance.Of(h).cuts++; Instance.AddMoment("cut", h); }

        void Update()
        {
            var round = RoundDirector.Instance; if (!round) return;
            if (round.Restarts != lastRestarts) { lastRestarts = round.Restarts; ResetState(); lastRound = RoundState.Running; } // tambien en invitados
            if (!NetAuth.IsServer || round.InLobby) return;
            if (round.State == RoundState.Running)
            {
                float dt = Time.deltaTime;
                foreach (var l in GrabRegistry.Links)
                {
                    if (l == null || !l.socket) continue;
                    var h = l.owner as PlayerHands; if (!h) continue;
                    if (l.socket.socketId == "cart_handle") Of(h).driveCart += dt * 0.5f; // dos manos en el asa cuentan como un tiempo
                    else if (NpcBody.Of(l.socket.body)) { Of(h).holdUncle += dt * 0.5f; AddMoment("first_grab", h); }
                }
                foreach (var pm in PlayerMotor.All)
                {
                    if (!pm) continue;
                    lastState.TryGetValue(pm, out var was); lastState[pm] = pm.State;
                    if (was == PlayerState.Fallen && pm.State != PlayerState.Fallen && assisting.TryGetValue(pm, out var a) && Time.time - a.at < 0.5f && a.who)
                    { Of(a.who).helps++; AddMoment("help", a.who); }
                }
                var zone = ExtractionZone.Instance; if (zone && zone.AllPointsInside) AddMoment("in_van", "", 2);
            }
            if (round.State != lastRound)
            {
                lastRound = round.State;
                if (round.State == RoundState.Success || round.State == RoundState.Timeout) { Last = Build(round); LastJson = JsonUtility.ToJson(Last); round.PublishReport(LastJson); }
            }
        }

        Report Build(RoundDirector round)
        {
            bool win = round.State == RoundState.Success;
            if (win && lastCloser) { Of(lastCloser).closes++; AddMoment("close", lastCloser); }
            var r = new Report { round = round.Restarts, win = win, time = round.Elapsed, challenge = round.CurrentChallenge.ToString() };
            r.falls = PlayerMotor.All.Where(p => p).Sum(p => p.FallCount);
            r.left = round.TimeLimit > 0f ? round.TimeLimit - round.Elapsed : -1f;
            var npc = NpcBrain.Instance; if (npc) { r.attacks = npc.AttacksStarted; r.hits = npc.HitsLanded; r.cut = npc.Interrupted; }
            r.moments = moments.OrderBy(m => m.t).Take(6).ToList();
            void B(string key, System.Func<P, float> v, bool secs)
            {
                var top = players.Values.OrderByDescending(v).FirstOrDefault();
                if (top != null && v(top) > 0.5f) r.best.Add(new Best { key = key, name = top.name, v = Mathf.RoundToInt(v(top)), seconds = secs });
            }
            B("hold", p => p.holdUncle, true); B("help", p => p.helps, false); B("unanchor", p => p.unanchors, false);
            B("drive", p => p.driveCart, true); B("cut", p => p.cuts, false); B("close", p => p.closes, false);
            // destacado (sesion 17): el hecho real mas «contable» de la ronda
            if (win && r.left >= 0f && r.left < 30f) { r.featured = "close_call"; r.featuredValue = Mathf.CeilToInt(r.left); r.featuredName = lastCloser ? NameOf(lastCloser) : ""; }
            else if (coordinatedAt >= 0f) { r.featured = "coordinated"; r.featuredName = coordinatedName; r.featuredValue = (int)coordinatedAt; }
            else if (r.cut > 0) { var c = players.Values.OrderByDescending(p => p.cuts).First(); r.featured = "cut"; r.featuredName = c.name; r.featuredValue = c.cuts; }
            else if (firstUnanchorAt >= 0f && firstUnanchorAt < 15f) { var u = players.Values.OrderByDescending(p => p.unanchors).First(); r.featured = "quick_unanchor"; r.featuredName = u.name; r.featuredValue = Mathf.CeilToInt(firstUnanchorAt); }
            else if (players.Values.Any(p => p.helps > 0)) { var h = players.Values.OrderByDescending(p => p.helps).First(); r.featured = "help"; r.featuredName = h.name; r.featuredValue = h.helps; }
            else if (win) { r.featured = "time"; r.featuredValue = (int)r.time; }
            Debug.Log("[Report] " + JsonUtility.ToJson(r));
            return r;
        }

        /// Llega a los invitados (RPC).
        public static void Receive(string json)
        {
            try { Last = JsonUtility.FromJson<Report>(json); LastJson = json; } catch { Last = null; LastJson = ""; }
        }

        static string Clock(float s) => $"{(int)(s / 60f)}:{(int)(s % 60f):00}";
        static string WhoName(Moment m) => m.who == 1 ? Loc.T("report.the_cart") : m.who == 2 ? Loc.T("report.the_uncle") : m.name;

        /// Destacado traducido (una linea grande).
        public static string Featured(Report r)
        {
            if (r == null || string.IsNullOrEmpty(r.featured)) return "";
            return Loc.T("report.f_" + r.featured, ("name", r.featuredName ?? ""), ("v", r.featuredValue.ToString()), ("time", Clock(r.featuredValue)));
        }

        /// Parte traducido (cada jugador, en su idioma). Los valores se insertan como parametros: nada se parsea.
        public static string Render(Report r, int maxBest = 3)
        {
            if (r == null) return "";
            var o = new List<string> { Loc.T("report.summary", ("time", Clock(r.time)), ("falls", r.falls.ToString())) };
            if (r.challenge != "Standard") o.Add(Loc.T("challenge.played_" + r.challenge.ToLowerInvariant()));
            if (r.win && r.left >= 0f && r.left < 30f) o.Add(Loc.T("report.close_call", ("left", Mathf.CeilToInt(r.left).ToString())));
            o.Add(Loc.T("report.uncle", ("attacks", r.attacks.ToString()), ("hits", r.hits.ToString()), ("cut", r.cut.ToString())));
            foreach (var m in r.moments) o.Add($"  {Clock(m.t)}  {Loc.T("report.m_" + m.key, ("name", WhoName(m)))}");
            foreach (var b in r.best.Take(maxBest)) o.Add(Loc.T("report.best_" + b.key, ("name", b.name), ("v", b.v + (b.seconds ? " s" : ""))));
            return string.Join("\n", o);
        }
    }
}
