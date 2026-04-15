using UnityEngine;

namespace WutheringWaves
{
    [System.Serializable]
    public class CharacterRuntimeData
    {
        [Header("=== 角色运行时数据 ===")]
        [Tooltip("当前生命值")]
        [SerializeField] internal float currentHealth; // 当前生命值

        public bool IsInitialized { get; private set; } // 是否完成初始化

        // 运行时数据初始化：从角色静态模板中读取初始生命值
        public void Initialize(CharacterDataSO characterDataSO)
        {
            //1.空值校验
            if (characterDataSO == null)
            {
                currentHealth = 0f;
                IsInitialized = false;
                return;
            }

            //2.初始化当前生命值
            currentHealth = characterDataSO.maxHealth;

            //3.标记初始化完成
            IsInitialized = true;
        }
    }
}
