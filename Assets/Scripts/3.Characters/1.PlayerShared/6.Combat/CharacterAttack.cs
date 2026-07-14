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
            if (currentStep == null
                || currentStep.hitConfig == null
                || currentStep.rangeConfig == null
                || characterData == null)
            {
                return;
            }

            // 2.读取当前攻击段的命中配置和范围配置
            AttackHitConfig hitConfig = currentStep.hitConfig;
            AttackRangeConfig rangeConfig = currentStep.rangeConfig;

            // 3.收集所有范围命中的目标
            // 使用HashSet避免同一敌人的多个碰撞体或多个范围造成重复伤害
            System.Collections.Generic.HashSet<IDamageable> hitTargets =
                CollectAttackTargets(rangeConfig);

            // 4.根据角色基础攻击力和当前攻击段倍率计算最终伤害
            float actualDamage = characterData.baseAttack * hitConfig.damageMultiplier;

            foreach (IDamageable damageable in hitTargets)
            {
                // 5.受伤接口必须来自Unity组件，才能获取目标位置
                Component damageableComponent = damageable as Component;
                if (damageableComponent == null || damageable.IsDead)
                {
                    continue;
                }

                // 6.计算受击目标相对角色的水平方向
                Vector3 hitDirection =
                    damageableComponent.transform.position - transform.position;
                hitDirection.y = 0f;

                if (hitDirection.sqrMagnitude <= 0.001f)
                {
                    continue;
                }

                // 7.创建伤害信息
                // 削韧值和受击反应将在下一步接入DamageInfo
                DamageInfo damageInfo = new DamageInfo(
                    actualDamage,
                    gameObject,
                    damageableComponent.transform.position,
                    hitDirection.normalized,
                    currentStep.attackId
                );

                // 8.调用目标自己的受伤逻辑
                damageable.TakeDamage(damageInfo);

                // 9.保留调试输出，方便确认范围伤害链路
                Debug.Log(
                    $"命中{damageableComponent.name} | 伤害:{actualDamage}"
                );
            }
        }

        // 收集当前攻击段命中的所有可受伤目标
        private System.Collections.Generic.HashSet<IDamageable> CollectAttackTargets(
            AttackRangeConfig rangeConfig)
        {
            // 1.使用集合保存目标，确保同一目标只命中一次
            System.Collections.Generic.HashSet<IDamageable> hitTargets =
                new System.Collections.Generic.HashSet<IDamageable>();

            // 2.开启范围叠加时，合并所有已启用范围
            if (rangeConfig.enableRangeCombination)
            {
                if (rangeConfig.useSphereRange)
                {
                    CollectSphereTargets(rangeConfig, hitTargets);
                }

                if (rangeConfig.useSectorRange)
                {
                    CollectSectorTargets(rangeConfig, hitTargets);
                }

                if (rangeConfig.useBoxRange)
                {
                    CollectBoxTargets(rangeConfig, hitTargets);
                }

                return hitTargets;
            }

            // 3.未开启范围叠加时，只使用指定的单一范围
            switch (rangeConfig.singleRangeType)
            {
                case AttackRangeType.Sphere:
                    CollectSphereTargets(rangeConfig, hitTargets);
                    break;

                case AttackRangeType.Sector:
                    CollectSectorTargets(rangeConfig, hitTargets);
                    break;

                case AttackRangeType.Box:
                    CollectBoxTargets(rangeConfig, hitTargets);
                    break;
            }

            return hitTargets;
        }

        // 收集球形范围内的目标
        private void CollectSphereTargets(
            AttackRangeConfig rangeConfig,
            System.Collections.Generic.HashSet<IDamageable> hitTargets)
        {
            // 1.半径无效时不执行范围检测
            if (rangeConfig.sphereRadius <= 0f)
            {
                return;
            }

            // 2.将本地偏移转换为世界坐标
            Vector3 attackCenter =
                transform.position
                + transform.TransformDirection(rangeConfig.sphereOffset);

            // 3.检测球形范围内的敌人碰撞体
            Collider[] hitEnemies = Physics.OverlapSphere(
                attackCenter,
                rangeConfig.sphereRadius,
                enemyLayer
            );

            // 4.将有效目标加入去重集合
            AddDamageableTargets(hitEnemies, hitTargets);
        }

        // 收集扇形范围内的目标
        private void CollectSectorTargets(
            AttackRangeConfig rangeConfig,
            System.Collections.Generic.HashSet<IDamageable> hitTargets)
        {
            // 1.半径或角度无效时不执行范围检测
            if (rangeConfig.sectorRadius <= 0f
                || rangeConfig.sectorAngle <= 0f)
            {
                return;
            }

            // 2.将本地偏移转换为世界坐标，作为扇形起点
            Vector3 attackCenter =
                transform.position
                + transform.TransformDirection(rangeConfig.sectorOffset);

            // 3.先用球形检测收集可能位于扇形内的碰撞体
            Collider[] hitEnemies = Physics.OverlapSphere(
                attackCenter,
                rangeConfig.sectorRadius,
                enemyLayer
            );

            for (int i = 0; i < hitEnemies.Length; i++)
            {
                Collider enemy = hitEnemies[i];
                if (enemy == null)
                {
                    continue;
                }

                // 4.计算目标相对扇形起点的水平方向
                Vector3 targetDirection = enemy.bounds.center - attackCenter;
                targetDirection.y = 0f;

                if (targetDirection.sqrMagnitude <= 0.001f)
                {
                    continue;
                }

                // 5.只保留角色前方扇形角度内的目标
                float angle = Vector3.Angle(
                    transform.forward,
                    targetDirection.normalized
                );

                if (angle > rangeConfig.sectorAngle / 2f)
                {
                    continue;
                }

                TryAddDamageableTarget(enemy, hitTargets);
            }
        }

        // 收集矩形范围内的目标
        private void CollectBoxTargets(
            AttackRangeConfig rangeConfig,
            System.Collections.Generic.HashSet<IDamageable> hitTargets)
        {
            // 1.任意尺寸小于等于0时不执行矩形检测
            if (rangeConfig.boxSize.x <= 0f
                || rangeConfig.boxSize.y <= 0f
                || rangeConfig.boxSize.z <= 0f)
            {
                return;
            }

            // 2.将本地偏移转换为世界坐标
            Vector3 attackCenter =
                transform.position
                + transform.TransformDirection(rangeConfig.boxOffset);

            // 3.OverlapBox使用半尺寸，并跟随角色当前朝向旋转
            Vector3 halfExtents = rangeConfig.boxSize * 0.5f;
            Collider[] hitEnemies = Physics.OverlapBox(
                attackCenter,
                halfExtents,
                transform.rotation,
                enemyLayer
            );

            // 4.将有效目标加入去重集合
            AddDamageableTargets(hitEnemies, hitTargets);
        }

        // 将一组碰撞体转换为可受伤目标
        private void AddDamageableTargets(
            Collider[] hitEnemies,
            System.Collections.Generic.HashSet<IDamageable> hitTargets)
        {
            // 1.逐个验证范围检测结果
            for (int i = 0; i < hitEnemies.Length; i++)
            {
                TryAddDamageableTarget(hitEnemies[i], hitTargets);
            }
        }

        // 尝试添加单个可受伤目标
        private void TryAddDamageableTarget(
            Collider enemy,
            System.Collections.Generic.HashSet<IDamageable> hitTargets)
        {
            // 1.碰撞体为空时不处理
            if (enemy == null)
            {
                return;
            }

            // 2.从碰撞体父级查找统一受伤接口
            IDamageable damageable = enemy.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead)
            {
                return;
            }

            // 3.HashSet会自动忽略同一受伤对象的重复添加
            hitTargets.Add(damageable);
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
