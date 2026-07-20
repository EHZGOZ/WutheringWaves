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

        [Header("当前攻击力")]
        [SerializeField] public float currentAttack; // 当前攻击力

        [Header("是否完成首次初始化")]
        [SerializeField] private bool hasInitialized; // 是否已经从CharacterDataSO完成过首次初始化

        #region 外部引用
        public bool HasInitialized => hasInitialized; // 是否完成首次初始化
        public float NormalizedHealth => maxHealth <= 0f ? 0f : currentHealth / maxHealth; // 生命值百分比
        public bool IsDead => currentHealth <= 0f; // 当前生命值归零时视为死亡
        #endregion

        #region 初始化
        // 确保每份角色运行时数据只完成一次基础初始化
        public void EnsureInitialized(CharacterDataSO characterDataSO)
        {
            // 1.已经完成首次初始化时，只校准生命值并停止处理
            // 不重新写入当前生命值，确保受伤或死亡状态得到保留
            if (hasInitialized)
            {
                ClampHealth();
                return;
            }

            // 2.首次初始化必须具有角色静态配置
            if (characterDataSO == null)
            {
                Debug.LogError("[CharacterRuntimeData] 首次初始化失败：characterDataSO为空。");
                return;
            }

            // 3.角色配置中的最大生命值必须有效
            if (characterDataSO.maxHealth <= 0f)
            {
                Debug.LogError(
                    $"[CharacterRuntimeData] 首次初始化失败：角色 {characterDataSO.characterName} 的最大生命值小于等于0。"
                );
                return;
            }

            // 4.只有首次创建这份运行时数据时，才从静态配置写入基础属性
            characterName = characterDataSO.characterName;
            maxHealth = characterDataSO.maxHealth;
            currentHealth = characterDataSO.maxHealth;
            currentAttack = characterDataSO.baseAttack;

            // 5.全部基础属性写入成功后，记录初始化完成
            hasInitialized = true;

            // 6.最后校准生命值范围
            ClampHealth();
        }
        #endregion

        #region 数据克隆
        // 克隆一份独立运行时数据：用于存档快照和调试镜像，避免共享引用
        public CharacterRuntimeData Clone()
        {
            return new CharacterRuntimeData
            {
                characterName = characterName,
                maxHealth = maxHealth,
                currentHealth = currentHealth,
                currentAttack = currentAttack,
                hasInitialized = hasInitialized
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
                    JinxiEvents.RaiseFloatingChanged(false);
                }
            }
            // 持续通知今汐技能UI刷新
            UIEvents.RaiseSkillIconUIRuntimeChanged(this, SkillUIType.ESkill,  ResolveJinxiESkillIconIndex(), ResolveJinxiESkillCooldown());
            UIEvents.RaiseSkillIconUIRuntimeChanged(this, SkillUIType.QBurst, jinxiQBurstCDTimer > 0f ? -1 : 0, jinxiQBurstCDTimer);

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
            UIEvents.RaiseSkillIconUIRuntimeChanged(this, SkillUIType.ESkill, 0, katixiyaESkillCDTimer);
            UIEvents.RaiseSkillIconUIRuntimeChanged(this, SkillUIType.QBurst, katixiyaQBurstCDTimer > 0f ? -1 : 0, katixiyaQBurstCDTimer);

        }
        #endregion

        #endregion
    }
}
