using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Build Windows x64 de PhysicsLab. Mono por defecto: IL2CPP necesita Visual Studio con C++ instalado.
    /// Uso: -executeMethod UncleExtraction.EditorTools.BuildTools.BuildWindows [-ueBackend IL2CPP] [-ueOut <carpeta>]
    public static class BuildTools
    {
        [MenuItem("Uncle Extraction/Build Windows PhysicsLab (Mono)")]
        public static void BuildWindowsMenu() => Build("Mono", "Builds/PhysicsLab");

        public static void BuildWindows()
        {
            int code = 1;
            try { code = Build(Arg("-ueBackend") ?? "Mono", Arg("-ueOut") ?? "Builds/PhysicsLab") ? 0 : 1; }
            catch (Exception e) { Debug.LogException(e); }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static bool Build(string backend, string outDir)
        {
            var impl = backend.Equals("IL2CPP", StringComparison.OrdinalIgnoreCase) ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, impl);
            if (!File.Exists(PhysicsLabBuilder.ScenePath)) PhysicsLabBuilder.Build();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { PhysicsLabBuilder.ScenePath },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = Path.Combine(outDir, "UncleExtraction_PhysicsLab.exe"),
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[Build] backend={impl} result={s.result} errors={s.totalErrors} warnings={s.totalWarnings} size={s.totalSize} time={s.totalTime} path={opts.locationPathName}");
            return s.result == BuildResult.Succeeded;
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
