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

        #region 运行时更新
        // 手动推进角色中档运行时数据：由PlayerController统一调用，普通C#类不会自动执行Unity Update
        public void UpdateRuntime(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            switch (characterName)
            {
                case CharacterName.今汐:
                    UpdateJinxiRuntime(deltaTime);
                    break;

                case CharacterName.卡提希娅:
                    UpdateKatixiyaRuntime(deltaTime);
                    break;
            }
        }

        #region 今汐
        [Header("=== 今汐技能运行时数据 ===")]
        [SerializeField] public float jinxiESkill1CDTimer; // 今汐一段E技能剩余冷却
        [SerializeField] public float jinxiQBurstCDTimer; // 今汐Q爆发剩余冷却

        [SerializeField] public float jinxiESkill2WindowTimer; // 今汐E技能二段窗口剩余时间
        [SerializeField] public float jinxiESkill3WindowTimer; // 今汐E技能三段窗口剩余时间
        [SerializeField] public float jinxiESkill4WindowTimer; // 今汐E技能四段窗口剩余时间
        [SerializeField] public float jinxiFloatingTimer; // 今汐御空剩余时间

        [SerializeField] public bool jinxiIsESkill2WindowOpen; // 今汐E技能二段窗口是否开启
        [SerializeField] public bool jinxiIsESkill3WindowOpen; // 今汐E技能三段窗口是否开启
        [SerializeField] public bool jinxiIsESkill4WindowOpen; // 今汐E技能四段窗口是否开启
        [SerializeField] public bool jinxiIsFloating; // 今汐是否处于御空状态
                                                      // 推进今汐技能冷却、派生窗口与御空时间
        private void UpdateJinxiRuntime(float deltaTime)
        {
            // ESkill1 冷却
            if (jinxiESkill1CDTimer > 0f)
            {
                jinxiESkill1CDTimer = Mathf.Max(0f, jinxiESkill1CDTimer - deltaTime);
            }

            // QBurst 冷却
            if (jinxiQBurstCDTimer > 0f)
            {
                jinxiQBurstCDTimer = Mathf.Max(0f, jinxiQBurstCDTimer - deltaTime);
            }

            // ESkill2 派生窗口
            if (jinxiIsESkill2WindowOpen)
            {
                jinxiESkill2WindowTimer -= deltaTime;

                if (jinxiESkill2WindowTimer <= 0f)
                {
                    jinxiESkill2WindowTimer = 0f;
                    jinxiIsESkill2WindowOpen = false;
                }
            }

            // ESkill3 派生窗口
            if (jinxiIsESkill3WindowOpen)
            {
                jinxiESkill3WindowTimer -= deltaTime;

                if (jinxiESkill3WindowTimer <= 0f)
                {
                    jinxiESkill3WindowTimer = 0f;
                    jinxiIsESkill3WindowOpen = false;
                }
            }

            // ESkill4 派生窗口
            if (jinxiIsESkill4WindowOpen)
            {
                jinxiESkill4WindowTimer -= deltaTime;

                if (jinxiESkill4WindowTimer <= 0f)
                {
                    jinxiESkill4WindowTimer = 0f;
                    jinxiIsESkill4WindowOpen = false;
                }
            }

            // 御空状态持续时间
            if (jinxiIsFloating)
            {
                jinxiFloatingTimer -= deltaTime;

                if (jinxiFloatingTimer <= 0f)
                {
                    jinxiFloatingTimer = 0f;
                    jinxiIsFloating = false;

                    // 御空自然结束时，通知表现层刷新龙角显隐
                    GameEvents.RaiseFloatingChanged(false);
                }
            }
            // 持续通知今汐技能UI刷新
            GameEvents.RaiseSkillIconUIRuntimeChanged(this, SkillUIType.ESkill,  ResolveJinxiESkillIconIndex(), ResolveJinxiESkillCooldown());
            GameEvents.RaiseSkillIconUIRuntimeChanged(this, SkillUIType.QBurst, jinxiQBurstCDTimer > 0f ? -1 : 0, jinxiQBurstCDTimer);

        }
        // 解析今汐当前E技能图标索引
        private int ResolveJinxiESkillIconIndex()
        {
            // 1.E4窗口优先级最高
            if (jinxiIsESkill4WindowOpen && jinxiESkill4WindowTimer > 0f)
            {
                return 3;
            }

            // 2.E3窗口优先级其次
            if (jinxiIsESkill3WindowOpen && jinxiESkill3WindowTimer > 0f)
            {
                return 2;
            }

            // 3.E2窗口优先级再次
            if (jinxiIsESkill2WindowOpen && jinxiESkill2WindowTimer > 0f)
            {
                return 1;
            }

            // 4.默认显示E1图标
            return 0;
        }

        // 解析今汐当前E技能冷却时间
        private float ResolveJinxiESkillCooldown()
        {
            // 1.派生战技窗口打开时，只切换图标，不显示上一层战技冷却
            if (jinxiIsESkill4WindowOpen && jinxiESkill4WindowTimer > 0f)
            {
                return 0f;
            }

            if (jinxiIsESkill3WindowOpen && jinxiESkill3WindowTimer > 0f)
            {
                return 0f;
            }

            if (jinxiIsESkill2WindowOpen && jinxiESkill2WindowTimer > 0f)
            {
                return 0f;
            }

            // 2.没有派生窗口时，才显示E1冷却
            return jinxiESkill1CDTimer;
        }


        #endregion

        #region 卡提希娅
        [Header("=== 卡提希娅技能运行时数据 ===")]
        [SerializeField] public float katixiyaESkillCDTimer;
        [SerializeField] public float katixiyaQBurstCDTimer; // Q爆发冷却计时

        // 推进卡提希娅技能冷却时间
        private void UpdateKatixiyaRuntime(float deltaTime)
        {
            // ESkill 冷却
            if (katixiyaESkillCDTimer > 0f)
            {
                katixiyaESkillCDTimer = Mathf.Max(0f, katixiyaESkillCDTimer - deltaTime);
            }

            // QBurst 冷却
            if (katixiyaQBurstCDTimer > 0f)
            {
                katixiyaQBurstCDTimer = Mathf.Max(0f, katixiyaQBurstCDTimer - deltaTime);
            }
            // 持续通知卡提希娅技能UI刷新
            GameEvents.RaiseSkillIconUIRuntimeChanged(this, SkillUIType.ESkill, 0, katixiyaESkillCDTimer);
            GameEvents.RaiseSkillIconUIRuntimeChanged(this, SkillUIType.QBurst, katixiyaQBurstCDTimer > 0f ? -1 : 0, katixiyaQBurstCDTimer);

        }
        #endregion

        #endregion

        #region 事件通知

        #endregion


    }
}
