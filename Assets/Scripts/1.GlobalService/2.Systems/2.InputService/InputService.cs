using UnityEngine;

namespace WutheringWaves
{
    // 输入服务：负责统一管理UI输入状态、玩家输入状态和鼠标光标状态，不直接读取具体移动、攻击、技能输入
    public class InputService : MonoBehaviour
    {
        public static InputService Instance { get; private set; }

        [Header(" 当前玩家输入读取器")]
        [SerializeField] private PlayerInputReader playerInputReader;

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;

        #region 外部访问
        public bool IsInitialized { get; private set; }
        public bool IsUIInputEnabled { get; private set; }
        public bool IsPlayerInputEnabled { get; private set; }
        public bool IsTextInputActive { get; private set; }
        #endregion

        #region 生命周期
        private void Awake()
        {
            // 1.保持单例，避免多个InputService同时控制输入状态
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 2.缓存单例引用
            Instance = this;
        }

        private void OnDestroy()
        {
            // 1.如果销毁的是当前单例，清空单例引用
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region 初始化
        // 初始化输入服务
        public void Initialize()
        {
            // 1.已经初始化过时直接返回，避免重复初始化输入状态
            if (IsInitialized)
            {
                return;
            }

            // 2.启动时默认启用UI输入，保证登录、注册和菜单可以正常操作
            IsUIInputEnabled = true;

            // 3.启动时默认禁用玩家输入，避免菜单阶段角色误响应
            IsPlayerInputEnabled = false;

            // 4.启动时默认显示并解锁鼠标，符合登录和主菜单场景
            SetCursorVisible(true);

            // 5.标记初始化完成
            IsInitialized = true;

            if (verboseLog)
            {
                Debug.Log("[输入服务] 初始化完成。", this);
            }
        }
        #endregion

        #region 绑定玩家输入
        // 绑定玩家控制器：进入游戏生成玩家后，由游戏会话服务传入
        public void BindPlayer(PlayerController playerController)
        {
            // 1.空值检查
            if (playerController == null)
            {
                ClearPlayer();
                return;
            }

            // 2.缓存当前玩家输入读取器，后续统一控制启用和禁用
            playerInputReader = playerController.CurrentPlayerInputReader;

            // 3.按当前输入服务记录的状态，同步玩家输入开关
            ApplyPlayerInputState();

            if (verboseLog)
            {
                Debug.Log("[输入服务] 已绑定当前玩家输入读取器。", this);
            }
        }

        // 清理玩家输入绑定：退出游戏、登出账号或切换玩家对象时调用
        public void ClearPlayer()
        {
            // 1.清空前先禁用旧玩家输入，避免回到菜单后旧对象继续响应
            DisablePlayerInput();

            // 2.清空玩家输入读取器引用
            playerInputReader = null;

            if (verboseLog)
            {
                Debug.Log("[输入服务] 已清理当前玩家输入绑定。", this);
            }
        }
        #endregion

        #region 输入开关

        #region UI输入开关
        // 启用UI输入：登录、注册、菜单界面使用
        public void EnableUIInput()
        {
            // 1.当前版本先记录状态，后面接入UI InputActionMap时再统一切换
            IsUIInputEnabled = true;
        }

        // 禁用UI输入：进入玩法界面或过场时使用
        public void DisableUIInput()
        {
            // 1.当前版本先记录状态，后面接入UI InputActionMap时再统一切换
            IsUIInputEnabled = false;
        }
        #endregion

        #region 玩家输入开关
        // 启用玩家输入：进入玩法界面时调用
        public void EnablePlayerInput()
        {
            // 1.先记录目标状态，即使当前还没有绑定玩家，也能在绑定后自动同步
            IsPlayerInputEnabled = true;

            // 2.应用玩家输入状态
            ApplyPlayerInputState();
        }

        // 禁用玩家输入：进入菜单、暂停、登录界面时调用
        public void DisablePlayerInput()
        {
            // 1.先记录目标状态，即使当前还没有绑定玩家，也能在绑定后自动同步
            IsPlayerInputEnabled = false;

            // 2.应用玩家输入状态
            ApplyPlayerInputState();
        }

        // 按当前记录的状态应用玩家输入开关
        private void ApplyPlayerInputState()
        {
            // 1.输入读取器为空时，只保留状态，不执行具体开关
            if (playerInputReader == null)
            {
                return;
            }

            // 2.根据记录状态启用或禁用玩家输入
            if (IsPlayerInputEnabled)
            {
                playerInputReader.EnablePlayerInput();
            }
            else
            {
                playerInputReader.DisablePlayerInput();
            }
        }
        #endregion

        #region 文本输入开关
        // 进入文本输入模式：聊天框、改名、账号密码输入时调用
        public void BeginTextInput()
        {
            // 1.记录当前正在输入文本
            IsTextInputActive = true;

            // 2.禁用玩家输入，避免WASD、数字键、技能键误触发角色行为
            DisablePlayerInput();

            // 3.启用UI输入，保证输入框、按钮、确认取消仍然可用
            EnableUIInput();

            // 4.显示并解锁鼠标，方便点击输入框和按钮
            SetCursorVisible(true);
        }

        // 退出文本输入模式：输入框提交、取消、失焦时调用
        public void EndTextInput(bool restorePlayerInput)
        {
            // 1.记录当前不再输入文本
            IsTextInputActive = false;

            // 2.仍然保持UI输入可用
            EnableUIInput();

            // 3.根据调用方决定是否恢复玩家输入
            if (restorePlayerInput)
            {
                EnablePlayerInput();
                SetCursorVisible(false);
            }
            else
            {
                DisablePlayerInput();
                SetCursorVisible(true);
            }
        }
        #endregion

        #endregion

        #region 光标控制
        // 设置鼠标光标可见性和锁定状态
        public void SetCursorVisible(bool visible)
        {
            // 1.设置鼠标是否可见
            Cursor.visible = visible;

            // 2.可见时解锁光标，不可见时锁定光标
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        // 根据暂停状态同步光标显示：暂停时显示光标，恢复时隐藏光标
        public void SetCursorByPauseState(bool pause, bool cursorVisibleWhenPaused = true)
        {
            // 1.暂停时按配置显示光标
            if (pause)
            {
                SetCursorVisible(cursorVisibleWhenPaused);
                return;
            }

            // 2.恢复游戏时使用相反状态，默认隐藏并锁定光标
            SetCursorVisible(!cursorVisibleWhenPaused);
        }
        #endregion
    }
}