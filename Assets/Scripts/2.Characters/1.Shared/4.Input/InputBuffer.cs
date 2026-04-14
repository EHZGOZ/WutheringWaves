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
        [SerializeField] private float longPressThreshold = 0.1f; // 核心：0.1秒内松开是点按，超过才是长按

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
            CheckHAttackKeyLongPressState(); //每帧检测重攻击按键长按状态并更新标记
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
                dashInputTimestamp = Time.time;
                runInputTimestamp = Time.time;

                WantsToDash = true;
                IsHoldingRun = false; // 按下瞬间不设置IsHoldingRun，等超过阈值再设置
            }
            else
            {
                IsHoldingRun = false;
                isRunKeyPressed = false;
                runInputTimestamp = -Mathf.Infinity;
            }
        }

        // 写入攻击/御空攻击/重攻击请求
        public void BufferAttack(bool isPressed)
        {
            EnsureInitialized();
            isHAttackKeyPressed = isPressed;

            if (isPressed)
            {
                attackInputTimestamp = Time.time;
                airAttackInputTimestamp = Time.time;
                hAttackInputTimestamp = Time.time;

                WantsToAttack = true;
                WantsToAirAttack = true;
                WantsToHAttack = false;
            }
            else
            {
                WantsToHAttack = false;
                isHAttackKeyPressed = false;
                hAttackInputTimestamp = -Mathf.Infinity;
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
            if (isHAttackKeyPressed && Time.time - hAttackInputTimestamp >= longPressThreshold)
            {
                WantsToHAttack = true;
            }
        }
        #endregion
    }
}
