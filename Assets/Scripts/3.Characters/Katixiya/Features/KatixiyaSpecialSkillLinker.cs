using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    /// <summary>
    /// 卡提希娅专属状态机驱动器
    /// 负责：
    /// 1. 卡提希娅攻击可用性判断
    /// 2. 普攻 / 下落攻击 / 战技 / 爆发的攻击段选择
    /// 3. 普攻连击窗口、E 技冷却、Q 技冷却维护
    /// 4. 向状态机提供统一的角色专属战斗入口
    /// </summary>
    public class KatixiyaSpecialSkillLinker : MonoBehaviour, ICharacterStateMachineDriver
    {
        private static readonly List<AttackStep> emptyAttackSteps = new List<AttackStep>();

        #region 核心依赖
        private CharacterContext context;     // 角色共享上下文
        private CharacterAttack attackLogic;  // 共享攻击基础层
        private CombatConfigSO combatConfig;  // 卡提希娅战斗配置
        private CharacterRuntimeData RuntimeData => context != null ? context.RuntimeData : null; // 卡提希娅运行时数据快捷入口


        [Header("=== 卡提希娅特殊机制配置 ===")]
        [Header("E 技冷却时间")]
        [SerializeField] private float eSkillCD = 5f;

        [Header("Q 技冷却时间")]
        [SerializeField] private float qBurstCD = 15f;
        #endregion

        #region 运行时状态
        private AttackStep currentStep; // 当前由卡提希娅驱动层选中的攻击段

        // 地面普攻连段状态
        private int currentComboCount;
        private float comboWindowTimer;
        private bool isComboWindowOpen;
        #endregion

        #region 对外状态
        public bool IsAvailable { get; private set; } // 当前角色是否启用卡提希娅专属驱动

        // 统一从 CombatConfigSO 读取攻击段配置，未配置时返回空列表避免空引用
        public List<AttackStep> AttackSteps => combatConfig != null && combatConfig.attackSteps != null ? combatConfig.attackSteps : emptyAttackSteps;
        public List<AttackStep> FallAttackSteps => combatConfig != null && combatConfig.fallAttackSteps != null ? combatConfig.fallAttackSteps : emptyAttackSteps;
        public List<AttackStep> HeavyAttackSteps => combatConfig != null && combatConfig.heavyAttackSteps != null ? combatConfig.heavyAttackSteps : emptyAttackSteps;
        public List<AttackStep> ESkillAttackSteps => combatConfig != null && combatConfig.eSkillAttackSteps != null ? combatConfig.eSkillAttackSteps : emptyAttackSteps;
        public List<AttackStep> QBurstAttackSteps => combatConfig != null && combatConfig.qBurstAttackSteps != null ? combatConfig.qBurstAttackSteps : emptyAttackSteps;
        public List<AttackStep> QteSkillAttackSteps => combatConfig != null && combatConfig.qteSkillAttackSteps != null ? combatConfig.qteSkillAttackSteps : emptyAttackSteps;


        public bool IsFloating => false; // 卡提希娅当前没有御空机制，先返回 false 兼容通用系统

        public float ESkillCDTimer => RuntimeData != null ? Mathf.Max(0f, RuntimeData.katixiyaESkillCDTimer) : 0f;
        public float QBurstCDTimer => RuntimeData != null ? Mathf.Max(0f, RuntimeData.katixiyaQBurstCDTimer) : 0f;
        #endregion

        #region 生命周期
        private void Update()
        {
            UpdateRuntime();
        }
        #endregion

        #region 初始化
        // 卡提希娅专属状态机驱动初始化：由 KatixiyaFeatureRoot 统一拉起
        public void Initialize(CharacterContext context)
        {
            this.context = context;
            attackLogic = context != null ? context.AttackLogic : GetComponent<CharacterAttack>();

            CharacterDataSO characterDataSO = context != null ? context.CharacterDataSO : null;
            combatConfig = characterDataSO != null ? characterDataSO.combatConfig : null;

            // 当前先沿用角色枚举判断卡提希娅身份
            IsAvailable = characterDataSO != null && characterDataSO.characterName == CharacterName.卡提希娅;

            ResetAllRuntimeState();
        }
        #endregion

        #region 状态注册
        // 向状态工厂注册卡提希娅专属状态
        public void RegisterSpecialStates(CharacterStateFactory factory, CharacterStateMachine machine)
        {
            if (factory == null || machine == null)
            {
                return;
            }

            // 注册卡提希娅完整状态集合：当前 KatixiyaStates 中的状态都由卡提希娅层显式接管
            factory.RegisterState(CharacterState.KatixiyaIdle, new KatixiyaIdleState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaMove, new KatixiyaMoveState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaStop, new KatixiyaStopState(machine, factory));

            factory.RegisterState(CharacterState.KatixiyaJump, new KatixiyaJumpState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaFall, new KatixiyaFallState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaLand, new KatixiyaLandState(machine, factory));

            factory.RegisterState(CharacterState.KatixiyaAttack, new KatixiyaAttackState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaHeavyAttack, new KatixiyaHeavyAttackState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaFallAttack, new KatixiyaFallAttackState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaAirAttack, new KatixiyaAirAttackState(machine, factory));

            factory.RegisterState(CharacterState.KatixiyaDash, new KatixiyaDashState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaAirDash, new KatixiyaAirDashState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaFloatDash, new KatixiyaFloatDashState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaDodge, new KatixiyaDodgeState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaFloatDodge, new KatixiyaFloatDodgeState(machine, factory));

            factory.RegisterState(CharacterState.KatixiyaESkill, new KatixiyaESkillState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaQBurst, new KatixiyaQBurstState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaQteSkill, new KatixiyaQteSkillState(machine, factory));

            factory.RegisterState(CharacterState.KatixiyaHit, new KatixiyaHitState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaDead, new KatixiyaDeadState(machine, factory));
        }
        #endregion

        #region 运行时更新
        // 更新卡提希娅专属的短命战斗状态：连击窗口切人即丢，继续保留在 Linker 本地
        public void UpdateRuntime()
        {
            if (!IsAvailable)
            {
                return;
            }

            // 地面普攻连击窗口倒计时
            if (isComboWindowOpen)
            {
                comboWindowTimer -= Time.deltaTime;
                if (comboWindowTimer <= 0f)
                {
                    ResetNormalCombo();
                }
            }
        }
        #endregion

        #region 普通攻击
        // 地面普通攻击可用性判断
        public bool IsAttackable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            return isCanInterrupt && isGrounded;
        }

        // 初始化地面普攻段：根据连击窗口状态决定当前应打哪一段
        public AttackStep InitializeNormalAttackStep()
        {
            AttackStep step = InitializeComboStep(AttackSteps, ref currentComboCount, ref isComboWindowOpen);
            currentStep = step;
            attackLogic?.SetCurrentStep(step);
            return step;
        }

        // 开启地面普攻连击窗口
        public void StartNormalComboWindow()
        {
            isComboWindowOpen = true;
            comboWindowTimer = attackLogic != null ? attackLogic.GetComboWindowDuration(currentStep) : 0f;
        }

        // 重置地面普攻连段
        public void ResetNormalCombo()
        {
            currentComboCount = 0;
            isComboWindowOpen = false;
            comboWindowTimer = 0f;
        }
        #endregion

        #region 重击
        // 重击可用性判断
        public bool IsHeavyAttackable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            bool hasEnoughStamina = context == null || context.PlayerStamina == null || context.PlayerStamina.CanHeavyAttack();
            return isCanInterrupt && hasEnoughStamina;
        }

        // 消耗地面重击体力
        public bool TryConsumeHeavyAttackStamina()
        {
            // 没有体力系统时默认允许，避免测试场景被体力组件阻塞
            bool hasStaminaSystem = context != null && context.PlayerStamina != null;
            return !hasStaminaSystem || context.PlayerStamina.TryConsumeHeavyAttack();
        }


        // 初始化重击攻击段：卡提希娅当前只有一段重击但是会根据情况判断是空中重击还是地面重击
        public AttackStep InitializeHeavyAttackStep()
        {
            if (HeavyAttackSteps.Count == 0)
            {
                Debug.LogError("HeavyAttackSteps 配置为空！");
                return null;
            }
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            AttackStep step;

            if (isGrounded)
            {
                 step = GetAttackStepAt(HeavyAttackSteps, 0);
            }
            else
            {
                 step = GetAttackStepAt(HeavyAttackSteps, 1);
            }
           
            if (step == null)
            {
                Debug.LogError("HeavyAttackSteps 缺少重击攻击段配置！");
                return null;
            }

            currentStep = step;
            attackLogic?.SetCurrentStep(step);
            return step;
        }

        #endregion

        #region 下落攻击
        // 下落攻击可用性判断
        public bool IsFallAttackable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            bool isInTheAir = context != null && context.MovementLogic != null && !context.MovementLogic.CustomCheckGrounded();
            return isCanInterrupt && isInTheAir;
        }
        #endregion

        #region 御空攻击
        // 卡提希娅当前没有御空攻击，保留接口避免通用状态查询报错
        public bool IsAirAttackable()
        {
            return false;
        }

        // 卡提希娅当前没有御空攻击，暂时返回空
        public AttackStep InitializeAirAttackStep(List<AttackStep> stepList)
        {
            return null;
        }

        // 卡提希娅当前没有御空攻击连段
        public void StartAirComboWindow()
        {

        }

        // 卡提希娅当前没有御空攻击连段
        public void ResetAirCombo()
        {

        }
        #endregion

        #region 战技
        // 战技可用性判断
        public bool IsESkillable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            return isCanInterrupt && CanUseESkill();
        }

        // E 技可用性判断
        public bool CanUseESkill()
        {
            bool isCDOver = ESkillCDTimer <= 0f;
         
            return isCDOver ;
        }


        // 初始化战技攻击段：卡提希娅当前只有一段 E 技
        public AttackStep InitializeESkillStep()
        {
            if (ESkillAttackSteps.Count == 0)
            {
                Debug.LogError("ESkillAttackSteps 配置为空！");
                return null;
            }

            AttackStep step = GetAttackStepAt(ESkillAttackSteps, 0);
            if (step == null)
            {
                Debug.LogError("ESkillAttackSteps 缺少 E 技攻击段配置！");
                return null;
            }

            currentStep = step;
            attackLogic?.SetCurrentStep(step);
            return step;
        }
        #endregion

        #region 爆发
        // 爆发可用性判断
        public bool IsQBurstable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            bool isCDOver = QBurstCDTimer <= 0f;
            //return isCanInterrupt && HasQBurstConfigured && isCDOver;
            return false;//还没做，防止误进入
        }

        #endregion

        #region 延奏QTE
        // 延奏QTE可用性判断
        public bool IsQteSkillable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            return isCanInterrupt;
        }

        // 初始化延奏QTE攻击段：当前只取一段
        public AttackStep InitializeQteSkillStep()
        {
            if (QteSkillAttackSteps.Count == 0)
            {
                Debug.LogError("QteSkillAttackSteps 配置为空！");
                return null;
            }
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            AttackStep step;

            if (isGrounded)
            {
                step = GetAttackStepAt(QteSkillAttackSteps, 0);
            }
            else
            {
                step = GetAttackStepAt(HeavyAttackSteps, 1);
            }
            if (step == null)
            {
                Debug.LogError("QteSkillAttackSteps 缺少延奏QTE攻击段配置！");
                return null;
            }

            currentStep = step;
            attackLogic?.SetCurrentStep(step);
            return step;
        }
        #endregion

        #region 技能使用回调
        // E 技使用后：进入冷却
        public void OnESkillUsed()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.katixiyaESkillCDTimer = eSkillCD;
            NotifySkillUIChanged();
        }


        // Q 爆发使用后：进入冷却
        public void OnQBurstUsed()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.katixiyaQBurstCDTimer = qBurstCD;
            NotifySkillUIChanged();
        }


        #endregion

        #region 内部工具
        // 初始化连段攻击步骤：窗口开启时进入下一段，否则回到第一段
        private AttackStep InitializeComboStep(List<AttackStep> stepList, ref int comboIndex, ref bool comboWindowOpen)
        {
            if (!IsAvailable)
            {
                return null;
            }

            if (stepList == null || stepList.Count == 0)
            {
                Debug.LogError("攻击段配置为空！");
                return null;
            }

            if (comboWindowOpen)
            {
                comboIndex++;
                if (comboIndex >= stepList.Count)
                {
                    comboIndex = 0;
                }
            }
            else
            {
                comboIndex = 0;
            }

            comboWindowOpen = false;
            return stepList[comboIndex];
        }

        // 安全获取指定索引的攻击段：越界时返回 null
        private AttackStep GetAttackStepAt(List<AttackStep> stepList, int index)
        {
            if (stepList == null || index < 0 || index >= stepList.Count)
            {
                return null;
            }

            return stepList[index];
        }
        // 重置卡提希娅专属驱动的全部运行时状态
        private void ResetAllRuntimeState()
        {
            currentStep = null;

            currentComboCount = 0;
            comboWindowTimer = 0f;
            isComboWindowOpen = false;

            if (RuntimeData != null)
            {
                RuntimeData.katixiyaESkillCDTimer = 0f;
                RuntimeData.katixiyaQBurstCDTimer = 0f;
            }
        }



        // 通知技能 UI 刷新
        private void NotifySkillUIChanged()
        {
            // 1.空值检查：没有上下文时无法通知HUD
            if (context == null)
            {
                return;
            }

            // 2.派发E技能图标刷新事件
            UIEvents.RaiseSkillIconUIChanged(context, SkillUIType.ESkill, 0, ESkillCDTimer);

            // 3.Q爆发冷却中显示未充能图标，冷却结束显示已充能图标
            int qBurstIconIndex = QBurstCDTimer > 0f ? -1 : 0;

            // 4.派发Q爆发图标刷新事件
            UIEvents.RaiseSkillIconUIChanged(context, SkillUIType.QBurst, qBurstIconIndex, QBurstCDTimer);
        }

        #endregion
    }
}
