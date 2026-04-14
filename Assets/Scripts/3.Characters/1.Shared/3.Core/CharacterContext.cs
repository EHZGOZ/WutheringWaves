using UnityEngine;

namespace WutheringWaves
{
    public class CharacterContext : MonoBehaviour
    {
        #region 核心入口引用
        [Header("=== 核心入口引用（优先自动获取，可手动补全） ===")]
        [Tooltip("角色门面入口：负责统一初始化和对外交互")]
        [SerializeField] private CharacterFacade facade;
        [Tooltip("旧角色总控：迁移过渡期间保留的兼容入口")]
        [SerializeField] private CharacterCore legacyCore;
        #endregion

        #region 共享运行时依赖
        [Header("=== 共享运行时依赖 ===")]
        [Tooltip("玩家输入读取器")]
        [SerializeField] private PlayerInputReader playerInputReader;
        [Tooltip("角色输入缓冲器")]
        [SerializeField] private InputBuffer inputBuffer;
        [Tooltip("动画控制器")]
        [SerializeField] private Animator animator;
        [Tooltip("角色控制器")]
        [SerializeField] private CharacterController characterController;
        [Tooltip("角色数据")]
        [SerializeField] private CharacterData characterData;
        [Tooltip("玩家相机逻辑")]
        [SerializeField] private PlayerCamera playerCamera;
        [Tooltip("角色状态机")]
        [SerializeField] private CharacterStateMachine stateMachine;
        [Tooltip("旧移动逻辑")]
        [SerializeField] private CharacterMovement movementLogic;
        [Tooltip("旧体力逻辑")]
        [SerializeField] private CharacterStamina staminaLogic;
        [Tooltip("旧战斗逻辑")]
        [SerializeField] private CharacterAttack attackLogic;
        [Tooltip("旧根运动逻辑")]
        [SerializeField] private CharacterRootMotion rootMotion;
        [Tooltip("旧表现逻辑")]
        [SerializeField] private CharacterManifestation manifestation;
        [Tooltip("新移动域控制器")]
        [SerializeField] private CharacterMovementController movementController;
        [Tooltip("新战斗域控制器")]
        [SerializeField] private CharacterAttackController attackController;
        [Tooltip("新体力域控制器")]
        [SerializeField] private CharacterStaminaController staminaController;
        [Tooltip("新根运动驱动")]
        [SerializeField] private RootMotionDriver rootMotionDriver;
        #endregion

        #region 对外只读属性
        public CharacterFacade Facade => facade;
        public CharacterCore LegacyCore => legacyCore;
        public PlayerInputReader PlayerInputReader => playerInputReader;
        public InputBuffer InputBuffer => inputBuffer;
        public Animator Animator => animator;
        public CharacterController CharacterController => characterController;
        public CharacterData CharacterData => characterData;
        public PlayerCamera PlayerCamera => playerCamera;
        public CharacterStateMachine StateMachine => stateMachine;
        public CharacterMovement MovementLogic => movementLogic;
        public CharacterStamina StaminaLogic => staminaLogic;
        public CharacterAttack AttackLogic => attackLogic;
        public CharacterRootMotion RootMotion => rootMotion;
        public CharacterManifestation Manifestation => manifestation;
        public CharacterMovementController MovementController => movementController;
        public CharacterAttackController AttackController => attackController;
        public CharacterStaminaController StaminaController => staminaController;
        public RootMotionDriver RootMotionDriver => rootMotionDriver;
        public bool IsInitialized { get; private set; }
        #endregion

        #region 初始化
        // 初始化角色上下文：统一解析当前角色身上的共享依赖
        public void Initialize(CharacterFacade ownerFacade = null)
        {
            //1.同步外部传入的角色门面
            if (ownerFacade != null)
            {
                facade = ownerFacade;
            }

            //2.解析共享依赖引用
            ResolveReferences();

            //3.刷新初始化状态
            UpdateInitializedState();
        }

        public void SetFacade(CharacterFacade ownerFacade)
        {
            if (ownerFacade != null)
            {
                facade = ownerFacade;
            }
        }

        public void SetLegacyCore(CharacterCore core)
        {
            if (core != null)
            {
                legacyCore = core;
            }
        }

        // 刷新上下文缓存：用于旧核心初始化完成后同步新引用
        public void Refresh()
        {
            ResolveReferences();
            UpdateInitializedState();
        }
        #endregion

        #region 依赖解析
        // 自动补齐当前角色根节点上的共享依赖
        private void ResolveReferences()
        {
            ResolveFacade();
            ResolveLegacyCore();
            ResolvePlayerInputReader();
            ResolveInputBuffer();
            ResolveAnimator();
            ResolveCharacterController();
            ResolveCharacterData();
            ResolvePlayerCamera();
            ResolveStateMachine();
            ResolveLegacyLogics();
            ResolveDomainControllers();
        }

        private void ResolveFacade()
        {
            if (facade == null)
            {
                facade = GetComponent<CharacterFacade>();
            }
        }

        private void ResolveLegacyCore()
        {
            if (legacyCore == null)
            {
                legacyCore = GetComponent<CharacterCore>();
            }
        }

        private void ResolvePlayerInputReader()
        {
            if (playerInputReader == null && facade != null && facade.PlayerInputReader != null)
            {
                playerInputReader = facade.PlayerInputReader;
            }

            if (playerInputReader == null)
            {
                playerInputReader = GetComponentInParent<PlayerInputReader>();
            }
        }

        private void ResolveInputBuffer()
        {
            if (inputBuffer == null && facade != null && facade.InputBuffer != null)
            {
                inputBuffer = facade.InputBuffer;
            }

            if (inputBuffer == null)
            {
                inputBuffer = GetComponent<InputBuffer>();
            }
        }

        private void ResolveAnimator()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void ResolveCharacterController()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        private void ResolveCharacterData()
        {
            // 角色数据优先使用显式配置，其次兼容旧核心上的数据引用
            if (characterData == null && legacyCore != null)
            {
                characterData = legacyCore.characterData;
            }
        }

        private void ResolvePlayerCamera()
        {
            if (playerCamera == null && facade != null && facade.Context != null && facade.Context != this)
            {
                playerCamera = facade.Context.PlayerCamera;
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponent<PlayerCamera>();
            }
        }

        private void ResolveStateMachine()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<CharacterStateMachine>();
            }
        }

        private void ResolveLegacyLogics()
        {
            if (movementLogic == null)
            {
                movementLogic = GetComponent<CharacterMovement>();
            }

            if (staminaLogic == null)
            {
                staminaLogic = GetComponent<CharacterStamina>();
            }

            if (attackLogic == null)
            {
                attackLogic = GetComponent<CharacterAttack>();
            }

            if (rootMotion == null)
            {
                rootMotion = GetComponentInChildren<CharacterRootMotion>();
            }

            if (manifestation == null)
            {
                manifestation = GetComponent<CharacterManifestation>();
            }
        }

        private void ResolveDomainControllers()
        {
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

        // 更新初始化状态：Context 的成立不再以 legacyCore 为前置条件
        private void UpdateInitializedState()
        {
            IsInitialized = playerInputReader != null
                && inputBuffer != null
                && animator != null
                && characterController != null;
        }
        #endregion
    }
}
