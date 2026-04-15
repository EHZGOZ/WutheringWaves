using UnityEngine;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 玩家共享体力组件：挂在 Player 层，统一管理所有角色共用的体力运行时逻辑
    public class PlayerStamina : MonoBehaviour
    {
        [Header("=== 体力基础配置 ===")]
        [Tooltip("最大体力值")]
        [SerializeField] private float maxStamina = 100f;
        [Tooltip("奔跑每秒消耗的体力")]
        [SerializeField] private float runDrainPerSecond = 2.5f;
        [Tooltip("单次冲刺消耗的体力")]
        [SerializeField] private float sprintCost = 15f;
        [Tooltip("单次御空冲刺消耗的体力")]
        [SerializeField] private float floatDashCost = 15f;
        [Tooltip("体力每秒恢复速度")]
        [SerializeField] private float regenPerSecond = 10f;
        [Tooltip("消耗体力后，延迟多久开始恢复")]
        [SerializeField] private float regenDelay = 1f;

        [Header("=== UI 显示配置 ===")]
        [Tooltip("体力回满后，延迟多久隐藏 UI")]
        [SerializeField] private float fullStaminaFadeDelay = 1f;

        private CharacterDataSO characterData; // 当前绑定角色的静态模板数据
        private float currentStamina; // 当前共享体力值
        private float lastStaminaCostTime = -999f; // 上一次消耗体力的时间
        private float lastVisibleRequestTime = -999f; // 上一次请求显示体力 UI 的时间
        private bool isRecoveringStamina; // 当前是否处于体力恢复状态
        private bool initialized; // 组件是否已经初始化完成
        private bool isVisible; // 当前体力 UI 是否可见

        #region 1. 对外只读属性
        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;
        public float NormalizedStamina => maxStamina <= 0f ? 0f : currentStamina / maxStamina;
        public float RunDrainPerSecond => runDrainPerSecond;
        public float SprintCost => sprintCost;
        public float FloatDashCost => floatDashCost;
        public float RegenPerSecond => regenPerSecond;
        public float RegenDelay => regenDelay;
        public bool IsRecoveringStamina => isRecoveringStamina;
        public bool IsVisible => isVisible;
        public bool IsInitialized => initialized;
        #endregion

        #region 2. 生命周期
        private void Update()
        {
            // 未初始化时不执行共享体力逻辑。
            if (!initialized)
            {
                return;
            }

            UpdateRecovery();
            UpdateVisibility();
        }
        #endregion

        #region 3. 初始化
        // 初始化玩家共享体力：首次进入时建立默认值，切角色时按当前角色配置重新同步上限
        public void Initialize(CharacterContext context)
        {
            // 1. 同步当前角色的静态模板数据。
            characterData = context != null ? context.CharacterDataSO : null;

            // 2. 把角色体力配置同步到玩家层体力组件。
            ApplyCharacterDataConfig();

            // 3. 首次初始化时，从角色模板读取默认体力。
            if (!initialized)
            {
                currentStamina = maxStamina;
                lastStaminaCostTime = -999f;
                lastVisibleRequestTime = -999f;
                isRecoveringStamina = false;
                initialized = true;
            }
            else
            {
                // 4. 切角色时沿用已存在的共享体力值，但要按当前角色上限做一次夹取。
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            }

            // 5. 同步初始显示状态并通知 UI。
            NotifyStaminaChanged();
            SetVisible(false, true);
        }

        // 同步当前角色的静态体力配置：让共享体力逻辑依旧从 CharacterDataSO 读取参数
        private void ApplyCharacterDataConfig()
        {
            if (characterData == null)
            {
                return;
            }

            maxStamina = characterData.maxStaminaData;
            runDrainPerSecond = characterData.staminaCostInRun;
            regenPerSecond = characterData.staminaRecovery;
            regenDelay = characterData.staminaRecoveryDelay;
        }
        #endregion

        #region 4. 动作体力判定
        public bool CanRun()
        {
            return currentStamina > 0.01f;
        }

        public bool CanSprint()
        {
            return currentStamina >= sprintCost;
        }

        public bool CanFloatDash()
        {
            return currentStamina >= floatDashCost;
        }
        #endregion

        #region 5. 动作体力消耗
        public bool TryConsumeSprint()
        {
            return TryConsume(sprintCost);
        }

        public bool TryConsumeFloatDash()
        {
            return TryConsume(floatDashCost);
        }

        public bool ConsumeRun(float deltaTime)
        {
            // 时间无效时，直接返回当前是否还能继续奔跑。
            if (deltaTime <= 0f)
            {
                return CanRun();
            }

            if (!CanRun())
            {
                return false;
            }

            float before = currentStamina;
            float cost = runDrainPerSecond * deltaTime;
            currentStamina = Mathf.Clamp(currentStamina - cost, 0f, maxStamina);

            if (!Mathf.Approximately(before, currentStamina))
            {
                lastStaminaCostTime = Time.time;
                lastVisibleRequestTime = Time.time;
                isRecoveringStamina = false;
                NotifyStaminaChanged();
                SetVisible(true);
            }

            return before > 0.01f;
        }

        public bool TryConsume(float amount)
        {
            if (amount < 0f)
            {
                Debug.LogWarning($"[{nameof(PlayerStamina)}] 体力消耗不能为负数: {amount}", this);
                return false;
            }

            if (amount == 0f)
            {
                return true;
            }

            if (currentStamina < amount)
            {
                return false;
            }

            currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
            lastStaminaCostTime = Time.time;
            lastVisibleRequestTime = Time.time;
            isRecoveringStamina = false;
            NotifyStaminaChanged();
            SetVisible(true);
            return true;
        }
        #endregion

        #region 6. 外部工具方法
        public void ForceRefreshEvents()
        {
            NotifyStaminaChanged();
            SetVisible(isVisible, true);
        }
        #endregion

        #region 7. 恢复逻辑
        private void UpdateRecovery()
        {
            bool canRecover = currentStamina < maxStamina && (Time.time - lastStaminaCostTime) >= regenDelay;
            if (!canRecover)
            {
                isRecoveringStamina = false;
                return;
            }

            float before = currentStamina;
            currentStamina = Mathf.Clamp(currentStamina + (regenPerSecond * Time.deltaTime), 0f, maxStamina);
            isRecoveringStamina = true;

            if (!Mathf.Approximately(before, currentStamina))
            {
                NotifyStaminaChanged();
            }
        }
        #endregion

        #region 8. UI 可见性
        private void UpdateVisibility()
        {
            if (currentStamina < maxStamina - 0.001f)
            {
                lastVisibleRequestTime = Time.time;
                SetVisible(true);
                return;
            }

            bool shouldStayVisible = (Time.time - lastVisibleRequestTime) < fullStaminaFadeDelay;
            SetVisible(shouldStayVisible);
        }

        private void SetVisible(bool visible, bool forceNotify = false)
        {
            if (!forceNotify && isVisible == visible)
            {
                return;
            }

            isVisible = visible;
            GameEvents.RaiseStaminaVisibilityChanged(this, isVisible);
        }
        #endregion

        #region 9. 事件通知
        private void NotifyStaminaChanged()
        {
            GameEvents.RaiseStaminaChanged(this, currentStamina, maxStamina, NormalizedStamina);
        }
        #endregion
    }
}
