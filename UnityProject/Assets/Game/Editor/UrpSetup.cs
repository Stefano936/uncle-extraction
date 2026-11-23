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

        /// Sesion 21: oclusion ambiental en pantalla (SSAO) para dar volumen a rincones, uniones y contacto con el piso.
        /// Se agrega como lo hace el editor de URP (objeto dentro del asset del renderer + mapa de ids). En calidad Baja la
        /// apaga GraphicsQuality en tiempo de ejecucion.
        public const string SsaoName = "SSAO";
        static void EnsureSsao(UniversalRendererData data)
        {
            var type = System.Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
            if (type == null) { Debug.LogWarning("[URP] sin tipo de SSAO en esta version"); return; }
            ScriptableRendererFeature feature = null;
            foreach (var f in data.rendererFeatures) if (f && f.GetType() == type) feature = f;
            if (!feature)
            {
                feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type); feature.name = SsaoName;
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long lid);
                var dso = new SerializedObject(data);
                var feats = dso.FindProperty("m_RendererFeatures"); var map = dso.FindProperty("m_RendererFeatureMap");
                feats.arraySize++; feats.GetArrayElementAtIndex(feats.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = lid;
                dso.ApplyModifiedPropertiesWithoutUndo();
            }
            var fso = new SerializedObject(feature);
            void F(string n, float v) { var p = fso.FindProperty("m_Settings." + n); if (p != null) p.floatValue = v; }
            void B(string n, bool v) { var p = fso.FindProperty("m_Settings." + n); if (p != null) p.boolValue = v; }
            F("Intensity", 1.6f); F("Radius", 0.32f); F("DirectLightingStrength", 0.3f); F("Falloff", 40f);
            B("Downsample", false); B("AfterOpaque", false);
            fso.ApplyModifiedPropertiesWithoutUndo();
            feature.SetActive(true);
            EditorUtility.SetDirty(feature); EditorUtility.SetDirty(data);
        }

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
            // Forward+: el bar usa ~15 luces calidas; sin limite de luces por objeto (con Forward el piso mostraba saltos)
            if (rendererData.renderingMode != RenderingMode.ForwardPlus) { rendererData.renderingMode = RenderingMode.ForwardPlus; EditorUtility.SetDirty(rendererData); }
            var so = new SerializedObject(asset); // sombras de luces adicionales (focos de la barra y farol): sin setter publico
            var sp = so.FindProperty("m_AdditionalLightShadowsSupported"); if (sp != null) sp.boolValue = true;
            var res = so.FindProperty("m_AdditionalLightsShadowmapResolution"); if (res != null) res.intValue = 2048;
            // sesion 21: sombras con mas forma (dos cascadas, suaves, mapas de luces adicionales mas finos): las patas, la
            // barra, el tio y el carrito apoyan en el piso en vez de flotar
            var casc = so.FindProperty("m_ShadowCascadeCount"); if (casc != null) casc.intValue = 2;
            var soft = so.FindProperty("m_SoftShadowsSupported"); if (soft != null) soft.boolValue = true;
            var softQ = so.FindProperty("m_SoftShadowQuality"); if (softQ != null) softQ.intValue = 3; // alta
            if (res != null) res.intValue = 4096;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            EnsureSsao(rendererData);

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
