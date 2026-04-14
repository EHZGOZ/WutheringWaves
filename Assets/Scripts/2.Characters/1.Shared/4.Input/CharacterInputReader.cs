using UnityEngine;
using UnityEngine.InputSystem;

namespace WutheringWaves
{
    // 输入读取器：只负责采集原始输入，不负责业务判定和状态消费
    public class CharacterInputReader : MonoBehaviour
    {
        #region 输入缓存（供外部读取，只读）
        // 鼠标滚轮 视角大小
        public Vector2 ZoomInput { get; private set; }
        // 鼠标移动 视角
        public Vector2 LookInput { get; private set; }
        // WASD 移动
        public Vector2 MoveInput { get; private set; }

        // 是否按下奔跑键/闪避键（Shift）
        public bool RunInput { get; private set; }
        // 空格 跳跃
        public bool JumpInput { get; private set; }
        // 鼠标右键 闪避
        public bool DodgeInput { get; private set; }

        // 鼠标左键 攻击
        public bool AttackInput { get; private set; }
        // 鼠标滚轮按下 锁定
        public bool LockInput { get; private set; }
        // E 共鸣技能
        public bool ResonanceSkillInput { get; private set; }
        // Q 共鸣解放
        public bool ResonanceOutbreakInput { get; private set; }
        #endregion

        [Header("新输入系统配置")]
        [Tooltip("角色的PlayerInput组件")]
        [SerializeField] private PlayerInput playerInput;
        [Tooltip("输入缓冲器：用于记录动作请求")]
        [SerializeField] private InputBuffer inputBuffer;

        private InputActionMap playerActionMap; // 缓存角色专属Action Map（精准控制用）
        private bool _isInitialized;

        #region 初始化
        // 初始化输入状态
        public void Initialize()
        {
            // 1. 自动获取PlayerInput组件（无需手动拖入）
            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }

            // 2. 缓存角色Action Map（用于精准屏蔽角色输入，保留UI输入）
            if (playerInput != null)
            {
                playerActionMap = playerInput.actions.FindActionMap("Player");
            }

            if (inputBuffer == null)
            {
                inputBuffer = GetComponent<InputBuffer>();
            }

            ResetInputStates();
            _isInitialized = true;
        }

        // 确保外部先读后用时也能拿到可用状态
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        // 重置所有原始输入缓存
        public void ResetInputStates()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            ZoomInput = Vector2.zero;

            JumpInput = false;
            RunInput = false;
            LockInput = false;
            DodgeInput = false;
            AttackInput = false;
            ResonanceSkillInput = false;
            ResonanceOutbreakInput = false;
        }
        #endregion

        #region InputSystem 绑定方法
        // 视角缩放输入响应（鼠标滚轮）
        public void OnZoom(InputValue value)
        {
            EnsureInitialized();

            Vector2 newInput = value.Get<Vector2>();
            if (ZoomInput != newInput)
            {
                ZoomInput = newInput;
            }
        }

        // 移动输入响应（WASD/摇杆）
        public void OnMove(InputValue value)
        {
            EnsureInitialized();

            Vector2 newInput = value.Get<Vector2>();
            if (MoveInput != newInput)
            {
                MoveInput = newInput;
            }
        }

        // 视角输入响应（鼠标移动）
        public void OnLook(InputValue value)
        {
            EnsureInitialized();

            Vector2 newInput = value.Get<Vector2>();
            if (LookInput != newInput)
            {
                LookInput = newInput;
            }
        }

        // 跳跃输入响应（空格键）
        public void OnJump(InputValue value)
        {
            EnsureInitialized();
            JumpInput = value.isPressed;
            if (value.isPressed)
            {
                inputBuffer?.BufferJump();
            }
        }

        // 奔跑输入响应（Shift键）
        public void OnRun(InputValue value)
        {
            EnsureInitialized();
            RunInput = value.isPressed;
            inputBuffer?.BufferRun(value.isPressed);
        }

        // 锁定输入响应（鼠标滚轮按下/特定按键）
        public void OnLock(InputValue value)
        {
            EnsureInitialized();
            LockInput = value.isPressed;
        }

        // 闪避输入响应（鼠标右键）
        public void OnDodge(InputValue value)
        {
            EnsureInitialized();
            DodgeInput = value.isPressed;
        }

        // 攻击输入响应（鼠标左键 Fire1）
        public void OnAttack(InputValue value)
        {
            EnsureInitialized();
            AttackInput = value.isPressed;
            inputBuffer?.BufferAttack(value.isPressed);
        }

        // 共鸣技能输入响应（E键）
        public void OnResonanceSkill(InputValue value)
        {
            EnsureInitialized();
            ResonanceSkillInput = value.isPressed;
            if (value.isPressed)
            {
                inputBuffer?.BufferESkill();
            }
        }

        // 共鸣解放输入响应（Q键）
        public void OnResonanceOutbreak(InputValue value)
        {
            EnsureInitialized();
            ResonanceOutbreakInput = value.isPressed;
            if (value.isPressed)
            {
                inputBuffer?.BufferQBurst();
            }
        }
        #endregion

        #region 对外提供输入控制方法
        /// <summary>
        /// 禁用角色输入（UI打开时调用）
        /// </summary>
        public void DisablePlayerInput()
        {
            EnsureInitialized();
            playerActionMap?.Disable();
        }

        /// <summary>
        /// 恢复角色输入（UI关闭时调用）
        /// </summary>
        public void EnablePlayerInput()
        {
            EnsureInitialized();
            playerActionMap?.Enable();
        }
        #endregion
    }
}
