using System;
using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Lineas del tio como subtitulos, vinculadas a situaciones reales de la partida (catalogo original
    /// preproduction/uncle_extraction/data/dialogue_en_es.csv, copiado sin cambios a Resources/Localization).
    /// Sin voz: el catalogo marca audio_status=NOT_RECORDED. Prioridad y enfriamiento del catalogo, memoria de las
    /// ultimas lineas, idioma del jugador y filtro de insultos (columnas clean_*). Perfil del alcance inicial: alcohol.
    /// Solo presentacion, local a cada jugador (no decide nada; no viaja por red).
    public class UncleDialogue : MonoBehaviour
    {
        public static UncleDialogue Instance { get; private set; }
        public static bool ProfanityFilter;

        [Serializable] public class Line { public string id, ev, speaker, profile, en, es, cleanEn, cleanEs; public int priority; public float cooldown; }
        static List<Line> lines;
        readonly Dictionary<string, float> lastSaid = new Dictionary<string, float>();
        readonly Queue<string> recent = new Queue<string>();
        float shownUntil, shownPriority; public string CurrentId { get; private set; } = "";
        public string Current => Time.time < shownUntil && CurrentLine != null ? Text(CurrentLine) : "";
        public string CurrentSpeaker => CurrentLine != null ? CurrentLine.speaker : "";
        Line CurrentLine;
        public int Said { get; private set; }

        RoundDirector round; NpcBrain npc; NpcBody body; NpcState lastState; NpcPosture lastPosture; bool grabbedOnce, strapped, secured, nearVan, doorBlocked, roundStarted;
        readonly Dictionary<PlayerMotor, int> falls = new Dictionary<PlayerMotor, int>();
        RoundState lastRound; int lastRestarts = -1;

        void Awake() { Instance = this; Load(); }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Start() { round = FindAnyObjectByType<RoundDirector>(); npc = FindAnyObjectByType<NpcBrain>(); body = npc ? npc.GetComponent<NpcBody>() : null; }

        public static List<Line> Load()
        {
            if (lines != null) return lines;
            lines = new List<Line>();
            var ta = Resources.Load<TextAsset>("Localization/dialogue_en_es");
            if (!ta) { Debug.LogWarning("[Dialogue] falta el catalogo"); return lines; }
            var rows = Csv(ta.text);
            if (rows.Count < 2) return lines;
            var h = rows[0]; int I(string k) => h.IndexOf(k);
            foreach (var r in rows.GetRange(1, rows.Count - 1))
            {
                if (r.Count < h.Count) continue;
                string prof = r[I("profile")];
                if (!(prof.Contains("alcohol") || prof == "" || prof == "all")) continue; // alcance inicial: alcohol
                lines.Add(new Line
                {
                    id = r[I("id")], ev = r[I("event")], speaker = r[I("speaker")], profile = prof,
                    priority = int.TryParse(r[I("priority")], out var p) ? p : 10, cooldown = float.TryParse(r[I("cooldown_s")], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var c) ? c : 20f,
                    en = r[I("text_en")], es = r[I("text_es")], cleanEn = r[I("clean_en")], cleanEs = r[I("clean_es")],
                });
            }
            Debug.Log($"[Dialogue] {lines.Count} lineas del perfil alcohol");
            return lines;
        }

        static List<List<string>> Csv(string text)
        {
            var rows = new List<List<string>>(); var row = new List<string>(); var cell = new System.Text.StringBuilder(); bool q = false;
            text = text.TrimStart('﻿');
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (q) { if (ch == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else q = false; } else cell.Append(ch); }
                else if (ch == '"') q = true;
                else if (ch == ',') { row.Add(cell.ToString()); cell.Clear(); }
                else if (ch == '\n') { row.Add(cell.ToString().TrimEnd('\r')); cell.Clear(); rows.Add(row); row = new List<string>(); }
                else cell.Append(ch);
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString().TrimEnd('\r')); rows.Add(row); }
            return rows;
        }

        public static string Text(Line l)
        {
            bool es = Loc.Language == Loc.EsUY;
            string s = ProfanityFilter ? (es ? l.cleanEs : l.cleanEn) : (es ? l.es : l.en);
            return string.IsNullOrEmpty(s) ? (es ? l.es : l.en) : s;
        }

        /// Pide una linea para un evento; devuelve true si se mostro (respeta prioridad, enfriamiento y memoria reciente).
        public bool Say(string ev)
        {
            var cands = Load().FindAll(l => l.ev == ev);
            if (cands.Count == 0) return false;
            cands.RemoveAll(l => lastSaid.TryGetValue(l.id, out float t) && Time.time - t < l.cooldown);
            if (cands.Count == 0) return false;
            var fresh = cands.FindAll(l => !recent.Contains(l.id)); if (fresh.Count > 0) cands = fresh;
            var pick = cands[UnityEngine.Random.Range(0, cands.Count)];
            if (Time.time < shownUntil && pick.priority < shownPriority) return false; // no pisar una linea mas importante
            CurrentLine = pick; CurrentId = pick.id; shownPriority = pick.priority;
            shownUntil = Time.time + Mathf.Clamp(1.5f + Text(pick).Length * 0.05f, 2.2f, 5f);
            lastSaid[pick.id] = Time.time; recent.Enqueue(pick.id); while (recent.Count > 6) recent.Dequeue();
            Said++;
            Debug.Log($"[Dialogue] {pick.id} ({ev})");
            return true;
        }

        void Update()
        {
            if (!round || !npc || !body) return;
            if (round.Restarts != lastRestarts) { lastRestarts = round.Restarts; grabbedOnce = strapped = secured = nearVan = doorBlocked = roundStarted = false; falls.Clear(); }
            bool running = round.State == RoundState.Running && !round.InLobby;
            if (running && !roundStarted) { roundStarted = true; Say("round_start"); }
            if (round.State != lastRound) { if (round.State == RoundState.Timeout) Say("round_timeout"); lastRound = round.State; }
            var zone0 = ExtractionZone.Instance;
            if (zone0 && zone0.Secured && !secured) { secured = true; Say("secured"); }
            if (!running) return;
            var st = npc.State;
            if (st != lastState)
            {
                switch (st)
                {
                    case NpcState.Protest: Say("protest"); break;
                    case NpcState.Anchor: Say("anchor_bar"); break;
                    case NpcState.Recover: case NpcState.ForcedRecovery: Say("recover"); break;
                    case NpcState.Escape: Say("escape"); break;
                    case NpcState.Distracted: Say("distraction"); break;
                    case NpcState.Windup:
                        if (npc.WriggleAttempt) Say("cart_escape");
                        else { var id = npc.CurrentAttack.id; Say(id == AttackId.Slap ? "slap_windup" : id == AttackId.Push ? "push_windup" : id == AttackId.Kick ? "kick_windup" : "shake_windup"); }
                        break;
                }
                lastState = st;
            }
            if (body.Posture == NpcPosture.Fallen && lastPosture == NpcPosture.Upright && st != NpcState.Carried) Say("self_fall");
            lastPosture = body.Posture;
            if (!grabbedOnce && AnyoneHoldsUncle()) { grabbedOnce = true; Say("grab_first"); }
            var cart = TransportCart.Instance;
            if (cart && cart.IsStrapped && !strapped) { strapped = true; Say("cart_seated"); } else if (cart && !cart.IsStrapped) strapped = false;
            var van = VanDoors.Instance; var zone = ExtractionZone.Instance;
            if (van && !nearVan && Vector3.Distance(body.Body.position, van.closePoint) < 4f) { nearVan = true; Say("van_approach"); }
            if (van && zone && zone.AllPointsInside && !zone.Secured)
            {
                bool blocked = van.BlockReason().StartsWith("van.part_outside");
                if (blocked && !doorBlocked) Say("door_blocked");
                doorBlocked = blocked;
            }
            foreach (var pm in PlayerMotor.All)
            {
                if (!pm) continue; int f = pm.FallCount;
                if (falls.TryGetValue(pm, out int prev) && f > prev) Say("player_fall");
                falls[pm] = f;
            }
        }

        bool AnyoneHoldsUncle()
        {
            foreach (var pm in PlayerMotor.All)
            {
                if (!pm) continue; var h = pm.GetComponent<PlayerHands>(); if (!h) continue;
                for (int k = 0; k < 2; k++) { var s = h.HeldSocket(k); if (s && body.Owns(s.body)) return true; }
            }
            return false;
        }
    }
}
