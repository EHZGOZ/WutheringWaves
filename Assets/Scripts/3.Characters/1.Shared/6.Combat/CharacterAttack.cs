using UnityEngine;

namespace WutheringWaves
{
    public class CharacterAttack : MonoBehaviour
    {
        private CharacterDataSO characterData;
        private AttackStep currentStep;

        [Header("=== 通用攻击配置 ===")]
        [Header("默认连击窗口持续时间（秒）")]
        public float comboWindowDuration = 1.5f;

        [Header("攻击可命中的敌人层级")]
        public LayerMask enemyLayer;

        #region 初始化
        public void Initialize(CharacterContext context)
        {
            characterData = context != null ? context.CharacterDataSO : null;
        }
        #endregion

        public void SetCurrentStep(AttackStep step)
        {
            currentStep = step;
        }

        #region 命中检测
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
        #endregion

        #region 攻击数据查询
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
        #endregion




    }
}
