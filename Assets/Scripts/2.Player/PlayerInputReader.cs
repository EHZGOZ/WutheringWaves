using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WutheringWaves
{
    public class PlayerInputReader : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 输入系统引用 ===")]
        [Tooltip("玩家根节点上的PlayerInput组件")]
        [SerializeField] private PlayerInput playerInput;
        [Tooltip("当前受控角色的输入缓冲")]
        [SerializeField] private InputBuffer inputBuffer;
        #endregion

        #region 运行时缓存
        private InputActionMap playerActionMap; // 玩家动作映射
        private bool isInitialized; // 是否已完成初始化
        #endregion

        #region 对外只读输入状态
        public Vector2 ZoomInput { get; private set; } // 镜头缩放输入
        public Vector2 LookInput { get; private set; } // 视角输入
        public Vector2 MoveInput { get; private set; } // 移动输入

        public bool RunInput { get; private set; } // 奔跑输入
        public bool JumpInput { get; private set; } // 跳跃输入
        public bool DodgeInput { get; private set; } // 闪避输入

        public bool AttackInput { get; private set; } // 普攻输入
        public bool LockInput { get; private set; } // 锁定输入
        public bool ESkillInput { get; private set; } // 共鸣技能输入
        public bool QBurstInput { get; private set; } // 共鸣解放输入
        public int LastSwitchCharacterSlot { get; private set; } // 最近一次切人请求槽位

        // 切人请求事件：参数为目标槽位（1/2/3）
        public event Action<int> OnSwitchCharacterRequested;
        #endregion

        #region 初始化
        // 绑定当前受控角色输入缓冲：保证玩家输入写入正确角色
        public void BindInputBuffer(InputBuffer buffer)
        {
            inputBuffer = buffer;
        }
        // 输入读取器初始化：获取输入组件，缓存动作映射，并重置输入状态
        public void Initialize()
        {
            if(isInitialized)
            {
                return;
            }

            //1.获取玩家输入组件
            ResolvePlayerInput();

            //2.缓存玩家动作映射
            ResolvePlayerActionMap();

            //3.重置所有输入状态
            ResetInputStates();

            //4.标记初始化完成
            isInitialized = true;

            //5.启用玩家输入
            EnablePlayerInput();
        }


        //1.获取玩家输入组件
        private void ResolvePlayerInput()
        {
            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }
        }

        //2.获取玩家动作映射
        private void ResolvePlayerActionMap()
        {
            if (playerInput != null)
            {
                playerActionMap = playerInput.actions.FindActionMap("Player");
            }
        }

        //3.重置输入状态：防止切角色或初始化时残留旧输入
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
            ESkillInput = false;
            QBurstInput = false;
            LastSwitchCharacterSlot = 0;
        }
        #endregion

        #region 输入回调
        // 缩放输入回调
        public void OnZoom(InputValue value)
        {
            ZoomInput = value.Get<Vector2>();
        }

        // 移动输入回调
        public void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();
            //Debug.Log($"[PlayerInputReader] MoveInput: {MoveInput}");
        }

        // 视角输入回调
        public void OnLook(InputValue value)
        {
            LookInput = value.Get<Vector2>();
            //Debug.Log($"[PlayerInputReader] LookInput: {LookInput}");
        }

        // 跳跃输入回调：按下时写入跳跃缓冲
        public void OnJump(InputValue value)
        {
            JumpInput = value.isPressed;
            if (value.isPressed)
            {
                inputBuffer?.BufferJump();
            }
        }

        // 奔跑输入回调：同步状态并写入奔跑缓冲
        public void OnRun(InputValue value)
        {
            RunInput = value.isPressed;
            inputBuffer?.BufferRun(value.isPressed);
        }

        // 锁定输入回调
        public void OnLock(InputValue value)
        {
            LockInput = value.isPressed;
        }

        // 闪避输入回调
        public void OnDodge(InputValue value)
        {
            DodgeInput = value.isPressed;
        }

        // 普攻输入回调：同步状态并写入攻击缓冲
        public void OnAttack(InputValue value)
        {

            AttackInput = value.isPressed;
            inputBuffer?.BufferAttack(value.isPressed);
        }

        // 共鸣技能输入回调：按下时写入技能缓冲
        public void OnResonanceESkillInput(InputValue value)
        {

            ESkillInput = value.isPressed;
            if (value.isPressed)
            {
                inputBuffer?.BufferESkill();
            }
        }

        // 共鸣解放输入回调：按下时写入大招缓冲
        public void OnResonanceQBurstInput(InputValue value)
        {

            QBurstInput = value.isPressed;
            if (value.isPressed)
            {
                inputBuffer?.BufferQBurst();
            }
        }

        // 1号位切人输入回调
        public void OnKey1(InputValue value)
        {

            HandleSwitchCharacterInput(1, value);
        }

        // 2号位切人输入回调
        public void OnKey2(InputValue value)
        {

            HandleSwitchCharacterInput(2, value);
        }

        // 3号位切人输入回调
        public void OnKey3(InputValue value)
        {

            HandleSwitchCharacterInput(3, value);
        }
        #endregion

        #region 输入开关
        // 禁用玩家输入：用于切角色、过场或UI接管输入
        public void DisablePlayerInput()
        {

            if (playerInput == null)
            {
                return;
            }

            // 1.先禁用动作映射
            playerActionMap?.Disable();

            // 2.停用 PlayerInput，避免继续接收玩家输入消息
            playerInput.DeactivateInput();

            // 3.清空输入状态，避免恢复时残留旧输入
            ResetInputStates();
        }


        // 启用玩家输入：恢复玩家动作映射
        public void EnablePlayerInput()
        {

            if (playerInput == null)
            {
                Debug.LogError("[PlayerInputReader] 启用玩家输入失败：PlayerInput为空。", this);
                return;
            }

            // 1.激活 PlayerInput，确保输入系统开始向当前对象发送输入消息
            playerInput.ActivateInput();

            // 2.明确切换到 Player 动作映射，避免当前停留在 UI 或其他 ActionMap
            playerInput.SwitchCurrentActionMap("Player");

            // 3.重新缓存当前动作映射
            playerActionMap = playerInput.currentActionMap;

            // 4.兜底启用动作映射
            playerActionMap?.Enable();
        }
        #endregion

        #region 内部工具
        // 统一处理切人按键：仅在按下瞬间抛出切人请求，避免长按重复触发
        private void HandleSwitchCharacterInput(int targetSlot, InputValue value)
        {
            if (!value.isPressed)
            {
                return;
            }

            // 记录最近一次切人目标，方便外部轮询或调试查看
            LastSwitchCharacterSlot = targetSlot;
            OnSwitchCharacterRequested?.Invoke(targetSlot);
        }
        #endregion
    }
}
