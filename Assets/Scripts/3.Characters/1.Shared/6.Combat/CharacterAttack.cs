using UnityEngine;

namespace WutheringWaves
{
    public class CharacterAttack : MonoBehaviour
    {
        private CharacterContext context;
        private Animator animator;
        private CharacterDataSO characterData;
        private AnimationConfigSO animationConfig;
        private AttackStep currentStep;

        [Header("=== 通用攻击配置 ===")]
        [Header("默认连击窗口持续时间（秒）")]
        public float comboWindowDuration = 1.5f;

        [Header("攻击可命中的敌人层级")]
        public LayerMask enemyLayer;

        public void Initialize(CharacterContext context)
        {
            this.context = context;
            animator = context != null ? context.Animator : null;
            characterData = context != null ? context.CharacterDataSO : null;
            animationConfig = characterData != null ? characterData.animationConfigSO : null;
        }

        public void SetCurrentStep(AttackStep step)
        {
            currentStep = step;
        }

        public void CheckAttackHit()
        {
            if (currentStep == null || currentStep.hitConfig == null || characterData == null)
            {
                return;
            }

            AttackHitConfig hitConfig = currentStep.hitConfig;
            Vector3 attackCenter = transform.position + transform.TransformDirection(hitConfig.attackOffset);
            Collider[] hitEnemies = Physics.OverlapSphere(attackCenter, hitConfig.attackRadius, enemyLayer);
            float actualDamage = characterData.baseAttack * hitConfig.damageMultiplier;

            foreach (Collider enemy in hitEnemies)
            {
                Vector3 dir = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dir);
                if (angle <= hitConfig.attackAngle / 2f)
                {
                    Debug.Log($"命中{enemy.name} | 伤害:{actualDamage}");
                }
            }
        }

        public string GetCharacterAnimationTriggerName(AttackStep step)
        {
            if (step == null || animationConfig == null)
            {
                return string.Empty;
            }

            AnimationClip clip = animationConfig.GetCombatClip(step.attackId);
            return clip != null ? clip.name : string.Empty;
        }

        public float GetCharacterAnimationLength(AttackStep step)
        {
            if (step == null)
            {
                return 0f;
            }

            if (animationConfig != null)
            {
                AnimationClip clip = animationConfig.GetCombatClip(step.attackId);
                if (clip != null)
                {
                    return clip.length;
                }
            }

            return GetExecutionDuration(step);
        }

        public AttackHitConfig GetHitConfig(AttackStep step)
        {
            return step != null ? step.hitConfig : null;
        }

        public float GetExecutionDuration(AttackStep step)
        {
            if (step == null || step.timingConfig == null)
            {
                return 0f;
            }

            return step.timingConfig.executionAttackCostTime;
        }

        public float GetComboWindowDuration(AttackStep step)
        {
            if (step == null || step.timingConfig == null)
            {
                return comboWindowDuration;
            }

            return step.timingConfig.comboWindowDuration > 0f ? step.timingConfig.comboWindowDuration : comboWindowDuration;
        }

        public string GetLocomotionAnimationName(LocomotionAnimationId animationId)
        {
            if (animationConfig != null)
            {
                AnimationClip clip = animationConfig.GetLocomotionClip(animationId);
                if (clip != null)
                {
                    return clip.name;
                }
            }

            switch (animationId)
            {
                case LocomotionAnimationId.Idle:
                    return "Idle";
                case LocomotionAnimationId.Move:
                    return "Move";
                case LocomotionAnimationId.Run:
                    return "Run";
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

        public float GetLocomotionAnimationLength(LocomotionAnimationId animationId)
        {
            if (animationConfig != null)
            {
                AnimationClip clip = animationConfig.GetLocomotionClip(animationId);
                if (clip != null)
                {
                    return clip.length;
                }
            }

            switch (animationId)
            {
                case LocomotionAnimationId.Land:
                    return 1.03f;
                default:
                    return 0f;
            }
        }
    }
}
