using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UncleExtraction
{
    /// Medicion de rendimiento reproducible (sesion 12; solo con -ueBench segundos_por_vista):
    /// entra a Bar_LastRound en solitario, deja quieto al tio, espera la carga y un calentamiento (-ueBenchWarmup, 8 s por
    /// defecto) que NO se mide, y recorre vistas fijas de camara (las mismas en todas las builds). Por vista y en total
    /// registra FPS, percentiles de tiempo de cuadro (p50/p95/p99/max) y tiempos de CPU (hilo principal) y GPU por cuadro
    /// (FrameTimingManager). -ueNoVsync quita sincronizacion vertical y limite de FPS (SOLO diagnostico: muestra el costo
    /// real sin el tope del monitor). Registra las condiciones: GPU, alimentacion, pantalla, vsync, limite, escala de render,
    /// MSAA, sombras y calidad. Autocontenido: el mismo archivo se agrega a builds anteriores para comparar.
    public class Bench : MonoBehaviour
    {
        float perView = 6f, warmup = 8f;
        static readonly (string name, Vector3 pos, Vector3 look)[] Views =
        {
            ("salon",     new Vector3(0.5f, 1.7f, 1.2f),  new Vector3(4.5f, 1.0f, 8.5f)),
            ("barra",     new Vector3(2.4f, 1.5f, 5.6f),  new Vector3(5.2f, 0.9f, 8.9f)),
            ("carrito",   new Vector3(4.0f, 1.4f, 0.6f),  new Vector3(5.6f, 0.5f, 2.4f)),
            ("puerta",    new Vector3(0.6f, 1.6f, 3.4f),  new Vector3(0f, 1.1f, -2f)),
            ("vereda",    new Vector3(5.5f, 2.6f, -4.0f), new Vector3(0f, 0.8f, -10f)),
            ("camioneta", new Vector3(1.6f, 1.6f, -6.4f), new Vector3(0f, 1.0f, -10.5f)),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var a = QaArgs.All;
            for (int i = 0; i < a.Length; i++)
                if (a[i] == "-ueBench")
                {
                    var go = new GameObject("Bench"); DontDestroyOnLoad(go); var b = go.AddComponent<Bench>();
                    if (i + 1 < a.Length && float.TryParse(a[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 2f) b.perView = v;
                    for (int k = 0; k < a.Length - 1; k++) if (a[k] == "-ueBenchWarmup" && float.TryParse(a[k + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) b.warmup = w;
                    if (Array.IndexOf(a, "-ueNoVsync") >= 0) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1; }
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Bar_LastRound") UnityEngine.SceneManagement.SceneManager.LoadScene("Bar_LastRound");
                    return;
                }
        }

        static string Conditions()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var cam = Camera.main; bool post = false;
            if (cam && cam.TryGetComponent<UniversalAdditionalCameraData>(out var cd)) post = cd.renderPostProcessing;
            int shadowLights = 0; foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.shadows != LightShadows.None && l.isActiveAndEnabled) shadowLights++;
            int lights = FindObjectsByType<Light>(FindObjectsSortMode.None).Length;
            return $"gpu=«{SystemInfo.graphicsDeviceName}» api={SystemInfo.graphicsDeviceType} cpu=«{SystemInfo.processorType}» alimentacion={SystemInfo.batteryStatus} bateria={(SystemInfo.batteryLevel < 0 ? "-" : (SystemInfo.batteryLevel * 100f).ToString("0") + "%")} " +
                   $"pantalla={Screen.width}x{Screen.height}@{Screen.currentResolution.refreshRateRatio.value:0} vsync={QualitySettings.vSyncCount} limite={Application.targetFrameRate} " +
                   $"escala={(urp ? urp.renderScale : 1f):0.00} msaa={(urp ? urp.msaaSampleCount : 0)} sombras_dist={(urp ? urp.shadowDistance : 0f):0} luces={lights} luces_con_sombra={shadowLights} posproceso={post}";
        }

        IEnumerator Start()
        {
            while (LevelLayout.Current == null || PlayerControls.Local == null || Camera.main == null) yield return null;
            var npc = FindAnyObjectByType<NpcBrain>(); if (npc) npc.brainEnabled = false;   // escena estable entre corridas
            var motor = FindAnyObjectByType<PlayerMotor>(); if (motor) motor.enabled = false;
            var cam = Camera.main; cam.transform.SetParent(null, true);
            Debug.Log($"[Bench] condiciones {Conditions()}");
            cam.transform.SetPositionAndRotation(Views[0].pos, Quaternion.LookRotation(Views[0].look - Views[0].pos));
            float t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < warmup) yield return null; // calentamiento: no se mide
            var all = new List<float>(); var allCpu = new List<double>(); var allGpu = new List<double>();
            var ft = new FrameTiming[1];
            foreach (var v in Views)
            {
                cam.transform.SetPositionAndRotation(v.pos, Quaternion.LookRotation(v.look - v.pos));
                for (int i = 0; i < 10; i++) yield return null; // primer cuadro de la vista fuera de la medicion
                var f = new List<float>(); var cpu = new List<double>(); var gpu = new List<double>();
                float s0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - s0 < perView)
                {
                    yield return null;
                    f.Add(Time.unscaledDeltaTime * 1000f);
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, ft) > 0) { if (ft[0].cpuMainThreadFrameTime > 0) cpu.Add(ft[0].cpuMainThreadFrameTime); if (ft[0].gpuFrameTime > 0) gpu.Add(ft[0].gpuFrameTime); }
                }
                Debug.Log($"[Bench] vista {v.name} {Stats(f, cpu, gpu, Time.realtimeSinceStartup - s0)}");
                all.AddRange(f); allCpu.AddRange(cpu); allGpu.AddRange(gpu);
            }
            Debug.Log($"[Bench] resumen {Stats(all, allCpu, allGpu, perView * Views.Length)} {Conditions()}");
            yield return null;
            Application.Quit();
        }

        static float P(List<float> v, float p) { if (v.Count == 0) return 0; var s = new List<float>(v); s.Sort(); return s[Mathf.Clamp(Mathf.CeilToInt(p * s.Count) - 1, 0, s.Count - 1)]; }
        static double P(List<double> v, float p) { if (v.Count == 0) return 0; var s = new List<double>(v); s.Sort(); return s[Mathf.Clamp(Mathf.CeilToInt(p * s.Count) - 1, 0, s.Count - 1)]; }
        static double Avg(List<double> v) { double t = 0; foreach (var x in v) t += x; return v.Count > 0 ? t / v.Count : 0; }
        static string Stats(List<float> f, List<double> cpu, List<double> gpu, float span) =>
            string.Format(CultureInfo.InvariantCulture, "fps={0:0.0} cuadro_ms p50={1:0.00} p95={2:0.00} p99={3:0.00} max={4:0.0} cpu_ms media={5:0.00} p95={6:0.00} gpu_ms media={7:0.00} p95={8:0.00} cuadros={9}",
                f.Count / Mathf.Max(0.01f, span), P(f, 0.5f), P(f, 0.95f), P(f, 0.99f), P(f, 1f), Avg(cpu), P(cpu, 0.95f), Avg(gpu), P(gpu, 0.95f), f.Count);
    }
}
