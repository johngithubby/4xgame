using UnityEditor;
using UnityEngine;

namespace LaneSurvivor.Editor
{
    public sealed class SwatSurvivorModelImporter : AssetPostprocessor
    {
        // Only the optimized technical-trial FBX should receive these mobile character import settings.
        private const string SwatModelAssetPath = "Assets/Resources/Survivor3D/SWAT_Survivor_Mobile.fbx";

        // Explicit external texture assets live here so Unity can import channels independently of FBX embedding.
        private const string SwatTextureFolder = "Assets/Resources/Survivor3D/Textures/";

        public override uint GetVersion()
        {
            // Increment this value whenever importer policy changes so Unity invalidates the cached FBX artifact.
            return 4;
        }

        private void OnPreprocessModel()
        {
            // Other models retain their authored settings because this postprocessor is deliberately path-specific.
            if (assetPath != SwatModelAssetPath)
            {
                return;
            }

            // ModelImporter is guaranteed for an FBX model preprocessing callback.
            ModelImporter importer = (ModelImporter)assetImporter;

            // Humanoid validation proves the Character Creator skeleton can accept Unity/Mixamo retargeted clips later.
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            // Clear any stale custom map so Unity auto-detects the standard names authored into the optimized FBX.
            importer.humanDescription = new HumanDescription();

            // Authored rifle idle/run actions replace the visibly stiff sine-only bone posing.
            importer.importAnimation = true;

            // Clip-loop settings are applied after first import by SwatSurvivorAnimationBuilder, when takes are available.

            // Bone transforms remain accessible for validation, attachments, and future animation retargeting.
            importer.optimizeGameObjects = false;
            importer.optimizeBones = false;

            // Medium compression reduces the 59k-triangle gameplay mesh without visibly changing its silhouette.
            importer.meshCompression = ModelImporterMeshCompression.Medium;

            // Runtime scripts never alter mesh vertices, so CPU-readable copies would waste mobile memory.
            importer.isReadable = false;

            // Four influences are sufficient for this tactical costume and map efficiently to mobile skinning.
            importer.skinWeights = ModelImporterSkinWeights.Custom;
            importer.maxBonesPerVertex = 4;

            // Imported slot names let runtime code resolve the matching external diffuse and normal-map assets.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

            // Import authored normals while calculating consistent tangents for normal-mapped PBR materials.
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;

            // Blendshapes are unnecessary behind the balaclava and were removed during mobile mesh optimization.
            importer.importBlendShapes = false;
        }

        private void OnPreprocessTexture()
        {
            // Other project textures must retain their existing platform and colour-space settings.
            if (!assetPath.StartsWith(SwatTextureFolder, System.StringComparison.Ordinal))
            {
                return;
            }

            // AssetPostprocessor guarantees a TextureImporter for texture preprocessing callbacks.
            TextureImporter importer = (TextureImporter)assetImporter;

            // Mobile-scale maps never need to exceed the 1024-pixel files exported from Blender.
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            // Normal and bump inputs need linear sampling and Unity's normal-map decoding path.
            bool isNormal = assetPath.Contains("_Normal.", System.StringComparison.Ordinal);
            bool isBump = assetPath.Contains("_Bump.", System.StringComparison.Ordinal);
            if (isNormal || isBump)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                importer.convertToNormalmap = isBump;
                importer.heightmapScale = isBump ? 0.08f : importer.heightmapScale;
                return;
            }

            // Diffuse maps remain colour textures; opacity/specular inputs use linear data sampling.
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = assetPath.Contains("_Diffuse", System.StringComparison.Ordinal);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
        }
    }
}
