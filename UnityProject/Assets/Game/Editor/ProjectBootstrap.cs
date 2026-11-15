using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// M0: agrega los paquetes base dejando que Unity resuelva la version recomendada para el editor instalado.
    /// Las versiones resultantes quedan fijadas en Packages/manifest.json y packages-lock.json (versionar ambos).
    /// Uso: Unity.exe -batchmode -nographics -projectPath UnityProject -executeMethod UncleExtraction.EditorTools.ProjectBootstrap.Run
    public static class ProjectBootstrap
    {
        static readonly string[] Packages = { "com.unity.inputsystem", "com.unity.test-framework", "com.unity.ugui", "com.unity.netcode.gameobjects", "com.unity.transport" };
        static AddAndRemoveRequest request; static double startedAt;

        [MenuItem("Uncle Extraction/M0: instalar paquetes base")]
        public static void Run()
        {
            Debug.Log($"[Bootstrap] Unity {Application.unityVersion}; adding {string.Join(", ", Packages)}");
            PlayerSettings.companyName = "Stefano936";
            PlayerSettings.productName = "Uncle Extraction";
            request = Client.AddAndRemove(Packages, null);
            startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (!request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup - startedAt > 900) { Debug.LogError("[Bootstrap] timeout"); Finish(2); }
                return;
            }
            if (request.Status == StatusCode.Success)
            {
                foreach (var p in request.Result) Debug.Log($"[Bootstrap] package {p.name}@{p.version}");
                SetActiveInputHandling(2); // 0 = clasico, 1 = Input System, 2 = ambos (laboratorio)
                AssetDatabase.SaveAssets();
                Debug.Log("[Bootstrap] OK");
                Finish(0);
            }
            else { Debug.LogError($"[Bootstrap] package error: {request.Error?.message}"); Finish(1); }
        }

        static void Finish(int code)
        {
            EditorApplication.update -= Poll;
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static void SetActiveInputHandling(int value)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) { Debug.LogWarning("[Bootstrap] ProjectSettings.asset no encontrado"); return; }
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null) { Debug.LogWarning("[Bootstrap] activeInputHandler no encontrado"); return; }
            prop.intValue = value; so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[Bootstrap] activeInputHandler={value}");
        }
    }
}
