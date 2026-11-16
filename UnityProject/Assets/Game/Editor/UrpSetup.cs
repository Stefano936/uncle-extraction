using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UncleExtraction.EditorTools
{
    /// Pipeline de render (especificacion tecnica: URP). Reproducible por codigo: crea el asset de URP y su renderer si
    /// faltan, los asigna a Graphics y a todos los niveles de calidad y, al cambiar de pipeline, reimporta los FBX del kit
    /// para que sus materiales se generen con URP/Lit. Lo llama PhysicsLabBuilder.Build antes de crear materiales.
    public static class UrpSetup
    {
        public const string Dir = "Assets/Game/Settings";
        public const string AssetPath = Dir + "/URP_UncleExtraction.asset";
        public const string RendererPath = Dir + "/URP_UncleExtraction_Renderer.asset";

        [MenuItem("Uncle Extraction/Configurar URP")]
        public static void Ensure()
        {
            Directory.CreateDirectory(Dir);
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (!rendererData)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
            if (!asset)
            {
                asset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            // Calidad pensada para PC con la escala del juego: sombras a 40 m (bar + vereda + camioneta), MSAA 2x.
            asset.shadowDistance = 40f;
            asset.msaaSampleCount = 2;
            asset.supportsHDR = true;
            EditorUtility.SetDirty(asset);

            bool changed = GraphicsSettings.defaultRenderPipeline != asset;
            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline != asset) { QualitySettings.renderPipeline = asset; changed = true; }
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();

            if (changed)
            {   // los materiales importados del kit se generaron con el pipeline anterior
                foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Kit3DImporter.Folder }))
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
                Debug.Log("[URP] pipeline asignado y modelos del kit reimportados");
            }
            Debug.Log($"[URP] activo: {GraphicsSettings.defaultRenderPipeline?.name}");
        }
    }
}
