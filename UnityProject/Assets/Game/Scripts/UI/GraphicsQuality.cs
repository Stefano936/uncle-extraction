using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace UncleExtraction
{
    /// Calidad grafica (Opciones): Alta o Baja. La eleccion guardada manda siempre; sin ella, Baja en GPU integradas o
    /// desconocidas (IsIntegrated). Medido en esta PC (banco de vistas fijas, enchufada): Iris Xe 1080p Alta 125 fps (GPU 7,2 ms),
    /// Baja 205 fps (GPU 3,7 ms); a bateria, en partida y en Alta, 11,7 fps.
    /// Baja: escala de render 75 %, sin MSAA, sombras solo de la luna y a 20 m, sin sombras de luces adicionales y sin bloom.
    /// Solo cambia el aspecto; nada de juego ni de red depende de esto.
    public static class GraphicsQuality
    {
        public static bool Low;
        static float baseScale = -1f, baseShadow; static int baseMsaa;

        /// Sin eleccion guardada: Baja solo en GPU integradas o desconocidas (por tipo de dispositivo, no por fabricante).
        public static bool DetectLow() => IsIntegrated(SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceVendorID, SystemInfo.graphicsMemorySize);

        /// Clasificacion pura (probada en EditMode). Intel: integrada salvo Arc (dedicada). AMD: integrada si es una APU
        /// («Radeon Graphics», «Radeon(TM) Graphics», «Vega N»), dedicada si es «RX»/«Pro»/«FirePro». NVIDIA: dedicada.
        /// Desconocida o render por software (Microsoft Basic Render): Baja. La memoria informada no decide sola: en las
        /// integradas es memoria compartida (la Iris Xe informa ~8 GB).
        public static bool IsIntegrated(string name, int vendorId, int memoryMB)
        {
            name = name ?? ""; string n = name.ToLowerInvariant();
            if (n.Contains("basic render") || n.Contains("llvmpipe") || n.Contains("swiftshader") || n.Length == 0) return true;
            bool intel = vendorId == 0x8086 || n.Contains("intel"), amd = vendorId == 0x1002 || n.Contains("amd") || n.Contains("radeon"), nvidia = vendorId == 0x10DE || n.Contains("nvidia") || n.Contains("geforce");
            if (nvidia) return false;
            if (intel) return !n.Contains("arc");
            if (amd)
            {
                if (n.Contains(" rx ") || n.Contains("rx ") || n.Contains(" pro") || n.Contains("firepro")) return false;
                return n.Contains("radeon graphics") || n.Contains("radeon(tm) graphics") || n.Contains("vega") || memoryMB < 2048;
            }
            return memoryMB < 2048; // otros fabricantes: por memoria
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook() { SceneManager.sceneLoaded += (_, __) => Apply(); Apply(); }

        /// Activa o apaga una caracteristica del renderer por nombre (la lista de renderers del asset no es publica).
        static void SetFeature(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset asset, string name, bool on)
        {
            var fi = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (!(fi?.GetValue(asset) is UnityEngine.Rendering.Universal.ScriptableRendererData[] list)) return;
            foreach (var d in list) if (d) foreach (var f in d.rendererFeatures) if (f && f.name == name && f.isActive != on) f.SetActive(on);
        }

        public static void Apply()
        {
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset && !Application.isEditor) // en el editor no se toca el asset del proyecto (quedaria modificado en disco)
            {
                if (baseScale < 0f) { baseScale = asset.renderScale; baseShadow = asset.shadowDistance; baseMsaa = asset.msaaSampleCount; }
                asset.renderScale = Low ? 0.75f : baseScale;
                asset.shadowDistance = Low ? 20f : baseShadow;
                asset.msaaSampleCount = Low ? 1 : baseMsaa;
                SetFeature(asset, "SSAO", !Low); // sesion 21: oclusion ambiental solo en Alta
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
