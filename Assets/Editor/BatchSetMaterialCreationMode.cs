using System;
using UnityEditor;
using UnityEngine;

namespace WutheringWaves.EditorTools
{
    public static class BatchSetMaterialCreationMode
    {
        [MenuItem("Tools/FBX/将选中FBX材质模式设为 None")]
        private static void SetSelectedFbxToNone()
        {
            SetMaterialImportModeForSelection(ModelImporterMaterialImportMode.None);
        }

        [MenuItem("Tools/FBX/将选中FBX材质模式设为 Standard")]
        private static void SetSelectedFbxToStandard()
        {
            SetMaterialImportModeForSelection(ModelImporterMaterialImportMode.ImportStandard);
        }

        [MenuItem("Tools/FBX/将选中FBX材质模式设为 ViaMaterialDescription")]
        private static void SetSelectedFbxToImportViaMaterialDescription()
        {
            SetMaterialImportModeForSelection(ModelImporterMaterialImportMode.ImportViaMaterialDescription);
        }

        // 批量修改当前选中的FBX导入设置
        private static void SetMaterialImportModeForSelection(ModelImporterMaterialImportMode targetMode)
        {
            UnityEngine.Object[] selectedObjects = Selection.objects;
            int changedCount = 0;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedObjects[i]);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                if (!assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null)
                {
                    continue;
                }

                if (importer.materialImportMode == targetMode)
                {
                    continue;
                }

                importer.materialImportMode = targetMode;
                importer.SaveAndReimport();
                changedCount++;
            }

            Debug.Log($"批量修改完成，共重新导入 {changedCount} 个 FBX。");
        }
    }
}
