using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace UncleExtraction
{
    /// Calidad grafica (Opciones): Alta o Baja. Sin eleccion guardada, se detecta: GPU integrada (Intel UHD/Iris, Radeon
    /// Graphics) o poca memoria de video => Baja. Medido en esta PC: Intel Iris Xe a 1080p en Alta = 11,7 fps.
    /// Baja: escala de render 75 %, sin MSAA, sombras solo de la luna y a 20 m, sin sombras de luces adicionales y sin bloom.
    /// Solo cambia el aspecto; nada de juego ni de red depende de esto.
    public static class GraphicsQuality
    {
        public static bool Low;
        static float baseScale = -1f, baseShadow; static int baseMsaa;

        public static bool DetectLow()
        {
            string n = SystemInfo.graphicsDeviceName ?? "";
            bool integrated = n.Contains("Intel") || n.Contains("UHD") || n.Contains("Iris") || n.Contains("Radeon(TM) Graphics") || n.Contains("Radeon Graphics");
            return integrated || SystemInfo.graphicsMemorySize < 2500;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook() { SceneManager.sceneLoaded += (_, __) => Apply(); Apply(); }

        public static void Apply()
        {
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset && !Application.isEditor) // en el editor no se toca el asset del proyecto (quedaria modificado en disco)
            {
                if (baseScale < 0f) { baseScale = asset.renderScale; baseShadow = asset.shadowDistance; baseMsaa = asset.msaaSampleCount; }
                asset.renderScale = Low ? 0.75f : baseScale;
                asset.shadowDistance = Low ? 20f : baseShadow;
                asset.msaaSampleCount = Low ? 1 : baseMsaa;
            }
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) continue;
                var mark = l.GetComponent<QualityShadowMark>();
                if (!mark) { mark = l.gameObject.AddComponent<QualityShadowMark>(); mark.original = l.shadows; }
                l.shadows = Low ? LightShadows.None : mark.original;
            }
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                if (v.profile && v.profile.TryGet<Bloom>(out var bloom)) bloom.active = !Low; // v.profile: copia propia de la escena
        }
    }

    /// Guarda si una luz tenia sombras en Alta (para volver a ponerlas al subir la calidad).
    public class QualityShadowMark : MonoBehaviour { public LightShadows original; }
}
