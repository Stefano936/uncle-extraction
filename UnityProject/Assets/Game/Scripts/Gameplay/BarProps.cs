using System.Collections.Generic;
using UnityEngine;

namespace UncleExtraction
{
    /// Un bar que reacciona (sesion 15): torre de vasos sobre una mesita alta junto al paso del salon. Si el carrito, el tio
    /// o un jugador la pasan rapido (>1,2 m/s) se viene abajo: estruendo, el barman reacciona y el tio se DISTRAE 2,5 s
    /// mirando el desastre (se suelta de la barra si estaba aferrado). Una vez por ronda: un compañero puede tirarla a
    /// proposito para provocar la pausa justo cuando otro libera el anclaje. La decide el anfitrion y llega a todos por RPC;
    /// los vasos que caen son solo visuales (no chocan con jugadores, tio ni carrito) y la torre se rearma al reiniciar.
    public class BarProps : MonoBehaviour, IResettable
    {
        public static BarProps Instance { get; private set; }
        public static readonly Vector3 TowerPos = new Vector3(-1.5f, 0f, 0.5f); // junto a la salida: cae si el carrito toma mal la curva (en 2,7/1,2 el carrito la tiraba siempre al arrancar)
        public const float TriggerSpeed = 1.2f, DistractSeconds = 2.5f;
        GameObject root; readonly List<Rigidbody> glasses = new List<Rigidbody>(); bool fallen;
        public bool Fallen => fallen;
        public int Falls { get; private set; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        int builtRound = -1;
        void Start() { Build(); }
        public void ResetState() { Build(); }
        /// Sesion 17: la torre sigue al numero de ronda REPLICADO en todos los pares (antes solo el anfitrion la rearmaba y un
        /// invitado podia quedar con la torre caida y sin efecto en la ronda siguiente).
        void Update()
        {
            var r = RoundDirector.Instance; if (!r) return;
            if (r.Restarts != builtRound) { Build(); builtRound = r.Restarts; }
        }

        void Build()
        {
            if (root) Destroy(root);
            glasses.Clear(); fallen = false;
            var L = LevelLayout.Current; if (!L || L.displayName != "The Last Round") return;
            root = new GameObject("Glass_Tower");
            var wood = Mat("Tower_Table", new Color(0.32f, 0.18f, 0.1f), 0.35f); var glassM = Mat("Tower_Glass", new Color(0.75f, 0.9f, 0.95f, 1f), 0.92f);
            // mesita alta (solida: es mobiliario) con colision real
            var top = Prim(PrimitiveType.Cylinder, "Table_Top", TowerPos + Vector3.up * 1.0f, new Vector3(0.6f, 0.03f, 0.6f), wood, true);
            Prim(PrimitiveType.Cylinder, "Table_Leg", TowerPos + Vector3.up * 0.5f, new Vector3(0.08f, 0.5f, 0.08f), wood, true);
            Prim(PrimitiveType.Cylinder, "Table_Foot", TowerPos + Vector3.up * 0.02f, new Vector3(0.4f, 0.02f, 0.4f), wood, false);
            // piramide de 10 vasos (4-3-2-1)
            int[] rows = { 4, 3, 2, 1 }; float y = 1.03f + 0.06f;
            for (int r = 0; r < rows.Length; r++, y += 0.12f)
                for (int i = 0; i < rows[r]; i++)
                {
                    float x = (i - (rows[r] - 1) * 0.5f) * 0.085f;
                    var g = Prim(PrimitiveType.Cylinder, "Glass", TowerPos + new Vector3(x, y, 0f), new Vector3(0.07f, 0.06f, 0.07f), glassM, true);
                    var rb = g.AddComponent<Rigidbody>(); rb.mass = 0.25f; rb.isKinematic = true; glasses.Add(rb);
                }
            // zona de deteccion (anfitrion)
            var trig = new GameObject("Tower_Trigger"); trig.transform.SetParent(root.transform, false); trig.transform.position = TowerPos + Vector3.up * 0.8f;
            var bc = trig.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(1.1f, 1.6f, 1.1f);
            trig.AddComponent<TowerTrigger>().owner = this;
            BuildBell();
            IgnoreActors();
        }

        // ---------- Campana de ultima ronda (sesion 17, 4.1) ----------
        public static readonly Vector3 BellPos = new Vector3(6.2f, 1.13f, 8.62f), BarmanPos = new Vector3(4.5f, 1.55f, 9.75f);
        public const float BellSeconds = 2.0f, BellCooldown = 20f, BellReleaseFactor = 0.4f; public const int BellMaxPerRound = 2;
        float bellReadyAt; int bellUses; GameObject bellDome; float bellShake;
        public bool HasBell => bellDome;
        public int BellUses => bellUses;
        /// "" = se puede tocar; si no, la clave del motivo (espera o agotada).
        public string BellBlock()
        {
            if (bellUses >= BellMaxPerRound) return "interact.bell_spent";
            float left = bellReadyAt - Time.time; return left > 0f ? Loc.Code("interact.bell_wait", ("s", Mathf.CeilToInt(left).ToString())) : "";
        }
        /// Anfitrion: un jugador la toca. Costo: espera de 20 s, dos por ronda y la segunda rinde la mitad.
        public void RingBell(PlayerHands who)
        {
            if (!NetAuth.IsServer || BellBlock() != "" || RoundDirector.Instance == null || RoundDirector.Instance.State != RoundState.Running) return;
            bellUses++; bellReadyAt = Time.time + BellCooldown;
            float dur = BellSeconds * (bellUses >= 2 ? 0.5f : 1f);
            bool heard = NpcBrain.Instance && NpcBrain.Instance.HearBell(BarmanPos, dur);
            Debug.Log($"[Props] campana por {(who ? who.OwnerName : "?")} (uso {bellUses}/{BellMaxPerRound}, {dur:0.0}s, el tio {(heard ? "se da vuelta" : "no reacciona: ocupado")})");
            RoundStats.Bell(who); RoundDirector.Instance.BellRang();
        }
        /// En todos los pares: suena, la campana se sacude y el tio dice su linea (una por toque).
        public void BellFx(int round)
        {
            var rd = RoundDirector.Instance; if (round >= 0 && rd && round != rd.Restarts) return;
            BellRings++;
            if (GameAudio.Instance) GameAudio.Instance.Play3D("bell", BellPos, 1f);
            if (UncleDialogue.Instance) UncleDialogue.Instance.Say("last_call");
            bellShake = 0.6f;
        }
        public int BellRings { get; private set; }
        void LateUpdate()
        {
            if (!bellDome || bellShake <= 0f) return;
            bellShake -= Time.deltaTime; float k = Mathf.Max(0f, bellShake) / 0.6f;
            bellDome.transform.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 55f) * 9f * k, 0f, Mathf.Cos(Time.time * 47f) * 9f * k);
        }
        void BuildBell()
        {
            bellUses = 0; bellReadyAt = 0f; bellShake = 0f;
            var brass = Mat("Bell_Brass", new Color(0.86f, 0.66f, 0.28f), 0.82f); var dark = Mat("Bell_Base", new Color(0.12f, 0.08f, 0.06f), 0.4f);
            Prim(PrimitiveType.Cylinder, "Bell_Base", BellPos + Vector3.up * 0.012f, new Vector3(0.13f, 0.012f, 0.13f), dark, false);
            bellDome = Prim(PrimitiveType.Sphere, "Bell_Dome", BellPos + Vector3.up * 0.055f, new Vector3(0.1f, 0.075f, 0.1f), brass, false);
            Prim(PrimitiveType.Cylinder, "Bell_Knob", BellPos + Vector3.up * 0.1f, new Vector3(0.018f, 0.012f, 0.018f), brass, false);
        }

        void IgnoreActors()
        {   // los vasos son decorado que cae: no empujan ni traban a nadie
            var actors = new List<Collider>();
            foreach (var pm in PlayerMotor.All) if (pm) actors.AddRange(pm.GetComponentsInChildren<Collider>());
            if (NpcBrain.Instance) actors.AddRange(NpcBrain.Instance.GetComponentsInChildren<Collider>());
            if (TransportCart.Instance) actors.AddRange(TransportCart.Instance.GetComponentsInChildren<Collider>());
            foreach (var g in glasses) foreach (var a in actors) if (g && a) Physics.IgnoreCollision(g.GetComponent<Collider>(), a);
        }

        /// Anfitrion: algo entro rapido a la zona.
        public void Touched(Collider c)
        {
            if (fallen || !NetAuth.IsServer || RoundDirector.Instance == null || RoundDirector.Instance.State != RoundState.Running) return;
            var rb = c.attachedRigidbody; if (!rb) return;
            bool actor = rb.GetComponentInParent<TransportCart>() || NpcBody.Of(rb) || rb.GetComponent<PlayerMotor>();
            if (!actor) return;
            Vector3 v = rb.linearVelocity; v.y = 0f;
            if (v.magnitude < TriggerSpeed) return;
            string who = rb.GetComponentInParent<TransportCart>() ? "carrito" : NpcBody.Of(rb) ? "tio" : rb.GetComponent<PlayerMotor>().playerName;
            Debug.Log($"[Props] torre de vasos al piso por {who} a {v.magnitude:0.0} m/s");
            RoundDirector.Instance.TowerFall(v.normalized); RoundStats.Tower(who == "carrito" ? 1 : who == "tio" ? 2 : 0, who == "carrito" || who == "tio" ? "" : who);
            if (NpcBrain.Instance) NpcBrain.Instance.DistractBy(TowerPos + Vector3.up, DistractSeconds, "¡Los vasos!");
        }

        /// En todos los pares (RPC): los vasos caen, suena y reacciona el barman.
        /// round: numero de ronda en que la decidio el anfitrion; un evento de otra ronda se descarta.
        public void Fall(Vector3 dir, int round = -1)
        {
            var rd = RoundDirector.Instance;
            if (round >= 0 && rd && round != rd.Restarts) { Debug.Log($"[Props] caida de la ronda {round} descartada (ronda actual {rd.Restarts})"); return; }
            if (fallen) return; fallen = true; Falls++;
            Debug.Log($"[Props] torre al piso (ronda {(rd ? rd.Restarts : -1)}, caidas {Falls})");
            IgnoreActors();
            foreach (var g in glasses) if (g) { g.isKinematic = false; g.AddForce((dir * 1.6f + Random.insideUnitSphere * 0.9f + Vector3.up * 0.8f) * g.mass, ForceMode.Impulse); g.AddTorque(Random.insideUnitSphere * 0.05f, ForceMode.Impulse); }
            if (GameAudio.Instance) GameAudio.Instance.Play3D("glass_crash", TowerPos + Vector3.up, 1f);
            if (UncleDialogue.Instance) UncleDialogue.Instance.Say("glass_crash");
        }

        GameObject Prim(PrimitiveType t, string n, Vector3 pos, Vector3 scale, Material m, bool collider)
        {
            var g = GameObject.CreatePrimitive(t); g.name = n; g.transform.SetParent(root.transform, false); g.transform.position = pos; g.transform.localScale = scale;
            if (!collider) Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }
        // sesion 17 (perfil): un material por nombre para toda la sesion (antes se creaba uno nuevo en cada reconstruccion
        // por ronda y los viejos quedaban vivos).
        static readonly System.Collections.Generic.Dictionary<string, Material> matCache = new();
        public static int MaterialsCreated => matCache.Count;
        static Material Mat(string n, Color c, float smooth)
        {
            if (matCache.TryGetValue(n, out var cached) && cached) return cached;
            var made = MatNew(n, c, smooth); matCache[n] = made; return made;
        }
        static Material MatNew(string n, Color c, float smooth)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit"); var m = new Material(sh ? sh : Shader.Find("Standard")) { name = n, color = c };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth); return m;
        }
    }

    public class TowerTrigger : MonoBehaviour
    {
        public BarProps owner;
        void OnTriggerStay(Collider c) { if (owner) owner.Touched(c); }
    }
}
