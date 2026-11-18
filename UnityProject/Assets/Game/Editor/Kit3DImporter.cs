using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Kit 3D de prototipo (preproduction/uncle_extraction_kit_3D, FBX -Z forward / Y up, metros).
    /// Fija los ajustes de importacion y deja un informe medible: medidas en Unity, huesos, clips y marcadores de agarre.
    /// Uso batch: -executeMethod UncleExtraction.EditorTools.Kit3DImporter.ImportAndReport
    public class Kit3DImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Game/Art/Kit3D";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1f; mi.useFileScale = true;         // el kit ya esta en metros: no duplicar conversiones
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.isReadable = false;
            bool uncle = Path.GetFileNameWithoutExtension(assetPath) == "uncle_prototype";
            mi.animationType = uncle ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            mi.importAnimation = uncle;
            mi.optimizeGameObjects = false;                      // conservar huesos y marcadores accesibles
            mi.importCameras = false; mi.importLights = false;
            if (uncle)
            {   // sesion 16: normales calculadas con suavizado de 60 grados. Las del archivo son planas (aspecto facetado):
                // asi cabeza, panza, nariz, brazos y piernas se ven redondos y los bordes de ropa y calzado siguen marcados.
                mi.importNormals = ModelImporterNormals.Calculate; mi.normalSmoothingAngle = 60f;
                mi.normalCalculationMode = ModelImporterNormalCalculationMode.AngleWeighted;
                mi.importTangents = ModelImporterTangents.CalculateMikk;
            }
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var mi = (ModelImporter)assetImporter;
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips) { c.lockRootPositionXZ = true; c.lockRootHeightY = true; c.lockRootRotation = true; c.loopTime = c.name.Contains("Idle"); }
            mi.clipAnimations = clips;                           // sin root motion para esta prueba (INTEGRAR_KIT.md)
        }

        /// Pose de reposo del rig del tio en Unity: posicion, ejes y largo de cada hueso (para el cuerpo articulado).
        public static void ReportUncleRig()
        {
            int code = 0;
            try
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/uncle_prototype.fbx");
                var inst = (GameObject)Object.Instantiate(src);
                foreach (var t in inst.GetComponentsInChildren<Transform>())
                {
                    if (t.parent == null) continue;
                    string kids = string.Join(",", Enumerable.Range(0, t.childCount).Select(i => t.GetChild(i).name));
                    Debug.Log($"[KitRig] {t.name} parent={t.parent.name} pos={t.position:F3} rot={t.rotation.eulerAngles:F1} up={t.up:F2} fwd={t.forward:F2} right={t.right:F2} kids=[{kids}]");
                }
                var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>();
                Debug.Log($"[KitRig] mesh bounds={smr.bounds.size:F2} rootBone={(smr.rootBone ? smr.rootBone.name : "-")} bones={smr.bones.Length}");
                Object.DestroyImmediate(inst);
            }
            catch (System.Exception e) { Debug.LogException(e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        public static void ImportAndReport()
        {
            int code = 0;
            try
            {
                AssetDatabase.ImportAsset(Folder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
                foreach (var path in Directory.GetFiles(Folder, "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!go) { Debug.LogError($"[Kit3D] no importo {path}"); code = 1; continue; }
                    var inst = (GameObject)Object.Instantiate(go);
                    var rs = inst.GetComponentsInChildren<Renderer>();
                    Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds();
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>();
                    string bones = smr ? string.Join(",", smr.bones.Select(x => x.name)) : "-";
                    var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).Select(c => $"{c.name}({c.length:0.00}s)");
                    var markers = inst.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Grab_") || t.name.Contains("Socket") || t.name.Contains("Volume") || t.name.Contains("Anchor") || t.name.Contains("Handle"))
                                      .Select(t => $"{t.name}@{inst.transform.InverseTransformPoint(t.position):F2}");
                    Debug.Log($"[Kit3D] {Path.GetFileName(path)} size={b.size:F2} center={b.center:F2} renderers={rs.Length} " +
                              $"bones=[{bones}] clips=[{string.Join(",", clips)}] markers=[{string.Join(" ", markers)}] children=[{string.Join(",", inst.GetComponentsInChildren<Transform>().Where(t => t.parent == inst.transform).Select(t => t.name))}]");
                    Object.DestroyImmediate(inst);
                }
            }
            catch (System.Exception e) { Debug.LogException(e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
