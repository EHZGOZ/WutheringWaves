using UnityEngine;

namespace WutheringWaves
{
    [System.Serializable]
    public class CharacterRuntimeData
    {
        [Header("=== 角色运行时数据 ===")]
        [Header("角色名称")]
        [SerializeField] public CharacterName characterName; // 角色名称
        [Header("最大生命值")]
        [SerializeField] public float maxHealth; // 最大生命值
        [Header("当前生命值")]
        [SerializeField] public float currentHealth; // 当前生命值

        public float NormalizedHealth => maxHealth <= 0f ? 0f : currentHealth / maxHealth; // 生命值百分比
        public bool IsDead => currentHealth <= 0f; // 是否死亡


        #region 初始化
        // 运行时数据初始化：从角色静态模板中读取初始生命值
        public void Initialize(CharacterDataSO characterDataSO)
        {
            //1.空值校验
            if (characterDataSO == null)
            {
                Debug.Log("characterDataSO为空");
                return;
            }

            //2.角色基础数据初始化当前
            characterName = characterDataSO.characterName;
            currentHealth = characterDataSO.maxHealth;
            maxHealth = characterDataSO.maxHealth;

            //3.生命值容错，保证初始化后的生命值始终处于合法范围
            ClampHealth();
        }

        #endregion

        #region 数据相关
        // 将外部运行时数据复制到当前对象：读档时保留原对象引用，只回填字段
        public void CopyFrom(CharacterRuntimeData source)
        {
            //1.空值校验
            if (source == null)
            {
                return;
            }

            //2.逐字段复制运行时数据
            characterName = source.characterName;
            currentHealth = source.currentHealth;
            maxHealth = source.maxHealth;

            //3.生命值容错：防止旧存档或异常存档中的生命值越界
            ClampHealth();
        }


        // 克隆一份独立运行时数据：用于存档快照和调试镜像，避免共享引用
        public CharacterRuntimeData Clone()
        {
            return new CharacterRuntimeData
            {
                characterName = characterName,
                currentHealth = currentHealth,
                maxHealth = maxHealth,
            };
        }
        #endregion

        #region 生命值相关
        // 受到伤害
        public void TakeDamage(float damage)
        {
            // 1.无效伤害不处理
            if (damage <= 0f || IsDead)
            {
                return;
            }

            // 2.扣除生命值，并限制范围
            currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        }

        // 恢复生命值
        public void Heal(float amount)
        {
            // 1.无效治疗不处理
            if (amount <= 0f || IsDead)
            {
                return;
            }

            // 2.恢复生命值，并限制范围
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        }

        // 设置当前生命值
        public void SetHealth(float value)
        {
            currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        }

        // 校准生命值：读档或配置变化后使用
        public void ClampHealth()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        #endregion

    }
}
