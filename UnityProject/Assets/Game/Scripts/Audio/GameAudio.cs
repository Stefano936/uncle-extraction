using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Sonido de la partida a partir del estado replicado (todos los pares; solo presentacion, no decide nada):
    /// senal de preparacion distinta por ataque y golpe lanzado en el tio (3D), impactos al caer un jugador, agarrar y
    /// soltar del jugador local (2D), cierre de la camioneta y fin de ronda. Las senales (Cues) no se atenuan por la voz
    /// ni por la musica (biblia 03, seccion 08). Volumen: GameSettings (general).
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }
        /// Cuantos sonidos se pidieron por id (pruebas).
        public readonly Dictionary<string, int> Played = new Dictionary<string, int>();
        RoundDirector round; NpcBrain npc; NpcBody body; AudioSource ui; readonly List<AudioSource> pool = new List<AudioSource>();
        NpcState lastNpc; RoundState lastRound; VanDoors.Phase lastVan; int lastAttacks;
        readonly Dictionary<PlayerMotor, int> falls = new Dictionary<PlayerMotor, int>();
        string held0 = "", held1 = "";

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            round = FindAnyObjectByType<RoundDirector>(); npc = FindAnyObjectByType<NpcBrain>(); body = npc ? npc.GetComponent<NpcBody>() : null;
            ui = gameObject.AddComponent<AudioSource>(); ui.spatialBlend = 0f; ui.playOnAwake = false;
            for (int i = 0; i < 8; i++)
            {
                var go = new GameObject("Sfx3D_" + i); go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>(); s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 2f; s.maxDistance = 30f; s.playOnAwake = false;
                pool.Add(s);
            }
            if (npc) { lastNpc = npc.State; lastAttacks = npc.AttacksStarted; }
            if (round) lastRound = round.State;
            if (VanDoors.Instance) lastVan = VanDoors.Instance.CurrentPhase;
        }

        /// Categoria de cada sonido: avisos (deben oirse siempre que el jugador no los baje a proposito) o efectos.
        public static bool IsAlert(string id) => id.StartsWith("cue_") || id == "ping" || id == "success" || id == "timeout" || id == "ui";
        public static float CategoryVolume(string id) => IsAlert(id) ? GameSettings.AlertsVolume : GameSettings.EffectsVolume;
        public void Play2D(string id, float vol = 1f) { Count(id); if (ui) ui.PlayOneShot(SfxSynth.Get(id), vol * CategoryVolume(id)); }
        public void Play3D(string id, Vector3 at, float vol = 1f, bool cue = false)
        {
            Count(id);
            AudioSource s = null; foreach (var p in pool) if (!p.isPlaying) { s = p; break; }
            if (!s) s = pool[0];
            s.transform.position = at; s.clip = SfxSynth.Get(id); s.volume = vol * CategoryVolume(id);
            s.spatialBlend = cue ? 0.6f : 1f; // las senales criticas se oyen aunque se mire al otro lado
            s.Play();
        }
        void Count(string id) { Played.TryGetValue(id, out int n); Played[id] = n + 1; }

        void Update()
        {
            if (npc && body)
            {
                var st = npc.State;
                if (st != lastNpc)
                {
                    Vector3 at = body.ChestPosition;
                    if (st == NpcState.Windup)
                    {
                        var id = npc.CurrentAttack.id;
                        Play3D(id == AttackId.Slap ? "cue_slap" : id == AttackId.Push ? "cue_push" : id == AttackId.Kick ? "cue_kick" : "cue_shake", at, 1f, true);
                    }
                    else if (st == NpcState.Attack) Play3D("whoosh", at, 0.8f);
                    lastNpc = st;
                }
            }
            foreach (var pm in PlayerMotor.All)
            {
                if (!pm) continue;
                int f = pm.FallCount;
                if (falls.TryGetValue(pm, out int prev) && f > prev) Play3D("impact", pm.transform.position + Vector3.up, 1f);
                falls[pm] = f;
            }
            var local = PlayerControls.Local ? PlayerControls.Local.GetComponent<PlayerHands>() : null;
            if (local)
            {
                string a = local.HeldSocketId(0), b = local.HeldSocketId(1);
                if ((a != "" && held0 == "") || (b != "" && held1 == "")) Play2D("grab", 0.7f);
                if ((a == "" && held0 != "") || (b == "" && held1 != "")) Play2D("release", 0.6f);
                held0 = a; held1 = b;
            }
            var van = VanDoors.Instance;
            if (van)
            {
                var ph = van.CurrentPhase;
                if (ph == VanDoors.Phase.Closed && lastVan != VanDoors.Phase.Closed) Play3D("van_close", van.closePoint, 1f);
                lastVan = ph;
            }
            if (round)
            {
                var rs = round.State;
                if (rs != lastRound && rs == RoundState.Success) Play2D("success", 0.8f);
                if (rs != lastRound && rs == RoundState.Timeout) Play2D("timeout", 0.8f);
                lastRound = rs;
            }
        }
    }
}
