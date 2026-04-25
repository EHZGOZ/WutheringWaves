using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    public enum LocomotionAnimationId
    {
        None = 0,
        Idle = 1,
        Move = 2,
        Run = 3,
        Stop_Run = 4,
        Jump_Walk = 5,
        Jump_Run = 6,
        Fall = 7,
        Land = 8,
        DashForward = 9,
        DashBackward = 10,
        AirDashForward = 11,
        AirDashBackward = 12,
        FloatDashingForward = 13,
        FloatDashingBackward = 14,
        Dodge = 15
    }

    public enum AttackId
    {
        None = 0,
        Attack01 = 1,
        Attack02 = 2,
        Attack03 = 3,
        Attack04 = 4,
        FallAttackStart = 5,
        FallAttackLoop = 6,
        FallAttackEnd = 7,
        FloatAttackGround01 = 8,
        FloatAttackGround02 = 9,
        FloatAttackGround03 = 10,
        FloatAttackGround04 = 11,
        FloatAttackAir01 = 12,
        FloatAttackAir02 = 13,
        FloatAttackAir03 = 14,
        FloatAttackAir04 = 15,
        ESkill01 = 16,
        ESkill02 = 17,
        ESkill03 = 18,
        ESkill04 = 19,
        QBurst = 20
    }

    [System.Serializable]
    // 单条移动动画绑定：维护移动状态键到动画剪辑的映射
    public class LocomotionAnimationBinding
    {
        [Header("移动动画ID")]
        public LocomotionAnimationId animationId = LocomotionAnimationId.None;

        [Header("移动动画 Clip")]
        public AnimationClip clip;
    }

    [System.Serializable]
    // 单条战斗动画绑定：维护 AttackId 到动画剪辑的映射
    public class CombatAnimationBinding
    {
        [Header("攻击步骤ID")]
        public AttackId attackId = AttackId.None;

        [Header("战斗动画 Clip")]
        public AnimationClip clip;
    }

    [CreateAssetMenu(menuName = "WutheringWaves/AnimationConfig", fileName = "AnimationConfig", order = 2)]
    // 动画配置：集中维护移动行为与战斗行为的动画剪辑映射
    public class AnimationConfigSO : ScriptableObject
    {
        [Header("===角色信息===")]
        [SerializeField] internal CharacterName characterName;

        [Header("=== 移动动画绑定列表 ===")]
        [Tooltip("按 LocomotionAnimationId 映射移动行为动画剪辑")]
        [SerializeField] internal List<LocomotionAnimationBinding> locomotionAnimations = new List<LocomotionAnimationBinding>();

        [Header("=== 战斗动画绑定列表 ===")]
        [Tooltip("按 AttackId 映射战斗行为动画剪辑")]
        [SerializeField] internal List<CombatAnimationBinding> combatAnimations = new List<CombatAnimationBinding>();

        // 根据移动动画ID获取对应的动画剪辑
        public AnimationClip GetLocomotionClip(LocomotionAnimationId animationId)
        {
            if (locomotionAnimations == null)
            {
                return null;
            }

            for (int i = 0; i < locomotionAnimations.Count; i++)
            {
                LocomotionAnimationBinding binding = locomotionAnimations[i];
                if (binding != null && binding.animationId == animationId)
                {
                    return binding.clip;
                }
            }

            return null;
        }

        // 根据移动动画ID获取动画名称
        public string GetLocomotionAnimationName(LocomotionAnimationId animationId)
        {
            //1.Idle / Move / Run 都由 Locomotion 混合树驱动
            if (animationId == LocomotionAnimationId.Idle
                || animationId == LocomotionAnimationId.Move
                || animationId == LocomotionAnimationId.Run)
            {
                return "Locomotion";
            }

            //2.其他动作优先读取配置里的动画Clip名称
            AnimationClip clip = GetLocomotionClip(animationId);
            if (clip != null)
            {
                return clip.name;
            }

            //3.配置缺失时使用默认动画名称兜底
            switch (animationId)
            {
                case LocomotionAnimationId.Stop_Run:
                    return "Stop_Run";
                case LocomotionAnimationId.Jump_Walk:
                    return "Jump_Walk";
                case LocomotionAnimationId.Jump_Run:
                    return "Jump_Run";
                case LocomotionAnimationId.Fall:
                    return "Fall_Loop";
                case LocomotionAnimationId.Land:
                    return "Land";
                case LocomotionAnimationId.DashForward:
                    return "DashF";
                case LocomotionAnimationId.DashBackward:
                    return "DashB";
                case LocomotionAnimationId.AirDashForward:
                    return "Jump_Second_F";
                case LocomotionAnimationId.AirDashBackward:
                    return "Jump_Second_B";
                case LocomotionAnimationId.FloatDashingForward:
                    return "SkillMove_F";
                case LocomotionAnimationId.FloatDashingBackward:
                    return "SkillMove_B";
                case LocomotionAnimationId.Dodge:
                    return "Dodge";
                default:
                    return string.Empty;
            }
        }
        // 根据移动动画ID获取动画长度
        public float GetLocomotionAnimationLength(LocomotionAnimationId animationId)
        {
            //1.优先读取配置里的动画Clip长度
            AnimationClip clip = GetLocomotionClip(animationId);
            if (clip != null)
            {
                return clip.length;
            }

            //2.配置缺失时使用默认时长兜底
            switch (animationId)
            {
                case LocomotionAnimationId.Land:
                    return 1.03f;
                default:
                    return 0f;
            }
        }


        // 根据 AttackId 获取对应的战斗动画剪辑
        public AnimationClip GetCombatClip(AttackId attackId)
        {
            if (combatAnimations == null)
            {
                return null;
            }

            for (int i = 0; i < combatAnimations.Count; i++)
            {
                CombatAnimationBinding binding = combatAnimations[i];
                if (binding != null && binding.attackId == attackId)
                {
                    return binding.clip;
                }
            }

            return null;
        }

        // 根据攻击ID获取战斗动画名称
        public string GetCombatAnimationName(AttackId attackId)
        {
            //1.优先读取配置里的动画Clip名称
            AnimationClip clip = GetCombatClip(attackId);
            if (clip != null)
            {
                return clip.name;
            }

            //2.配置缺失时返回空字符串
            return string.Empty;
        }

        // 根据攻击ID获取战斗动画长度
        public float GetCombatAnimationLength(AttackId attackId)
        {
            //1.优先读取配置里的动画Clip长度
            AnimationClip clip = GetCombatClip(attackId);
            if (clip != null)
            {
                return clip.length;
            }

            //2.配置缺失时返回0
            return 0f;
        }

    }
}
