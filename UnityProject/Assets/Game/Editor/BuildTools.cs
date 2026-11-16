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
        public static void BuildWindowsMenu() => Build("Mono", "Builds/UncleExtraction");

        public static void BuildWindows()
        {
            int code = 1;
            try { code = Build(Arg("-ueBackend") ?? "Mono", Arg("-ueOut") ?? "Builds/UncleExtraction", System.Array.IndexOf(Environment.GetCommandLineArgs(), "-ueDev") >= 0) ? 0 : 1; }
            catch (Exception e) { Debug.LogException(e); }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static bool Build(string backend, string outDir, bool dev = false)
        {
            var impl = backend.Equals("IL2CPP", StringComparison.OrdinalIgnoreCase) ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, impl);
            PlayerSettings.runInBackground = true; // el anfitrion (M2) no debe congelarse sin foco; tambien las pruebas de humo
            if (!File.Exists(PhysicsLabBuilder.ScenePath) || !File.Exists(PhysicsLabBuilder.BarScenePath) || !File.Exists(FrontendBuilder.ScenePath)) PhysicsLabBuilder.Build();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { FrontendBuilder.ScenePath, PhysicsLabBuilder.BarScenePath, PhysicsLabBuilder.ScenePath, PhysicsLabBuilder.ArticulatedLabScenePath },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = Path.Combine(outDir, "UncleExtraction.exe"),
                options = dev ? BuildOptions.Development : BuildOptions.None // dev: simulador de red y logs de depuracion
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[Build] dev={dev} backend={impl} result={s.result} errors={s.totalErrors} warnings={s.totalWarnings} size={s.totalSize} time={s.totalTime} path={opts.locationPathName}");
            if (s.result == BuildResult.Succeeded)
            {   // instrucciones junto al ejecutable (docs/COMO_PROBAR.md)
                var guide = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "docs", "COMO_PROBAR.md");
                if (File.Exists(guide)) File.Copy(guide, Path.Combine(outDir, "LEEME.txt"), true);
                // version de origen: commit, si habia cambios sin commitear y cuando se genero (cada resultado se asocia a esto)
                string head = Git("rev-parse --short HEAD"), dirty = Git("status --porcelain --untracked-files=no");
                File.WriteAllText(Path.Combine(outDir, "VERSION.txt"),
                    $"Commit de origen: {head}{(dirty != "" ? " + cambios sin commitear" : "")}
Generada: {DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}
Tipo: {(dev ? "desarrollo" : "release")} {impl}
");
            }
            return s.result == BuildResult.Succeeded;
        }

        static string Git(string args)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("git", args) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Directory.GetParent(Application.dataPath).Parent.FullName };
                using var p = System.Diagnostics.Process.Start(psi); string o = p.StandardOutput.ReadToEnd().Trim(); p.WaitForExit(5000); return o;
            }
            catch (Exception e) { return "desconocido (" + e.Message + ")"; }
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
