using UnityEngine;
using UnityEngine.UI;

namespace UncleExtraction
{
    /// Guia de la primera partida (sesion 13): una indicacion por vez, que avanza al detectar la accion real del jugador
    /// LOCAL. No detiene la partida de nadie (en cooperativo cada uno ve la suya). Se omite desde la pausa, se repite desde
    /// la pausa, y no vuelve a aparecer sola una vez terminada u omitida (preferencia «ue.firstSteps»). Los textos usan las
    /// teclas asignadas y el modo de agarre elegido (mantener o alternar).
    public class FirstSteps : MonoBehaviour
    {
        public static FirstSteps Instance { get; private set; }
        enum Step { Move, Aim, Grab, Release, TwoHands, Attack, Recover, Cart, Extract, Done }
        Step step = Step.Done;
        float stepStart; Vector3 movedFrom; bool sawHold;
        RectTransform box; Text title, body;
        public string CurrentKey => step == Step.Done ? "" : "tut." + step.ToString().ToLowerInvariant();
        public bool Active => step != Step.Done;

        public static bool Completed { get { try { return PlayerPrefs.GetInt("ue.firstSteps", 0) == 1; } catch { return false; } } }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (box) Destroy(box.gameObject); }

        void Start()
        {
            var canvas = Ui.Canvas("FirstSteps", 15);
            box = Ui.PanelBox("Guide", canvas.transform, new Vector2(0f, 0.5f), new Vector2(Ui.Margin, 60), new Vector2(Ui.K(470), Ui.K(150))); box.pivot = new Vector2(0f, 0.5f);
            title = Ui.Label(box, "Title", "", Ui.FontHud, Ui.Focus, TextAnchor.UpperLeft, true);
            title.rectTransform.offsetMin = new Vector2(20, 0); title.rectTransform.offsetMax = new Vector2(-20, -12);
            body = Ui.Label(box, "Body", "", Ui.FontHud + 2, Ui.Text, TextAnchor.UpperLeft);
            body.rectTransform.offsetMin = new Vector2(20, 14); body.rectTransform.offsetMax = new Vector2(-20, -(14 + Ui.K(28)));
            box.gameObject.SetActive(false);
            if (!Completed && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ueAutopilot") < 0) Begin();
        }

        public void Begin() { Go(Step.Move); }
        public void Skip() { Go(Step.Done); try { PlayerPrefs.SetInt("ue.firstSteps", 1); PlayerPrefs.Save(); } catch { } }

        void Go(Step s)
        {
            step = s; stepStart = Time.time; sawHold = false;
            var pc = PlayerControls.Local; if (pc) movedFrom = pc.transform.position;
            if (s == Step.Done) { try { PlayerPrefs.SetInt("ue.firstSteps", 1); PlayerPrefs.Save(); } catch { } }
        }

        static string GrabKeys(int hand) => InputBindings.Display(hand == 0 ? Act.GrabLeft : Act.GrabRight);

        void Update()
        {
            var round = RoundDirector.Instance; var pc = PlayerControls.Local; var npc = NpcBrain.Instance;
            bool show = step != Step.Done && pc && round && !round.InLobby && round.State == RoundState.Running && !GameFlowUI.BlockingInput;
            if (box && box.gameObject.activeSelf != show) box.gameObject.SetActive(show);
            if (step == Step.Done || !pc || !round || round.InLobby || round.State != RoundState.Running) return;
            var hands = pc.GetComponent<PlayerHands>();
            bool any = hands && (hands.HeldSocketId(0) != "" || hands.HeldSocketId(1) != "");
            bool both = hands && hands.HeldSocketId(0) != "" && hands.HeldSocketId(1) != "";
            float age = Time.time - stepStart;
            switch (step)
            {
                case Step.Move: if (Vector3.Distance(pc.transform.position, movedFrom) > 1.5f) Go(Step.Aim); break;
                case Step.Aim: if (hands && hands.Candidate && hands.Candidate.body && hands.Candidate.body.GetComponentInParent<NpcBody>()) Go(Step.Grab); else if (any) Go(Step.Release); break;
                case Step.Grab: if (any) Go(Step.Release); break;
                case Step.Release: if (any) sawHold = true; if (sawHold && !any) Go(Step.TwoHands); break;
                case Step.TwoHands: if (both) Go(Step.Attack); break;
                case Step.Attack: if (npc && (npc.State == NpcState.Windup || npc.State == NpcState.Attack)) Go(Step.Recover); else if (age > 45f) Go(Step.Cart); break;
                case Step.Recover: if (npc && npc.State == NpcState.Recover) sawHold = true; if ((sawHold && npc && npc.State != NpcState.Recover) || age > 30f) Go(Step.Cart); break;
                case Step.Cart: if (TransportCart.Instance && TransportCart.Instance.IsStrapped) Go(Step.Extract); else if (age > 90f) Go(Step.Extract); break;
                case Step.Extract: break; // termina con la ronda
            }
            if (show) Render();
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
                case Step.Cart: body.text = Loc.T("tut.cart", ("key", InputBindings.Display(Act.Interact))); break;
                case Step.Extract: body.text = Loc.T("tut.extract", ("key", InputBindings.Display(Act.Interact))); break;
            }
        }
    }
}
