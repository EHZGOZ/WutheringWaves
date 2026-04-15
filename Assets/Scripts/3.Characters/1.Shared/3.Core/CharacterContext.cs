using UnityEngine;

namespace WutheringWaves
{
    public class CharacterContext : MonoBehaviour
    {
        //1. 核心入口引用
        [Header("=== 核心入口引用（优先自动获取，可手动补全） ===")]
        [Tooltip("角色门面入口：负责统一初始化和对外交互")]
        [SerializeField] private CharacterFacade facade; // 角色门面入口

        // 2. 共享运行时依赖
        [Header("=== CharacterFacade注入 ===")]
        [Header("玩家控制器")]
        [SerializeField] private PlayerController playerController; // 玩家控制器
        [Header("玩家输入读取器")]
        [SerializeField] private PlayerInputReader playerInputReader; // 玩家输入读取器
        [Header("玩家共享体力组件")]
        [SerializeField] private PlayerStamina playerStamina; // 玩家共享体力组件
        [Header("角色基础数据")]
        [SerializeField] private CharacterDataSO characterDataSO; // 角色基础数据

        [Header("=== 基础组件===")]
        [Header("动画控制器")]
        [SerializeField] private Animator animator; // 角色动画控制器
        [Header("角色控制器")]
        [SerializeField] private CharacterController characterController; // 角色控制器

        
        [Header("===角色运行时数据===")]
        [SerializeField] private CharacterRuntimeData runtimeData = new CharacterRuntimeData(); // 角色运行时数据

        [Header("角色输入缓冲器")]
        [SerializeField] private InputBuffer inputBuffer; // 角色输入缓冲器
        [Header("角色状态机")]
        [SerializeField] private CharacterStateMachine stateMachine; // 角色状态机
        [Header("角色根运动逻辑")]
        [SerializeField] private CharacterRootMotion rootMotion; // 角色根运动逻辑
        [Header("角色表现逻辑")]
        [SerializeField] private CharacterManifestation manifestation; //角色表现逻辑

        [Header("=== 旧架构逻辑引用 ===")]
        [Header("旧移动逻辑")]
        [SerializeField] private CharacterMovement movementLogic; // 旧移动逻辑
        [Header("旧战斗逻辑")]
        [SerializeField] private CharacterAttack attackLogic; // 旧战斗逻辑
        
        

        #region 对外只读属性
        public CharacterFacade Facade => facade;
        public PlayerInputReader PlayerInputReader => playerInputReader;
        public PlayerStamina PlayerStamina => playerStamina;
        public Animator Animator => animator;
        public CharacterController CharacterController => characterController;

        public CharacterDataSO CharacterDataSO => characterDataSO;
        public CharacterRuntimeData CharacterRuntimeData => runtimeData;

        public InputBuffer InputBuffer => inputBuffer;
        public CharacterStateMachine StateMachine => stateMachine;
        public CharacterRootMotion RootMotion => rootMotion;
        public CharacterManifestation Manifestation => manifestation;

        public CharacterMovement MovementLogic => movementLogic;
        public CharacterAttack AttackLogic => attackLogic;
       
        public bool IsInitialized { get; private set; }
        #endregion

        #region 1. 初始化逻辑
        // 设置玩家
        public void SetPlayerControllerReader(PlayerController playerController)
        {
            if (playerController != null)
            {
                this.playerController = playerController;
            }
        }
        // 设置玩家输入
        public void SetPlayerInputReader(PlayerInputReader playerInputReader)
        {
            if (playerInputReader != null)
            {
                this.playerInputReader = playerInputReader;
            }
        }
        // 设置玩家共享体力：由Facade把Player层依赖显式注入到角色上下文
        public void SetPlayerStamina(PlayerStamina playerStamina)
        {
            if (playerStamina != null)
            {
                this.playerStamina = playerStamina;
            }
        }
        // 设置角色基础数据
        public void SetCharacterDataSO(CharacterDataSO characterDataSO)
        {
            if (characterDataSO != null)
            {
                this.characterDataSO = characterDataSO;
            }
        }
        // Context初始化总入口：统一解析当前角色身上的共享依赖
        public void Initialize(CharacterFacade ownerFacade = null)
        {
            //1.同步外部传入的角色门面
            if (ownerFacade != null)
            {
                facade = ownerFacade;
            }
            //2.解析所有共享依赖
            ResolveReferences();
        }

        #endregion

        #region 2. 依赖解析
        // 统一解析所有共享依赖：保证角色运行时只从Context对外提供引用
        private void ResolveReferences()
        {
            //1.解析角色门面
            ResolveFacade();
            //2.先解析玩家控制器
            ResolvePlayerController();
            //3.解析输入读取器
            ResolvePlayerInputReader();
            //4.解析玩家共享体力组件
            ResolvePlayerStamina();
            //5.解析角色基础数据
            ResolveCharacterDataSO();

            //6.解析动画器
            ResolveAnimator();
            //7.解析角色控制器
            ResolveCharacterController();

            //8.解析角色运行时数据
            ResolveRuntimeData();

            //9.解析输入缓冲器
            ResolveInputBuffer();
            //10.解析状态机
            ResolveStateMachine();
            //11.解析根运动
            ResolveRootMotion();
            //12.解析外观
            Resolvemanifestation();

            //13.解析旧架构逻辑
            ResolveLegacyLogics();

        }

        //1.解析角色门面
        private void ResolveFacade()
        {
            if (facade == null)
            {
                facade = GetComponent<CharacterFacade>();
            }
        }

        //2.解析玩家控制器 Player层
        private void ResolvePlayerController()
        {
            if (playerController == null)
            {
                Debug.Log("CharacterContext已经从父节点获取PlayerController");
                playerController = GetComponentInParent<PlayerController>();
            }
        }

        //3.解析输入读取器
        private void ResolvePlayerInputReader()
        {
            // 兜底从父节点查找，兼容输入读取器挂在Player根节点上playerInputReader
            if (playerInputReader == null)
            {
                Debug.Log("CharacterContext已经从父节点获取PlayerInputReader");
                playerInputReader = GetComponentInParent<PlayerInputReader>();
            }
        }

        //4.解析玩家共享体力组件
        private void ResolvePlayerStamina()
        {
            //兼容从父节点直接查找 PlayerStamina 的结构
            if (playerStamina == null)
            {
                Debug.Log("CharacterContext已经从父节点获取PlayerStamina");
                playerStamina = GetComponentInParent<PlayerStamina>();
            }
        }

        //5.解析角色基础数据
        private void ResolveCharacterDataSO()
        {
            if (characterDataSO == null)
            {
                Debug.Log("CharacterContext暂无characterDataSO");
            }
        }

        //6.解析动画器
        private void ResolveAnimator()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        //7.解析角色控制器
        private void ResolveCharacterController()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        //8.解析角色运行时数据
        private void ResolveRuntimeData()
        {
            // 先确保运行时数据对象存在，避免后续初始化时空引用
            if (runtimeData == null)
            {
                runtimeData = new CharacterRuntimeData();
            }

            // 首次初始化时，从角色静态模板中同步当前生命值
            if (!runtimeData.IsInitialized)
            {
                runtimeData.Initialize(characterDataSO);
            }
        }

        //9.解析输入缓冲器
        private void ResolveInputBuffer()
        {
            // 兜底从当前角色对象上查找
            if (inputBuffer == null)
            {
                inputBuffer = GetComponent<InputBuffer>();
            }
        }

        //10.解析状态机
        private void ResolveStateMachine()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<CharacterStateMachine>();
            }
        }

        //11.解析根运动
        private void ResolveRootMotion()
        {
            if (rootMotion == null)
            {
                rootMotion = GetComponentInChildren<CharacterRootMotion>();
            }
        }

        //12.解析外观
        private void Resolvemanifestation()
        {

            if (manifestation == null)
            {
                manifestation = GetComponent<CharacterManifestation>();
            }
        }
        //13.解析旧架构逻辑
        private void ResolveLegacyLogics()
        {
            if (movementLogic == null)
            {
                movementLogic = GetComponent<CharacterMovement>();
            }

            if (attackLogic == null)
            {
                attackLogic = GetComponent<CharacterAttack>();
            }
        }


        #endregion


    }
}
