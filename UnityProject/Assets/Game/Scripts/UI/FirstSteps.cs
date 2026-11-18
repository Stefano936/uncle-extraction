using UnityEngine;
using UnityEngine.UI;

namespace UncleExtraction
{
    /// Guia de la primera partida: una indicacion por vez, que avanza al detectar la accion real del jugador LOCAL. No
    /// detiene la partida de nadie (en cooperativo cada uno ve la suya). Los textos usan las teclas asignadas y el modo de
    /// agarre elegido.
    /// Ciclo (sesion 14, hallazgo A):
    /// - GANAR la ronda con la guia activa la completa (estado Completed): ya sabe hacer todo lo necesario.
    /// - OMITIR desde la pausa la cierra (estado Skipped). Ninguna de las dos vuelve a aparecer sola.
    /// - PERDER no la completa: el paso actual se guarda y la proxima ronda (reintento, menu y otra partida, otro proceso)
    ///   sigue desde ahi.
    /// - REPETIR desde la pausa empieza de nuevo (sin borrar el estado hasta que vuelva a terminar u omitirse).
    /// - Oculta (pausa, Opciones, sala, fin de ronda) no avanza ni corre sus tiempos: nada avanza por acciones de otros
    ///   mientras el jugador no la ve.
    /// - Traslado: vale el carrito o arrastrarlo; el paso avanza con el tio atado al carrito o acercado a la camioneta.
    public class FirstSteps : MonoBehaviour
    {
        public static FirstSteps Instance { get; private set; }
        public enum Step { Move, Aim, Grab, Release, TwoHands, Attack, Recover, Transport, Extract, Done }
        public enum Outcome { None = 0, Completed = 1, Skipped = 2 }
        const string PrefState = "ue.firstSteps", PrefStep = "ue.firstSteps.step";
        Step step = Step.Done;
        float visibleAge, walked; Vector3 lastPos; bool sawHold; float startDist = -1f; RoundState lastState = RoundState.Running;
        RectTransform box; Text title, body;
        public string CurrentKey => step == Step.Done ? "" : "tut." + step.ToString().ToLowerInvariant();
        public bool Active => step != Step.Done;
        public Step Current => step;

        public static Outcome Saved { get { try { return (Outcome)PlayerPrefs.GetInt(PrefState, 0); } catch { return Outcome.None; } } }
        public static bool Completed => Saved != Outcome.None; // terminada u omitida: no se muestra sola
        static Step SavedStep { get { try { int s = PlayerPrefs.GetInt(PrefStep, 0); return s >= 0 && s < (int)Step.Done ? (Step)s : Step.Move; } catch { return Step.Move; } } }
        static void Save(string k, int v) { try { PlayerPrefs.SetInt(k, v); PlayerPrefs.Save(); } catch { } }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (canvas) Destroy(canvas.gameObject); }

        float builtScale = -1f; Canvas canvas;
        void Build()
        {
            if (box) Destroy(box.gameObject);
            if (!canvas) { canvas = Ui.Canvas("FirstSteps", 15); if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ueNoHud") >= 0) canvas.enabled = false; }
            builtScale = Ui.TextScale;
            box = Ui.PanelBox("Guide", canvas.transform, new Vector2(0f, 0.5f), new Vector2(Ui.Margin, 60), new Vector2(Ui.K(470), Ui.K(150))); box.pivot = new Vector2(0f, 0.5f);
            title = Ui.Label(box, "Title", "", Ui.FontHud, Ui.Focus, TextAnchor.UpperLeft, true);
            title.rectTransform.offsetMin = new Vector2(20, 0); title.rectTransform.offsetMax = new Vector2(-20, -12);
            body = Ui.Label(box, "Body", "", Ui.FontHud + 2, Ui.Text, TextAnchor.UpperLeft);
            body.rectTransform.offsetMin = new Vector2(20, 14); body.rectTransform.offsetMax = new Vector2(-20, -(14 + Ui.K(28)));
            box.gameObject.SetActive(false);
        }

        void Start()
        {
            Build();
            if (!Completed && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ueAutopilot") < 0) Go(SavedStep);
        }

        public void Begin() { Go(Step.Move); }
        public void Skip() { Finish(Outcome.Skipped); }
        /// Herramientas (auditoria de interfaz): mostrar/ocultar sin tocar el progreso guardado del jugador.
        public void ShowForTool() { step = Step.Move; visibleAge = 0f; }
        public void HideForTool() { step = Step.Done; }
        void Finish(Outcome o) { step = Step.Done; Save(PrefState, (int)o); Save(PrefStep, 0); Debug.Log($"[Guide] {(o == Outcome.Completed ? "completada" : "omitida")}"); }

        void Go(Step s)
        {
            step = s; visibleAge = 0f; sawHold = false; startDist = -1f;
            walked = 0f; var pc = PlayerControls.Local; if (pc) lastPos = pc.transform.position;
            if (s != Step.Done) Save(PrefStep, (int)s);
        }

        static string GrabKeys(int hand) => InputBindings.Display(hand == 0 ? Act.GrabLeft : Act.GrabRight);

        void Update()
        {
            var round = RoundDirector.Instance; var pc = PlayerControls.Local; var npc = NpcBrain.Instance;
            if (builtScale != Ui.TextScale) Build(); // tamaño de texto cambiado en plena partida
            // fin de ronda: ganar completa la guia; perder la deja donde estaba (se retoma en el proximo intento)
            if (round && !round.InLobby && round.State != lastState)
            {
                if (round.State == RoundState.Success && Active) Finish(Outcome.Completed);
                if (lastState != RoundState.Running && round.State == RoundState.Running && Active && pc) { walked = 0f; visibleAge = 0f; }
                lastState = round.State;
            }
            bool live = step != Step.Done && pc && round && !round.InLobby && round.State == RoundState.Running;
            bool show = live && !GameFlowUI.BlockingInput;
            if (box && box.gameObject.activeSelf != show) box.gameObject.SetActive(show);
            if (!show) { if (pc) lastPos = pc.transform.position; return; } // oculta: ni avanza ni corre el tiempo
            visibleAge += Time.deltaTime;
            Vector3 now = pc.transform.position; float step1 = Vector3.Distance(new Vector3(now.x, 0, now.z), new Vector3(lastPos.x, 0, lastPos.z)); lastPos = now;
            if (step1 < 0.6f) walked += step1; // un salto de un cuadro (teletransporte, rescate, fisica que se pone al dia al salir de la pausa) no es caminar
            var hands = pc.GetComponent<PlayerHands>();
            bool any = hands && (hands.HeldSocketId(0) != "" || hands.HeldSocketId(1) != "");
            bool both = hands && hands.HeldSocketId(0) != "" && hands.HeldSocketId(1) != "";
            switch (step)
            {
                case Step.Move: if (walked > 1.5f) Go(Step.Aim); break; // solo lo caminado con la guia visible
                case Step.Aim: if (hands && hands.Candidate && hands.Candidate.body && NpcBody.Of(hands.Candidate.body)) Go(Step.Grab); else if (any) Go(Step.Release); break;
                case Step.Grab: if (any) Go(Step.Release); break;
                case Step.Release: if (any) sawHold = true; if (sawHold && !any) Go(Step.TwoHands); break;
                case Step.TwoHands: if (both) Go(Step.Attack); break;
                case Step.Attack: if (npc && (npc.State == NpcState.Windup || npc.State == NpcState.Attack)) Go(Step.Recover); else if (visibleAge > 45f) Go(Step.Transport); break;
                case Step.Recover: if (npc && npc.State == NpcState.Recover) sawHold = true; if ((sawHold && npc && npc.State != NpcState.Recover) || visibleAge > 30f) Go(Step.Transport); break;
                case Step.Transport:
                {
                    float d = UncleToVan();
                    if (startDist < 0f && d >= 0f) startDist = d;
                    bool strapped = TransportCart.Instance && TransportCart.Instance.IsStrapped;
                    bool dragged = d >= 0f && startDist > 0f && (d < startDist * 0.5f || d < 5f); // arrastrandolo, sin carrito
                    if (strapped || dragged || (ExtractionZone.Instance && ExtractionZone.Instance.AllPointsInside)) Go(Step.Extract);
                    break;
                }
                case Step.Extract: break; // termina al ganar la ronda
            }
            if (step != Step.Done) Render();
        }

        static float UncleToVan()
        {
            var v = VanDoors.Instance; var b = NpcBrain.Instance ? NpcBrain.Instance.GetComponent<NpcBody>() : null;
            if (!v || !b) return -1f;
            Vector3 a = b.ChestPosition, c = v.closePoint; a.y = c.y = 0f; return Vector3.Distance(a, c);
        }

        void Render()
        {
            int n = (int)step + 1, total = (int)Step.Done;
            title.text = Loc.T("tut.title", ("n", n.ToString()), ("total", total.ToString()));
            string how = GrabIntent.Toggle ? Loc.T("tut.tap") : Loc.T("tut.hold");
            switch (step)
            {
                case Step.Move: body.text = Loc.T("tut.move", ("keys", InputBindings.MoveDisplay())); break;
                case Step.Aim: body.text = Loc.T("tut.aim"); break;
                case Step.Grab: body.text = Loc.T("tut.grab", ("left", GrabKeys(0)), ("right", GrabKeys(1)), ("how", how)); break;
                case Step.Release: body.text = GrabIntent.Toggle ? Loc.T("tut.release_toggle", ("key", InputBindings.Display(Act.ReleaseAll))) : Loc.T("tut.release", ("key", InputBindings.Display(Act.ReleaseAll))); break;
                case Step.TwoHands: body.text = Loc.T("tut.twohands", ("effort", InputBindings.Display(Act.Effort))); break;
                case Step.Attack: body.text = Loc.T("tut.attack", ("dodge", InputBindings.Display(Act.Dodge))); break;
                case Step.Recover: body.text = Loc.T("tut.recover", ("key", InputBindings.Display(Act.Interact))); break;
                case Step.Transport: body.text = Loc.T("tut.transport", ("key", InputBindings.Display(Act.Interact))); break;
                case Step.Extract: body.text = Loc.T("tut.extract", ("key", InputBindings.Display(Act.Interact))); break;
            }
        }
    }
}
