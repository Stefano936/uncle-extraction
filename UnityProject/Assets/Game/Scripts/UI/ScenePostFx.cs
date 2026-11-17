using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UncleExtraction
{
    /// Activa el posproceso en la camara local solo en escenas que lo definen (Bar_LastRound, con su volumen). El prefab del
    /// jugador es comun a todas las escenas: en los laboratorios el posproceso no aporta nada y cuesta un pase por cuadro.
    public class ScenePostFx : MonoBehaviour
    {
        Camera last;
        void Update()
        {
            var cam = Camera.main;
            if (!cam || cam == last) return;
            last = cam;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }
    }
}
