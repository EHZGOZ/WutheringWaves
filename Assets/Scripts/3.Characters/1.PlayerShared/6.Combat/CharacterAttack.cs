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
            // 1.攻击数据不完整时，不执行命中检测
            if (currentStep == null || currentStep.hitConfig == null || characterData == null)
            {
                return;
            }

            // 2.读取当前攻击段的命中配置
            AttackHitConfig hitConfig = currentStep.hitConfig;

            // 3.根据角色当前位置和攻击偏移，计算本次范围判定中心点
            Vector3 attackCenter = transform.position + transform.TransformDirection(hitConfig.attackOffset);

            // 4.使用范围检测查找敌人层级内的目标
            Collider[] hitEnemies = Physics.OverlapSphere(attackCenter, hitConfig.attackRadius, enemyLayer);

            // 5.根据角色基础攻击力和当前攻击段倍率计算最终伤害
            float actualDamage = characterData.baseAttack * hitConfig.damageMultiplier;

            foreach (Collider enemy in hitEnemies)
            {
                // 6.空值保护，避免异常Collider影响后续检测
                if (enemy == null)
                {
                    continue;
                }

                // 7.计算敌人相对角色的水平方向，用于扇形角度判断
                Vector3 hitDirection = enemy.transform.position - transform.position;
                hitDirection.y = 0f;

                if (hitDirection.sqrMagnitude <= 0.001f)
                {
                    continue;
                }

                // 8.角度超出当前攻击扇形范围时，不算命中
                float angle = Vector3.Angle(transform.forward, hitDirection.normalized);
                if (angle > hitConfig.attackAngle / 2f)
                {
                    continue;
                }

                // 9.从被命中的目标身上查找可受伤接口
                IDamageable damageable = enemy.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead)
                {
                    continue;
                }

                // 10.创建伤害信息，后续敌人受击、击退、掉落归属都从这里读取
                DamageInfo damageInfo = new DamageInfo(
                    actualDamage,
                    gameObject,
                    enemy.transform.position,
                    hitDirection.normalized,
                    currentStep.attackId
                );

                // 11.调用目标自己的受伤逻辑
                damageable.TakeDamage(damageInfo);

                // 12.保留调试输出，方便确认范围伤害链路已经打通
                Debug.Log($"命中{enemy.name} | 伤害:{actualDamage}");
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
