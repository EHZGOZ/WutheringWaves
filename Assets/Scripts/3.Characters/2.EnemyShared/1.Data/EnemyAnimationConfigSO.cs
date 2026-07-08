using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    public enum EnemyAnimationId
    {
        None = 0,
        Idle = 1,
        Chase = 2,
        Hit = 3,
        Dead = 4,
        Attack = 5
    }

    [System.Serializable]
    // 单条敌人动画绑定：维护敌人动画语义ID到动画剪辑的映射
    public class EnemyAnimationBinding
    {
        [Header("敌人动画ID")]
        public EnemyAnimationId animationId = EnemyAnimationId.None;

        [Header("敌人动画 Clip")]
        public AnimationClip clip;
    }

    [CreateAssetMenu(menuName = "WutheringWaves/EnemyAnimationConfig", fileName = "EnemyAnimationConfig", order = 11)]
    // 敌人动画配置：集中维护敌人状态动画与战斗动画的剪辑映射
    public class EnemyAnimationConfigSO : ScriptableObject
    {
        [Header("=== 敌人动画绑定列表 ===")]
        [Tooltip("按 EnemyAnimationId 映射敌人动画剪辑")]
        [SerializeField] internal List<EnemyAnimationBinding> enemyAnimations = new List<EnemyAnimationBinding>();

        // 根据敌人动画ID获取对应的动画剪辑
        public AnimationClip GetEnemyAnimationClip(EnemyAnimationId animationId)
        {
            // 1.配置列表为空时直接返回空
            if (enemyAnimations == null)
            {
                return null;
            }

            // 2.遍历配置列表，查找对应动画ID
            for (int i = 0; i < enemyAnimations.Count; i++)
            {
                EnemyAnimationBinding binding = enemyAnimations[i];
                if (binding != null && binding.animationId == animationId)
                {
                    return binding.clip;
                }
            }

            // 3.没有找到对应配置时返回空
            return null;
        }

        // 根据敌人动画ID获取动画名称
        public string GetEnemyAnimationName(EnemyAnimationId animationId)
        {
            // 1.优先读取配置里的动画Clip名称
            AnimationClip clip = GetEnemyAnimationClip(animationId);
            if (clip != null)
            {
                return clip.name;
            }

            // 2.配置缺失时返回空字符串，避免播放不存在的动画状态
            return string.Empty;
        }

        // 根据敌人动画ID获取动画长度
        public float GetEnemyAnimationLength(EnemyAnimationId animationId)
        {
            // 1.优先读取配置里的动画Clip长度
            AnimationClip clip = GetEnemyAnimationClip(animationId);
            if (clip != null)
            {
                return clip.length;
            }

            // 2.配置缺失时返回0
            return 0f;
        }
    }
}