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

        #region 工具方法
        public void SetCurrentStep(AttackStep step)
        {
            currentStep = step;
        }
        #endregion

        #region 攻击转向
        [Header("=== 攻击转向配置 ===")]
        [Header("攻击自动转向搜索半径")]
        public float attackTurnSearchRadius = 8f;

        [Header("攻击自动转向最大角度")]
        public float attackTurnMaxAngle = 120f;

        // 查找最近的敌人：只负责找目标，不负责旋转
        public Transform FindNearestEnemyForAttack()
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, attackTurnSearchRadius, enemyLayer);

            Transform nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                Vector3 direction = enemy.transform.position - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude <= 0.001f)
                {
                    continue;
                }

                // 限制角度，避免角色攻击时突然180度转身打背后的怪
                float angle = Vector3.Angle(transform.forward, direction.normalized);
                if (angle > attackTurnMaxAngle * 0.5f)
                {
                    continue;
                }

                float distance = direction.sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy.transform;
                }
            }

            return nearestEnemy;
        }
        #endregion


    }
}
