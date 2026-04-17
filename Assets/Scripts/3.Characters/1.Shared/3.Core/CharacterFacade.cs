using System.Linq;
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
        [Header("角色Look Root：手动填入")]
        [SerializeField] private Transform cameraTarget;

        [Header("=== 核心组件（优先自动获取，可手动补全） ===")]
        [Header("角色共享上下文：集中管理角色运行时共享依赖")]
        [SerializeField] private CharacterContext context;

        [Header("=== 角色独属（自动获取，可手动补全） ===")]
        [SerializeField] private JinxiFeatureRoot jinxiFeatureRoot;
        [SerializeField] private KatixiyaFeatureRoot katixiyaFeatureRoot;
        #endregion

        #region 对外只读属性
        public PlayerController PlayerController => playerController; // 玩家输入读取器
        public PlayerInputReader PlayerInputReader => playerInputReader; // 玩家输入读取器
        public PlayerStamina PlayerStamina => playerStamina; // 玩家共享体力
        public CharacterDataSO CharacterDataSO => characterDataSO; // 玩家共享体力
        public CharacterContext Context => context; // 角色共享上下文
        public Transform CameraTarget => cameraTarget != null ? cameraTarget : transform; // 当前角色相机观察点
        public bool IsPlayerControlled { get; private set; } // 当前角色是否由玩家直接控制
        public bool IsInitialized { get; private set; } // 是否完成初始化
        #endregion

        #region 初始化
        // 角色门面初始化总入口：统一接管上下文、输入、共享模块与角色专属模块的启动顺序
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

            // 4. 初始化角色专属模块，并把专属驱动能力注入共享状态机
            InitializeCharacterExclusiveModules();

            // 5. 按 CharacterContext 的依赖顺序，初始化角色运行模块
            InitializeCharacterRuntimeModules();

            // 6. 标记初始化完成
            IsInitialized = true;
        }

        // 自动补齐角色根节点上的核心组件引用
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

        // 先初始化共享上下文，保证后续所有模块有统一依赖来源
        private void InitializeContext()
        {
            context?.SetPlayerControllerReader(playerController);
            context?.SetPlayerInputReader(playerInputReader);
            context?.SetPlayerStamina(playerStamina);
            context?.SetCharacterDataSO(characterDataSO);
            context?.Initialize(this);
        }

        // 初始化角色专属模块：根据角色数据决定拉起哪些专属模块，并把状态机所需的专属能力注入进去
        private void InitializeCharacterExclusiveModules()
        {
            if (characterDataSO == null)
            {
                return;
            }

            switch (characterDataSO.characterName)
            {
                case CharacterName.今汐:
                    InitializeJinxiExclusiveModules();
                    break;
                case CharacterName.卡提希娅:
                    InitializeKatixiyaExclusiveModules();
                    break;
            }
        }

        // 初始化今汐专属模块，并把今汐专属状态机驱动 / 龙表现控制器接入共享状态机
        private void InitializeJinxiExclusiveModules()
        {
            if (jinxiFeatureRoot == null)
            {
                jinxiFeatureRoot = GetComponent<JinxiFeatureRoot>();
            }

            jinxiFeatureRoot?.Initialize(context);

            JinxiSpecialSkillLinker jinxiSpecialSkillLinker = jinxiFeatureRoot != null
                ? jinxiFeatureRoot.SpecialSkillLinker
                : GetComponent<JinxiSpecialSkillLinker>();

            JinxiDragonController jinxiDragonController = jinxiFeatureRoot != null
                ? jinxiFeatureRoot.DragonController
                : GetComponent<JinxiDragonController>();

            // 今汐龙表现由今汐驱动层统一编排，避免继续由状态机直接持有今汐表现细节
            jinxiSpecialSkillLinker?.SetDragonController(jinxiDragonController);

            context?.StateMachine?.SetStateMachineDriver(jinxiSpecialSkillLinker);
            context?.StateMachine?.SetJinxiSpecialSkillLinker(jinxiSpecialSkillLinker);
        }

        // 初始化卡提希娅专属模块，并把卡提希娅专属状态机驱动接入共享状态机
        private void InitializeKatixiyaExclusiveModules()
        {
            if (katixiyaFeatureRoot == null)
            {
                katixiyaFeatureRoot = GetComponent<KatixiyaFeatureRoot>();
            }

            katixiyaFeatureRoot?.Initialize(context);

            KatixiyaSpecialSkillLinker katixiyaSpecialSkillLinker = katixiyaFeatureRoot != null
                ? katixiyaFeatureRoot.SpecialSkillLinker
                : GetComponent<KatixiyaSpecialSkillLinker>();

            context?.StateMachine?.SetStateMachineDriver(katixiyaSpecialSkillLinker);
            context?.StateMachine?.SetKatixiyaSpecialSkillLinker(katixiyaSpecialSkillLinker);
        }

        // 按 CharacterContext 的依赖顺序初始化角色运行模块
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
            // 6.初始化武器控制器，保证攻击表现触发前武器系统已准备好
            context?.WeaponController?.Initialize(context);
            // 7.初始化特效控制器，保证攻击表现触发前特效系统已准备好
            context?.EffectController?.Initialize(context);
            // 8.初始化移动逻辑，保证移动判定可正常工作
            context?.MovementLogic?.Initialize(context);
            // 9.初始化战斗逻辑，保证攻击逻辑可正常工作
            context?.AttackLogic?.Initialize(context);
        }
        #endregion

        #region 切人控制
        // 切换玩家控制权：当前阶段先记录控制权状态，后续其他模块按需读取
        public void SetPlayerControlled(bool isControlled)
        {
            IsPlayerControlled = isControlled;
        }

        // 切出角色：清理残留输入，避免旧角色继续消费输入请求
        public void OnSwitchOut()
        {
            context?.StateMachine?.ResetInputState();
        }

        // 切入角色：重置输入状态，避免新角色吃到切人前的残留输入
        public void OnSwitchIn()
        {
            context?.StateMachine?.ResetInputState();
        }
        #endregion
    }
}
