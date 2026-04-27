using System;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    /// <summary>
    /// 今汐专属状态机驱动器
    /// 负责：
    /// 1. 今汐攻击可用性判断
    /// 2. 普攻 / 御空攻击 / 战技 / 爆发的攻击段选择
    /// 3. 连击窗口、派生窗口、冷却与御空状态维护
    /// 4. 向状态机提供统一的角色专属战斗入口
    /// </summary>
    public class JinxiSpecialSkillLinker : MonoBehaviour, ICharacterStateMachineDriver
    {
        private static readonly List<AttackStep> emptyAttackSteps = new List<AttackStep>();

        #region 核心依赖
        private CharacterContext context;     // 角色共享上下文
        private CharacterAttack attackLogic;  // 共享攻击基础层（仅用于查询通用战斗数据）
        private CombatConfigSO combatConfig;  // 今汐战斗配置
        private JinxiDragonController jinxiDragonController; // 今汐龙表现控制器

        [Header("=== 今汐特殊机制配置 ===")]
        [Header("流光夕影冷却时间")]
        [SerializeField] private float skill1CD = 5f;

        [Header("移岁诛邪冷却时间")]
        [SerializeField] private float qBurstCD = 15f;

        [Header("神霓飞芒窗口持续时间")]
        [SerializeField] private float skill2WindowCD = 5f;

        [Header("逐天取月窗口持续时间")]
        [SerializeField] private float skill3WindowCD = 5f;

        [Header("乘岁凌霄窗口持续时间")]
        [SerializeField] private float skill4WindowCD = 5f;

        [Header("御空状态持续时间")]
        [SerializeField] private float floatingDuration = 10f;

        #region 运行时状态
        private AttackStep currentStep; // 当前由今汐驱动层选中的攻击段

        // 地面普攻连段状态
        private int currentComboCount;
        private float comboWindowTimer;
        private bool isComboWindowOpen;

        // 御空攻击连段状态
        private int currentAirComboCount;
        private float airComboWindowTimer;
        private bool isAirComboWindowOpen;

        // 技能 / 爆发 / 派生窗口 / 御空计时器
        private float skill1CDTimer;
        private float qBurstCDTimer;
        private float skill2WindowTimer;
        private float skill3WindowTimer;
        private float skill4WindowTimer;
        private float floatingTimer;

        // 技能派生窗口与御空状态
        private bool isSkill2WindowOpen;
        private bool isSkill3WindowOpen;
        private bool isSkill4WindowOpen;
        private bool isFloating;
        #endregion

        #region 对外状态
        public bool IsAvailable { get; private set; } // 当前角色是否启用今汐专属驱动

        // 统一从 CombatConfigSO 读取攻击段配置，未配置时返回空列表避免空引用
        public List<AttackStep> AttackSteps => combatConfig != null && combatConfig.attackSteps != null ? combatConfig.attackSteps : emptyAttackSteps;
        public List<AttackStep> FallAttackSteps => combatConfig != null && combatConfig.fallAttackSteps != null ? combatConfig.fallAttackSteps : emptyAttackSteps;
        public List<AttackStep> HeavyAttackSteps => combatConfig != null && combatConfig.heavyAttackSteps != null ? combatConfig.heavyAttackSteps : emptyAttackSteps;
        public List<AttackStep> SkillAirAttackSteps => combatConfig != null && combatConfig.skillAirAttackSteps != null ? combatConfig.skillAirAttackSteps : emptyAttackSteps;
        public List<AttackStep> SkillAttackSteps => combatConfig != null && combatConfig.skillAttackSteps != null ? combatConfig.skillAttackSteps : emptyAttackSteps;
        public List<AttackStep> ESkillAttackSteps => combatConfig != null && combatConfig.eSkillAttackSteps != null ? combatConfig.eSkillAttackSteps : emptyAttackSteps;
        public List<AttackStep> QBurstAttackSteps => combatConfig != null && combatConfig.qBurstAttackSteps != null ? combatConfig.qBurstAttackSteps : emptyAttackSteps;
        public List<AttackStep> QteSkillAttackSteps => combatConfig != null && combatConfig.qteSkillAttackSteps != null ? combatConfig.qteSkillAttackSteps : emptyAttackSteps;


        public bool IsFloating => isFloating;
        public float Skill1CD => skill1CD;
        public float Skill1CDTimer => Mathf.Max(0f, skill1CDTimer);
        public float QBurstCD => qBurstCD;
        public float QBurstCDTimer => Mathf.Max(0f, qBurstCDTimer);
        public float Skill2WindowTimer => isSkill2WindowOpen ? Mathf.Max(0f, skill2WindowTimer) : 0f;
        public float Skill3WindowTimer => isSkill3WindowOpen ? Mathf.Max(0f, skill3WindowTimer) : 0f;
        public float Skill4WindowTimer => isSkill4WindowOpen ? Mathf.Max(0f, skill4WindowTimer) : 0f;
        public bool IsSkill2WindowOpen => isSkill2WindowOpen && skill2WindowTimer > 0f;
        public bool IsSkill3WindowOpen => isSkill3WindowOpen && skill3WindowTimer > 0f;
        public bool IsSkill4WindowOpen => isSkill4WindowOpen && skill4WindowTimer > 0f;
        public bool CanUseSkill2 => IsSkill2WindowOpen;
        public bool CanUseSkill3 => IsSkill3WindowOpen;
        public bool CanUseSkill4 => IsSkill4WindowOpen;
        public bool HasQBurstConfigured => QBurstAttackSteps.Count > 0;

        // 当前 E 技能 UI 应展示的阶段：4 优先级最高，依次回退到 1
        public int CurrentESkillUIIndex
        {
            get
            {
                if (IsSkill4WindowOpen) return 4;
                if (IsSkill3WindowOpen) return 3;
                if (IsSkill2WindowOpen) return 2;
                return 1;
            }
        }
        #endregion
        #endregion

        #region 生命周期
        private void Update()
        {
            UpdateRuntime();
        }
        #endregion

        #region 初始化
        // 今汐专属状态机驱动初始化：由 JinxiFeatureRoot 统一拉起
        public void Initialize(CharacterContext context)
        {
            this.context = context;
            attackLogic = context != null ? context.AttackLogic : GetComponent<CharacterAttack>();

            CharacterDataSO characterDataSO = context != null ? context.CharacterDataSO : null;
            combatConfig = characterDataSO != null ? characterDataSO.combatConfig : null;

            // 当前先沿用角色枚举判断今汐身份，后续如果角色驱动入口继续扩展，可再抽成更通用的角色特性判断
            IsAvailable = characterDataSO != null && characterDataSO.characterName == CharacterName.今汐;

            ResetAllRuntimeState();
        }
        

        // 由外部装配入口注入今汐龙表现控制器，后续统一由今汐驱动层编排龙表现
        public void SetDragonController(JinxiDragonController controller)
        {
            jinxiDragonController = controller;
        }
        #endregion

        #region 状态注册
        // 向状态工厂补注册今汐专属 / 今汐强化版状态
        public void RegisterSpecialStates(CharacterStateFactory factory, CharacterStateMachine machine)
        {
            if (factory == null || machine == null)
            {
                return;
            }

            // 注册今汐完整状态集合：当前 JinxiStates 中的状态都由今汐层显式接管
            factory.RegisterState(CharacterState.JinxiIdle, new JinxiIdleState(machine, factory));
            factory.RegisterState(CharacterState.JinxiMove, new JinxiMoveState(machine, factory));
            factory.RegisterState(CharacterState.JinxiStop, new JinxiStopState(machine, factory));

            factory.RegisterState(CharacterState.JinxiJump, new JinxiJumpState(machine, factory));
            factory.RegisterState(CharacterState.JinxiFall, new JinxiFallState(machine, factory));
            factory.RegisterState(CharacterState.JinxiLand, new JinxiLandState(machine, factory));

            factory.RegisterState(CharacterState.JinxiAttack, new JinxiAttackState(machine, factory));
            factory.RegisterState(CharacterState.JinxiHeavyAttack, new JinxiHeavyAttackState(machine, factory));
            factory.RegisterState(CharacterState.JinxiFallAttack, new JinxiFallAttackState(machine, factory));
            factory.RegisterState(CharacterState.JinxiAirAttack, new JinxiAirAttackState(machine, factory));

            factory.RegisterState(CharacterState.JinxiDash, new JinxiDashState(machine, factory));
            factory.RegisterState(CharacterState.JinxiAirDash, new JinxiAirDashState(machine, factory));
            factory.RegisterState(CharacterState.JinxiFloatDash, new JinxiFloatDashState(machine, factory));
            factory.RegisterState(CharacterState.JinxiDodge, new JinxiDodgeState(machine, factory));
            factory.RegisterState(CharacterState.JinxiFloatDodge, new JinxiFloatDodgeState(machine, factory));

            factory.RegisterState(CharacterState.JinxiESkill, new JinxiESkillState(machine, factory));
            factory.RegisterState(CharacterState.JinxiQBurst, new JinxiQBurstState(machine, factory));
            factory.RegisterState(CharacterState.JinxiQteSkill, new JinxiQteSkillState(machine, factory));

            factory.RegisterState(CharacterState.JinxiHit, new JinxiHitState(machine, factory));
            factory.RegisterState(CharacterState.JinxiDead, new JinxiDeadState(machine, factory));
        }
        #endregion

        #region 运行时更新
        // 更新今汐专属的连击窗口、技能冷却、派生窗口与御空持续时间
        public bool UpdateRuntime()
        {
            if (!IsAvailable)
            {
                return false;
            }

            bool isDirty = false;

            // 地面普攻连击窗口倒计时
            if (isComboWindowOpen)
            {
                comboWindowTimer -= Time.deltaTime;
                if (comboWindowTimer <= 0f)
                {
                    ResetNormalCombo();
                }
            }

            // 御空攻击连击窗口倒计时
            if (isAirComboWindowOpen)
            {
                airComboWindowTimer -= Time.deltaTime;
                if (airComboWindowTimer <= 0f)
                {
                    ResetAirCombo();
                }
            }

            // E 技一段冷却
            if (skill1CDTimer > 0f)
            {
                skill1CDTimer = Mathf.Max(0f, skill1CDTimer - Time.deltaTime);
                isDirty = true;
            }

            // Q 爆发冷却
            if (qBurstCDTimer > 0f)
            {
                qBurstCDTimer = Mathf.Max(0f, qBurstCDTimer - Time.deltaTime);
                isDirty = true;
            }

            // Skill2 派生窗口
            if (isSkill2WindowOpen)
            {
                skill2WindowTimer -= Time.deltaTime;
                isDirty = true;
                if (skill2WindowTimer <= 0f)
                {
                    skill2WindowTimer = 0f;
                    isSkill2WindowOpen = false;
                }
            }

            // Skill3 派生窗口
            if (isSkill3WindowOpen)
            {
                skill3WindowTimer -= Time.deltaTime;
                isDirty = true;
                if (skill3WindowTimer <= 0f)
                {
                    skill3WindowTimer = 0f;
                    isSkill3WindowOpen = false;
                }
            }

            // Skill4 派生窗口
            if (isSkill4WindowOpen)
            {
                skill4WindowTimer -= Time.deltaTime;
                isDirty = true;
                if (skill4WindowTimer <= 0f)
                {
                    skill4WindowTimer = 0f;
                    isSkill4WindowOpen = false;
                }
            }

            // 御空状态持续时间
            if (isFloating)
            {
                floatingTimer -= Time.deltaTime;
                if (floatingTimer <= 0f)
                {
                    SetFloating(false);
                }
            }

            if (isDirty)
            {
                NotifySkillUIChanged();
            }

            return isDirty;
        }
        #endregion

        #region 普通攻击
        // 地面普通攻击可用性判断
        public bool IsAttackable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            return isCanInterrupt && isGrounded && !isFloating;
        }
        // 初始化地面普攻段：根据连击窗口状态决定当前应打哪一段
        public AttackStep InitializeNormalAttackStep()
        {
            AttackStep step = InitializeComboStep(AttackSteps, ref currentComboCount, ref isComboWindowOpen);
            currentStep = step;
            attackLogic?.SetCurrentStep(step);
            return step;
        }
        // 开启地面普攻连击窗口；地面第四段普攻会顺带开启 Skill2 派生窗口
        public void StartNormalComboWindow()
        {
            isComboWindowOpen = true;
            comboWindowTimer = attackLogic != null ? attackLogic.GetComboWindowDuration(currentStep) : 0f;

            if (!isFloating && currentComboCount == 3)
            {
                OpenSkill2Window();
            }
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
        // 地面重击可用性判断
        public bool IsHeavyAttackable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            return isCanInterrupt && isGrounded && !isFloating;
        }

        // 初始化重击攻击段：今汐当前只有一段重击
        public AttackStep InitializeHeavyAttackStep()
        {
            if (HeavyAttackSteps.Count == 0)
            {
                Debug.LogError("HeavyAttackSteps 配置为空！");
                return null;
            }

            AttackStep step = GetAttackStepAt(HeavyAttackSteps, 0);
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
            return isCanInterrupt && isInTheAir && !isFloating;
        }
       
        #endregion

        #region 御空攻击
        // 御空攻击可用性判断
        public bool IsAirAttackable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            return isCanInterrupt && isFloating;
        }
        // 初始化御空攻击段：根据御空连击窗口决定当前应打哪一段
        public AttackStep InitializeAirAttackStep(List<AttackStep> stepList)
        {
            AttackStep step = InitializeComboStep(stepList, ref currentAirComboCount, ref isAirComboWindowOpen);
            currentStep = step;
            attackLogic?.SetCurrentStep(step);
            return step;
        }
        // 开启御空攻击连击窗口；御空第四段攻击会顺带开启 Skill4 派生窗口
        public void StartAirComboWindow()
        {
            isAirComboWindowOpen = true;
            airComboWindowTimer = attackLogic != null ? attackLogic.GetComboWindowDuration(currentStep) : 0f;

            if (currentAirComboCount == 3)
            {
                OpenSkill4Window();
            }
        }

        // 重置御空攻击连段
        public void ResetAirCombo()
        {
            currentAirComboCount = 0;
            isAirComboWindowOpen = false;
            airComboWindowTimer = 0f;
        }
        #endregion

        #region 战技
        // 战技可用性判断：任一派生阶段可用即可进入 JinxiESkill 状态
        public bool IsESkillable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            bool canUseESkill = CanUseSkill1() || CanUseSkill2 || CanUseSkill3 || CanUseSkill4;
            return isCanInterrupt && canUseESkill;
        }
        // 地面一段 E 技可用性判断
        public bool CanUseSkill1()
        {
            bool isCDOver = skill1CDTimer <= 0f;
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            return isCDOver && isGrounded;
        }
        // 初始化战技攻击段：根据当前派生窗口决定 Skill1 / 2 / 3 / 4 的对应攻击段
        public AttackStep InitializeESkillStep()
        {
            if (ESkillAttackSteps.Count == 0)
            {
                Debug.LogError("ESkillAttackSteps 配置为空！");
                return null;
            }

            AttackStep step = GetAttackStepAt(ESkillAttackSteps, GetCurrentESkillStepIndex());
            if (step == null)
            {
                Debug.LogError("ESkillAttackSteps 缺少当前窗口所需的攻击段配置！");
                return null;
            }

            currentStep = step;
            attackLogic?.SetCurrentStep(step);
            return step;
        }

        // 根据当前派生窗口状态，返回 E 技能实际应使用的攻击段索引
        public int GetCurrentESkillStepIndex()
        {
            if (IsSkill4WindowOpen) return 3;
            if (IsSkill3WindowOpen) return 2;
            if (IsSkill2WindowOpen) return 1;
            return 0;
        }
        #endregion

        #region 爆发
        // 爆发可用性判断
        public bool IsQBurstable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            bool isCDOver = qBurstCDTimer <= 0f;
            return isCanInterrupt && HasQBurstConfigured && isCDOver;
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

            AttackStep step = GetAttackStepAt(QteSkillAttackSteps, 0);
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
        // Skill1 使用后：进入冷却，并关闭 Skill2 派生窗口
        public void OnSkill1Used()
        {
            skill1CDTimer = skill1CD;
            isSkill2WindowOpen = false;
            skill2WindowTimer = 0f;
            NotifySkillUIChanged();
        }

        // Skill2 使用后：进入御空，并开启 Skill3 派生窗口
        public void OnSkill2Used()
        {
            isSkill2WindowOpen = false;
            skill2WindowTimer = 0f;
            OpenSkill3Window();
            SetFloating(true);
            NotifySkillUIChanged();
        }

        // Skill3 使用后：关闭 Skill3 派生窗口
        public void OnSkill3Used()
        {
            isSkill3WindowOpen = false;
            skill3WindowTimer = 0f;
            NotifySkillUIChanged();
        }

        // Skill4 使用后：收束空中派生链，关闭 Skill3 / Skill4 两个窗口
        public void OnSkill4Used()
        {
            isSkill4WindowOpen = false;
            isSkill3WindowOpen = false;
            skill4WindowTimer = 0f;
            skill3WindowTimer = 0f;
            NotifySkillUIChanged();
        }

        // Q 爆发使用后：进入爆发冷却
        public void OnQBurstUsed()
        {
            qBurstCDTimer = qBurstCD;
            NotifySkillUIChanged();
        }
        #endregion

        #region 技能窗口与御空控制
        // 开启 Skill2 派生窗口（由地面普攻第四段触发）
        public void OpenSkill2Window()
        {
            isSkill2WindowOpen = true;
            skill2WindowTimer = skill2WindowCD;
            NotifySkillUIChanged();
        }

        // 开启 Skill3 派生窗口（由 Skill2 使用后触发）
        public void OpenSkill3Window()
        {
            isSkill3WindowOpen = true;
            skill3WindowTimer = skill3WindowCD;
            NotifySkillUIChanged();
        }

        // 开启 Skill4 派生窗口（由御空第四段攻击触发）
        public void OpenSkill4Window()
        {
            isSkill4WindowOpen = true;
            skill4WindowTimer = skill4WindowCD;
            NotifySkillUIChanged();
        }

        // 设置御空状态，并通过事件总线通知表现层同步更新
        public void SetFloating(bool value)
        {
            isFloating = value;
            floatingTimer = value ? floatingDuration : 0f;

            if (attackLogic != null)
            {
                GameEvents.RaiseFloatingChanged(attackLogic, isFloating);
            }
        }
        #endregion

        #region 龙表现转发
        // 今汐状态只通过今汐驱动层调用龙表现，避免状态类直接耦合龙控制器
        public void PlayDragonAction(AttackStep step)
        {
            jinxiDragonController?.PlayDragonAction(step);
        }

        // 今汐状态退出时通过驱动层统一关闭龙表现
        public void HideDragonInstantly()
        {
            jinxiDragonController?.HideDragonInstantly();
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

        // 重置今汐专属驱动的全部运行时状态
        private void ResetAllRuntimeState()
        {
            currentStep = null;

            currentComboCount = 0;
            comboWindowTimer = 0f;
            isComboWindowOpen = false;

            currentAirComboCount = 0;
            airComboWindowTimer = 0f;
            isAirComboWindowOpen = false;

            skill1CDTimer = 0f;
            qBurstCDTimer = 0f;
            skill2WindowTimer = 0f;
            skill3WindowTimer = 0f;
            skill4WindowTimer = 0f;
            floatingTimer = 0f;

            isSkill2WindowOpen = false;
            isSkill3WindowOpen = false;
            isSkill4WindowOpen = false;
            isFloating = false;
        }

        // 通知技能 UI 刷新
        private void NotifySkillUIChanged()
        {
            if (attackLogic != null)
            {
                GameEvents.RaiseSkillUIStateChanged(attackLogic);
            }
        }
        #endregion
    }
}

