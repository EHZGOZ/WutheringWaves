using UnityEngine;

namespace WutheringWaves
{
    public class CharacterFacade : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 核心组件（优先自动获取，可手动补全） ===")]
        [Tooltip("角色共享上下文：集中管理角色运行时共享依赖")]
        [SerializeField] private CharacterContext context;
        [Tooltip("旧架构角色核心：迁移过渡期间继续承载旧版业务逻辑")]
        [SerializeField] private CharacterCore legacyCore;
        [Tooltip("玩家输入读取器：位于Player层，负责采集玩家原始输入")]
        [SerializeField] private PlayerInputReader playerInputReader;
        [Tooltip("输入缓冲器：负责缓存角色动作请求")]
        [SerializeField] private InputBuffer inputBuffer;

        [Header("=== 新架构控制器（可选） ===")]
        [Tooltip("移动域控制器：后续逐步接管旧移动逻辑")]
        [SerializeField] private CharacterMovementController movementController;
        [Tooltip("战斗域控制器：后续逐步接管旧战斗逻辑")]
        [SerializeField] private CharacterAttackController attackController;
        [Tooltip("体力域控制器：后续逐步接管旧体力逻辑")]
        [SerializeField] private CharacterStaminaController staminaController;
        [Tooltip("根运动驱动：后续逐步接管旧根运动逻辑")]
        [SerializeField] private RootMotionDriver rootMotionDriver;

        [Header("=== 启动选项 ===")]
        [SerializeField] private bool initializeOnAwake = true; // 是否在Awake阶段自动初始化
        [SerializeField] private bool disablePlayerInputOnStart = false; // 初始化完成后是否默认禁用玩家输入
        #endregion

        #region 对外只读属性
        public CharacterContext Context => context; // 角色共享上下文
        public CharacterCore LegacyCore => legacyCore; // 旧版角色核心
        public PlayerInputReader PlayerInputReader => playerInputReader; // 玩家输入读取器
        public InputBuffer InputBuffer => inputBuffer; // 输入缓冲器
        public bool IsInitialized { get; private set; } // 是否完成初始化
        #endregion

        #region 生命周期
        private void Awake()
        {
            //1.按配置决定是否自动初始化
            if (initializeOnAwake)
            {
                Initialize();
            }
        }
        #endregion

        #region 初始化
        // 角色门面初始化总入口：统一接管上下文、输入、旧核心与新控制器的启动顺序
        public void Initialize()
        {
            //1.防止重复初始化
            if (IsInitialized)
            {
                return;
            }

            //2.自动补齐核心组件引用
            AutoGetComponents();

            //3.初始化角色共享上下文
            InitializeContext();

            //4.初始化输入链路
            InitializeInputStack();

            //5.初始化旧核心逻辑
            InitializeLegacyCore();

            //6.初始化新架构控制器
            InitializeControllers();

            //7.按配置决定是否默认禁用玩家输入
            if (disablePlayerInputOnStart)
            {
                DisablePlayerInput();
            }

            //8.标记初始化完成
            IsInitialized = true;
        }
        #endregion

        #region 对外能力
        // 对外提供统一的玩家输入启用入口
        public void EnablePlayerInput()
        {
            EnsureInitialized();
            context?.PlayerInputReader?.EnablePlayerInput();
        }

        // 对外提供统一的玩家输入禁用入口
        public void DisablePlayerInput()
        {
            EnsureInitialized();
            context?.PlayerInputReader?.DisablePlayerInput();
        }

        // 兼容旧版链路：对外暴露角色数据读取入口
        public CharacterData GetCharacterData()
        {
            return context != null ? context.CharacterData : null;
        }

        // 兼容旧版链路：对外暴露角色体力逻辑读取入口
        public CharacterStamina GetCharacterStamina()
        {
            return context != null ? context.StaminaLogic : null;
        }
        #endregion

        #region 初始化子步骤
        // 自动补齐角色根节点上的核心组件引用
        private void AutoGetComponents()
        {
            if (context == null)
            {
                context = GetComponent<CharacterContext>();
            }

            if (legacyCore == null)
            {
                legacyCore = GetComponent<CharacterCore>();
            }

            if (playerInputReader == null)
            {
                playerInputReader = GetComponentInParent<PlayerInputReader>();
            }

            if (inputBuffer == null)
            {
                inputBuffer = GetComponent<InputBuffer>();
            }

            if (movementController == null)
            {
                movementController = GetComponent<CharacterMovementController>();
            }

            if (attackController == null)
            {
                attackController = GetComponent<CharacterAttackController>();
            }

            if (staminaController == null)
            {
                staminaController = GetComponent<CharacterStaminaController>();
            }

            if (rootMotionDriver == null)
            {
                rootMotionDriver = GetComponentInChildren<RootMotionDriver>();
            }
        }

        // 先初始化共享上下文：保证后续所有模块有统一依赖来源
        private void InitializeContext()
        {
            context?.SetFacade(this);
            context?.SetLegacyCore(legacyCore);
            context?.Initialize(this);
        }

        // 初始化输入链路底层：先准备输入缓冲，供后续角色逻辑读取
        private void InitializeInputStack()
        {
            inputBuffer?.Initialize();
        }

        // 初始化旧核心：当前阶段仍由它承载状态机与旧版业务链路
        private void InitializeLegacyCore()
        {
            legacyCore?.Initialize(this, context);
            context?.Refresh();
        }

        // 初始化新架构控制器：先建立与Context的桥接关系，后续再逐步迁移业务
        private void InitializeControllers()
        {
            movementController?.Initialize(context);
            attackController?.Initialize(context);
            staminaController?.Initialize(context);
            rootMotionDriver?.Initialize(context);
        }

        // 确保对外调用时角色门面已经完成初始化
        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                Initialize();
            }
        }
        #endregion
    }
}
