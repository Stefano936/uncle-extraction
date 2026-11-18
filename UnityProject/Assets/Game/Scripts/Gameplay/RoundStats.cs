using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UncleExtraction
{
    /// Parte de extraccion (sesion 15). El ANFITRION registra solo hechos de la ronda (agarres, levantadas, mano liberada,
    /// carrito, cierre, golpes frenados, caidas, momentos con su tiempo) y al terminar arma el parte como lineas codificadas
    /// que cada jugador traduce. Solo destacados positivos (nada premia sabotear a otro). Se reinicia con la ronda.
    public class RoundStats : MonoBehaviour, IResettable
    {
        public static RoundStats Instance { get; private set; }
        public class P { public string name = "?"; public float holdUncle, driveCart; public int helps, unanchors, straps, cuts, closes; }
        readonly Dictionary<ulong, P> players = new Dictionary<ulong, P>();
        readonly List<(float t, string code)> moments = new List<(float, string)>();
        readonly HashSet<string> once = new HashSet<string>();
        PlayerHands lastCloser; readonly Dictionary<PlayerMotor, (PlayerHands who, float at)> assisting = new Dictionary<PlayerMotor, (PlayerHands, float)>();
        readonly Dictionary<PlayerMotor, PlayerState> lastState = new Dictionary<PlayerMotor, PlayerState>();
        RoundState lastRound = RoundState.Running; int lastRestarts = -1;
        /// Ultimo parte armado (codigos separados por saltos de linea). En los invitados llega por RPC.
        public static string LastReport = "";

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        public void ResetState() { players.Clear(); moments.Clear(); once.Clear(); lastCloser = null; assisting.Clear(); lastState.Clear(); LastReport = ""; }

        static ulong Id(PlayerHands h) => h && h.Controls ? h.Controls.OwnerClientId : 0;
        P Of(PlayerHands h)
        {
            ulong id = Id(h);
            if (!players.TryGetValue(id, out var p)) players[id] = p = new P();
            var m = h ? h.GetComponent<PlayerMotor>() : null; if (m) p.name = m.playerName;
            return p;
        }
        float T => RoundDirector.Instance ? RoundDirector.Instance.Elapsed : 0f;
        void Moment(string key, PlayerHands who = null)
        {
            if (!once.Add(key)) return;
            var m = who ? who.GetComponent<PlayerMotor>() : null;
            moments.Add((T, m ? $"report.m_{key}|name={m.playerName}" : $"report.m_{key}"));
        }

        // ---- llamadas desde el juego (solo el anfitrion) ----
        public static void Unanchored(PlayerHands h) { if (!Instance || !NetAuth.IsServer) return; Instance.Of(h).unanchors++; Instance.Moment("unanchor", h); }
        public static void Strapped(PlayerHands h) { if (!Instance || !NetAuth.IsServer) return; if (h) Instance.Of(h).straps++; Instance.Moment("strap", h); }
        public static void Closing(PlayerHands h) { if (!Instance || !NetAuth.IsServer) return; Instance.lastCloser = h; }
        public static void Assisting(PlayerHands h, PlayerMotor mate) { if (!Instance || !NetAuth.IsServer || !mate) return; Instance.assisting[mate] = (h, Time.time); }
        public static void Tower(string who) { if (!Instance || !NetAuth.IsServer) return; if (Instance.once.Add("tower")) Instance.moments.Add((Instance.T, $"report.m_tower|name={who}")); }
        public static void AttackCut(PlayerHands h) { if (!Instance || !NetAuth.IsServer || !h) return; Instance.Of(h).cuts++; Instance.Moment("cut", h); }

        void Update()
        {
            if (!NetAuth.IsServer) return;
            var round = RoundDirector.Instance; if (!round) return;
            if (round.Restarts != lastRestarts) { lastRestarts = round.Restarts; ResetState(); lastRound = RoundState.Running; }
            if (round.InLobby) return;
            if (round.State == RoundState.Running)
            {
                float dt = Time.deltaTime;
                foreach (var l in GrabRegistry.Links)
                {
                    if (l == null || !l.socket) continue;
                    var h = l.owner as PlayerHands; if (!h) continue;
                    if (l.socket.socketId == "cart_handle") Of(h).driveCart += dt * 0.5f; // dos manos en el asa cuentan como un tiempo
                    else if (NpcBody.Of(l.socket.body)) { Of(h).holdUncle += dt * 0.5f; Moment("first_grab", h); }
                }
                foreach (var pm in PlayerMotor.All)
                {
                    if (!pm) continue;
                    lastState.TryGetValue(pm, out var was); lastState[pm] = pm.State;
                    if (was == PlayerState.Fallen && pm.State != PlayerState.Fallen && assisting.TryGetValue(pm, out var a) && Time.time - a.at < 0.5f && a.who)
                    { Of(a.who).helps++; Moment("help", a.who); }
                }
                var zone = ExtractionZone.Instance; if (zone && zone.AllPointsInside) Moment("in_van");
            }
            if (round.State != lastRound)
            {
                lastRound = round.State;
                if (round.State == RoundState.Success || round.State == RoundState.Timeout) { Build(round); round.PublishReport(LastReport); }
            }
        }

        void Build(RoundDirector round)
        {
            bool win = round.State == RoundState.Success;
            if (win && lastCloser) { Of(lastCloser).closes++; Moment("close", lastCloser); }
            var sb = new StringBuilder();
            float el = round.Elapsed;
            int falls = PlayerMotor.All.Where(p => p).Sum(p => p.FallCount);
            sb.Append($"report.summary|time={Clock(el)}|falls={falls}");
            if (round.CurrentChallenge != ChallengeMode.Standard) sb.Append($"\nchallenge.played_{round.CurrentChallenge.ToString().ToLowerInvariant()}");
            float left = round.TimeLimit > 0f ? round.TimeLimit - el : -1f;
            if (win && left >= 0f && left < 30f) sb.Append($"\nreport.close_call|left={Mathf.CeilToInt(left)}");
            var npc = NpcBrain.Instance; if (npc) sb.Append($"\nreport.uncle|attacks={npc.AttacksStarted}|hits={npc.HitsLanded}|cut={npc.Interrupted}");
            foreach (var m in moments.OrderBy(m => m.t).Take(5)) sb.Append($"\nreport.moment|t={Clock(m.t)}|what={m.code.Replace("|", "¦")}");
            // destacados positivos (uno por categoria, solo si hubo algo)
            void Best(string key, System.Func<P, float> v, string unit)
            {
                var top = players.Values.OrderByDescending(v).FirstOrDefault();
                if (top != null && v(top) > 0.5f) sb.Append($"\nreport.best_{key}|name={top.name}|v={Mathf.RoundToInt(v(top))}{unit}");
            }
            Best("hold", p => p.holdUncle, " s"); Best("help", p => p.helps, ""); Best("unanchor", p => p.unanchors, "");
            Best("drive", p => p.driveCart, " s"); Best("cut", p => p.cuts, ""); Best("close", p => p.closes, "");
            LastReport = sb.ToString();
            Debug.Log("[Report] " + LastReport.Replace("\n", " || "));
        }
        static string Clock(float s) => $"{(int)(s / 60f)}:{(int)(s % 60f):00}";

        /// Traduce el parte (en cada jugador). Los momentos traen su propio codigo anidado (¦ en lugar de |).
        public static string Render(string report, int maxBest = 3)
        {
            if (string.IsNullOrEmpty(report)) return "";
            var lines = report.Split('\n'); var outp = new List<string>(); int best = 0;
            foreach (var l in lines)
            {
                if (l.StartsWith("report.best_") && ++best > maxBest) continue;
                if (l.StartsWith("report.moment|"))
                {
                    string t = Between(l, "t="), what = l.Substring(l.IndexOf("what=") + 5).Replace("¦", "|");
                    outp.Add($"  {t}  {Loc.Coded(what)}"); continue;
                }
                outp.Add(Loc.Coded(l));
            }
            return string.Join("\n", outp);
        }
        static string Between(string s, string k) { int i = s.IndexOf(k); if (i < 0) return ""; i += k.Length; int j = s.IndexOf('|', i); return j < 0 ? s.Substring(i) : s.Substring(i, j - i); }
    }
}
