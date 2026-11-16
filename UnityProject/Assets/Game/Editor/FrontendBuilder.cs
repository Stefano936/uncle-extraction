using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Escena de menu (Frontend): camara con fondo oscuro y FrontendMenu. Reproducible desde el editor o en batch.
    public static class FrontendBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/Frontend.unity";

        public static void Build()
        {
            var prev = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Menu_Camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.12f, 0.08f, 0.1f);
            cam.gameObject.AddComponent<AudioListener>();
            new GameObject("Frontend").AddComponent<UncleExtraction.FrontendMenu>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[FrontendBuilder] saved {ScenePath}");
            if (!string.IsNullOrEmpty(prev) && prev != ScenePath) EditorSceneManager.OpenScene(prev, OpenSceneMode.Single);
        }
    }
}
