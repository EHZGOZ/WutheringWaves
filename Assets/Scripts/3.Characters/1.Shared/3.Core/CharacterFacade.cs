using UnityEngine;

namespace WutheringWaves
{
    public class CharacterFacade : MonoBehaviour
    {
        #region 核心引用    
        [Header("=== 外层核心组件（优先自动获取，可手动补全） ===")]
        [Header("玩家控制器：位于 Player 层")]
        [SerializeField] private PlayerController playerController; // 玩家控制器
        [Header("玩家输入读取器：位于 Player 层")]
        [SerializeField] private PlayerInputReader playerInputReader;
        [Header("玩家共享体力：位于 Player 层")]
        [SerializeField] private PlayerStamina playerStamina;
        [Header("角色基础数据：手动填入")]
        [SerializeField] private CharacterDataSO characterDataSO;

        [Header("=== 核心组件（优先自动获取，可手动补全） ===")]
        [Header("角色共享上下文：集中管理角色运行时共享依赖")]
        [SerializeField] private CharacterContext context;


        #endregion

        #region 对外只读属性
        public PlayerController PlayerController => playerController; // 玩家输入读取器
        public PlayerInputReader PlayerInputReader => playerInputReader; // 玩家输入读取器
        public PlayerStamina PlayerStamina => playerStamina; // 玩家共享体力
        public CharacterDataSO CharacterDataSO => characterDataSO; // 玩家共享体力

        public CharacterContext Context => context; // 角色共享上下文
        
        public bool IsInitialized { get; private set; } // 是否完成初始化
        #endregion


        #region 初始化
        // 角色门面初始化总入口：统一接管上下文、输入、旧逻辑组件与新控制器的启动顺序
        public void Initialize()
        {
            // 1. 防止重复初始化
            if (IsInitialized)
            {
                return;
            }

            // 2. 自动补齐核心组件引用
            AutoGetComponents();

            // 3. 初始化角色共享上下文
            InitializeContext();

            // 4. 按 CharacterContext 中的依赖顺序，初始化角色运行模块
            InitializeCharacterRuntimeModules();

            //5 . 标记初始化完成
            IsInitialized = true;
        }
        //2.自动补齐角色根节点上的核心组件引用
        private void AutoGetComponents()
        {
            //玩家控制
            if (playerController == null)
            {
                playerController = GetComponentInParent<PlayerController>();
            }
            //玩家输入
            if (playerInputReader == null)
            {
                playerInputReader = GetComponentInParent<PlayerInputReader>();
            }
            //玩家体力
            if (playerStamina == null)
            {
                playerStamina = GetComponentInParent<PlayerStamina>();
            }
            //角色数据
            if (characterDataSO == null)
            {
                Debug.Log("CharacterFacade暂无characterDataSO");
            }

            //角色上下文
            if (context == null)
            {
                context = GetComponent<CharacterContext>();
            }
           
        }

        //3.先初始化共享上下文，保证后续所有模块有统一依赖来源
        private void InitializeContext()
        {
            context?.SetPlayerControllerReader(playerController);
            context?.SetPlayerInputReader(playerInputReader);
            context?.SetPlayerStamina(playerStamina);
            context?.SetCharacterDataSO(characterDataSO);
            context?.Initialize(this);
        }
        
        //4.按 CharacterContext 的依赖顺序初始化角色运行模块：
        private void InitializeCharacterRuntimeModules()
        {
            // 1.先初始化玩家共享体力，保证后续体力判定读取到的是当前角色配置
            context?.PlayerStamina?.Initialize(context);
            // 2.初始化角色输入缓冲器，保证状态机读取输入前缓冲器已可用
            context?.InputBuffer?.Initialize();
            // 3.初始化角色状态机，建立当前角色状态流转入口
            context?.StateMachine?.Initialize(context);
            // 4.初始化角色根运动逻辑，保证动画驱动位移链路已接好
            context?.RootMotion?.Initialize(context);
            // 5.初始化角色表现逻辑，保证外观表现订阅与材质缓存已准备好
            context?.Manifestation?.Initialize(context);
            // 6.初始化旧移动逻辑，保证旧链路阶段的移动判定可正常工作
            context?.MovementLogic?.Initialize(context);
            // 7.初始化旧战斗逻辑，保证旧链路阶段的攻击逻辑可正常工作
            context?.AttackLogic?.Initialize(context);
        }

        #endregion
    }
}
