using System.IO;
using UnityEditor;
using UnityEngine;

namespace WutheringWaves.EditorTools
{
    public static class JinxiDragonHornExtractor
    {
        private const string SourceFbxPath = "Assets/GameAssets/1.CharacterModel/1.Player/今汐/旧/今汐建模.fbx";
        private const string HornObjectName = "R2T1JinxiDragonHornMd10011";
        private const string OutputFolder = "Assets/GameAssets/1.CharacterModel/1.Player/今汐/提取龙角";
        private const string OutputMeshName = "JinxiDragonHorn_FromOldModel_HeadSpace.asset";
        private const string OutputPrefabName = "JinxiDragonHorn_FromOldModel_HeadSpace.prefab";

        [MenuItem("Tools/今汐/从旧模型提取头部龙角")]
        private static void ExtractDragonHorn()
        {
            GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbxPath);
            if (sourceAsset == null)
            {
                Debug.LogError($"没有找到旧模型 FBX：{SourceFbxPath}");
                return;
            }

            GameObject sourceInstance = Object.Instantiate(sourceAsset);
            sourceInstance.name = sourceAsset.name + "_ExtractionTemp";
            sourceInstance.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                Transform hornTransform = FindChildByName(sourceInstance.transform, HornObjectName);
                if (hornTransform == null)
                {
                    Debug.LogError($"旧模型里没有找到子对象：{HornObjectName}");
                    return;
                }

                Transform headTransform = FindFirstChildByName(sourceInstance.transform, "Bip001Head", "Bip001_Head", "Head");
                if (headTransform == null)
                {
                    Debug.LogError("旧模型里没有找到头部骨骼，已取消提取。请检查头骨名称是否为 Bip001Head / Bip001_Head / Head。");
                    return;
                }

                Renderer sourceRenderer = hornTransform.GetComponent<Renderer>();
                Mesh sourceMesh = BuildHeadSpaceMesh(hornTransform, headTransform, sourceRenderer);
                if (sourceMesh == null)
                {
                    Debug.LogError($"子对象 {HornObjectName} 上没有可提取的 Mesh。");
                    return;
                }

                EnsureOutputFolder();

                string meshPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(OutputFolder, OutputMeshName).Replace("\\", "/"));
                AssetDatabase.CreateAsset(sourceMesh, meshPath);

                GameObject prefabRoot = new GameObject("JinxiDragonHorn_FromOldModel");
                MeshFilter meshFilter = prefabRoot.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = prefabRoot.AddComponent<MeshRenderer>();
                meshFilter.sharedMesh = sourceMesh;
                meshRenderer.sharedMaterials = sourceRenderer != null ? sourceRenderer.sharedMaterials : new Material[0];

                string prefabPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(OutputFolder, OutputPrefabName).Replace("\\", "/"));
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Object.DestroyImmediate(prefabRoot);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"龙角提取完成：{prefabPath}\n使用方法：把这个 prefab 拖到新今汐的 Head/Bip001Head 骨骼下面，并把本地 Position/Rotation/Scale 设为 0/0/1。");
            }
            finally
            {
                Object.DestroyImmediate(sourceInstance);
            }
        }

        private static Mesh BuildHeadSpaceMesh(Transform hornTransform, Transform headTransform, Renderer sourceRenderer)
        {
            Mesh mesh = null;
            Matrix4x4 sourceLocalToWorld;

            SkinnedMeshRenderer skinnedMeshRenderer = hornTransform.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                mesh = new Mesh
                {
                    name = "JinxiDragonHorn_FromOldModel_HeadSpace"
                };
                skinnedMeshRenderer.BakeMesh(mesh);
                sourceLocalToWorld = hornTransform.localToWorldMatrix;
            }
            else
            {
                MeshFilter meshFilter = hornTransform.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    return null;
                }

                mesh = Object.Instantiate(meshFilter.sharedMesh);
                mesh.name = "JinxiDragonHorn_FromOldModel_HeadSpace";
                sourceLocalToWorld = hornTransform.localToWorldMatrix;
            }

            Matrix4x4 toHeadLocal = headTransform.worldToLocalMatrix * sourceLocalToWorld;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = toHeadLocal.MultiplyPoint3x4(vertices[i]);
            }

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = toHeadLocal.MultiplyVector(normals[i]).normalized;
            }

            for (int i = 0; i < tangents.Length; i++)
            {
                Vector3 tangent = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                tangent = toHeadLocal.MultiplyVector(tangent).normalized;
                tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[i].w);
            }

            mesh.vertices = vertices;
            if (normals.Length == vertices.Length)
            {
                mesh.normals = normals;
            }

            if (tangents.Length == vertices.Length)
            {
                mesh.tangents = tangents;
            }

            mesh.RecalculateBounds();
            if (sourceRenderer != null)
            {
                mesh.bounds = TransformBounds(sourceRenderer.bounds, headTransform.worldToLocalMatrix);
            }

            return mesh;
        }

        private static Bounds TransformBounds(Bounds worldBounds, Matrix4x4 worldToLocal)
        {
            Vector3 center = worldToLocal.MultiplyPoint3x4(worldBounds.center);
            Vector3 extents = worldBounds.extents;
            Vector3 axisX = worldToLocal.MultiplyVector(new Vector3(extents.x, 0f, 0f));
            Vector3 axisY = worldToLocal.MultiplyVector(new Vector3(0f, extents.y, 0f));
            Vector3 axisZ = worldToLocal.MultiplyVector(new Vector3(0f, 0f, extents.z));
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
            return new Bounds(center, extents * 2f);
        }

        private static Transform FindFirstChildByName(Transform root, params string[] names)
        {
            foreach (string name in names)
            {
                Transform found = FindChildByName(root, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == name)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static void EnsureOutputFolder()
        {
            if (AssetDatabase.IsValidFolder(OutputFolder))
            {
                return;
            }

            string[] parts = OutputFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
