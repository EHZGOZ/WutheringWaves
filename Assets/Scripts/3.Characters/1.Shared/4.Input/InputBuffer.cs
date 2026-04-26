using UnityEngine;

namespace WutheringWaves
{
    // 输入缓冲器：只负责记录动作请求、长按判定和消费请求
    public class InputBuffer : MonoBehaviour
    {
        [Header("=== 输入判定 ===")]
        [Tooltip("输入缓冲时间")]
        [SerializeField] private float inputBufferTime = 0.3f;//记录玩家操作 inputBufferTime内有效
        [Tooltip("长按判定阈值：按住超过这个时间才会判断为长按，单位秒")]
        [SerializeField] private float longPressThreshold = 0.15f; // 核心：0.1秒内松开是点按，超过才是长按

        private bool _isInitialized;

        #region 对外只读属性
        public float InputBufferTime => inputBufferTime;
        public float LongPressThreshold => longPressThreshold;

        public bool WantsToJump { get; private set; }      // 触发跳跃用（按下瞬间）
        public bool WantsToDash { get; private set; }      // 触发冲刺用（按下瞬间）
        public bool IsHoldingRun { get; private set; }     // 持续奔跑用（按住状态）

        public bool WantsToAttack { get; private set; }    // 触发攻击用（按下瞬间）
        public bool WantsToAirAttack { get; private set; } // 触发御空攻击用（按下瞬间）
        public bool WantsToHAttack { get; private set; }   // 触发重攻击用（长按）

        public bool WantsToQBurst { get; private set; }    // 触发爆发用（按下瞬间）
        public bool WantsToESkill { get; private set; }    // 触发战技用（按下瞬间）
        #endregion

        #region 时间戳缓存
        private float jumpInputTimestamp;//跳跃时间戳
        private float dashInputTimestamp;//用于冲刺的shift按下时间戳
        private float runInputTimestamp; //用于奔跑的shift按下时间戳

        private float attackInputTimestamp;//用于Attack按下时间戳
        private float airAttackInputTimestamp;//用于御空攻击按下时间戳
        private float hAttackInputTimestamp;//用于重攻击按下时间戳

        private float qBurstInputTimestamp;//用于爆发按下时间戳
        private float eSkillInputTimestamp;//用于战技按下时间戳
        #endregion

        #region 按住状态缓存
        private bool isRunKeyPressed; // 记录Shift当前是否处于按下状态
        private bool isHAttackKeyPressed;// 记录Attack当前是否处于按下状态
        private bool hasTriggeredHeavyAttackThisPress;// 本次按压是否已经触发过重击
        #endregion

        #region 初始化
        public void Initialize()
        {
            ResetAllRequests();
            _isInitialized = true;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        // 重置所有请求和时间戳（角色初始化/死亡重置时使用）
        public void ResetAllRequests()
        {
            WantsToJump = false;
            jumpInputTimestamp = -Mathf.Infinity;

            WantsToDash = false;
            dashInputTimestamp = -Mathf.Infinity;

            IsHoldingRun = false;
            isRunKeyPressed = false;
            runInputTimestamp = -Mathf.Infinity;

            WantsToAttack = false;
            attackInputTimestamp = -Mathf.Infinity;

            WantsToAirAttack = false;
            airAttackInputTimestamp = -Mathf.Infinity;

            WantsToHAttack = false;
            isHAttackKeyPressed = false;
            hAttackInputTimestamp = -Mathf.Infinity;
            hasTriggeredHeavyAttackThisPress = false;

            WantsToQBurst = false;
            qBurstInputTimestamp = -Mathf.Infinity;

            WantsToESkill = false;
            eSkillInputTimestamp = -Mathf.Infinity;
        }
        #endregion

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            CheckRunKeyLongPressState();//每帧检测奔跑按键长按状态并更新标记
            CheckHAttackKeyLongPressState();//每帧检测重攻击按键长按状态并更新标记
        }

        #region 请求写入方法（供输入读取器/外观层调用）
        // 写入跳跃请求
        public void BufferJump()
        {
            EnsureInitialized();
            WantsToJump = true;
            jumpInputTimestamp = Time.time;
        }

        // 写入奔跑/冲刺请求
        public void BufferRun(bool isPressed)
        {
            EnsureInitialized();
            isRunKeyPressed = isPressed;

            if (isPressed)
            {
                WantsToDash = true;
                dashInputTimestamp = Time.time;

                IsHoldingRun = false;
                runInputTimestamp = Time.time;
            }
            else
            {
                IsHoldingRun = false;
                isRunKeyPressed = false;
            }
        }


        // 写入攻击/御空攻击/重攻击请求
        public void BufferAttack(bool isPressed)
        {
            EnsureInitialized();

            if (isPressed)
            {
                // 1.记录当前攻击键已按下
                isHAttackKeyPressed = true;

                // 2.记录按下起始时间，用于后续松开时判断轻击/重击
                hAttackInputTimestamp = Time.time;

                // 3.重置本次按压的重击触发标记
                hasTriggeredHeavyAttackThisPress = false;

                // 4.按下时先不直接写入攻击请求，避免轻击和重击范围重叠
                WantsToAttack = false;
                WantsToAirAttack = false;
                WantsToHAttack = false;
            }
            else
            {
                // 1.只有原本处于按下状态时，才处理松开判定
                if (!isHAttackKeyPressed)
                {
                    return;
                }

                // 2.计算本次按住时长
                float pressDuration = Time.time - hAttackInputTimestamp;

                // 3.先结束按住状态
                isHAttackKeyPressed = false;

                // 4.如果本次按压已经触发过重击，松开时不再补发任何攻击请求
                if (hasTriggeredHeavyAttackThisPress)
                {
                    hasTriggeredHeavyAttackThisPress = false;
                    return;
                }

                // 5.阈值之前松开：判定为轻攻击
                if (pressDuration < longPressThreshold)
                {
                    attackInputTimestamp = Time.time;
                    airAttackInputTimestamp = Time.time;

                    WantsToAttack = true;
                    WantsToAirAttack = true;
                    WantsToHAttack = false;
                }
                // 6.兜底：如果超阈值时还没来得及在 Update 中触发重击，则在松开时补发一次
                else
                {
                    hAttackInputTimestamp = Time.time;

                    WantsToAttack = false;
                    WantsToAirAttack = false;
                    WantsToHAttack = true;
                    hasTriggeredHeavyAttackThisPress = false;
                }
            }
        }


        // 写入战技请求
        public void BufferESkill()
        {
            EnsureInitialized();
            eSkillInputTimestamp = Time.time;
            WantsToESkill = true;
        }

        // 写入爆发请求
        public void BufferQBurst()
        {
            EnsureInitialized();
            qBurstInputTimestamp = Time.time;
            WantsToQBurst = true;
        }
        #endregion

        #region 跳跃相关
        //是否有有效的跳跃请求，如果有效消费跳跃请求
        public bool CheckAndConsumeJumpRequest()
        {
            EnsureInitialized();

            if (WantsToJump && (Time.time - jumpInputTimestamp) <= inputBufferTime)
            {
                WantsToJump = false;
                return true;
            }
            return false;
        }

        //跳跃时移除多余跳跃请求
        public void CleanWantsToJumpRequest()
        {
            WantsToJump = false;
        }
        #endregion

        #region 冲刺相关
        //是否有有效的冲刺请求，如果有效消费冲刺请求
        public bool CheckAndConsumeDashRequest()
        {
            EnsureInitialized();

            if (WantsToDash && (Time.time - dashInputTimestamp) <= inputBufferTime)
            {
                return true;
            }
            return false;
        }

        //冲刺时移除多余冲刺请求
        public void CleanWantsToDashRequest()
        {
            WantsToDash = false;
        }
        #endregion

        #region 攻击相关
        public bool CheckAndConsumeAttackRequest()
        {
            EnsureInitialized();

            if (WantsToAttack && (Time.time - attackInputTimestamp) <= inputBufferTime)
            {
                WantsToAttack = false;
                return true;
            }
            return false;
        }

        //攻击时移除多余攻击请求
        public void CleanWantsToAttackRequest()
        {
            WantsToAttack = false;
        }
        #endregion

        #region 重击相关
        public bool CheckAndConsumeHeavyAttackRequest()
        {
            EnsureInitialized();

            if (WantsToHAttack && (Time.time - hAttackInputTimestamp) <= inputBufferTime)
            {
                WantsToHAttack = false;
                return true;
            }
            return false;
        }

        //重击时移除多余重击请求
        public void CleanWantsToHeavyAttackRequest()
        {
            WantsToHAttack = false;
        }
        #endregion

        #region 御空攻击相关
        public bool CheckAndConsumeAirAttackRequest()
        {
            EnsureInitialized();

            if (WantsToAirAttack && (Time.time - airAttackInputTimestamp) <= inputBufferTime)
            {
                WantsToAirAttack = false;
                return true;
            }
            return false;
        }

        //御空攻击时移除多余御空攻击请求
        public void CleanWantsToAirAttackRequest()
        {
            WantsToAirAttack = false;
        }
        #endregion

        #region 爆发相关
        public bool CheckAndConsumeQBurstRequest()
        {
            EnsureInitialized();

            if (WantsToQBurst && (Time.time - qBurstInputTimestamp) <= inputBufferTime)
            {
                WantsToQBurst = false;
                return true;
            }
            return false;
        }

        //爆发时移除爆发请求
        public void CleanWantsToQBurstRequest()
        {
            WantsToQBurst = false;
        }
        #endregion

        #region 战技相关
        public bool CheckAndConsumeESkillRequest()
        {
            EnsureInitialized();

            if (WantsToESkill && (Time.time - eSkillInputTimestamp) <= inputBufferTime)
            {
                WantsToESkill = false;
                return true;
            }
            return false;
        }

        //战技时移除多余战技请求
        public void CleanWantsToESkilltRequest()
        {
            WantsToESkill = false;
        }
        #endregion

        #region 长按判定
        //每帧检测奔跑按键长按状态并更新标记（Update调用）
        private void CheckRunKeyLongPressState()
        {
            if (isRunKeyPressed && Time.time - runInputTimestamp >= longPressThreshold)
            {
                IsHoldingRun = true;
            }
        }

        //每帧检测重攻击按键长按状态并更新标记（Update调用）
        private void CheckHAttackKeyLongPressState()
        {
            // 1.只有攻击键处于按下状态，且本次按压还没触发过重击时才继续判断
            if (!isHAttackKeyPressed || hasTriggeredHeavyAttackThisPress)
            {
                return;
            }

            // 2.按住超过阈值时，立即触发重击请求
            if (Time.time - hAttackInputTimestamp >= longPressThreshold)
            {
                WantsToAttack = false;
                WantsToAirAttack = false;

                WantsToHAttack = true;
                hAttackInputTimestamp = Time.time;

                hasTriggeredHeavyAttackThisPress = true;
            }
        }
        #endregion
    }
}
