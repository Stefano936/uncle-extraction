using System;
using UnityEditor;
using UnityEngine;

namespace UncleExtraction.EditorTools
{
    /// Fine surface relief for the existing authored materials and the procedural scene builders.
    public static class SurfaceFinishing
    {
        const string TextureRoot = "Assets/Game/Art/Materials/Textures/";
        static readonly string[] MaterialRoots =
        {
            "Assets/Game/Art/Materials", "Assets/Game/Art/RouteS21/Materials",
            "Assets/Game/Materials", "Assets/Game/Resources/Materials"
        };

        [MenuItem("Uncle Extraction/Actualizar acabados de superficies")]
        public static void Apply()
        {
            var fabric = Surface("04_fabric_basecolor.png", false);
            var weave = Surface("Generated/04_fabric_basecolor_normal.png", true);
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", MaterialRoots))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!material || material.shader.name != "Universal Render Pipeline/Lit" ||
                    material.GetFloat("_Surface") != 0 || material.GetColor("_EmissionColor").maxColorComponent > 0 ||
                    IsGraphic(material.name)) continue;

                string name = material.name;
                if (IsFabric(name))
                {
                    var tiling = material.GetTextureScale("_BaseMap");
                    material.SetTexture("_BaseMap", fabric);
                    material.SetTexture("_MainTex", fabric);
                    SetNormal(material, weave, name.StartsWith("Player_Sleeve") ? .58f : .44f, tiling);
                }
                else
                {
                    var existing = material.GetTexture("_BumpMap");
                    if (existing && !AssetDatabase.GetAssetPath(existing).StartsWith(TextureRoot + "Detail/")) continue;
                    var (family, strength, scale) = Detail(name);
                    var tiling = material.GetTexture("_BaseMap") ? material.GetTextureScale("_BaseMap") : Vector2.one * scale;
                    SetNormal(material, Surface($"Detail/{family}_normal.png", true), strength, tiling);
                }
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
                count++;
            }
            Debug.Log($"[SurfaceFinishing] {count} materiales con relieve de superficie");
        }

        static bool IsGraphic(string name) =>
            name.StartsWith("Neon_") || name.StartsWith("Window_") || name.StartsWith("Dart_") ||
            name.StartsWith("Bottle_") || name.StartsWith("Poster_") || name.StartsWith("Telegraph") ||
            name.Contains("Eye") || name.Contains("Pupil") || name.Contains("Glass") ||
            name is "Lamp_Head" or "Sconce" ||
            name.StartsWith("RS_Win") || name.StartsWith("RS_Bottle") ||
            name is "RS_Mirror" or "RS_Label" or "RS_Plate" or
                "Indicator" or "Zone" or "Exit_Sign" or "Sign_Text" or "Bulb" or "Beer" or
                "Van_Head" or "Van_Tail" or "Van_Dome" or "BackBar_Mirror" or "Road_Paint" or "Zone_Tape";

        static bool IsFabric(string name) =>
            name.Contains("Shirt", StringComparison.OrdinalIgnoreCase) || name.Contains("Shorts") ||
            name.EndsWith("_Sock") || name.StartsWith("Player_Sleeve") || name is "RS_Carpet" or "RS_Awning";

        static (string family, float strength, float scale) Detail(string name)
        {
            if (name.Contains("Skin") || name is "Hand" or "Nose" or "Uncle") return ("skin", .24f, 2f);
            if (name.Contains("Hair")) return ("hair", .44f, 2f);
            if (name.Contains("Leather") || name.Contains("Shoe") || name.Contains("Rubber") ||
                name.Contains("Tire") || name is "Van_Bumper" or "Van_Black" or "RS_Vinyl" or "RS_Black")
                return ("leather", .55f, 3f);
            if (name is "Hat_Magenta" or "Hat_Pompom") return ("fabric", .46f, 2f);
            if (name.Contains("Metal") || name.Contains("Steel") || name.Contains("Brass") ||
                name.Contains("Buckle") || name.Contains("Paint") || name.StartsWith("RS_Van") ||
                name.StartsWith("Van_") || name.StartsWith("Lamp_") ||
                name is "RS_Chrome" or "RS_Shutter" or "RS_Grille" ||
                name is "Van" or "Cart" or "Hat_Gold" or "Sconce" or "Can_Housing" or "Ramp_Edge")
                return ("metal", .33f, 2f);
            if (name.Contains("Wood") || name is "Crate" or "Wainscot" or "Bar_Molding" or "Counter_Front" or "Counter_Top")
                return ("wood", .39f, 2f);
            return ("stone", .38f, 3f);
        }

        static void SetNormal(Material material, Texture2D map, float strength, Vector2 tiling)
        {
            material.SetTexture("_BumpMap", map);
            material.SetFloat("_BumpScale", strength);
            material.SetTextureScale("_BumpMap", tiling);
            material.SetTextureScale("_BaseMap", tiling);
            material.EnableKeyword("_NORMALMAP");
        }

        static Texture2D Surface(string file, bool normal)
        {
            string path = TextureRoot + file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException($"Falta la textura {path}");
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type || importer.wrapMode != TextureWrapMode.Repeat ||
                importer.filterMode != FilterMode.Trilinear || !importer.mipmapEnabled ||
                importer.anisoLevel != 4 || importer.sRGBTexture == normal ||
                importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.textureType = type;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 4;
                importer.sRGBTexture = !normal;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
