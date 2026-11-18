using UnityEngine;

namespace UncleExtraction
{
    public enum ChallengeMode : byte { Standard = 0, NoCart = 1, Narrow = 2 }

    /// Desafios opcionales en el mismo bar (sesion 15). La ronda estandar sigue siendo la principal.
    /// - Sin carrito: el carrito queda guardado fuera de juego (cinemático, lejos); hay que arrastrarlo.
    /// - Ruta estrecha: una barricada de cajones de cerveza cruza el salon (z = 4) y deja un unico paso de 1,2 m (el carrito
    ///   mide 0,95 m): con carrito hay que apuntar bien; arrastrando, el desvio cambia el plan. Es estatica y se arma igual en
    ///   todos los pares a partir del modo replicado, asi que no necesita objetos de red.
    /// El autopiloto (pruebas) usa la ruta alternativa por el paso.
    public class Challenges : MonoBehaviour, IResettable
    {
        public static Challenges Instance { get; private set; }
        ChallengeMode applied = (ChallengeMode)255; GameObject barricade; Vector3[] stdDragPath, stdCartPath;

        public static readonly Vector3[] NarrowDragPath = { new Vector3(3.6f, 0, 6.2f), new Vector3(0.8f, 0, 5.6f), new Vector3(-2.6f, 0, 5.3f), new Vector3(-2.6f, 0, 2.6f), new Vector3(-1.2f, 0, 1.4f), new Vector3(0, 0, -1.0f), new Vector3(0, 0, -3.5f), new Vector3(0, 0, -6.5f), new Vector3(0, 0, -8.3f), new Vector3(0, 0, -10.6f) };
        // segmentos [xMin, xMax] de la barricada en z = 4; paso libre entre -3,2 y -2,0
        static readonly Vector2[] Segments = { new Vector2(-9f, -7.5f), new Vector2(-6.0f, -3.2f), new Vector2(-2.0f, 9f) };
        public const float BarricadeZ = 4.0f;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        public void ResetState() { applied = (ChallengeMode)255; }

        int appliedRound = -1;
        public static readonly Vector3 CartStorage = new Vector3(16f, 0f, 16f);
        void Update()
        {
            var r = RoundDirector.Instance; if (!r) return;
            var mode = r.CurrentChallenge;
            // sesion 17: se vuelve a aplicar en CADA ronda (numero de ronda replicado) y en todos los pares, sin estado guardado
            // que pueda quedar viejo (antes «cartStored» impedia volver a guardar el carrito tras un reinicio en Sin carrito)
            if (mode != applied || r.Restarts != appliedRound) { Apply(mode); applied = mode; appliedRound = r.Restarts; }
            else if (mode == ChallengeMode.NoCart) KeepStored();
        }

        /// Sin carrito: en todos los pares el carrito no se ve, no choca y no se puede agarrar; el anfitrion lo deja guardado.
        static void SetCartPresent(TransportCart cart, bool present)
        {
            // con el modelo propio (Cart_Visual) las primitivas funcionales estan ocultas a proposito: solo se tocan las del modelo
            var visual = cart.transform.Find("Cart_Visual");
            foreach (var r in cart.GetComponentsInChildren<Renderer>(true))
            {
                bool isVisual = visual ? r.transform.IsChildOf(visual) : r.gameObject.name != "Cart_Bed";
                if (isVisual) r.enabled = present;
            }
            foreach (var c in cart.GetComponentsInChildren<Collider>(true)) c.enabled = present;
            foreach (var s in cart.GetComponentsInChildren<GrabSocket>(true)) s.enabled = present;
        }
        void KeepStored()
        {
            var cart = TransportCart.Instance; if (!cart || !NetAuth.IsServer) return;
            if ((cart.transform.position - CartStorage).sqrMagnitude > 0.25f) Store(cart); // un reinicio lo devolvio al bar
        }
        static void Store(TransportCart cart)
        {
            var rb = cart.GetComponent<Rigidbody>();
            if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; rb.position = CartStorage; }
            cart.transform.position = CartStorage; TransportCart.Teleport(cart, CartStorage, cart.transform.rotation);
        }

        void Apply(ChallengeMode mode)
        {
            var L = LevelLayout.Current; bool bar = L && L.displayName == "The Last Round";
            if (L && stdDragPath == null) { stdDragPath = L.uncleDragPath; stdCartPath = L.cartPullPath; }
            // barricada
            if (barricade) { Destroy(barricade); barricade = null; }
            if (mode == ChallengeMode.Narrow && bar) barricade = BuildBarricade();
            if (L) L.uncleDragPath = mode == ChallengeMode.Narrow && bar ? NarrowDragPath : stdDragPath;
            // carrito: presente o guardado segun el modo de ESTA ronda
            var cart = TransportCart.Instance;
            if (cart)
            {
                bool stored = mode == ChallengeMode.NoCart;
                SetCartPresent(cart, !stored);
                if (NetAuth.IsServer)
                {
                    var rb = cart.GetComponent<Rigidbody>();
                    if (stored) Store(cart);
                    else if (rb)
                    {   // vuelve de Sin carrito (o el reinicio ya lo trajo): a su lugar de inicio si seguia guardado, y siempre con fisica
                        if ((cart.transform.position - CartStorage).sqrMagnitude < 1f) { rb.position = cart.SpawnPos; rb.rotation = cart.SpawnRot; cart.transform.SetPositionAndRotation(cart.SpawnPos, cart.SpawnRot); TransportCart.Teleport(cart, cart.SpawnPos, cart.SpawnRot); }
                        if (rb.isKinematic) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                    }
                }
            }
            Debug.Log($"[Challenge] modo {mode}{(mode == ChallengeMode.Narrow && !bar ? " (solo existe en The Last Round)" : "")}");
        }

        static GameObject BuildBarricade()
        {
            var root = new GameObject("Challenge_Narrow");
            var crate = MatColor("Crate", new Color(0.62f, 0.14f, 0.12f), 0.25f);
            var crate2 = MatColor("Crate_Alt", new Color(0.86f, 0.62f, 0.12f), 0.25f);
            var bottle = MatColor("Bottles", new Color(0.18f, 0.32f, 0.16f), 0.75f);
            int k = 0;
            foreach (var seg in Segments)
            {
                float len = seg.y - seg.x, cx = (seg.x + seg.y) * 0.5f;
                var col = new GameObject("Wall"); col.transform.SetParent(root.transform, false);
                col.transform.position = new Vector3(cx, 0.55f, BarricadeZ);
                var bc = col.AddComponent<BoxCollider>(); bc.size = new Vector3(len, 1.1f, 0.5f); // colision simple = lo que se ve
                // cajones apilados (0,45 x 0,3 x 0,5) con botellas arriba: visual sin colision propia
                for (float x = seg.x + 0.25f; x < seg.y - 0.2f; x += 0.48f)
                    for (int lvl = 0; lvl < 3; lvl++)
                    {
                        var c = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(c.GetComponent<Collider>());
                        c.transform.SetParent(root.transform, false);
                        c.transform.position = new Vector3(x, 0.17f + lvl * 0.36f, BarricadeZ + ((k + lvl) % 2 == 0 ? 0.03f : -0.03f));
                        c.transform.rotation = Quaternion.Euler(0, ((k * 7 + lvl * 13) % 9) - 4, 0); c.transform.localScale = new Vector3(0.44f, 0.34f, 0.48f);
                        c.GetComponent<Renderer>().sharedMaterial = (k + lvl) % 3 == 0 ? crate2 : crate; k++;
                        if (lvl == 2) for (int b = 0; b < 3; b++)
                        {
                            var bt = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(bt.GetComponent<Collider>());
                            bt.transform.SetParent(root.transform, false); bt.transform.position = new Vector3(x - 0.13f + b * 0.13f, 1.0f + 0.09f, BarricadeZ);
                            bt.transform.localScale = new Vector3(0.055f, 0.09f, 0.055f); bt.GetComponent<Renderer>().sharedMaterial = bottle;
                        }
                    }
            }
            // flecha pintada en el piso hacia el paso (orienta sin HUD)
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(arrow.GetComponent<Collider>()); arrow.name = "Gap_Marker";
            arrow.transform.SetParent(root.transform, false); arrow.transform.position = new Vector3(-2.6f, 0.012f, BarricadeZ - 0.9f);
            arrow.transform.rotation = Quaternion.Euler(90, 0, 0); arrow.transform.localScale = new Vector3(1.0f, 1.2f, 1f);
            arrow.GetComponent<Renderer>().sharedMaterial = MatColor("Gap_Paint", new Color(0.95f, 0.8f, 0.2f), 0.1f);
            return root;
        }

        static Material MatColor(string n, Color c, float smooth)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit"); var m = new Material(sh ? sh : Shader.Find("Standard")) { name = n, color = c };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth); return m;
        }
    }
}
