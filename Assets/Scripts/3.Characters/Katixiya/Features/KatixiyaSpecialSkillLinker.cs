using System;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    /// <summary>
    /// 卡提希娅专属状态机驱动器
    /// 负责：
    /// 1. 卡提希娅攻击可用性判断
    /// 2. 普攻 / 御空攻击 / 战技 / 爆发的攻击段选择
    /// 3. 连击窗口、派生窗口、冷却与御空状态维护
    /// 4. 向状态机提供统一的角色专属战斗入口
    /// </summary>
    public class KatixiyaSpecialSkillLinker : MonoBehaviour, ICharacterStateMachineDriver
    {
        private static readonly List<AttackStep> emptyAttackSteps = new List<AttackStep>();

        #region 核心依赖
        private CharacterContext context;     // 角色共享上下文
        private CharacterAttack attackLogic;  // 共享攻击基础层（仅用于查询通用战斗数据）
        private CombatConfigSO combatConfig;  // 卡提希娅战斗配置

        [Header("=== 卡提希娅特殊机制配置 ===")]
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
        private AttackStep currentStep; // 当前由卡提希娅驱动层选中的攻击段

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
        public bool IsAvailable { get; private set; } // 当前角色是否启用卡提希娅专属驱动

        // 统一从 CombatConfigSO 读取攻击段配置，未配置时返回空列表避免空引用
        public List<AttackStep> AttackSteps => combatConfig != null && combatConfig.attackSteps != null ? combatConfig.attackSteps : emptyAttackSteps;
        public List<AttackStep> FallAttackSteps => combatConfig != null && combatConfig.fallAttackSteps != null ? combatConfig.fallAttackSteps : emptyAttackSteps;
        public List<AttackStep> SkillAirAttackSteps => combatConfig != null && combatConfig.skillAirAttackSteps != null ? combatConfig.skillAirAttackSteps : emptyAttackSteps;
        public List<AttackStep> SkillAttackSteps => combatConfig != null && combatConfig.skillAttackSteps != null ? combatConfig.skillAttackSteps : emptyAttackSteps;
        public List<AttackStep> ESkillAttackSteps => combatConfig != null && combatConfig.eSkillAttackSteps != null ? combatConfig.eSkillAttackSteps : emptyAttackSteps;
        public List<AttackStep> QBurstAttackSteps => combatConfig != null && combatConfig.qBurstAttackSteps != null ? combatConfig.qBurstAttackSteps : emptyAttackSteps;

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

        #region 初始化
        // 卡提希娅专属状态机驱动初始化：由 KatixiyaFeatureRoot 统一拉起
        public void Initialize(CharacterContext context)
        {
            this.context = context;
            attackLogic = context != null ? context.AttackLogic : GetComponent<CharacterAttack>();

            CharacterDataSO characterDataSO = context != null ? context.CharacterDataSO : null;
            combatConfig = characterDataSO != null ? characterDataSO.combatConfig : null;

            // 当前先沿用角色枚举判断卡提希娅身份，后续如果角色驱动入口继续扩展，可再抽成更通用的角色特性判断
            IsAvailable = characterDataSO != null && characterDataSO.characterName == CharacterName.卡提希娅;

            ResetAllRuntimeState();
        }
        // 向状态工厂补注册卡提希娅专属 / 卡提希娅强化版状态
        public void RegisterSpecialStates(CharacterStateFactory factory, CharacterStateMachine machine)
        {
            if (factory == null || machine == null)
            {
                return;
            }

            // 注册卡提希娅完整状态集合：当前 KatixiyaStates 中的状态都由卡提希娅层显式接管
            factory.RegisterState(CharacterState.KatixiyaIdle, new KatixiyaIdleState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaMove, new KatixiyaMoveState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaJump, new KatixiyaJumpState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaFall, new KatixiyaFallState(machine, factory));
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
            factory.RegisterState(CharacterState.KatixiyaHit, new KatixiyaHitState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaDead, new KatixiyaDeadState(machine, factory));
            factory.RegisterState(CharacterState.KatixiyaTransition, new KatixiyaTransitionState(machine, factory));
        }

        #endregion

        #region 运行时更新
        private void Update()
        {
            UpdateRuntime();
        }
        // 更新卡提希娅专属的连击窗口、技能冷却、派生窗口与御空持续时间
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
        // 战技可用性判断：任一派生阶段可用即可进入 KatixiyaESkill 状态
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

        #region 变奏

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
        // 卡提希娅当前没有额外龙表现控制器，这里保留空实现兼容状态调用
        public void PlayDragonAction(AttackStep step)
        {
        }

        // 卡提希娅当前没有额外龙表现控制器，这里保留空实现兼容状态调用
        public void HideDragonInstantly()
        {
        }
        #endregion

        #region 技能UI显示
        // 获取当前 E 技能 UI 应展示阶段的总时长
        public float GetCurrentESkillDisplayDuration()
        {
            switch (CurrentESkillUIIndex)
            {
                case 4:
                    return skill4WindowCD;
                case 3:
                    return skill3WindowCD;
                case 2:
                    return skill2WindowCD;
                default:
                    return skill1CD;
            }
        }

        // 获取当前 E 技能 UI 应展示阶段的剩余时长
        public float GetCurrentESkillDisplayTimer()
        {
            switch (CurrentESkillUIIndex)
            {
                case 4:
                    return Skill4WindowTimer;
                case 3:
                    return Skill3WindowTimer;
                case 2:
                    return Skill2WindowTimer;
                default:
                    return Skill1CDTimer;
            }
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


