using System;
using UnityEngine;


namespace WutheringWaves
{
    #region 角色状态枚举
    //角色核心状态枚举
    public enum CharacterState
    {
        JinxiIdle,              // 待机状态
        JinxiMove,        // 移动状态
        JinxiStop,       // 收步状态

        JinxiJump,      // 跳跃状态
        JinxiFall,         // 坠落状态
        JinxiLand,       // 着地状态

        JinxiAttack,    // 攻击状态
        JinxiHeavyAttack,//重击状态
        JinxiFallAttack,    //下落攻击状态
        JinxiAirAttack,     //御空攻击状态


        JinxiDash,        // 冲刺状态
        JinxiAirDash,    // 空中冲刺状态
        JinxiFloatDash,//御空冲刺状态

        JinxiDodge,    //闪避状态
        JinxiFloatDodge,//御空闪避状态

        JinxiESkill,         //战技状态
        JinxiQBurst,         //爆发状态
        JinxiQteSkill,       //延奏技能状态


        JinxiHit,        // 受击状态
        JinxiDead,       // 死亡状态
///////////////////////////////////////
        KatixiyaIdle,              // 待机状态
        KatixiyaMove,        // 移动状态
        KatixiyaStop,       // 收步状态

        KatixiyaJump,      // 跳跃状态
        KatixiyaFall,         // 坠落状态
        KatixiyaLand,       // 着地状态

        KatixiyaAttack,    // 攻击
        KatixiyaHeavyAttack,//重击
        KatixiyaFallAttack,    //下落攻击
        KatixiyaAirAttack,     //御空攻击


        KatixiyaDash,        // 冲刺状态
        KatixiyaAirDash,    // 空中冲刺状态
        KatixiyaFloatDash,//御空冲刺状态

        KatixiyaDodge,    //闪避状态
        KatixiyaFloatDodge,//御空闪避状态


        KatixiyaESkill,         //战技状态
        KatixiyaQBurst,         //爆发状态
        KatixiyaQteSkill,       //延奏技能状态


        KatixiyaHit,        // 受击状态
        KatixiyaDead,       // 死亡状态


        Error
    }
    #endregion

    #region 状态机核心类（FSM总调度，上下文管理，状态切换核心）
    /// 角色状态机核心
    /// 负责：1.管理所有共享数据/组件依赖 2.调度状态生命周期 3.提供状态切换核心方法 4.对外暴露状态信息
    public class CharacterStateMachine : MonoBehaviour
    {
        #region 1. 核心依赖配置（由CharacterContext自动注入，与新架构对接）
        [Header("=== 核心依赖（由CharacterContext自动注入，无需手动赋值）===")]
        [Header("角色共享上下文")]
        public CharacterContext context;
        [Header("角色静态模板数据")]
        public CharacterDataSO characterData;
        [Header("角色运行时数据")]
        public CharacterRuntimeData runtimeData;
        [Header("动画控制器")]
        public Animator Animator;
        [Header("输入读取器")]
        public PlayerInputReader PlayerInputReader;
        [Header("输入缓冲器")]
        public InputBuffer InputBuffer;
        [Header("角色控制器（物理移动）")]
        public CharacterController characterController;
        [Header("移动逻辑脚本")]
        public CharacterMovement movementLogic;    
        [Header("攻击处理器")]
        public CharacterAttack attackLogic;
        [Header("特效处理器")]
        public EffectController effectController;
        [Header("效果表现处理器")]
        public CharacterManifestation manifestation;

        private ICharacterStateMachineDriver stateMachineDriver;

        [Header("===角色特殊===")]
        [Header("今汐")]
        private JinxiSpecialSkillLinker jinxiSpecialSkillLinker;
        [Header("卡提希娅")]
        private KatixiyaSpecialSkillLinker katixiyaSpecialSkillLinker;

        #endregion

        #region 2. FSM核心组件
        public CharacterStateFactory StateFactory { get; private set; } // 状态工厂实例
        public CharacterBaseState CurrentState { get; private set; }   // 当前状态实例（实际执行逻辑）
        public CharacterState CurrentStateType { get; private set; }   // 当前状态类型（对外暴露的枚举，方便查询）

        public CharacterState PreviousStateType { get; private set; }// 上一个状态类型
        public CharacterState PreviousPreviousStateType { get; private set; }// 上上一个状态类型

        #endregion

        #region 3. 状态共享数据
        public AttackStep currentStep;
        //是否状态锁定
        public bool IsStateLocked { get; set; } = false;

        //外部只读
        public ICharacterStateMachineDriver StateMachineDriver => stateMachineDriver;
        public JinxiSpecialSkillLinker JinxiSpecialSkillLinker => jinxiSpecialSkillLinker;
        public KatixiyaSpecialSkillLinker KatixiyaSpecialSkillLinker => katixiyaSpecialSkillLinker;

        #endregion

        #region 4. 初始化（由CharacterContext调用，保证初始化顺序）
        // 状态机初始化（由CharacterContext统一调用，注入所有核心依赖）
        public void Initialize(CharacterContext context)
        {
            // 注入依赖
            this.context = context;
            characterData = context != null ? context.CharacterDataSO : null;
            runtimeData = context != null ? context.CharacterRuntimeData : null;
            Animator = context != null ? context.Animator : null;
            characterController = context != null ? context.CharacterController : null;
            movementLogic = context != null ? context.MovementLogic : null;
            PlayerInputReader = context != null ? context.PlayerInputReader : null;
            InputBuffer = context != null ? context.InputBuffer : null;
            attackLogic = context != null ? context.AttackLogic : null;
            effectController = context != null ? context.EffectController : null;
            manifestation = context != null ? context.Manifestation : null;

            // 创建状态工厂 → 注册角色特殊状态 → 获取默认状态 → 进入初始状态
            StateFactory = new CharacterStateFactory(this);
            stateMachineDriver?.RegisterSpecialStates(StateFactory, this);

            // 初始化上一状态为当前角色默认待机，避免不同角色共用一套初始状态枚举
            CharacterState defaultStateType = ResolveDefaultStateType();
            PreviousStateType = defaultStateType;
            PreviousPreviousStateType = defaultStateType;

            SwitchState(GetDefaultState());
        }
        private CharacterBaseState GetDefaultState()
        {
            CharacterBaseState defaultState = StateFactory.GetState(CharacterState.Error);
            if (characterData.characterName == CharacterName.今汐)
            {
                defaultState = StateFactory.GetState(CharacterState.JinxiIdle);
            }
            else if (characterData.characterName == CharacterName.卡提希娅)
            {
                defaultState = StateFactory.GetState(CharacterState.KatixiyaIdle);
            }

            if (defaultState == null)
            {
                throw new ArgumentException("状态工厂未注册 相关 状态，无法获取默认状态。");
            }

            return defaultState;
        }

        // 根据角色名解析默认状态类型，供初始化上一状态记录使用
        private CharacterState ResolveDefaultStateType()
        {
            if (characterData == null)
            {
                return CharacterState.Error;
            }

            return characterData.characterName switch
            {
                CharacterName.今汐 => CharacterState.JinxiIdle,
                CharacterName.卡提希娅 => CharacterState.KatixiyaIdle,
                _ => CharacterState.Error
            };
        }
        #endregion

        #region 5. FSM核心方法：状态切换（唯一入口，自动执行生命周期）
        /// <summary>
        /// 状态切换核心方法（私有，仅内部/基类通过工厂调用）
        /// 自动执行：原状态Exit → 新状态Enter → 更新当前状态信息
        /// </summary>
        /// <param name="newState">目标状态实例</param>
        internal void SwitchState(CharacterBaseState newState)
        {
            // 状态锁定/新状态为空，禁止切换
            if (IsStateLocked || newState == null) return;
            PreviousPreviousStateType = PreviousStateType;
            PreviousStateType = GetCurrentStateType(CurrentState);
           
            var oldState = CurrentStateType;
            // 原状态退出
            CurrentState?.ExitState();
            // 更新当前状态
            CurrentState = newState;
            CurrentStateType = GetCurrentStateType(newState);
            // 状态变化事件走全局总线，减少模块硬耦合
            GameEvents.RaiseCharacterStateChanged(this, oldState, CurrentStateType);
            // 进入新状态
            CurrentState.EnterState();
        }
        // 根据状态实例获取对应的枚举类型（内部辅助方法）
        private CharacterState GetCurrentStateType(CharacterBaseState state)
        {
            return state switch
            {
                JinxiIdleState => CharacterState.JinxiIdle,// 待机
                JinxiMoveState => CharacterState.JinxiMove,// 移动
                JinxiStopState => CharacterState.JinxiStop,// 收步状态

                JinxiJumpState => CharacterState.JinxiJump,// 跳跃
                JinxiFallState => CharacterState.JinxiFall, // 空中下落/滞空阶段
                JinxiLandState => CharacterState.JinxiLand,// 着地状态

                JinxiAttackState => CharacterState.JinxiAttack,// 攻击
                JinxiHeavyAttackState => CharacterState.JinxiHeavyAttack,//重击
                JinxiFallAttackState => CharacterState.JinxiFallAttack,//下落攻击
                JinxiAirAttackState => CharacterState.JinxiAirAttack,//空中攻击

                JinxiDashState => CharacterState.JinxiDash,// 冲刺
                JinxiAirDashState => CharacterState.JinxiAirDash,// 空中冲刺
                JinxiFloatDashState => CharacterState.JinxiFloatDash,//御空冲刺

                JinxiDodgeState => CharacterState.JinxiDodge,//闪避
                JinxiFloatDodgeState => CharacterState.JinxiFloatDodge,//御空闪避

                JinxiESkillState => CharacterState.JinxiESkill,//战技
                JinxiQBurstState => CharacterState.JinxiQBurst,//爆发
                JinxiQteSkillState => CharacterState.JinxiQteSkill,//延奏技能

                JinxiHitState => CharacterState.JinxiHit,// 受击
                JinxiDeadState => CharacterState.JinxiDead,// 死亡




                KatixiyaIdleState => CharacterState.KatixiyaIdle,// 待机
                KatixiyaMoveState => CharacterState.KatixiyaMove,// 移动
                KatixiyaStopState => CharacterState.KatixiyaStop,// 收步

                KatixiyaJumpState => CharacterState.KatixiyaJump,// 跳跃
                KatixiyaFallState => CharacterState.KatixiyaFall, // 坠落
                KatixiyaLandState => CharacterState.KatixiyaLand,// 着地状态

                KatixiyaAttackState => CharacterState.KatixiyaAttack,// 攻击
                KatixiyaHeavyAttackState => CharacterState.KatixiyaHeavyAttack,//重击
                KatixiyaFallAttackState => CharacterState.KatixiyaFallAttack,//下落攻击
                KatixiyaAirAttackState => CharacterState.KatixiyaAirAttack,//空中攻击

                KatixiyaDashState => CharacterState.KatixiyaDash,// 冲刺
                KatixiyaAirDashState => CharacterState.KatixiyaAirDash,// 空中冲刺
                KatixiyaFloatDashState => CharacterState.KatixiyaFloatDash,//御空冲刺

                KatixiyaDodgeState => CharacterState.KatixiyaDodge,//闪避
                KatixiyaFloatDodgeState => CharacterState.KatixiyaFloatDodge,//御空闪避

                KatixiyaESkillState => CharacterState.KatixiyaESkill,//战技
                KatixiyaQBurstState => CharacterState.KatixiyaQBurst,//爆发
                KatixiyaQteSkillState => CharacterState.KatixiyaQteSkill,//延奏技能


                KatixiyaHitState => CharacterState.KatixiyaHit,// 受击
                KatixiyaDeadState => CharacterState.KatixiyaDead,// 死亡
                
                

                _ => CharacterState.Error
            };
        }
        #endregion

        #region 6. 生命周期（帧更新，传递给当前状态）
        private void Update()
        {
            // 死亡状态/状态为空，直接返回
            if ((CurrentStateType == CharacterState.JinxiDead || CurrentStateType == CharacterState.KatixiyaDead) || CurrentState == null) return;

            // 1.执行当前状态的帧更新逻辑
            CurrentState.UpdateState();

        }


        #endregion

        #region 7. 公共方法判断方法
        // 强制回到当前角色默认状态（当前今汐/卡提希娅都是待机）
        public void ForceResetToDefaultState()
        {
            // 1.空值检查
            if (StateFactory == null || characterData == null)
            {
                return;
            }

            // 2.解除状态锁，避免切人时因为旧状态锁定导致无法切回默认状态
            IsStateLocked = false;

            //// 3.清空当前角色输入状态，避免切人瞬间把旧输入带进新角色
            //ResetInputState();先别清

            // 4.切回当前角色默认状态
            SwitchState(GetDefaultState());
        }

        // 判断当前状态是否可被打断
        public bool IsInterruptible()
        {
            return CurrentStateType != CharacterState.JinxiDodge
                && CurrentStateType != CharacterState.KatixiyaDodge
                && CurrentStateType != CharacterState.JinxiQBurst
                && CurrentStateType != CharacterState.KatixiyaQBurst
                && CurrentStateType != CharacterState.JinxiHit
                && CurrentStateType != CharacterState.KatixiyaHit
                && CurrentStateType != CharacterState.JinxiDead
                && CurrentStateType != CharacterState.KatixiyaDead;
        }

        // 判断当前角色是否注册了目标状态：供后续状态转换判断做安全拦截
        public bool HasState(CharacterState stateType)
        {
            return StateFactory != null && StateFactory.HasState(stateType);
        }

        // 仅在血量发生下降时触发一次受击请求，避免“掉过血后永久受击”
        public bool TryConsumeHitRequest()
        {
            return false;
        }

   

        // 兼容旧状态实现的输入读取代理：后续状态迁移时统一从StateMachine访问
        public Vector2 MoveInput => PlayerInputReader != null ? PlayerInputReader.MoveInput : Vector2.zero;
        public bool IsHoldingRun => InputBuffer != null && InputBuffer.IsHoldingRun;
        public bool WantsToDash => InputBuffer != null && InputBuffer.WantsToDash;

        public bool CheckAndConsumeJumpRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeJumpRequest();
        public bool CheckAndConsumeDashRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeDashRequest();
        public bool CheckAndConsumeAttackRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeAttackRequest();
        public bool CheckAndConsumeFallAttackRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeFallAttackRequest();
        public bool CheckAndConsumeHeavyAttackRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeHeavyAttackRequest();
        public bool CheckAndConsumeAirAttackRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeAirAttackRequest();
        public bool CheckAndConsumeESkillRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeESkillRequest();
        public bool CheckAndConsumeQBurstRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeQBurstRequest();
        public bool CheckAndConsumeQteSkillRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeQteSkillRequest();


        public void CleanWantsToJumpRequest() => InputBuffer?.CleanWantsToJumpRequest();
        public void CleanWantsToDashRequest() => InputBuffer?.CleanWantsToDashRequest();
        public void CleanWantsToAttackRequest() => InputBuffer?.CleanWantsToAttackRequest();
        public void CleanWantsToFallAttackRequest() => InputBuffer?.CleanWantsToFallAttackRequest();
        public void CleanWantsToHeavyAttackRequest() => InputBuffer?.CleanWantsToHeavyAttackRequest();
        public void CleanWantsToAirAttackRequest() => InputBuffer?.CleanWantsToAirAttackRequest();
        public void CleanWantsToESkillRequest() => InputBuffer?.CleanWantsToESkillRequest();
        public void CleanWantsToQBurstRequest() => InputBuffer?.CleanWantsToQBurstRequest();
        public void CleanWantsToQteSkillRequest() => InputBuffer?.CleanWantsToQteSkillRequest();


        // 重置输入层状态（死亡/复活等需要清空请求时使用）
        public void ResetInputState()
        {
            PlayerInputReader?.ResetInputStates();
            InputBuffer?.ResetAllRequests();
        }

        #endregion

        #region 8.角色专属
        // 由外部组合入口注入角色专属状态机驱动，状态机自身不直接查找角色专属模块
        public void SetStateMachineDriver(ICharacterStateMachineDriver driver)
        {
            stateMachineDriver = driver;
        }

        // 由外部组合入口显式注入今汐专属玩法驱动，避免继续把角色玩法逻辑塞进状态机驱动接口
        public void SetJinxiSpecialSkillLinker(JinxiSpecialSkillLinker linker)
        {
            jinxiSpecialSkillLinker = linker;
        }

        // 由外部组合入口显式注入卡提希娅专属玩法驱动，避免状态机继续直接查找角色专属模块
        public void SetKatixiyaSpecialSkillLinker(KatixiyaSpecialSkillLinker linker)
        {
            katixiyaSpecialSkillLinker = linker;
        }
        #endregion

        #region 9. Gizmo绘制（编辑器下显示当前状态）
        // 单个 Gizmo 文本的配置类
        [System.Serializable]
        public class GizmoLabelSettings
        {
            [Tooltip("是否显示此信息")]
            public bool show = true;

            [Tooltip("文字颜色")]
            public Color color = Color.yellow;

            [Tooltip("相对于角色头顶的 Y 轴偏移（上下）")]
            public float yOffset = 0f;

            [Tooltip("相对于角色的 X 轴偏移（左右，正右负左）")]
            public float xOffset = 0f;

            [Tooltip("字体大小")]
            public int fontSize = 12;

            [Tooltip("字体样式")]
            public FontStyle fontStyle = FontStyle.Bold;
        }

        [Header("=== Gizmo 总开关 ===")]
        [Tooltip("是否显示所有调试 Gizmo 文字")]
        [SerializeField] private bool showAllGizmos = true;

        [Header("=== 各个 Gizmo 独立配置 ===")]
        [SerializeField] private GizmoLabelSettings currentStateGizmo = new GizmoLabelSettings { yOffset = 2.0f, color = Color.yellow };
        [SerializeField] private GizmoLabelSettings previousStateGizmo = new GizmoLabelSettings { yOffset = 1.7f, color = Color.cyan };
        [SerializeField] private GizmoLabelSettings previousPreviousStateGizmo = new GizmoLabelSettings { yOffset = 1.1f, color = Color.magenta };
        [SerializeField] private GizmoLabelSettings groundedStateGizmo = new GizmoLabelSettings { yOffset = 1.4f, color = Color.green };

        // 编辑器下实时绘制 Gizmo（运行/编辑模式均生效）
        private void OnDrawGizmos()
        {
            // 总开关关闭 / 空引用直接返回
            if (!showAllGizmos || this == null) return;

#if UNITY_EDITOR
            // 1. 绘制当前状态
            if (currentStateGizmo.show)
            {
                DrawGizmoLabel(
                    $"当前状态：{CurrentStateType}",
                    currentStateGizmo
                );
            }

            // 2. 绘制上一状态
            if (previousStateGizmo.show)
            {
                DrawGizmoLabel(
                    $"上一状态：{PreviousStateType}",
                    previousStateGizmo
                );
            }
            // 3. 绘制上上状态
            if (previousPreviousStateGizmo.show)
            {
                DrawGizmoLabel(
                    $"上上状态：{PreviousPreviousStateType}",
                    previousPreviousStateGizmo
                );
            }

            // 4. 绘制接地状态
            if (groundedStateGizmo.show && characterController != null)
            {
                //string groundedText = characterController.isGrounded ? "接地：True" : "接地：False";
                string groundedText = movementLogic.CustomCheckGrounded() ? "接地：True" : "接地：False";
                // 接地时显示绿色，未接地显示红色（动态覆盖颜色）
                Color tempColor = groundedStateGizmo.color;
                groundedStateGizmo.color = movementLogic.CustomCheckGrounded() ? Color.green : Color.red;

                DrawGizmoLabel(groundedText, groundedStateGizmo);

                groundedStateGizmo.color = tempColor; // 恢复配置颜色
            }
#endif
        }

        // 辅助方法：绘制单个 Gizmo 标签
        private void DrawGizmoLabel(string text, GizmoLabelSettings settings)
        {
#if UNITY_EDITOR
            // 1. 计算绘制位置
            Vector3 basePos = transform.position + Vector3.up * settings.yOffset;
            // 应用 X 轴偏移（相对于角色右方）
            Vector3 finalPos = basePos + transform.right * settings.xOffset;

            // 2. 设置样式
            GUIStyle style = new GUIStyle
            {
                fontSize = settings.fontSize,
                fontStyle = settings.fontStyle,
                normal = { textColor = settings.color }
            };

            // 3. 绘制文字
            UnityEditor.Handles.color = settings.color;
            UnityEditor.Handles.Label(finalPos, text, style);
#endif
        }
        #endregion

        #region 10.动画查询
        // 根据移动动画ID获取动画名称
        public string GetLocomotionAnimationName(LocomotionAnimationId animationId)
        {
            //1.空值检查
            if (characterData == null || characterData.animationConfigSO == null)
            {
                return string.Empty;
            }

            //2.从动画配置中获取移动动画名称
            return characterData.animationConfigSO.GetLocomotionAnimationName(animationId);
        }

        // 根据移动动画ID获取动画长度
        public float GetLocomotionAnimationLength(LocomotionAnimationId animationId)
        {
            //1.空值检查
            if (characterData == null || characterData.animationConfigSO == null)
            {
                return 0f;
            }

            //2.从动画配置中获取移动动画长度
            return characterData.animationConfigSO.GetLocomotionAnimationLength(animationId);
        }

        // 根据攻击动画ID获取战斗动画名称
        public string GetCombatAnimationName(AttackId attackId)
        {
            //1.空值检查
            if (characterData == null || characterData.animationConfigSO == null)
            {
                return string.Empty;
            }

            //2.从动画配置中获取战斗动画名称
            return characterData.animationConfigSO.GetCombatAnimationName(attackId);
        }

        // 根据攻击动画ID获取战斗动画长度
        public float GetCombatAnimationLength(AttackId attackId)
        {
            //1.空值检查
            if (characterData == null || characterData.animationConfigSO == null)
            {
                return 0f;
            }

            //2.从动画配置中获取战斗动画长度
            return characterData.animationConfigSO.GetCombatAnimationLength(attackId);
        }


        #endregion

    }
    #endregion


}






