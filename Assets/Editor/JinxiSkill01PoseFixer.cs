using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WutheringWaves.EditorTools
{
    // 今汐 Skill01 姿势修正工具：生成一个不修改原FBX的 Skill01_Fixed.anim，用来验证 Skill01 接 Idle_Pose 的平移问题
    public static class JinxiSkill01PoseFixer
    {
        private const string Skill01Path = "Assets/AAAUsedAssets/1.Animations/1.今汐/Generic/Attack/E/Skill01.fbx";
        private const string IdlePosePath = "Assets/AAAUsedAssets/1.Animations/1.今汐/Generic/Locomotion/Idle_Pose.fbx";
        private const string OutputPath = "Assets/AAAUsedAssets/1.Animations/1.今汐/Generic/Attack/E/Skill01_Fixed.anim";
        private const string RootBonePath = "Bip001";
        private const float FixBlendDuration = 0.12f;

        [MenuItem("Tools/今汐/生成 Skill01 对齐 Idle_Pose 动画")]
        public static void GenerateFixedSkill01()
        {
            AnimationClip sourceClip = LoadClip(Skill01Path, "Skill01");
            AnimationClip idlePoseClip = LoadClip(IdlePosePath, "Idle_Pose");

            if (sourceClip == null || idlePoseClip == null)
            {
                Debug.LogError("生成 Skill01_Fixed 失败：没有找到 Skill01 或 Idle_Pose 动画。");
                return;
            }

            AnimationClip fixedClip = Object.Instantiate(sourceClip);
            // 文件名使用 Skill01_Fixed.anim 方便区分，Clip 名保留 Skill01，避免状态机 CrossFade 找不到原状态名
            fixedClip.name = "Skill01";

            AlignRootPositionXZToIdlePose(fixedClip, sourceClip, idlePoseClip);

            AssetDatabase.DeleteAsset(OutputPath);
            AssetDatabase.CreateAsset(fixedClip, OutputPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"已生成 Skill01_Fixed：{OutputPath}");
        }

        // 从FBX资源中读取指定名字的动画Clip
        private static AnimationClip LoadClip(string assetPath, string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            return assets
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => clip != null && clip.name == clipName);
        }

        // 将 Skill01 结尾的Root水平位置拉到 Idle_Pose 第一帧，避免切入 Idle_Pose 时出现水平平移
        private static void AlignRootPositionXZToIdlePose(AnimationClip fixedClip, AnimationClip sourceClip, AnimationClip idlePoseClip)
        {
            float endTime = sourceClip.length;
            float blendStartTime = Mathf.Max(0f, endTime - FixBlendDuration);

            FixPositionCurve(fixedClip, sourceClip, idlePoseClip, "m_LocalPosition.x", blendStartTime, endTime);
            FixPositionCurve(fixedClip, sourceClip, idlePoseClip, "m_LocalPosition.z", blendStartTime, endTime);
        }

        // 修正单条Root位置曲线：保留前面动作，只让最后一小段自然对齐到 Idle_Pose 的起始值
        private static void FixPositionCurve(AnimationClip fixedClip, AnimationClip sourceClip, AnimationClip idlePoseClip, string propertyName, float blendStartTime, float endTime)
        {
            EditorCurveBinding sourceBinding = FindRootPositionBinding(sourceClip, propertyName);
            EditorCurveBinding idlePoseBinding = FindRootPositionBinding(idlePoseClip, propertyName);

            AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(sourceClip, sourceBinding);
            AnimationCurve idlePoseCurve = AnimationUtility.GetEditorCurve(idlePoseClip, idlePoseBinding);

            if (sourceCurve == null || idlePoseCurve == null)
            {
                Debug.LogWarning($"跳过 {propertyName} 修正：没有找到 {RootBonePath} 的对应曲线。");
                return;
            }

            float startValue = sourceCurve.Evaluate(blendStartTime);
            float targetValue = idlePoseCurve.Evaluate(0f);

            List<Keyframe> keys = new List<Keyframe>();
            for (int i = 0; i < sourceCurve.keys.Length; i++)
            {
                Keyframe key = sourceCurve.keys[i];
                if (key.time < blendStartTime)
                {
                    keys.Add(key);
                }
            }

            keys.Add(new Keyframe(blendStartTime, startValue));
            keys.Add(new Keyframe(endTime, targetValue));

            AnimationCurve fixedCurve = new AnimationCurve(keys.ToArray());
            for (int i = 0; i < fixedCurve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(fixedCurve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(fixedCurve, i, AnimationUtility.TangentMode.Auto);
            }

            AnimationUtility.SetEditorCurve(fixedClip, sourceBinding, fixedCurve);
            Debug.Log($"已修正 Skill01 {sourceBinding.path}.{propertyName}：结尾值 {sourceCurve.Evaluate(endTime):F4} -> {targetValue:F4}");
        }

        // 查找Root骨骼的位置曲线，优先使用 Bip001；找不到时返回空绑定并打印提示
        private static EditorCurveBinding FindRootPositionBinding(AnimationClip clip, string propertyName)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

            EditorCurveBinding binding = bindings.FirstOrDefault(currentBinding =>
                currentBinding.type == typeof(Transform)
                && currentBinding.propertyName == propertyName
                && (currentBinding.path == RootBonePath || currentBinding.path.EndsWith("/" + RootBonePath)));

            if (!string.IsNullOrEmpty(binding.propertyName))
            {
                return binding;
            }

            binding = bindings.FirstOrDefault(currentBinding =>
                currentBinding.type == typeof(Transform)
                && currentBinding.propertyName == propertyName
                && currentBinding.path.Contains(RootBonePath));

            if (!string.IsNullOrEmpty(binding.propertyName))
            {
                return binding;
            }

            string availableBindings = string.Join("\n", bindings
                .Where(currentBinding => currentBinding.type == typeof(Transform) && currentBinding.propertyName.Contains("Position"))
                .Select(currentBinding => $"{currentBinding.path}.{currentBinding.propertyName}"));

            Debug.LogWarning($"{clip.name} 没有找到 {RootBonePath}.{propertyName} 曲线，可用Position曲线：\n{availableBindings}");
            return new EditorCurveBinding
            {
                path = RootBonePath,
                type = typeof(Transform),
                propertyName = propertyName
            };
        }
    }
}
