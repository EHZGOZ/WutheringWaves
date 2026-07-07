using UnityEngine;

namespace WutheringWaves
{
    [System.Serializable]
    public class EnemyRuntimeData
    {
        [Header("=== 敌人运行时数据 ===")]
        [Header("最大生命值")]
        [SerializeField] public float maxHealth; // 敌人最大生命值

        [Header("当前生命值")]
        [SerializeField] public float currentHealth; // 敌人当前生命值

        public float NormalizedHealth => maxHealth <= 0f ? 0f : currentHealth / maxHealth; // 敌人生命值百分比
        public bool IsDead => currentHealth <= 0f; // 敌人是否死亡

        #region 初始化
        // 初始化敌人运行时数据：从敌人静态配置中读取基础生命值
        public void Initialize(EnemyDataSO enemyDataSO)
        {
            // 1.空值校验，避免敌人配置缺失时报错
            if (enemyDataSO == null)
            {
                Debug.Log("enemyDataSO为空");
                return;
            }

            // 2.从静态配置中初始化生命值
            maxHealth = enemyDataSO.maxHealth;
            currentHealth = enemyDataSO.maxHealth;

            // 3.生命值容错，确保初始化后数值合法
            ClampHealth();
        }
        #endregion

        #region 生命值相关
        // 受到伤害
        public void TakeDamage(float damage)
        {
            // 1.无效伤害或已经死亡时不处理
            if (damage <= 0f || IsDead)
            {
                return;
            }

            // 2.扣除生命值，并限制到合法范围
            currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        }

        // 恢复生命值
        public void Heal(float amount)
        {
            // 1.无效治疗或已经死亡时不处理
            if (amount <= 0f || IsDead)
            {
                return;
            }

            // 2.恢复生命值，并限制到合法范围
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        }

        // 校准生命值：初始化或后续读档时使用
        public void ClampHealth()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }
        #endregion
    }
}