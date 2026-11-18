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
        float nextStrain; AudioSource amb, roll; Vector3 lastCartVel; float lastBump;

        /// Ambiente del bar (bucle, efectos) y ruedas del carrito (bucle 3D que sigue al carrito; volumen y tono por velocidad;
        /// golpe suave cuando frena de golpe contra algo). Solo presentacion, en todos los pares.
        void LoopSources()
        {
            if (!amb) { amb = gameObject.AddComponent<AudioSource>(); amb.clip = SfxSynth.Get("ambience"); amb.loop = true; amb.spatialBlend = 0f; amb.playOnAwake = false; }
            bool live = round && round.State == RoundState.Running;
            amb.volume = 0.22f * GameSettings.EffectsVolume; if (live && !amb.isPlaying) amb.Play();
            var cart = TransportCart.Instance; if (!cart) return;
            if (!roll)
            {
                var go = new GameObject("CartRoll"); go.transform.SetParent(cart.transform, false);
                roll = go.AddComponent<AudioSource>(); roll.clip = SfxSynth.Get("cart_roll"); roll.loop = true; roll.spatialBlend = 1f; roll.minDistance = 2f; roll.maxDistance = 25f; roll.rolloffMode = AudioRolloffMode.Linear; roll.volume = 0f; roll.Play();
            }
            var rb = cart.GetComponent<Rigidbody>();
            Vector3 v = rb ? rb.linearVelocity : Vector3.zero; if (rb && rb.isKinematic) v = (cart.transform.position - lastCartPos) / Mathf.Max(1e-4f, Time.deltaTime);
            lastCartPos = cart.transform.position;
            float sp = new Vector3(v.x, 0, v.z).magnitude;
            roll.volume = Mathf.Clamp01(sp / 2.5f) * 0.6f * GameSettings.EffectsVolume; roll.pitch = 0.8f + Mathf.Clamp01(sp / 3f) * 0.5f;
            if ((lastCartVel - v).magnitude > 1.3f && lastCartVel.magnitude > 1f && Time.time - lastBump > 0.35f) { Play3D("cart_bump", cart.transform.position + Vector3.up * 0.5f, 0.8f); lastBump = Time.time; }
            lastCartVel = v;
        }
        Vector3 lastCartPos;

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
            LoopSources();
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
                // soltar a proposito (sin el boton) vs resbalarse (el boton sigue apretado: lo arrancaron, distancia, golpe)
                bool lostA = a == "" && held0 != "", lostB = b == "" && held1 != "";
                if (lostA || lostB)
                {
                    bool slipped = (lostA && LabInput.RawHeld(Act.GrabLeft)) || (lostB && LabInput.RawHeld(Act.GrabRight));
                    Play2D(slipped ? "slip" : "release", slipped ? 0.75f : 0.6f);
                }
                held0 = a; held1 = b;
                // tension alta: la tela cruje cada tanto (cuanto mas tension, mas seguido)
                float tn = Mathf.Max(a != "" ? local.Tension01(0) : 0f, b != "" ? local.Tension01(1) : 0f);
                if (tn > 0.65f && Time.time >= nextStrain) { Play2D("strain", 0.25f + 0.35f * tn); nextStrain = Time.time + Mathf.Lerp(1.1f, 0.45f, tn); }
            }
            var van = VanDoors.Instance;
            if (van)
            {
                var ph = van.CurrentPhase;
                if (ph == VanDoors.Phase.Closed && lastVan != VanDoors.Phase.Closed) { Play3D("van_close", van.closePoint, 1f); Play3D("latch", van.closePoint, 0.9f); }
                lastVan = ph;
            }
            if (round)
            {
                var rs = round.State;
                if (rs != lastRound && rs == RoundState.Success) Play2D("win_sting", 0.8f);
                if (rs != lastRound && rs == RoundState.Timeout) Play2D("timeout", 0.8f);
                lastRound = rs;
            }
        }
    }
}
