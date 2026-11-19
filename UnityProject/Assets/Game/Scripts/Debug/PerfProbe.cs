using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Profiling;

namespace UncleExtraction
{
    /// Medicion de rendimiento en la build (solo con -uePerf [segundos]; sin el argumento no hace nada).
    /// Cada ventana registra "[Perf] ..." con: cuadros por segundo, tiempo de cuadro medio/p95/p99/maximo, tiempo de CPU del
    /// hilo principal y de GPU por cuadro (FrameTimingManager: trabajo real, aunque haya sincronizacion vertical),
    /// memoria (asignada y reservada por Unity, montón administrado, memoria del proceso), jugadores y cuerpos fisicos.
    /// Al salir registra "[Perf] resumen" de toda la sesion, con la memoria inicial y final (fugas entre reintentos).
    /// Mide ESTA PC: varios procesos en una PC compiten por CPU y GPU y no equivalen a varios equipos.
    public class PerfProbe : MonoBehaviour
    {
        float window = 10f, windowStart, sessionStart;
        readonly List<float> frames = new List<float>(), all = new List<float>();
        readonly List<double> cpu = new List<double>(), gpu = new List<double>();
        readonly List<double> cpuAll = new List<double>(), gpuAll = new List<double>();
        readonly FrameTiming[] ft = new FrameTiming[1];
        long memStart, memMax, procMax; // procMax: pico de memoria reservada por Unity (la del proceso no la da Mono)
        bool started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var a = QaArgs.All;
            for (int i = 0; i < a.Length; i++)
                if (a[i] == "-uePerf")
                {
                    var go = new GameObject("PerfProbe"); DontDestroyOnLoad(go);
                    var p = go.AddComponent<PerfProbe>();
                    if (i + 1 < a.Length && float.TryParse(a[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) && w >= 1f) p.window = w;
                    return;
                }
        }

        void Start()
        {
            Debug.Log($"[Perf] equipo cpu=«{SystemInfo.processorType}» nucleos={SystemInfo.processorCount} gpu=«{SystemInfo.graphicsDeviceName}» api={SystemInfo.graphicsDeviceType} " +
                      $"vram={SystemInfo.graphicsMemorySize}MB ram={SystemInfo.systemMemorySize}MB pantalla={Screen.width}x{Screen.height}@{Screen.currentResolution.refreshRateRatio.value:0} " +
                      $"vsync={QualitySettings.vSyncCount} objetivo={Application.targetFrameRate} calidad={QualitySettings.names[QualitySettings.GetQualityLevel()]} frameTiming={FrameTimingManager.IsFeatureEnabled()}");
            windowStart = sessionStart = Time.realtimeSinceStartup;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime * 1000f;
            if (Time.frameCount > 10) { frames.Add(dt); all.Add(dt); }
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, ft) > 0)
            {
                if (ft[0].cpuMainThreadFrameTime > 0) { cpu.Add(ft[0].cpuMainThreadFrameTime); cpuAll.Add(ft[0].cpuMainThreadFrameTime); }
                if (ft[0].gpuFrameTime > 0) { gpu.Add(ft[0].gpuFrameTime); gpuAll.Add(ft[0].gpuFrameTime); }
            }
            long mem = Profiler.GetTotalAllocatedMemoryLong();
            if (!started && Time.frameCount > 60) { started = true; memStart = mem; }
            memMax = Math.Max(memMax, mem);
            if (Time.realtimeSinceStartup - windowStart >= window) Flush();
        }

        static float Pct(List<float> v, float p) { if (v.Count == 0) return 0; var s = new List<float>(v); s.Sort(); return s[Mathf.Clamp(Mathf.CeilToInt(p * s.Count) - 1, 0, s.Count - 1)]; }
        static double Avg(List<double> v) { if (v.Count == 0) return 0; double t = 0; foreach (var x in v) t += x; return t / v.Count; }
        static double Pct(List<double> v, float p) { if (v.Count == 0) return 0; var s = new List<double>(v); s.Sort(); return s[Mathf.Clamp(Mathf.CeilToInt(p * s.Count) - 1, 0, s.Count - 1)]; }

        static string Mem(out long proc)
        {
            proc = 0;
            proc = Profiler.GetTotalReservedMemoryLong();
            return $"mem_unity={Profiler.GetTotalAllocatedMemoryLong() / 1048576}MB reservada={Profiler.GetTotalReservedMemoryLong() / 1048576}MB " +
                   $"mono={Profiler.GetMonoUsedSizeLong() / 1048576}/{Profiler.GetMonoHeapSizeLong() / 1048576}MB gfx={Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576}MB";
        }

        void Flush()
        {
            float span = Time.realtimeSinceStartup - windowStart;
            if (frames.Count > 0)
            {
                string mem = Mem(out long proc); procMax = Math.Max(procMax, proc);
                var r = RoundDirector.Instance;
                Debug.Log($"[Perf] ventana fps={frames.Count / span:0.0} cuadro_ms media={Sum(frames) / frames.Count:0.00} p95={Pct(frames, 0.95f):0.00} p99={Pct(frames, 0.99f):0.00} max={Pct(frames, 1f):0.0} " +
                          $"cpu_ms={Avg(cpu):0.00}/p95 {Pct(cpu, 0.95f):0.00} gpu_ms={Avg(gpu):0.00}/p95 {Pct(gpu, 0.95f):0.00} {mem} " +
                          $"jugadores={PlayerMotor.All.Count} cuerpos={FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length} ronda={(r ? r.State + (r.InLobby ? "/sala" : "") : "-")} reinicios={(r ? r.Restarts : 0)} escena={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            }
            frames.Clear(); cpu.Clear(); gpu.Clear(); windowStart = Time.realtimeSinceStartup;
        }

        static float Sum(List<float> v) { float t = 0; foreach (var x in v) t += x; return t; }

        void OnApplicationQuit()
        {
            Flush();
            float span = Time.realtimeSinceStartup - sessionStart;
            string mem = Mem(out long proc); procMax = Math.Max(procMax, proc);
            Debug.Log($"[Perf] resumen duracion={span:0}s cuadros={all.Count} fps_medio={all.Count / Mathf.Max(1f, span):0.0} cuadro_ms p50={Pct(all, 0.5f):0.00} p95={Pct(all, 0.95f):0.00} p99={Pct(all, 0.99f):0.00} max={Pct(all, 1f):0.0} " +
                      $"cuadros_>33ms={all.FindAll(x => x > 33.3f).Count} cpu_ms media={Avg(cpuAll):0.00} p95={Pct(cpuAll, 0.95f):0.00} gpu_ms media={Avg(gpuAll):0.00} p95={Pct(gpuAll, 0.95f):0.00} " +
                      $"mem_unity_inicio={memStart / 1048576}MB final={Profiler.GetTotalAllocatedMemoryLong() / 1048576}MB pico={memMax / 1048576}MB reservada_pico={procMax / 1048576}MB {mem}");
        }
    }
}
