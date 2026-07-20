using System;
using System.Linq;
using UnityEngine;


namespace WutheringWaves
{
    public class CharacterContext : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 核心组件（手动补全） ===")]
        [Header("角色基础数据：手动填入")]
        [SerializeField] private CharacterDataSO characterDataSO;
        [Header("角色Look Root：手动填入")]
        [SerializeField] private Transform cameraTarget;

        [Header("=== 外层核心组件（自动获取） ===")]
        [Header("玩家控制器：位于 Player 层")]
        [SerializeField] private PlayerController playerController; // 玩家控制器
        [Header("玩家输入读取器：位于 Player 层")]
        [SerializeField] private PlayerInputReader playerInputReader;
        [Header("玩家共享体力：位于 Player 层")]
        [SerializeField] private PlayerStamina playerStamina;

        [Header("=== 内层核心组件（自动获取） ===")]
        [Header("===角色运行时数据===")]
        [SerializeField] private CharacterRuntimeData runtimeData = new(); // 角色运行时数据
        [Header("角色状态机")]
        [SerializeField] private CharacterStateMachine stateMachine; // 角色状态机
        [Header("角色输入缓冲器")]
        [SerializeField] private InputBuffer inputBuffer; // 角色输入缓冲器
        [Header("角色属性")]
        [SerializeField] private CharacterAttributes attributes; // 角色属性
        [Header("移动逻辑")]
        [SerializeField] private CharacterMovement movementLogic; // 移动逻辑
        [Header("战斗逻辑")]
        [SerializeField] private CharacterAttack attackLogic; // 战斗逻辑
        [Header("角色根运动逻辑")]
        [SerializeField] private CharacterRootMotion rootMotion; // 角色根运动逻辑
        [Header("角色表现逻辑")]
        [SerializeField] private CharacterManifestation manifestation; //角色表现逻辑
        [Header("武器控制器")]
        [SerializeField] private WeaponController weaponController; // 武器控制器
        [Header("特效控制器")]
        [SerializeField] private EffectController effectController; // 特效控制器
        [Header("音效控制器")]
        [SerializeField] private AudioController audioController; // 音效控制器
        [Header("=== 基础组件（自动获取）===")]
        [Header("动画控制器")]
        [SerializeField] private Animator animator; // 角色动画控制器
        [Header("角色控制器")]
        [SerializeField] private CharacterController characterController; // 角色控制器

        [Header("=== 角色独属（自动获取） ===")]
        [SerializeField] private JinxiFeatureRoot jinxiFeatureRoot;
        [SerializeField] private KatixiyaFeatureRoot katixiyaFeatureRoot;
        #endregion

        #region 对外只读属性
        //核心组件
        public CharacterDataSO CharacterDataSO => characterDataSO; // 角色基础数据
        public Transform CameraTarget => cameraTarget != null ? cameraTarget : transform; // 当前角色相机观察点

        // 外层核心组件
        public PlayerController PlayerController => playerController; // 玩家控制器
        public PlayerInputReader PlayerInputReader => playerInputReader; // 玩家输入读取器
        public PlayerStamina PlayerStamina => playerStamina; // 玩家共享体力
        
        //内层核心组件
        public CharacterRuntimeData RuntimeData => runtimeData; // 角色运行时数据
        public CharacterStateMachine StateMachine => stateMachine; // 角色状态机
        public InputBuffer InputBuffer => inputBuffer; // 角色输入缓冲器
        public CharacterAttributes Attributes => attributes; // 角色输入缓冲器
        public CharacterMovement MovementLogic => movementLogic; // 移动逻辑
        public CharacterAttack AttackLogic => attackLogic; // 战斗逻辑
        public CharacterRootMotion RootMotion => rootMotion; // 角色根运动逻辑
        public CharacterManifestation Manifestation => manifestation; // 角色表现逻辑
        public WeaponController WeaponController => weaponController; // 武器控制器
        public EffectController EffectController => effectController; // 特效控制器
        public AudioController AudioController => audioController; // 音效控制器

        //基础组件
        public Animator Animator => animator; // 角色动画控制器
        public CharacterController CharacterController => characterController; // 角色控制器
        //角色独属

        public JinxiFeatureRoot JinxiFeatureRoot => jinxiFeatureRoot; // 今汐独属模块根节点
        public KatixiyaFeatureRoot KatixiyaFeatureRoot => katixiyaFeatureRoot; // 卡提希娅独属模块根节点
        #endregion

        #region 生命周期

        #endregion

        #region 初始化
        // 角色门面初始化总入口：统一接管上下文、输入、共享模块与角色专属模块的启动顺序
        public void Initialize(CharacterRuntimeData runtimeData)
        {
            // 1.验证Inspector中的核心组件
            ValidateInspectorReferences();

            // 2.绑定并初始化角色运行时数据
            // 运行数据解析失败时停止后续初始化，避免错误数据进入角色组件
            if (!ResolveRuntimeData(runtimeData))
            {
                return;
            }

            // 3.自动补齐外层核心组件
            AutoGetOuterComponents();

            // 4.自动补齐基础组件
            AutoGetBaseComponents();

            // 5.自动补齐内层核心组件
            AutoGetCoreComponents();

            // 6.初始化内层核心组件
            InitializeCoreComponentsWithoutStateMachine();

            // 7.初始化角色专属模块，并把专属驱动能力注入共享状态机
            InitializeCharacterExclusiveModules();

            // 8.启动状态机
            InitializeStateMachine();
        }

        #region 验证核心组件
        //验证核心组件
        private void ValidateInspectorReferences()
        {
            if (characterDataSO == null)
            {
                Debug.Log("角色基础数据缺失");
            }
            if (cameraTarget == null)
            {
                Debug.Log("角色Look Root");
            }
        }
        #endregion

        #region 解析角色运行时数据
        // 解析并绑定PlayerRuntimeData队伍槽位中的角色运行时数据
        private bool ResolveRuntimeData(CharacterRuntimeData runtimeData)
        {
            // 1.运行时数据必须由PlayerRuntimeData的队伍槽位提供
            if (runtimeData == null)
            {
                // 主动清空旧引用，避免角色使用Inspector中残留的数据
                this.runtimeData = null;

                Debug.LogError(
                    "[CharacterContext] 解析角色运行时数据失败：传入的CharacterRuntimeData为空。",
                    this
                );
                return false;
            }

            // 2.角色预制体必须配置对应的静态角色数据
            if (characterDataSO == null)
            {
                this.runtimeData = null;

                Debug.LogError(
                    $"[CharacterContext] 解析角色运行时数据失败：角色 {runtimeData.characterName} 缺少CharacterDataSO。",
                    this
                );
                return false;
            }

            // 3.直接保存PlayerRuntimeData槽位中的运行数据引用
            // 不进行复制，保证两边始终指向同一个CharacterRuntimeData对象
            this.runtimeData = runtimeData;

            // 4.无条件交给CharacterRuntimeData检查首次初始化状态
            // 是否需要写入基础属性完全由hasInitialized决定
            this.runtimeData.EnsureInitialized(characterDataSO);

            // 5.初始化完成后再次校验结果
            // 配置异常时EnsureInitialized不会写入hasInitialized
            if (!this.runtimeData.HasInitialized)
            {
                this.runtimeData = null;

                Debug.LogError(
                    $"[CharacterContext] 解析角色运行时数据失败：角色 {runtimeData.characterName} 未完成基础数据初始化。",
                    this
                );
                return false;
            }

            return true;
        }
        #endregion

        #region 自动补齐外层核心组件
        // 自动补齐角色根节点上的核心组件引用
        private void AutoGetOuterComponents()
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
        }
        #endregion

        #region 自动补齐基础组件
        private void AutoGetBaseComponents()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        #endregion

        #region 自动补齐内层核心组件
        private void AutoGetCoreComponents()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<CharacterStateMachine>();
            }
            if (inputBuffer == null)
            {
                inputBuffer = GetComponent<InputBuffer>();
            }
            if (movementLogic == null)
            {
                movementLogic = GetComponent<CharacterMovement>();
            }
            if (attackLogic == null)
            {
                attackLogic = GetComponent<CharacterAttack>();
            }
            if (rootMotion == null)
            {
                rootMotion = GetComponentInChildren<CharacterRootMotion>(true);
            }
            if (weaponController == null)
            {
                weaponController = GetComponent<WeaponController>();
            }
            if (effectController == null)
            {
                effectController = GetComponent<EffectController>();
            }
            if (audioController == null)
            {
                audioController = GetComponent<AudioController>();
            }

            if (manifestation == null)
            {
                manifestation = GetComponentInChildren<CharacterManifestation>();
            }
        }
        #endregion

        #region 初始化内层核心组件(无状态机)
        private void InitializeCoreComponentsWithoutStateMachine()
        {
            inputBuffer.Initialize();
            movementLogic.Initialize(this);
            attackLogic.Initialize(this);
            rootMotion.Initialize(this);
            weaponController.Initialize(this);
            effectController.Initialize(this);
            audioController.Initialize(this);
            manifestation.Initialize(this);
        }

        #endregion

        #region 初始化角色专属模块
        // 初始化角色专属模块：根据角色数据决定拉起哪些专属模块，并把状态机所需的专属能力注入进去
        private void InitializeCharacterExclusiveModules()
        {
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

            jinxiFeatureRoot?.Initialize(this);

            JinxiSpecialSkillLinker jinxiSpecialSkillLinker = jinxiFeatureRoot != null
                ? jinxiFeatureRoot.SpecialSkillLinker
                : GetComponent<JinxiSpecialSkillLinker>();

            JinxiDragonController jinxiDragonController = jinxiFeatureRoot != null
                ? jinxiFeatureRoot.DragonController
                : GetComponent<JinxiDragonController>();
            JinxiSpecialSwordController jinxiSpecialSwordController = jinxiFeatureRoot != null
                ? jinxiFeatureRoot.SpecialSwordController
                : GetComponent<JinxiSpecialSwordController>();


            // 今汐龙表现由今汐驱动层统一编排，避免继续由状态机直接持有今汐表现细节
            jinxiSpecialSkillLinker?.SetDragonController(jinxiDragonController);
            jinxiSpecialSkillLinker?.SetSpecialSwordController(jinxiSpecialSwordController);


            stateMachine?.SetStateMachineDriver(jinxiSpecialSkillLinker);
            stateMachine?.SetJinxiSpecialSkillLinker(jinxiSpecialSkillLinker);
        }

        // 初始化卡提希娅专属模块，并把卡提希娅专属状态机驱动接入共享状态机
        private void InitializeKatixiyaExclusiveModules()
        {
            if (katixiyaFeatureRoot == null)
            {
                katixiyaFeatureRoot = GetComponent<KatixiyaFeatureRoot>();
            }

            katixiyaFeatureRoot?.Initialize(this);

            KatixiyaSpecialSkillLinker katixiyaSpecialSkillLinker = katixiyaFeatureRoot != null
                ? katixiyaFeatureRoot.SpecialSkillLinker
                : GetComponent<KatixiyaSpecialSkillLinker>();

            stateMachine?.SetStateMachineDriver(katixiyaSpecialSkillLinker);
            stateMachine?.SetKatixiyaSpecialSkillLinker(katixiyaSpecialSkillLinker);
        }
        #endregion

        #region 初始化状态机
        //初始化状态机
        private void InitializeStateMachine()
        {
            stateMachine.Initialize(this);
        }
        #endregion

        #endregion

        #region 生命值控制
        // 受到伤害：角色生命值统一入口
        public void TakeDamage(float damage)
        {
            //1.空值检查
            if (runtimeData == null)
            {
                return;
            }

            //2.记录扣血前是否存活，用于后面判断是否需要进入受击
            bool wasAlive = !runtimeData.IsDead;

            //3.由运行时数据处理扣血
            runtimeData.TakeDamage(damage);

            //4.通知外部刷新生命值表现
            NotifyHealthChanged();

        }

        // 恢复生命值：角色生命值统一入口
        public void Heal(float amount)
        {
            //1.空值检查
            if (runtimeData == null)
            {
                return;
            }

            //2.由运行时数据处理回血
            runtimeData.Heal(amount);

            //3.通知外部刷新生命值表现
            NotifyHealthChanged();
        }

        // 强制刷新生命值：切换角色、初始化或UI重新绑定时使用
        public void ForceRefreshHealth()
        {
            NotifyHealthChanged();
        }

        // 通知生命值发生变化
        private void NotifyHealthChanged()
        {
            if (runtimeData == null)
            {
                return;
            }

            GameEvents.RaiseHealthChanged(this, runtimeData.currentHealth, runtimeData.maxHealth, runtimeData.NormalizedHealth);
        }
        #endregion
        
        #region 生命值测试
        [Header("=== 生命值测试 ===")]
        [SerializeField] private bool enableHealthTest = false; // 是否启用生命值测试
        [SerializeField] private float testDamage = 100f; // 单次测试扣血
        [SerializeField] private bool enableHealthDrainTest = false; // 是否启用持续掉血测试
        [SerializeField] private float healthDrainPerSecond = 10f; // 每秒持续扣除生命值

        // 更新生命值测试逻辑
        private void UpdateHealthTest()
        {
            //1.未开启测试时不处理
            if (!enableHealthTest)
            {
                return;
            }

            //2.按H扣除一次生命值
            if (Input.GetKeyDown(KeyCode.H))
            {
                TakeDamage(testDamage);
            }

            //3.按J恢复一次生命值
            if (Input.GetKeyDown(KeyCode.J))
            {
                Heal(testDamage);
            }

            //4.开启持续掉血时，每帧按时间扣血
            if (enableHealthDrainTest)
            {
                TakeDamage(healthDrainPerSecond * Time.deltaTime);
            }
        }
        #endregion


    }
}
