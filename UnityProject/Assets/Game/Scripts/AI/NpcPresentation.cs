using UnityEngine;

namespace UncleExtraction
{
    /// Senales visibles de intencion: color del cuerpo, pose de brazos/piernas e indicador sobre la cabeza.
    /// Solo presentacion: no decide si un golpe ocurrio (GDD 06).
    public class NpcPresentation : MonoBehaviour
    {
        public NpcBrain brain;
        public Renderer[] bodyRenderers;
        public Transform armL, armR, legL, legR;
        public Transform indicator;
        public Renderer indicatorRenderer;

        static readonly Color idle = new Color(0.55f, 0.35f, 0.75f);
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock mpb;
        Quaternion armL0, armR0, legL0, legR0;

        void Start()
        {
            if (armL) armL0 = armL.localRotation; if (armR) armR0 = armR.localRotation;
            if (legL) legL0 = legL.localRotation; if (legR) legR0 = legR.localRotation;
            // sesion 17: el brillo de aviso necesita _EMISSION. Se activa en COPIAS de los materiales de la malla del tio (una por
            // renderer, al empezar), no en los .mat del proyecto: en el editor URP le quitaba la palabra clave al asset de la
            // camisa al dibujarlo y PlayMode dejaba el proyecto modificado.
            if (bodyRenderers != null)
                foreach (var r in bodyRenderers)
                {
                    if (!r || r.sharedMaterials.Length <= 1) continue;
                    foreach (var m in r.materials)
                    {
                        if (!m || !m.HasProperty(EmissionId)) continue;
                        m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; m.SetColor(EmissionId, Color.black);
                    }
                }
        }

        void Update()
        {
            if (!brain) return;
            Color c = idle; Vector3 aL = Vector3.zero, aR = Vector3.zero, lL = Vector3.zero, lR = Vector3.zero;
            bool showInd = false; float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
            switch (brain.State)
            {
                case NpcState.Windup:
                    showInd = true;
                    switch (brain.CurrentAttack.id)
                    {
                        case AttackId.Slap: c = new Color(1f, 0.6f, 0.1f); aR = new Vector3(0, 0, 150); break;          // hombro y mano arriba
                        case AttackId.Push: c = new Color(1f, 0.25f, 0.2f); aL = new Vector3(-60, 0, 0); aR = new Vector3(-60, 0, 0); break; // palmas atras
                        case AttackId.Kick: c = new Color(0.95f, 0.85f, 0.1f); lR = new Vector3(-80, 0, 0); break;      // rodilla flexionada
                        case AttackId.Shake: c = new Color(0.2f, 0.9f, 1f); aL = new Vector3(0, 0, -40); aR = new Vector3(0, 0, 40); break; // inhala
                    }
                    c = Color.Lerp(c, Color.white, pulse * 0.35f);
                    break;
                case NpcState.Attack:
                    c = Color.red; showInd = true;
                    if (brain.CurrentAttack.id == AttackId.Push) { aL = aR = new Vector3(80, 0, 0); }
                    if (brain.CurrentAttack.id == AttackId.Slap) aR = new Vector3(70, 0, 40);
                    if (brain.CurrentAttack.id == AttackId.Kick) lR = new Vector3(70, 0, 0);
                    break;
                case NpcState.Recover: c = new Color(0.35f, 0.55f, 1f); break;
                case NpcState.ForcedRecovery: c = new Color(0.6f, 0.9f, 1f); aL = new Vector3(0, 0, -10); aR = new Vector3(0, 0, 10); break;
                case NpcState.Anchor: c = new Color(0.8f, 0.3f, 0.9f); break;
                case NpcState.Secured: c = new Color(0.3f, 0.9f, 0.4f); break;
                case NpcState.Distracted: c = new Color(1f, 0.85f, 0.35f); aR = new Vector3(-40, 0, 20); break; // brazo hacia el vaso
            }
            foreach (var r in bodyRenderers)
            {
                if (!r) continue;
                if (r.sharedMaterials.Length > 1)
                {   // malla del kit con varios materiales: brillo del color del ataque solo al prepararse y al golpear
                    float k = brain.State == NpcState.Windup ? 0.08f + 0.12f * pulse : brain.State == NpcState.Attack ? 0.28f : 0f; // senal sin quemar el color de cerca (con bloom)
                    mpb ??= new MaterialPropertyBlock(); r.GetPropertyBlock(mpb); mpb.SetColor(EmissionId, c * k); r.SetPropertyBlock(mpb);
                }
                else r.material.color = c;
            }
            Pose(armL, armL0, aL); Pose(armR, armR0, aR); Pose(legL, legL0, lL); Pose(legR, legR0, lR);
            if (indicator)
            {
                if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-ueNoHud") >= 0) showInd = false; // material de tienda
                indicator.gameObject.SetActive(showInd);
                if (showInd)
                {
                    indicator.localScale = Vector3.one * (0.18f + 0.08f * pulse);
                    if (indicatorRenderer) indicatorRenderer.material.color = c;
                }
            }
        }

        static void Pose(Transform t, Quaternion rest, Vector3 euler)
        {
            if (t) t.localRotation = Quaternion.Slerp(t.localRotation, rest * Quaternion.Euler(euler), Smooth.Factor(0.25f));
        }
    }
}
