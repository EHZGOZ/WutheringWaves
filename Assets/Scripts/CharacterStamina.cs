using System;
using UnityEngine;

namespace WutheringWaves
{
    ///角色耐力管理组件
    [DisallowMultipleComponent]
    public class CharacterStamina : MonoBehaviour
    {
        [Header("=== 耐力基础配置 ===")]
        [Tooltip("最大耐力值")]
        [SerializeField] private float maxStamina = 100f;
        [Tooltip("奔跑每秒消耗的耐力")]
        [SerializeField] private float runDrainPerSecond = 2.5f;
        [Tooltip("单次冲刺消耗的耐力")]
        [SerializeField] private float sprintCost = 15f;
        [Tooltip("单次漂浮冲刺消耗的耐力")]
        [SerializeField] private float floatDashCost = 15f;
        [Tooltip("耐力每秒恢复速度")]
        [SerializeField] private float regenPerSecond = 10f;
        [Tooltip("消耗耐力后，延迟多久开始恢复")]
        [SerializeField] private float regenDelay = 1f;

        [Header("=== UI显示配置 ===")]
        [Tooltip("耐力回满后，延迟多久隐藏UI")]
        [SerializeField] private float fullStaminaFadeDelay = 1f;

        // ========================== 私有成员变量 ==========================   
        private CharacterData characterData;  // 角色数据载体（存储耐力相关状态）
        private float currentStamina;  // 当前剩余耐力 
        private float lastConsumeTime = -999f;// 最后一次消耗耐力的时间（用于计算恢复延迟）
        private float lastVisibleRequestTime = -999f;  // 最后一次请求显示UI的时间（用于计算隐藏延迟）
        private bool initialized; // 组件初始化完成标记  
        private bool isVisible; // 耐力UI当前是否可见

        // ========================== 公共只读属性（外部只读访问） ==========================
        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;
        public float NormalizedStamina => maxStamina <= 0f ? 0f : currentStamina / maxStamina;// 归一化耐力值（0~1，用于UI进度条）
        public float RunDrainPerSecond => runDrainPerSecond;
        public float SprintCost => sprintCost;
        public float FloatDashCost => floatDashCost;
        public float RegenPerSecond => regenPerSecond;
        public float RegenDelay => regenDelay;
        public bool IsVisible => isVisible;

        #region 生命周期
        // 初始化耐力组件
        public void Initialize(CharacterCore owner)
        {
            // 获取角色数据
            characterData = owner != null ? owner.GetCharacterData() : null;
            // 初始化当前耐力为最大值
            currentStamina = Mathf.Clamp(maxStamina, 0f, maxStamina);
            // 重置时间标记
            lastConsumeTime = -999f;
            lastVisibleRequestTime = -999f;
            // 标记初始化完成
            initialized = true;

            // 同步数据到角色载体
            SyncCharacterData(false);
            // 通知耐力数值变化
            NotifyStaminaChanged();
            // 初始化UI为隐藏状态
            SetVisible(false, true);
        }


        private void Update()
        {
            // 未初始化则不执行逻辑
            if (!initialized)
            {
                return;
            }

            // 更新耐力恢复逻辑
            UpdateRecovery();
            // 更新UI可见性逻辑
            UpdateVisibility();
        }
        #endregion


        #region 动作耐力判定
        // 判断是否可以奔跑（耐力大于极小值）
        public bool CanRun()
        {
            return currentStamina > 0.01f;
        }

        // 判断是否可以冲刺（耐力≥冲刺消耗）
        public bool CanSprint()
        {
            return currentStamina >= sprintCost;
        }

        // 判断是否可以御空冲刺（耐力≥漂浮冲刺消耗）
        public bool CanFloatDash()
        {
            return currentStamina >= floatDashCost;
        }
        #endregion

        #region 动作耐力消耗
        // 尝试消耗冲刺耐力
        public bool TryConsumeSprint()
        {
            return TryConsume(sprintCost);
        }

        /// <summary>
        /// 尝试消耗漂浮冲刺耐力
        /// </summary>
        public bool TryConsumeFloatDash()
        {
            return TryConsume(floatDashCost);
        }

        /// <summary>
        /// 奔跑持续消耗耐力（按时间消耗）
        /// </summary>
        /// <param name="deltaTime">消耗时长</param>
        /// <returns>是否成功消耗/可以继续奔跑</returns>
        public bool ConsumeRun(float deltaTime)
        {
            // 时间无效直接返回奔跑状态
            if (deltaTime <= 0f)
            {
                return CanRun();
            }

            // 耐力不足，无法奔跑
            if (!CanRun())
            {
                return false;
            }

            // 计算本次消耗的耐力
            float before = currentStamina;
            float cost = runDrainPerSecond * deltaTime;
            currentStamina = Mathf.Clamp(currentStamina - cost, 0f, maxStamina);

            // 耐力发生变化，更新状态
            if (!Mathf.Approximately(before, currentStamina))
            {
                lastConsumeTime = Time.time;
                lastVisibleRequestTime = Time.time;
                SyncCharacterData(false);
                NotifyStaminaChanged();
                SetVisible(true);
            }

            // 返回是否可以继续奔跑
            return before > 0.01f;
        }

        /// <summary>
        /// 通用耐力消耗方法（带安全校验）
        /// </summary>
        /// <param name="amount">消耗数值</param>
        /// <returns>是否消耗成功</returns>
        public bool TryConsume(float amount)
        {
            // 禁止负数消耗
            if (amount < 0f)
            {
                Debug.LogWarning($"[{nameof(CharacterStamina)}] 耐力消耗不能为负数: {amount}", this);
                return false;
            }

            // 消耗为0，直接成功
            if (amount == 0f)
            {
                return true;
            }

            // 耐力不足，消耗失败
            if (currentStamina < amount)
            {
                return false;
            }

            // 执行耐力消耗
            currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
            // 更新消耗时间
            lastConsumeTime = Time.time;
            // 更新UI显示时间
            lastVisibleRequestTime = Time.time;
            // 同步数据
            SyncCharacterData(false);
            // 通知数值变化
            NotifyStaminaChanged();
            // 显示UI
            SetVisible(true);
            return true;
        }
        #endregion

        #region 外部调用工具方法
        /// <summary>
        /// 强制刷新所有事件（用于外部主动同步状态）
        /// </summary>s
        public void ForceRefreshEvents()
        {
            NotifyStaminaChanged();
            SetVisible(isVisible, true);
        }
        #endregion

        #region 耐力恢复逻辑
        /// <summary>
        /// 更新耐力自动恢复
        /// </summary>
        private void UpdateRecovery()
        {
            // 判定条件：耐力未满 + 超过恢复延迟时间
            bool canRecover = currentStamina < maxStamina && (Time.time - lastConsumeTime) >= regenDelay;
            if (!canRecover)
            {
                SyncCharacterData(false);
                return;
            }

            // 执行耐力恢复
            float before = currentStamina;
            currentStamina = Mathf.Clamp(currentStamina + (regenPerSecond * Time.deltaTime), 0f, maxStamina);

            // 数值变化则通知更新
            if (!Mathf.Approximately(before, currentStamina))
            {
                SyncCharacterData(true);
                NotifyStaminaChanged();
            }
            else
            {
                SyncCharacterData(true);
            }
        }
        #endregion

        #region UI可见性控制
        /// <summary>
        /// 更新耐力UI的显示/隐藏
        /// </summary>
        private void UpdateVisibility()
        {
            // 耐力未满：强制显示UI，并刷新显示时间
            if (currentStamina < maxStamina - 0.001f)
            {
                lastVisibleRequestTime = Time.time;
                SetVisible(true);
                return;
            }

            // 耐力已满：判断是否达到隐藏延迟时间
            bool shouldStayVisible = (Time.time - lastVisibleRequestTime) < fullStaminaFadeDelay;
            SetVisible(shouldStayVisible);
        }

        #endregion

        #region 事件通知
        /// <summary>
        /// 触发耐力数值变化事件
        /// </summary>
        private void NotifyStaminaChanged()
        {
            GameEvents.RaiseStaminaChanged(this, currentStamina, maxStamina, NormalizedStamina);
        }
        /// <summary>
        /// 设置UI可见性，并触发事件
        /// </summary>
        /// <param name="visible">是否可见</param>
        /// <param name="forceNotify">是否强制通知（无视状态）</param>
        private void SetVisible(bool visible, bool forceNotify = false)
        {
            // 状态未变化且不强制，则不执行
            if (!forceNotify && isVisible == visible)
            {
                return;
            }

            isVisible = visible;
            GameEvents.RaiseStaminaVisibilityChanged(this, isVisible);
        }
        #endregion

        #region 数据同步
        /// <summary>
        /// 同步耐力数据到角色数据组件
        /// </summary>
        /// <param name="isRecovering">是否正在恢复耐力</param>
        private void SyncCharacterData(bool isRecovering)
        {
            if (characterData == null)
            {
                return;
            }

            characterData.currentStamina = currentStamina;
            characterData.lastStaminaCostTime = lastConsumeTime;
            characterData.canRecoveringStamina = isRecovering ? 1f : 0f;
        }
        #endregion





    }
}
