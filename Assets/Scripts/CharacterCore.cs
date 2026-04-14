using UnityEngine;

namespace WutheringWaves
{
    // 单个角色的旧总控脚本：当前阶段仅保留旧链路兼容外壳，公开能力优先转发到 Facade / Context
    public class CharacterCore : MonoBehaviour
    {
        #region 兼容字段
        [Header("=== 核心数据配置 ===")]
        [Tooltip("角色动画控制器")]
        [SerializeField] internal Animator animator;
        [Tooltip("角色专属数值数据")]
        [SerializeField] internal CharacterData characterData;

        [Header("=== 辅助脚本配置（优先自动获取，可手动补全） ===")]
        [Tooltip("根运动处理器")]
        [SerializeField] internal CharacterRootMotion rootMotion;
        [Tooltip("效果表现逻辑")]
        [SerializeField] internal CharacterManifestation manifestationLogic;

        [Header("=== 功能脚本配置（优先自动获取，可手动补全） ===")]
        [Tooltip("输入读取器")]
        [SerializeField] internal PlayerInputReader playerInputReader;
        [Tooltip("输入缓冲器")]
        [SerializeField] internal InputBuffer inputBuffer;
        [Tooltip("角色第三人称相机")]
        [SerializeField] internal PlayerCamera playerCamera;
        [Tooltip("角色状态机")]
        [SerializeField] internal CharacterStateMachine stateMachine;
        [Tooltip("移动逻辑")]
        [SerializeField] internal CharacterMovement movementLogic;
        [Tooltip("体力逻辑")]
        [SerializeField] internal CharacterStamina staminaLogic;
        [Tooltip("战斗逻辑")]
        [SerializeField] internal CharacterAttack attackLogic;

        [Header("=== 新架构桥接（可自动获取） ===")]
        [SerializeField] private CharacterFacade facade;
        [SerializeField] private CharacterContext context;
        [SerializeField] private bool initializeOnAwake = true; // 是否在Awake阶段自动初始化兼容壳

        internal CharacterController characterController;
        #endregion

        #region 对外只读属性
        public bool IsInitialized { get; private set; }
        public CharacterFacade Facade => facade;
        public CharacterContext Context => context;
        #endregion

        #region 初始化
        private void Awake()
        {
            //1.按配置决定是否自动初始化
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        // 旧核心统一初始化入口：当前只负责兼容旧逻辑启动，并同步新架构上下文
        public void Initialize(CharacterFacade ownerFacade = null, CharacterContext injectedContext = null)
        {
            //1.同步外部传入的桥接入口
            if (ownerFacade != null)
            {
                facade = ownerFacade;
            }

            if (injectedContext != null)
            {
                context = injectedContext;
            }

            //2.自动补齐兼容字段
            AutoGetComponents();

            //3.同步 Context：保证旧核心始终作为兼容外壳挂接到新上下文
            SyncContext();

            //4.重复初始化时只刷新桥接，不再重复启动旧链路
            if (IsInitialized)
            {
                return;
            }

            //5.校验关键组件
            ValidateComponents();

            //6.启动旧链路兼容逻辑
            InitializeLegacyCompatibility();

            //7.再次同步上下文，确保旧逻辑初始化后的共享引用被 Context 看到
            SyncContext();

            //8.标记初始化完成
            IsInitialized = true;
        }

        // 自动获取旧链路兼容字段：优先同步 Context，其次回退到旧版获取方式
        private void AutoGetComponents()
        {
            if (facade == null)
            {
                facade = GetComponent<CharacterFacade>();
            }

            if (context == null)
            {
                context = GetComponent<CharacterContext>();
            }

            // 优先从 Context 同步共享依赖，降低对旧核心自身字段的所有权
            SyncRuntimeReferencesFromContext();

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (rootMotion == null) rootMotion = GetComponentInChildren<CharacterRootMotion>();
            if (stateMachine == null) stateMachine = GetComponent<CharacterStateMachine>();
            if (playerInputReader == null) playerInputReader = GetComponentInParent<PlayerInputReader>();
            if (inputBuffer == null) inputBuffer = GetComponent<InputBuffer>();
            if (attackLogic == null) attackLogic = GetComponent<CharacterAttack>();
            if (movementLogic == null) movementLogic = GetComponent<CharacterMovement>();
            if (staminaLogic == null) staminaLogic = GetComponent<CharacterStamina>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = GetComponent<PlayerCamera>();
            if (manifestationLogic == null) manifestationLogic = GetComponent<CharacterManifestation>();
        }

        // 从 CharacterContext 同步共享运行时依赖：旧壳字段只做兼容缓存，不再作为唯一数据源
        private void SyncRuntimeReferencesFromContext()
        {
            if (context == null)
            {
                return;
            }

            if (animator == null) animator = context.Animator;
            if (characterData == null) characterData = context.CharacterData;
            if (rootMotion == null) rootMotion = context.RootMotion;
            if (manifestationLogic == null) manifestationLogic = context.Manifestation;
            if (playerInputReader == null) playerInputReader = context.PlayerInputReader;
            if (inputBuffer == null) inputBuffer = context.InputBuffer;
            if (playerCamera == null) playerCamera = context.PlayerCamera;
            if (stateMachine == null) stateMachine = context.StateMachine;
            if (movementLogic == null) movementLogic = context.MovementLogic;
            if (staminaLogic == null) staminaLogic = context.StaminaLogic;
            if (attackLogic == null) attackLogic = context.AttackLogic;
            if (characterController == null) characterController = context.CharacterController;
        }

        // 校验关键组件，缺失则打印错误提示
        private void ValidateComponents()
        {
            if (characterData == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未绑定 CharacterData！", this);
            if (animator == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 Animator 组件！", this);
            if (stateMachine == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 CharacterStateMachine 组件！", this);
            if (playerInputReader == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 PlayerInputReader 组件！", this);
            if (inputBuffer == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 InputBuffer 组件！", this);
            if (characterController == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 CharacterController 组件！请在角色根物体挂载该Unity内置组件", this);
            if (playerCamera == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 PlayerCamera 组件！", this);
            if (staminaLogic == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 CharacterStamina 组件！", this);
            if (rootMotion == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 CharacterRootMotion 组件！", this);
            if (manifestationLogic == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 CharacterManifestation 组件！", this);
        }

        // 启动旧链路兼容逻辑：当前仍为状态机 / 旧移动 / 旧战斗提供最低限度启动支持
        private void InitializeLegacyCompatibility()
        {
            characterData?.Initialize();
            staminaLogic?.Initialize(this);
            rootMotion?.Initialize(this);
            manifestationLogic?.Initialize(this);
            inputBuffer?.Initialize();
            stateMachine?.Initialize(this, characterData, animator, characterController, movementLogic, playerInputReader, inputBuffer, attackLogic, manifestationLogic);
            movementLogic?.Initialize(this);
            attackLogic?.Initialize(this);
        }

        // 同步 Context：让新架构统一读取旧兼容壳上的共享依赖
        private void SyncContext()
        {
            if (context == null)
            {
                return;
            }

            context.SetLegacyCore(this);
            context.SetFacade(facade);
            context.Refresh();
            SyncRuntimeReferencesFromContext();
        }
        #endregion

        #region 兼容接口转发
        // 兼容旧版链路：优先转发到 Context 的角色数据
        public CharacterData GetCharacterData()
        {
            CharacterData data = context != null ? context.CharacterData : characterData;
            if (data == null)
            {
                Debug.LogError($"【{gameObject.name}】CharacterCore 未绑定 CharacterData，无法获取 currentHealth！", this);
                return null;
            }

            return data;
        }

        // 兼容旧版链路：优先转发到 Context 的体力逻辑
        public CharacterStamina GetCharacterStamina()
        {
            return context != null ? context.StaminaLogic : staminaLogic;
        }

        // 奔跑 / 闪避等逻辑会通过这里统一消耗体力
        public bool StaminaCost(float staminaCost)
        {
            CharacterStamina stamina = context != null ? context.StaminaLogic : staminaLogic;
            return stamina != null && stamina.TryConsume(staminaCost);
        }

        // 禁用玩家输入（UI打开时调用）
        public void DisablePlayerInput()
        {
            if (facade != null)
            {
                facade.DisablePlayerInput();
                return;
            }

            playerInputReader?.DisablePlayerInput();
        }

        // 恢复玩家输入（UI关闭时调用）
        public void EnablePlayerInput()
        {
            if (facade != null)
            {
                facade.EnablePlayerInput();
                return;
            }

            playerInputReader?.EnablePlayerInput();
        }
        #endregion
    }
}
