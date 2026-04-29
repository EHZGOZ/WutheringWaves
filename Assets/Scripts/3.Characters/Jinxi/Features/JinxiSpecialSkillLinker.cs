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
        private CharacterRuntimeData RuntimeData => context != null ? context.CharacterRuntimeData : null; // 今汐运行时数据快捷入口


        [Header("=== 今汐特殊机制配置 ===")]
        [Header("流光夕影冷却时间")]
        [SerializeField] private float eSkill1CD = 5f;

        [Header("移岁诛邪冷却时间")]
        [SerializeField] private float qBurstCD = 15f;

        [Header("神霓飞芒窗口持续时间")]
        [SerializeField] private float eSkill2WindowDuration = 5f;

        [Header("逐天取月窗口持续时间")]
        [SerializeField] private float eSkill3WindowDuration = 5f;

        [Header("乘岁凌霄窗口持续时间")]
        [SerializeField] private float eSkill4WindowDuration = 5f;

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

        public bool IsFloating => RuntimeData != null && RuntimeData.jinxiIsFloating;

        public float ESkill1CD => eSkill1CD;
        
        public float QBurstCD => qBurstCD;
        
        public float ESkill1CDTimer => RuntimeData != null ? Mathf.Max(0f, RuntimeData.jinxiESkill1CDTimer) : 0f;
        public float QBurstCDTimer => RuntimeData != null ? Mathf.Max(0f, RuntimeData.jinxiQBurstCDTimer) : 0f;

        public bool IsESkill2WindowOpen => RuntimeData != null
            && RuntimeData.jinxiIsESkill2WindowOpen
            && RuntimeData.jinxiESkill2WindowTimer > 0f;

        public bool IsESkill3WindowOpen => RuntimeData != null
            && RuntimeData.jinxiIsESkill3WindowOpen
            && RuntimeData.jinxiESkill3WindowTimer > 0f;

        public bool IsESkill4WindowOpen => RuntimeData != null
            && RuntimeData.jinxiIsESkill4WindowOpen
            && RuntimeData.jinxiESkill4WindowTimer > 0f;

        public bool CanUseESkill2 => IsESkill2WindowOpen;
        public bool CanUseESkill3 => IsESkill3WindowOpen;
        public bool CanUseESkill4 => IsESkill4WindowOpen;
        public bool HasQBurstConfigured => QBurstAttackSteps.Count > 0;

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
        // 更新今汐专属的短命战斗状态：连击窗口切人即丢，继续保留在 Linker 本地
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

            // 御空攻击连击窗口倒计时
            if (isAirComboWindowOpen)
            {
                airComboWindowTimer -= Time.deltaTime;
                if (airComboWindowTimer <= 0f)
                {
                    ResetAirCombo();
                }
            }
        }
        #endregion

        #region 攻击
        // 地面普通攻击可用性判断
        public bool IsAttackable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            return isCanInterrupt && isGrounded && !IsFloating;
        }

        // 初始化地面普攻攻击段：连击窗口开启时进入下一段，否则回到第一段
        public AttackStep InitializeNormalAttackStep()
        {
            AttackStep step = InitializeComboStep(AttackSteps, ref currentComboCount, ref isComboWindowOpen);
            currentStep = step;
            attackLogic?.SetCurrentStep(step);
            return step;
        }

        // 进入地面普攻连击窗口：第四段普攻命中后开启 ESkill2 派生窗口
        public void StartNormalComboWindow()
        {
            isComboWindowOpen = true;
            comboWindowTimer = attackLogic != null ? attackLogic.GetComboWindowDuration(currentStep) : 0f;

            if (currentComboCount == 3)
            {
                OpenESkill2Window();
            }
        }

        // 重置地面普攻连段
        public void ResetNormalCombo()
        {
            currentComboCount = 0;
            comboWindowTimer = 0f;
            isComboWindowOpen = false;
        }
        #endregion

        #region 重击
        // 地面重击可用性判断
        public bool IsHeavyAttackable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            bool hasEnoughStamina = context == null || context.PlayerStamina == null || context.PlayerStamina.CanHeavyAttack();
            return isCanInterrupt && isGrounded && !IsFloating && hasEnoughStamina;
        }
        // 消耗地面重击体力
        public bool TryConsumeHeavyAttackStamina()
        {
            // 没有体力系统时默认允许，避免测试场景被体力组件阻塞
            bool hasStaminaSystem = context != null && context.PlayerStamina != null;
            return !hasStaminaSystem || context.PlayerStamina.TryConsumeHeavyAttack();
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
            return isCanInterrupt && isInTheAir && !IsFloating;
        }

        // 初始化下落攻击步骤：今汐当前固定三段下落攻击（起手 / 循环 / 收尾）
        public void InitializeFallAttackSteps(out AttackStep stepStart, out AttackStep stepLoop, out AttackStep stepEnd)
        {
            stepStart = null;
            stepLoop = null;
            stepEnd = null;

            if (FallAttackSteps.Count < 3)
            {
                Debug.LogError("FallAttackSteps 配置数量不足，至少需要 3 段！");
                return;
            }

            stepStart = GetAttackStepAt(FallAttackSteps, 0);
            stepLoop = GetAttackStepAt(FallAttackSteps, 1);
            stepEnd = GetAttackStepAt(FallAttackSteps, 2);

            currentStep = stepEnd != null ? stepEnd : stepStart;
            attackLogic?.SetCurrentStep(currentStep);
        }
        #endregion

        #region 御空攻击
        // 御空攻击可用性判断
        public bool IsAirAttackable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            return isCanInterrupt && IsFloating;
        }

        // 初始化御空攻击攻击段：连击窗口开启时进入下一段，否则回到第一段
        public AttackStep InitializeAirAttackStep()
        {
            AttackStep step = InitializeComboStep(SkillAirAttackSteps, ref currentAirComboCount, ref isAirComboWindowOpen);
            currentStep = step;
            attackLogic?.SetCurrentStep(step);
            return step;
        }

        // 进入御空攻击连击窗口：第四段御空攻击命中后开启 ESkill4 派生窗口
        public void StartAirComboWindow()
        {
            isAirComboWindowOpen = true;
            airComboWindowTimer = attackLogic != null ? attackLogic.GetComboWindowDuration(currentStep) : 0f;

            if (currentAirComboCount == 3)
            {
                OpenESkill4Window();
            }
        }

        // 重置御空攻击连段
        public void ResetAirCombo()
        {
            currentAirComboCount = 0;
            airComboWindowTimer = 0f;
            isAirComboWindowOpen = false;
        }
        #endregion

        #region 战技
        // 战技可用性判断：优先按 ESkill4 → ESkill3 → ESkill2 → ESkill1 顺序判定
        public bool IsESkillable()
        {
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            if (!isCanInterrupt)
            {
                return false;
            }

            if (CanUseESkill4)
            {
                return true;
            }

            if (CanUseESkill3)
            {
                return true;
            }

            if (CanUseESkill2)
            {
                return true;
            }

            return CanUseESkill1();
        }
        // 地面一段 E 技可用性判断
        public bool CanUseESkill1()
        {
            bool isCDOver = ESkill1CDTimer <= 0f;
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            return isCDOver && isGrounded;
        }
        // 初始化战技攻击段：优先按 ESkill4 → ESkill3 → ESkill2 → ESkill1 顺序选择
        public AttackStep InitializeESkillStep()
        {
            if (ESkillAttackSteps.Count == 0)
            {
                Debug.LogError("ESkillAttackSteps 配置为空！");
                return null;
            }

            AttackStep step = null;

            if (CanUseESkill4)
            {
                step = GetAttackStepAt(ESkillAttackSteps, 3);
            }
            else if (CanUseESkill3)
            {
                step = GetAttackStepAt(ESkillAttackSteps, 2);
            }
            else if (CanUseESkill2)
            {
                step = GetAttackStepAt(ESkillAttackSteps, 1);
            }
            else if (CanUseESkill1())
            {
                step = GetAttackStepAt(ESkillAttackSteps, 0);
            }

            if (step == null)
            {
                Debug.LogError("ESkillAttackSteps 缺少当前可用战技攻击段配置！");
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
        // ESkill1 使用后：进入冷却，并关闭 ESkill2 派生窗口
        public void OnESkill1Used()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.jinxiESkill1CDTimer = eSkill1CD;
            RuntimeData.jinxiIsESkill2WindowOpen = false;
            RuntimeData.jinxiESkill2WindowTimer = 0f;
            NotifySkillUIChanged();
        }

        // ESkill2 使用后：关闭 ESkill2 派生窗口，并开启 ESkill3 派生窗口与御空状态
        public void OnESkill2Used()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.jinxiIsESkill2WindowOpen = false;
            RuntimeData.jinxiESkill2WindowTimer = 0f;

            OpenESkill3Window();
            SetFloating(true);
            NotifySkillUIChanged();
        }

        // ESkill3 使用后：关闭 ESkill3 派生窗口
        public void OnESkill3Used()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.jinxiIsESkill3WindowOpen = false;
            RuntimeData.jinxiESkill3WindowTimer = 0f;
            NotifySkillUIChanged();
        }

        // ESkill4 使用后：收束空中派生链，关闭 ESkill3 / ESkill4 两个窗口
        public void OnESkill4Used()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.jinxiIsESkill3WindowOpen = false;
            RuntimeData.jinxiIsESkill4WindowOpen = false;
            RuntimeData.jinxiESkill3WindowTimer = 0f;
            RuntimeData.jinxiESkill4WindowTimer = 0f;
            NotifySkillUIChanged();
        }

        // Q 爆发使用后：进入爆发冷却
        public void OnQBurstUsed()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.jinxiQBurstCDTimer = qBurstCD;
            NotifySkillUIChanged();
        }
        #endregion

        #region 技能窗口与御空控制
        // 开启 ESkill2 派生窗口（由地面普攻第四段触发）
        public void OpenESkill2Window()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.jinxiIsESkill2WindowOpen = true;
            RuntimeData.jinxiESkill2WindowTimer = eSkill2WindowDuration;
            NotifySkillUIChanged();
        }

        // 开启 ESkill3 派生窗口（由 ESkill2 使用后触发）
        public void OpenESkill3Window()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.jinxiIsESkill3WindowOpen = true;
            RuntimeData.jinxiESkill3WindowTimer = eSkill3WindowDuration;
            NotifySkillUIChanged();
        }

        // 开启 ESkill4 派生窗口（由御空第四段攻击触发）
        public void OpenESkill4Window()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.jinxiIsESkill4WindowOpen = true;
            RuntimeData.jinxiESkill4WindowTimer = eSkill4WindowDuration;
            NotifySkillUIChanged();
        }

        // 设置御空状态，并通过事件总线通知表现层同步更新
        public void SetFloating(bool value)
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.jinxiIsFloating = value;
            RuntimeData.jinxiFloatingTimer = value ? floatingDuration : 0f;

            if (attackLogic != null)
            {
                GameEvents.RaiseFloatingChanged(attackLogic, RuntimeData.jinxiIsFloating);
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

            // 地面普攻连段重置：属于切人即丢的短命战斗状态，继续保留在 Linker 本地
            currentComboCount = 0;
            comboWindowTimer = 0f;
            isComboWindowOpen = false;

            // 御空攻击连段重置：属于切人即丢的短命战斗状态，继续保留在 Linker 本地
            currentAirComboCount = 0;
            airComboWindowTimer = 0f;
            isAirComboWindowOpen = false;

            // 中档运行时技能状态重置：迁移到 CharacterRuntimeData，保证角色禁用后数据仍可保留
            if (RuntimeData != null)
            {
                RuntimeData.jinxiESkill1CDTimer = 0f;
                RuntimeData.jinxiQBurstCDTimer = 0f;

                RuntimeData.jinxiESkill2WindowTimer = 0f;
                RuntimeData.jinxiESkill3WindowTimer = 0f;
                RuntimeData.jinxiESkill4WindowTimer = 0f;
                RuntimeData.jinxiFloatingTimer = 0f;

                RuntimeData.jinxiIsESkill2WindowOpen = false;
                RuntimeData.jinxiIsESkill3WindowOpen = false;
                RuntimeData.jinxiIsESkill4WindowOpen = false;
                RuntimeData.jinxiIsFloating = false;
            }
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

