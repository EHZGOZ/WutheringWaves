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
        }

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

    }
}
