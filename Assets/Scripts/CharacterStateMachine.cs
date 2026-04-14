using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEditor;


//using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SocialPlatforms;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace WutheringWaves
{
    #region 角色状态枚举
    //角色核心状态枚举
    public enum CharacterState
    {
        Idle,              // 待机
        Moving,        // 移动

        Jumping,      // 跳跃
        Falling,         // 空中下落/滞空阶段

        Attacking,    // 攻击
        HeavyAttacking,//重击
        FallAttacking,    //下落攻击
        AirAttacking,     //御空攻击


        Dashing,        // 冲刺
        AirDashing,    // 空中冲刺
        FloatDashing,//御空冲刺

        Dodging,    //闪避
        FloatDodging,//御空闪避

        ESkill,         //战技
        QBurst,         //爆发

        Hit,        // 受击
        Dead,       // 死亡
        Transition,// 过渡动画状态
        Error
    }
    #endregion

    #region 状态抽象基类（定义所有状态的核心生命周期规范）
    // 所有角色状态的抽象基类
    public abstract class CharacterBaseState
    {
        #region 核心依赖（所有状态可访问的上下文）
        protected CharacterStateMachine Context; // 状态机核心（上下文，存储所有共享数据/组件）
        protected CharacterStateFactory Factory; // 状态工厂（用于状态切换时获取目标状态实例）
        #endregion

        #region 构造函数（注入依赖，所有子类必须实现）
        protected CharacterBaseState(CharacterStateMachine currentContext, CharacterStateFactory characterStateFactory)
        {
            Context = currentContext;
            Factory = characterStateFactory;
        }
        #endregion

        #region 状态生命周期（抽象方法，子类必须实现具体逻辑）
        /// <summary>进入状态时执行（仅调用一次，初始化状态数据/动画/参数）</summary>
        public abstract void EnterState();
        /// <summary>状态帧更新逻辑（每帧调用，处理状态核心业务）</summary>
        public abstract void UpdateState();
        /// /// <summary>退出状态时执行（仅调用一次，清理状态数据）</summary>
        public abstract void ExitState();
        #endregion

        #region 状态切换辅助方法（子类通过此方法切换状态，统一入口）
        /// <summary>切换到目标状态（子类内部调用，自动触发生命周期）</summary>
        /// <param name="targetState">目标状态枚举</param>
        protected void SwitchState(CharacterState targetState)
        {
            Context.SwitchState(Factory.GetState(targetState));
        }
        #endregion

        #region 通用判断方法（所有状态可复用的基础判断，封装在基类）
        //// 判断是否为可打断状态（非闪避/非受击/非死亡可被大部分状态打断）
        //protected bool IsInterruptible()
        //{
        //    return Context.CurrentStateType != CharacterState.Dodging&&Context.CurrentStateType != CharacterState.Hit && Context.CurrentStateType != CharacterState.Dead;
        //}
        //// 判断地面冲刺是否可用
        //protected bool IsDashAvailable()
        //{
        //    // 条件1：是否为可打断状态（非受击/非死亡，基础状态判断）
        //    bool isCanInterrupt = IsInterruptible();
        //    // 条件2：必须在地面
        //    bool isGrounded =!Context.CheckFallTransition();
        //    // 条件3：全局CD是否结束（两次连续冲刺后，必须等全局CD才能重新开始冲刺）
        //    bool isGlobalCDOver = Context.DashGlobalCDTimer <= 0;
        //    // 条件4：当前冲刺次数是否未达上限（最多连续2次，次数0/1时满足，2次则触发全局CD并重置）
        //    bool isUnderMaxCount = Context.DashCount < 2;
        //    // 条件5：内置CD是否结束（两次连续冲刺之间的0.3s冷却，仅非首次冲刺需要判断）
        //    bool isInternalCDOver = Context.DashInternalCDTimer <= 0;

        //    return isCanInterrupt &&isGrounded&& isGlobalCDOver && isUnderMaxCount && (Context.DashCount == 0 || isInternalCDOver);
        //}
        ////判断空中冲刺是否可用
        //protected bool IsAirDashAvailable()
        //{
        //    // 条件1：是否为可打断状态（非闪避/非受击/非死亡，基础状态判断）
        //    bool isCanInterrupt = IsInterruptible();
        //    // 条件2：必须在空中
        //    bool isInAir = !Context.CharacterController.isGrounded;
        //    // 条件3：本次坠落还未被使用
        //    bool hasNotUsed = !Context.HasAirDashed;
            
        //    return isCanInterrupt && isInAir && hasNotUsed ;
        //}
        //// 判断跳跃是否可用
        //protected bool IsJumpAvailable()
        //{
        //    // 条件1：是否为可打断状态
        //    bool isCanInterrupt = IsInterruptible();
        //    return isCanInterrupt;
        //}
        #endregion
    }
    #endregion

    #region 状态工厂类（管理所有状态实例，单例化创建，避免重复实例化）
    // 负责创建并缓存所有具体状态实例，提供统一的状态获取接口
    public class CharacterStateFactory
    {
        #region 状态实例缓存（私有，仅工厂内部管理）
        private CharacterIdleState _idleState;// 待机
        private CharacterMovingState _movingState;// 移动
        private CharacterJumpingState _jumpingState; // 跳跃

        private CharacterFallingState _fallingState;// 空中下落/滞空阶段

        private CharacterAttackingState _attackingState;// 攻击
        private CharacterHeavyAttackingState _heavyAttackingState;//重击
        private CharacterFallAttackingState _fallAttackingState;//下落攻击
        private CharacterAirAttackingState _airAttackingState; //空中攻击

        private CharacterDashingState _dashingState;// 冲刺
        private CharacterAirDashingState _airDashingState;// 空中冲刺
        private CharacterFloatDashingState _floatDashingState;//御空冲刺

        private CharacterDodgingState _dodgingState; //闪避
        private CharacterFloatDodgingState _floatDodgingState;//御空闪避

        private CharacterESkillState _eSkillState;//战技
        private CharacterQBurstState _qBurstState;//爆发

        private CharacterHitState _hitState;// 受击
        private CharacterDeadState _deadState;// 死亡
        private CharacterTransitionState _transitionState; // 过渡动画状态
        #endregion

        #region 工厂上下文（状态机核心，传递给所有状态）
        private readonly CharacterStateMachine _context;
        #endregion

        #region 构造函数（初始化所有状态实例，仅执行一次）
        public CharacterStateFactory(CharacterStateMachine context)
        {
            _context = context;
            // 初始化所有状态实例，注入上下文和工厂自身
            _idleState = new CharacterIdleState(_context, this);// 待机
            _movingState = new CharacterMovingState(_context, this);// 移动
            _jumpingState = new CharacterJumpingState(_context, this);// 跳跃

            _fallingState = new CharacterFallingState(_context, this);// 空中下落/滞空阶段

            _attackingState = new CharacterAttackingState(_context, this);// 攻击
            _heavyAttackingState = new CharacterHeavyAttackingState(_context, this);//重击
            _fallAttackingState = new CharacterFallAttackingState(_context, this);//下落攻击
            _airAttackingState = new CharacterAirAttackingState(_context, this);//御空冲刺

            _dashingState = new CharacterDashingState(_context, this);// 冲刺
            _airDashingState = new CharacterAirDashingState(_context, this);// 空中冲刺
            _floatDashingState = new CharacterFloatDashingState(_context, this);//御空冲刺

            _dodgingState = new CharacterDodgingState(_context, this);//闪避
            _floatDodgingState = new CharacterFloatDodgingState(_context, this);//御空闪避

            _eSkillState = new CharacterESkillState(_context, this);//战技
            _qBurstState = new CharacterQBurstState(_context, this);//爆发

            _hitState = new CharacterHitState(_context, this);// 受击
            _deadState = new CharacterDeadState(_context, this);// 死亡
            _transitionState = new CharacterTransitionState(_context, this);// 过渡动画状态

        }
        #endregion

        #region 统一状态获取接口（外部仅通过此方法获取状态实例）
        /// <summary>根据状态枚举获取对应的状态实例</summary>
        /// <param name="stateType">目标状态枚举</param>
        /// <returns>对应的状态实例</returns>
        /// <exception cref="ArgumentOutOfRangeException">未定义的状态枚举</exception>
        public CharacterBaseState GetState(CharacterState stateType)
        {
            return stateType switch
            {
                CharacterState.Idle => _idleState,// 待机
                CharacterState.Moving => _movingState,// 移动
                CharacterState.Jumping => _jumpingState,// 跳跃

                CharacterState.Falling => _fallingState,// 空中下落/滞空阶段

                CharacterState.Attacking => _attackingState,// 攻击
                CharacterState.HeavyAttacking => _heavyAttackingState,//重击
                CharacterState.FallAttacking => _fallAttackingState,//下落攻击
                CharacterState.AirAttacking => _airAttackingState,//空中攻击

                CharacterState.Dashing => _dashingState,// 冲刺
                CharacterState.AirDashing => _airDashingState,// 空中冲刺
                CharacterState.FloatDashing => _floatDashingState,//御空冲刺

                CharacterState.Dodging => _dodgingState,//闪避
                CharacterState.FloatDodging => _floatDodgingState,//御空闪避

                CharacterState.ESkill => _eSkillState,//战技
                CharacterState.QBurst => _qBurstState,//爆发

                CharacterState.Hit => _hitState,// 受击
                CharacterState.Dead => _deadState,// 死亡
                CharacterState.Transition => _transitionState,// 过渡动画状态
                _ => throw new ArgumentOutOfRangeException(nameof(stateType), stateType, "未定义的角色状态枚举")
            };
        }
        #endregion

        #region 快捷获取默认状态（初始化时使用）
        /// <summary>获取角色默认初始状态（闲置）</summary>
        public CharacterBaseState GetDefaultState() => _idleState;
        #endregion
    }
    #endregion

    #region 状态机核心类（FSM总调度，上下文管理，状态切换核心）
    /// 角色状态机核心
    /// 负责：1.管理所有共享数据/组件依赖 2.调度状态生命周期 3.提供状态切换核心方法 4.对外暴露状态信息
    public class CharacterStateMachine : MonoBehaviour
    {
        #region 1. 核心依赖配置（由CharacterCore自动注入，与现有项目对接）
        [Header("=== 核心依赖（由CharacterCore自动注入，无需手动赋值）===")]
        [Tooltip("角色总控核心")]
        public CharacterCore _core;
        [Tooltip("角色数值数据")]
        public CharacterData characterData;
        [Tooltip("动画控制器")]
        public Animator Animator;
        [Tooltip("输入读取器")]
        public PlayerInputReader PlayerInputReader;
        [Tooltip("输入缓冲器")]
        public InputBuffer InputBuffer;
        [Tooltip("角色控制器（物理移动）")]
        public CharacterController characterController;
        [Tooltip("移动逻辑脚本")]
        public CharacterMovement movementLogic;    
        [Tooltip("攻击处理器")]
        public CharacterAttack attackLogic;
        [Tooltip("效果表现处理器")]
        public CharacterManifestation manifestation;
        #endregion

        #region 2. FSM核心组件
        public CharacterStateFactory StateFactory { get; private set; } // 状态工厂实例
        public CharacterBaseState CurrentState { get; private set; }   // 当前状态实例（实际执行逻辑）
        public CharacterState CurrentStateType { get; private set; }   // 当前状态类型（对外暴露的枚举，方便查询）

        public CharacterState PreviousStateType { get; private set; }// 上一个状态类型
        public CharacterState PreviousPreviousStateType { get; private set; }// 上上一个状态类型

        #endregion

        #region 3. 状态共享数据
        // 过渡动画参数（源状态切状态前赋值，Transition 状态消费）
        public TransitionParams CurrentTransitionParams;
        public AttackStep currentStep;
        //是否状态锁定
        public bool IsStateLocked { get; set; } = false;
        // 血量快照（用于边沿检测受击）
        private float _lastObservedHealth;
        #endregion

        #region 4. 初始化（由CharacterCore调用，保证初始化顺序）
        // 状态机初始化（CharacterCore中调用，注入所有核心依赖）
        public void Initialize(CharacterCore core, CharacterData data, Animator anim,
            CharacterController controller, CharacterMovement movement, PlayerInputReader playerInputReader, InputBuffer inputBuffer, CharacterAttack attack,CharacterManifestation Manifest)
        {
            // 注入依赖
            _core = core;
            characterData = data;
            Animator = anim;
            characterController = controller;
            movementLogic = movement;
            PlayerInputReader = playerInputReader;
            InputBuffer = inputBuffer;
            attackLogic = attack;
            manifestation = Manifest;
            // 创建状态工厂 → 获取默认状态 → 进入初始状态
            StateFactory = new CharacterStateFactory(this);
            //初始化上一状态为Idle
            PreviousStateType = CharacterState.Idle;
            PreviousPreviousStateType = CharacterState.Idle;
            _lastObservedHealth = characterData != null ? characterData.currentHealth : 0f;
            SwitchState(StateFactory.GetDefaultState());
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
                CharacterIdleState => CharacterState.Idle,// 待机
                CharacterMovingState => CharacterState.Moving,// 移动
                CharacterJumpingState => CharacterState.Jumping,// 跳跃

                CharacterFallingState => CharacterState.Falling, // 空中下落/滞空阶段

                CharacterAttackingState => CharacterState.Attacking,// 攻击
                CharacterHeavyAttackingState => CharacterState.HeavyAttacking,//重击
                CharacterFallAttackingState => CharacterState.FallAttacking,//下落攻击
                CharacterAirAttackingState => CharacterState.AirAttacking,//空中攻击

                CharacterDashingState => CharacterState.Dashing,// 冲刺
                CharacterAirDashingState => CharacterState.AirDashing,// 空中冲刺
                CharacterFloatDashingState => CharacterState.FloatDashing,//御空冲刺

                CharacterDodgingState => CharacterState.Dodging,//闪避
                CharacterFloatDodgingState => CharacterState.FloatDodging,//御空闪避

                CharacterESkillState => CharacterState.ESkill,//战技
                CharacterQBurstState => CharacterState.QBurst,//爆发

                CharacterHitState => CharacterState.Hit,// 受击
                CharacterDeadState => CharacterState.Dead,// 死亡
                CharacterTransitionState => CharacterState.Transition,// 过渡动画状态
                _ => CharacterState.Error
            };
        }
        #endregion

        #region 6. 生命周期（帧更新，传递给当前状态）
        private void Update()
        {
            // 死亡状态/状态为空，直接返回
            if (CurrentStateType == CharacterState.Dead || CurrentState == null) return;

            // 1.执行当前状态的帧更新逻辑
            CurrentState.UpdateState();

        }
     

        #endregion

        #region 7. 公共方法判断方法

        // 判断当前状态是否可被打断
        public bool IsInterruptible()
        {
            return CurrentStateType != CharacterState.Dodging
                && CurrentStateType != CharacterState.QBurst
                && CurrentStateType != CharacterState.Hit
                && CurrentStateType != CharacterState.Dead;
        }

        // 仅在血量发生下降时触发一次受击请求，避免“掉过血后永久受击”
        public bool TryConsumeHitRequest()
        {
            CharacterData data = _core?.GetCharacterData();
            if (data == null) return false;

            float currentHealth = data.currentHealth;
            bool hasTakenDamage = currentHealth < _lastObservedHealth;
            _lastObservedHealth = currentHealth;

            return hasTakenDamage && currentHealth > 0f;
        }

        // 兼容旧状态实现的输入读取代理：后续状态迁移时统一从StateMachine访问
        public Vector2 MoveInput => PlayerInputReader != null ? PlayerInputReader.MoveInput : Vector2.zero;
        public bool IsHoldingRun => InputBuffer != null && InputBuffer.IsHoldingRun;
        public bool WantsToDash => InputBuffer != null && InputBuffer.WantsToDash;

        public bool CheckAndConsumeJumpRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeJumpRequest();
        public bool CheckAndConsumeDashRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeDashRequest();
        public bool CheckAndConsumeAttackRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeAttackRequest();
        public bool CheckAndConsumeAirAttackRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeAirAttackRequest();
        public bool CheckAndConsumeESkillRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeESkillRequest();
        public bool CheckAndConsumeQBurstRequest() => InputBuffer != null && InputBuffer.CheckAndConsumeQBurstRequest();

        public void CleanWantsToJumpRequest() => InputBuffer?.CleanWantsToJumpRequest();
        public void CleanWantsToDashRequest() => InputBuffer?.CleanWantsToDashRequest();
        public void CleanWantsToAttackRequest() => InputBuffer?.CleanWantsToAttackRequest();
        public void CleanWantsToAirAttackRequest() => InputBuffer?.CleanWantsToAirAttackRequest();
        public void CleanWantsToESkilltRequest() => InputBuffer?.CleanWantsToESkilltRequest();

        // 重置输入层状态（死亡/复活等需要清空请求时使用）
        public void ResetInputState()
        {
            PlayerInputReader?.ResetInputStates();
            InputBuffer?.ResetAllRequests();
        }

        #endregion

        #region 8. Gizmo绘制（编辑器下显示当前状态）
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

        #region 9.过渡动画参数结构体
        // 过渡动画参数：用于源状态向 Transition 状态传递配置
        public struct TransitionParams
        {
            public string AnimationTrigger; // 动画 Trigger 名称（如 "Landing"）
            public float Duration;           // 动画固定时长（秒）
            public CharacterState DefaultNextState; // 无打断时，动画结束后切到的状态
        }
        #endregion

    }
    #endregion

    #region 具体状态实现类（所有状态继承自抽象基类，实现生命周期方法）

    #region 过渡动画状态
    // 过渡动画状态
    public class CharacterTransitionState : CharacterBaseState
    {
        private float _transitionTimer; // 过渡动画计时器

        public CharacterTransitionState(CharacterStateMachine context, CharacterStateFactory factory)
            : base(context, factory) { }

        public override void EnterState()
        {
            // 1. 初始化数据
            InitializeTransitionData();

            // 2. 播放进入过渡状态动画
            TransitionEnterAnimation();

            //3.检查移动急停初始化
            CheckMoveToIdle();

            //4.重置连招
            Context.attackLogic.ResetCombo();
        }

        #region EnterState子方法
        //1. 初始化数据
        private void InitializeTransitionData()
        {
            _transitionTimer = Context.CurrentTransitionParams.Duration;
        }
        // 2. 过渡状态动画
        private void TransitionEnterAnimation()
        {
             Context.Animator.SetTrigger(Context.CurrentTransitionParams.AnimationTrigger);
           // Context.Animator.CrossFadeInFixedTime(Context.CurrentTransitionParams.AnimationTrigger, 0.3f, 0,  0);
        }
        //3.检查 移动急停初始化
        private void CheckMoveToIdle()
        {
            if (Context.PreviousStateType == CharacterState.Moving)
            {
                //调用初始化
                if (Context.movementLogic.HasPressedShift())
                {
                    //奔跑缓冲
                    Context.movementLogic.InitializeStopping(Context.movementLogic.runStoppingDistance, Context.movementLogic.runStoppingTime);
                }
                else
                {
                    //移动缓冲
                    Context.movementLogic.InitializeStopping(Context.movementLogic.moveStoppingDistance, Context.movementLogic.moveStoppingTime);
                }
            }
        }
        #endregion

        public override void UpdateState()
        {
            //1.常态重力
            Context.movementLogic.ApplyGroundingForce();
            
            //2.实现移动急停 二次衰减式惯性滑行
            RealizationMoveToIdle();
            //3. 状态转换
            CheckStateTransitions();
            //4.计时并自然过渡到默认状态
            UpdateTransitionTimer();
        }

        #region UpdateState子方法

        //实现 移动急停 二次衰减式惯性滑行
        private void RealizationMoveToIdle()
        {
            if (Context.PreviousStateType == CharacterState.Moving)
            {
                if (Context.movementLogic.HasPressedShift())
                {
                    //1.实现急停逻辑
                    Context.movementLogic.HandleStoppingMovement();
                    //2.动画传参
                    Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
                }
                else
                {
                    //1.实现急停逻辑
                    Context.movementLogic.HandleStoppingMovement();
                    //2.动画传参
                    Context.movementLogic.UpdateStopMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
                }

            }
        }

        //计时并自然过渡到默认状态
        private void UpdateTransitionTimer()
        {
            // 1. 倒计时
            _transitionTimer -= Time.deltaTime;

            // 2. 计时结束，自然过渡到默认状态
            if (_transitionTimer <= 0f)
            {
                //_isTransitionComplete = true;
                SwitchState(Context.CurrentTransitionParams.DefaultNextState);
            }
        }
        #endregion

        public override void ExitState()
        {
            //1.退出过渡状态动画
            TransitionExitAnimation();
            //2. 重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出过渡状态动画
        private void TransitionExitAnimation()
        {
            // 1. 重置动画 Trigger（防止残留）
            Context.Animator.ResetTrigger(Context.CurrentTransitionParams.AnimationTrigger);
            // 2. 防止动画出错
            Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
        }
        #endregion

        // 状态转换
        private void CheckStateTransitions()
        {
            //死亡状态
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                SwitchState(CharacterState.Dead);
                return;
            }
            //爆发状态
            if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.QBurst);
                return;
            }
            //受击状态
            if (Context.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.Hit);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            //冲刺状态
            if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.Dashing);
                return;
            }
            // 跳跃状态
            if (Context.CheckAndConsumeJumpRequest() && Context.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.Jumping);
                return;
            }
            //下落状态
            if (!Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Falling);
                return;
            }
            //御空攻击状态
            if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
            {
                SwitchState(CharacterState.AirAttacking);
                return;
            }
            //攻击状态
            if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable())
            {
                SwitchState(CharacterState.Attacking);
                return;
            }        
            //移动状态
            if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold&&Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Moving);
                return;
            }
        }
    }
    #endregion

    #region 待机状态
    //待机状态
    public class CharacterIdleState : CharacterBaseState
    {
        public CharacterIdleState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }
        private float _stateTime;
        private int _idleNum; // 随机数
        public override void EnterState()
        {
            // 1. 进入待机状态动画
            IdleEnterAnimation();
            // 2.初始化待机状态
            InitializeIdleState();
            // 3.重置连招
            Context.attackLogic.ResetCombo();
        }

        #region EnterState子状态
        //1. 进入待机状态动画
        private void IdleEnterAnimation()
        {
            //1.待机状态动画
            if (Context.PreviousStateType == CharacterState.QBurst)
            {
                Context.Animator.SetTrigger("Stand2");
            }
            else
            {
                Context.Animator.SetTrigger("Idle");
            }
            //Context.Animator.SetTrigger("Idle");
            //2.展示背负装饰剑
            Context.manifestation.ShowDecorationSwordFade();
        }
        //2.初始化待机状态
        private void InitializeIdleState()
        {
            _stateTime = 0f;
            _idleNum = UnityEngine.Random.Range(0, 10);
        }
        #endregion

        public override void UpdateState()
        {
            //1.常态重力
            Context.movementLogic.ApplyGroundingForce();
            //2. 更新待机状态动画
            IdleUpdateAnimation();
            //3.更新状态
            UpdateIdleState();
            //3.状态转换判断
            CheckStateTransitions();
        }

        #region UpdateState子状态
        //2. 更新待机状态动画
        private void IdleUpdateAnimation()
        {
            //待机动画 传入参数
            Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
        }

        private void UpdateIdleState()
        {
            _stateTime += Time.deltaTime;
        }
        #endregion

        public override void ExitState()
        {
            // 1. 退出待机状态动画
            IdleExitAnimation();
            //2. 配置待机动作过渡参数并切换状态
            PrepareToTransition();
            //3. 重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子状态
        // 1. 退出待机状态动画
        private void IdleExitAnimation()
        {
            //1.清除动画残留
            Context.Animator.ResetTrigger("Idle");
        }
        //2. 配置待机动作过渡参数并切换状态
        private void PrepareToTransition()
        {
            if (_idleNum >= 5)
            {
                Context.CurrentTransitionParams = new CharacterStateMachine.TransitionParams
                {
                    AnimationTrigger = "Idle1", // 你的落地动画 Trigger 名称
                    Duration = 12.6f, // 假设落地动画时长0.3秒，根据实际Clip修改
                    DefaultNextState = CharacterState.Idle // 无打断时默认切 Idle
                };
            }
            else
            {
                Context.CurrentTransitionParams = new CharacterStateMachine.TransitionParams
                {
                    AnimationTrigger = "Idle2", // 你的落地动画 Trigger 名称
                    Duration = 15.2f, // 假设落地动画时长0.3秒，根据实际Clip修改
                    DefaultNextState = CharacterState.Idle // 无打断时默认切 Idle
                };
            }
        }
        #endregion

        //状态转换判断
        private void CheckStateTransitions()
        {
            //死亡状态
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                SwitchState(CharacterState.Dead);
                return;
            }
            //爆发状态
            if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.QBurst);
                return;
            }
            //受击状态
            if (Context.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.Hit);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            //冲刺状态
            if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.Dashing);
                return;
            }
            //下落状态
            if (!Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Falling);
                return;
            }
            // 跳跃状态
            if (Context.CheckAndConsumeJumpRequest() && Context.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.Jumping);
                return;
            }
            //御空攻击状态
            if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
            {
                SwitchState(CharacterState.AirAttacking);
                return;
            }
            //攻击状态
            if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable())
            {
                SwitchState(CharacterState.Attacking);
                return;
            }
            //移动状态
            if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold && Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Moving);
                return;
            }
            //过渡状态
            if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold&&_stateTime>=10f)
            {
                // 统一先切到 Transition，由 Transition 处理后续
                SwitchState(CharacterState.Transition);
                return;
            }
        }
    }
    #endregion

    #region 移动状态
    //移动状态
    public class CharacterMovingState : CharacterBaseState
    {
        private float _stateTimer;//已处于移动状态的时间
        public CharacterMovingState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            //1.初始化移动状态
            InitlizeMovingState();
            //2. 进入移动状态动画
            MovingEnterAnimation();
            //3.重置连招
            Context.attackLogic.ResetCombo();
        }

        #region EnterState子方法
        //1.初始化移动状态
        private void InitlizeMovingState()
        {
            _stateTimer = 0f;
        }
        //2.进入移动状态动画
        private void MovingEnterAnimation()
        {
            //1.清除动画残留
            Context.Animator.SetTrigger("Move");
        }
        #endregion

        public override void UpdateState()
        {
            //1.常态重力
            Context.movementLogic.ApplyGroundingForce();
            //2.实际移动与旋转
            Context.movementLogic.UpdateMovement(Context.MoveInput, Context.IsHoldingRun);
            //3.移动动画
            MovingUpdateAnimation();
            //4.状态更新
            UpdateMovingState();
            //5.状态转换判断
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //3.移动动画
        private void MovingUpdateAnimation()
        {
            //移动动画
            Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
        }
        //4.状态更新
        private void UpdateMovingState()
        {
            _stateTimer += Time.deltaTime;
        }
        #endregion

        public override void ExitState()
        {
            //1.清除移动动画残存
            MovingExitAnimation();
            //2. 配置落地过渡参数并切换状态
            PrepareToTransition();
            //3. 重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        // 1. 退出移动状态动画
        private void MovingExitAnimation()
        {
            //1.清除移动动画残存
            Context.Animator.ResetTrigger("Move");
        }
        //2. 配置缓冲过渡参数并切换状态
        private void PrepareToTransition()
        {
            //奔跑缓冲）
            if (Context.movementLogic.HasPressedShift())
            {
                Context.CurrentTransitionParams = new CharacterStateMachine.TransitionParams
                {
                    AnimationTrigger = "Buffering", // 你的落地动画 Trigger 名称
                    Duration = Context.movementLogic.runStoppingTime, // 假设落地动画时长0.3秒，根据实际Clip修改
                    DefaultNextState = CharacterState.Idle // 无打断时默认切 Idle
                };
                //Context.Animator.SetFloat("BufferingFloat", 1f);
                return;
            }
            //移动缓冲
            if (!Context.movementLogic.HasPressedShift())
            {
                Context.CurrentTransitionParams = new CharacterStateMachine.TransitionParams
                {
                    AnimationTrigger = "Idle", // 你的落地动画 Trigger 名称
                    Duration = Context.movementLogic.moveStoppingTime, // 假设落地动画时长0.3秒，根据实际Clip修改
                    DefaultNextState = CharacterState.Idle // 无打断时默认切 Idle
                };
                //Context.Animator.SetFloat("BufferingFloat", 0f);
                //Context.Animator.CrossFadeInFixedTime("Stop_Walk", 0.5f, 0,  0);
                //Context.Animator.CrossFadeInFixedTime("Stop_Run", 0.3f, 0,  0);
                return;
            }
        }
        #endregion

        private void CheckStateTransitions()
        {
            //死亡状态
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                SwitchState(CharacterState.Dead);
                return;
            }
            //爆发状态
            if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.QBurst);
                return;
            }
            //受击状态
            if (Context.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.Hit);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            //冲刺状态
            if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.Dashing);
                return;
            }
            //下落状态
            if (!Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Falling);
                return;
            }
            // 跳跃状态
            if (Context.CheckAndConsumeJumpRequest() && Context.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.Jumping);
                return;
            }
            //御空攻击状态
            if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
            {
                SwitchState(CharacterState.AirAttacking);
                return;
            }
            //攻击状态
            if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable())
            {
                SwitchState(CharacterState.Attacking);
                return;
            }   
            //移动  待机  Transition状态
            if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold&&Context.movementLogic.CustomCheckGrounded())
            {
                // 统一先切到 Transition，由 Transition 处理后续
                SwitchState(CharacterState.Transition);
                return;
            }
        }
    }
    #endregion

    #region 跳跃状态
    //跳跃状态
    public class CharacterJumpingState : CharacterBaseState
    {
        private const float JumpLockTime = 0.33f;
        private float _stateTimer;//已处于跳跃状态的时间

        public CharacterJumpingState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            //1.进入跳跃状态动画
            JumpingEnterAnimation();
            //2.初始化跳跃状态
            InitializeJumpingState();
            //3.重置连招
            Context.attackLogic.ResetCombo();
        }

        #region EnterState子状态
        // 1. 进入跳跃状态动画
        private void JumpingEnterAnimation()
        {
            Context.Animator.SetTrigger("Jump");
        }
        //2.初始化跳跃状态
        private void InitializeJumpingState()
        {
            //1.消费跳跃请求
            Context.CleanWantsToJumpRequest();//消费跳跃请求
            //2.锁定状态：禁止切换其他状态
            Context.IsStateLocked = true;
            //3.重置计时器，开始计时
            _stateTimer = 0f;
        }
        #endregion

        public override void UpdateState()
        {
            //1.跳跃初始锁定与解锁
            UpdateJumpLockTime();
            //2.状态转换判断
            CheckInterruptions();
            Context.movementLogic.HandleJumpMovement(Context.MoveInput);
        }

        #region UpdateState子状态
        //跳跃阶段状态解锁
        private void UpdateJumpLockTime()
        {
            _stateTimer += Time.deltaTime;
            if (_stateTimer >= JumpLockTime)
                Context.IsStateLocked = false;
        }
        #endregion

        public override void ExitState()
        {
            // 1. 退出跳跃状态动画
            JumpingExitAnimation();
            //2. 重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子状态
        // 1. 退出跳跃状态动画
        private void JumpingExitAnimation()
        {
            Context.Animator.ResetTrigger("Jump");
        }
        #endregion

        //状态转换判断
        private void CheckInterruptions()
        {
            //死亡状态
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                SwitchState(CharacterState.Dead);
                return;
            }
            //爆发状态
            if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.QBurst);
                return;
            }
            //受击状态
            if (Context.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.Hit);
                return;
            }
            // 御空冲刺状态
            if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsFloatDashAvailable())
            {
                SwitchState(CharacterState.FloatDashing);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            //御空攻击状态
            if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
            {
                SwitchState(CharacterState.AirAttacking);
                return;
            }
            //坠落状态
            if (_stateTimer >= JumpLockTime) 
            {
                SwitchState(CharacterState.Falling);
                return;
            }
        }
    }
    #endregion

    #region 下落状态
    //下落状态
    public class CharacterFallingState : CharacterBaseState
    {
        //已处于下落状态的时间
        private float _stateTimer;
        public CharacterFallingState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }
        public override void EnterState()
        {
            //1.下落动画
            FallingEnterAnimation();
            //2.重置下坠时间
            InitializeFallingState();
            //3.重置连招
            Context.attackLogic.ResetCombo();
        }

        #region EnterState子状态
        // 1. 进入下落状态动画
        private void FallingEnterAnimation()
        {
            //Context.Animator.SetTrigger("Fall");
            if(Context.PreviousStateType==CharacterState.AirDashing|| Context.PreviousStateType == CharacterState.FloatDashing|| Context.PreviousStateType == CharacterState.Jumping)
            {
                Context.Animator.CrossFadeInFixedTime("Fall_Loop", 0.2f, 0, 0);
            }
            else
            {
                Context.Animator.CrossFadeInFixedTime("Fall_Loop", 0, 0, 0);
            }

        }
        //2.初始化下落状态
        private void InitializeFallingState()
        {
            //2.重置下坠时间
            _stateTimer = 0f;
        }
        #endregion

        public override void UpdateState()
        {
            //1.更新下坠时间
            _stateTimer += Time.deltaTime;
            //2.下坠逻辑
            Context.movementLogic.HandleFallingMovement(Context.MoveInput);
            Context.movementLogic.ApplyGroundingForce();
           
            //3.状态转换判断
            CheckInterruptions();
        }

        #region UpdateState子状态

        #endregion

        public override void ExitState()
        {
            //1.退出下落状态动画
            FallingExitAnimation();
            //2.落地垂直速度重置
            Context.movementLogic.ResetVerticalVelocity();
            //3. 配置落地过渡参数并切换状态
            PrepareToTransition();
        }

        #region ExitState子状态
        // 1. 退出下落状态动画
        private void FallingExitAnimation()
        {
            //Context.Animator.ResetTrigger("Fall");
        }
        //2. 配置落地过渡参数并切换状态
        private void PrepareToTransition()
        {
            Context.CurrentTransitionParams = new CharacterStateMachine.TransitionParams
            {
                AnimationTrigger = "Land", // 你的落地动画 Trigger 名称
                Duration = 1.03f, // 假设落地动画时长0.3秒，根据实际Clip修改
                DefaultNextState = CharacterState.Idle // 无打断时默认切 Idle
            };
        }
        #endregion

        //状态转换判断
        private void CheckInterruptions()
        {
            // 死亡状态
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                SwitchState(CharacterState.Dead);
                return;
            }
            //爆发状态
            if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.QBurst);
                return;
            }
            //受击状态
            if (Context.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.Hit);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            // 御空冲刺状态
            if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsFloatDashAvailable())
            {
                SwitchState(CharacterState.FloatDashing);
                return;
            }
            // 空中冲刺状态
            if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsAirDashAvailable())
            {
                SwitchState(CharacterState.AirDashing);
                return;
            }
            //御空攻击状态
            if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
            {
                SwitchState(CharacterState.AirAttacking);
                return;
            }
            //下落攻击状态
            if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsFallAttackable())
            {
                SwitchState(CharacterState.FallAttacking);
                return;
            }
            //移动  待机  Transition状态
            if (Context.movementLogic.CustomCheckGrounded())
            {
                // 统一先切到 Transition，由 Transition 处理后续
                SwitchState(CharacterState.Transition);
                return;
            }
        }
    }
    #endregion

    #region 攻击状态
    //攻击状态
    public class CharacterAttackingState : CharacterBaseState
    {
        private enum AttackPhase
        {
            // 攻击执行阶段：核心攻击动作 不可打断 造成伤害 判定命中 
            Execution,
            //攻击恢复阶段：收招/后摇动画 可打断 自由退出攻击状态 
            Recovery
        }
        private AttackPhase _phase;
        private AttackStep _attackStep;

        private float _stateTime;//处于攻击状态的时长
        private bool hasUpataAttackingUpdateAnimation;
        public CharacterAttackingState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            //1.初始化攻击数据
            InitializeAttackData();
            //2.初始化攻击状态
            InitializeAttackState();
            //3.进入攻击状态动画
            AttackingEnterAnimation();
        }

        #region EnterState子方法
        //1.初始化攻击数据
        private void InitializeAttackData()
        {
            //1.获取攻击段数
            _attackStep = Context.attackLogic.InitializeSteps(Context.attackLogic.attackSteps);
            //2.同步攻击阶段信息
            Context.currentStep = _attackStep;
        }
        //2.初始化攻击状态
        private void InitializeAttackState()
        { 
            //清理攻击缓存
            Context.CleanWantsToAttackRequest();
            //状态切换锁定
            Context.IsStateLocked = true;
            //初始化攻击状态
            _phase = AttackPhase.Execution;
            //初始化阶段时长
            _stateTime = 0;
            //初始化数据
            hasUpataAttackingUpdateAnimation = false;
        }
        // 3. 进入攻击状态动画
        private void AttackingEnterAnimation()
        {
            //装饰剑隐藏
            Context.manifestation.HideDecorationSwordFade();
            //龙隐藏
            Context.attackLogic.HidedragonInstantly();
            //攻击动画
            Context.Animator.SetTrigger(_attackStep.attackAnimation.attackAnimationName);
            //御剑动画
            Context.attackLogic.StartFlyingSword(_attackStep);
            //龙动画
            Context.attackLogic.CheckDragonAnimation(_attackStep);
            //特效动画
            Context.attackLogic.CheckEffect(_attackStep);
        }
        #endregion

        public override void UpdateState()
        {
            //更新状态锁定
            UpdateStateTimeAndChangePhase();
            //更新攻击状态动画
            AttackingUpdateAnimation();
            //状态转换
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //更新状态锁定
        private void UpdateStateTimeAndChangePhase()
        {
            _stateTime += Time.deltaTime;
            if (_stateTime > _attackStep.attackAnimation.executionAttackCostTime)
            {
                if(_phase ==AttackPhase.Execution)
                {
                    Context.attackLogic.StartComboWindow();
                    
                    Context.IsStateLocked = false;
                }
                _phase = AttackPhase.Recovery;
                
            }
        }
        //更新攻击状态动画
        private void AttackingUpdateAnimation()
        {
            //防止动画出错
            Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
            if (_stateTime * 1.5f > _attackStep.attackAnimation.GetAnimationLength(Context.Animator)&&!hasUpataAttackingUpdateAnimation)
            {
                hasUpataAttackingUpdateAnimation = true;
                Context.manifestation.ShowDecorationSwordFade();
            }
        }
        #endregion

        public override void ExitState()
        {
            //1.退出攻击状态动画
            AttackingExitAnimation();
            //2. 重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子状态
        // 1. 退出攻击状态动画
        private void AttackingExitAnimation()
        {
            //防止动画残留
            Context.Animator.ResetTrigger(_attackStep.attackAnimation.attackAnimationName);
            //剑隐藏
            Context.attackLogic.EndFlyingSword();
            //龙隐藏
            Context.attackLogic.HidedragonInstantly();
            //隐藏特效
            EffectService.DestroyAll();
        }
        #endregion

        private void CheckStateTransitions()
        {
            // 死亡状态
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                SwitchState(CharacterState.Dead);
                return;
            }
            //爆发状态
            if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.QBurst);
                return;
            }
            // 受击状态
            if (Context.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.Hit);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            //冲刺状态
            if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsDashAvailable())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.Dashing);
                return;
            }
            // 跳跃状态
            if (Context.CheckAndConsumeJumpRequest() && Context.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.Jumping);
                return;
            }
            //攻击状态
            if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable())
            {
                SwitchState(CharacterState.Attacking);
                return;
            }

            //移动状态
            if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold && Context.movementLogic.CustomCheckGrounded()&&_stateTime>=1f)
            {
                SwitchState(CharacterState.Moving);
                return;
            }
            // 待机状态
            if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold && _stateTime >= _attackStep.attackAnimation.GetAnimationLength(Context.Animator))
            {
                SwitchState(CharacterState.Idle);
                return;
            }
        }
    }
    #endregion

    #region 重击状态
    //重击状态
    public class CharacterHeavyAttackingState : CharacterBaseState
    {
        private enum HeavyAttackPhase
        {
            Execution, // 重击执行阶段（不可打断，释放伤害）
            Recovery   // 重击恢复阶段（可打断，收招后摇）
        }

        private float _stateTime;              // 重击状态时长
        private HeavyAttackPhase _phase;       // 重击阶段
        private float _heavyAttackCostTime;    // 重击总动画时长
        private float _executionCostTime;      // 重击执行阶段时长

        public CharacterHeavyAttackingState(CharacterStateMachine context, CharacterStateFactory factory)
            : base(context, factory) { }

        public override void EnterState()
        {
            // 1. 初始化重击数据
            InitializeHeavyAttackData();
            // 2. 锁定状态（重击前摇不可打断）
            Context.IsStateLocked = true;
            // 3. 进入重击状态动画
            HeavyAttackingEnterAnimation();
            // 4. 清空重击输入缓存
            //Context.InputHandler.CleanWantsToHeavyAttackRequest();
        }
         // 3. 进入重击状态动画
        private void HeavyAttackingEnterAnimation()
        {
            Context.Animator.SetTrigger("HeavyAttack");
        }

        private void InitializeHeavyAttackData()
        {
            _stateTime = 0;
            _phase = HeavyAttackPhase.Execution;

            // 获取重击动画时长（需在Animator中配置HeavyAttack动画）
            _heavyAttackCostTime = Context.Animator.runtimeAnimatorController.animationClips
                .First(clip => clip.name == "HeavyAttack").length;
            // 重击执行阶段（前摇+攻击判定），可根据需求调整比例
            _executionCostTime = _heavyAttackCostTime * 0.6f;
        }

        public override void UpdateState()
        {
            // 更新状态时间+切换阶段
            UpdateAttackPhase();
            // 动画参数同步
            Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
            // 状态切换判断
            CheckStateTransitions();
        }

        private void UpdateAttackPhase()
        {
            _stateTime += Time.deltaTime;
            // 执行阶段结束 → 进入可打断的恢复阶段
            if (_stateTime > _executionCostTime)
            {
                _phase = HeavyAttackPhase.Recovery;
                Context.IsStateLocked = false;
            }
        }

        public override void ExitState()
        {
            // 1. 退出重击状态动画
            HeavyAttackingExitAnimation();
        }
        // 1. 退出重击状态动画
        private void HeavyAttackingExitAnimation()
        {
            Context.Animator.ResetTrigger("HeavyAttack");
        }

        private void CheckStateTransitions()
        {
            // 死亡
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                SwitchState(CharacterState.Dead);
                return;
            }
            // 受击
            if (Context.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.Hit);
                return;
            }
            // 冲刺
            if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.Dashing);
                return;
            }
            // 跳跃
            if (Context.CheckAndConsumeJumpRequest() && Context.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.Jumping);
                return;
            }
            // 普通攻击（重击恢复阶段可衔接普攻）
            if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable())
            {
                SwitchState(CharacterState.Attacking);
                return;
            }
            // 下落
            if (!Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Falling);
                return;
            }
            // 移动
            if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold && Context.characterController.isGrounded)
            {
                SwitchState(CharacterState.Moving);
                return;
            }
            // 待机（重击结束）
            if (_stateTime >= _heavyAttackCostTime)
            {
                SwitchState(CharacterState.Idle);
                return;
            }
        }
    }
    #endregion

    #region 下落攻击状态
    // 下落攻击状态
    public class CharacterFallAttackingState : CharacterBaseState
    {
        private enum FallAttackPhase
        {
            start,  // 启动动作（正常重力）
            loop,   // 持续下落攻击（强重力，快速下砸）
            end,    // 收刀动作（接地后）
            over    // 收刀完成，可切换状态
        }
        public CharacterFallAttackingState(CharacterStateMachine context, CharacterStateFactory factory)
        : base(context, factory) { }

        private float _stateTime;         // 总状态时长
        private float _endStateTime;      // 收刀阶段时长
        private bool hasUpdateFallAttackingAnimation;

        private FallAttackPhase _phase;   // 当前阶段
        private AttackStep _fallattackStepStart; // 启动动作数据
        private AttackStep _fallattackStepLoop;  // 持续下落数据
        private AttackStep _fallattackStepEnd;   // 收刀动作数据
        public override void EnterState()
        {
            //1.初始化化下落攻击数据
            InitliazeFallAttackingData();
            //2.初始化化下落攻击状态
            InitliazeFallAttackingState();
            //3.进入下落攻击状态动画
            FallAttackingEnterAnimation();
            //4.重置连招
            Context.attackLogic.ResetCombo();
        }

        #region EnterState子状态
        //1.初始化化下落攻击数据
        private void InitliazeFallAttackingData()
        {
            // 1. 动画数据赋值（确保 fallAttackSteps 有至少3个元素）
            if (Context.attackLogic.fallAttackSteps.Count >= 3)
            {
                _fallattackStepStart = Context.attackLogic.fallAttackSteps[0];
                _fallattackStepLoop = Context.attackLogic.fallAttackSteps[1];
                _fallattackStepEnd = Context.attackLogic.fallAttackSteps[2];
            }
            else
            {
                Debug.LogError("fallAttackSteps 列表元素不足3个！请在 Inspector 中配置 Start/Loop/End 三段数据。");
            }
            //2.同步攻击阶段信息
            Context.currentStep = _fallattackStepStart;
        }
        //2.初始化化下落攻击状态
        private void InitliazeFallAttackingState()
        {
            //1.初始化数据
            _stateTime = 0f;
            _endStateTime = 0f;
            hasUpdateFallAttackingAnimation = false;
            //2.状态锁定
            Context.IsStateLocked = true;
            //3.初始化下落攻击状态 
            _phase = FallAttackPhase.start;
            
        }
        // 3. 进入下落攻击状态动画
        private void FallAttackingEnterAnimation()
        {
            //下落攻击开始动画
            Context.Animator.SetTrigger("FallAttackingStart");
            //装饰剑隐藏
            Context.manifestation.HideDecorationSwordFade();
            //御剑
            Context.attackLogic.StartFlyingSword(_fallattackStepStart);
        }
        #endregion

        public override void UpdateState()
        {   
            // 1. 三个阶段转换逻辑
            UpdateFallAttackingPhase();
            // 2.常态重力判断
            HandleGroundForce();    
            // 3. 更新下落攻击状态动画
            FallAttackingUpdateAnimation();
            // 4. 状态切换判断
            CheckStateTransitions();
        }

        #region UpdateState子状态
        //1. 三个阶段转换逻辑
        private void UpdateFallAttackingPhase()
        {
            //1.状态时长递增
            _stateTime += Time.deltaTime;
            // 1. 强制守门：Start 动画没播完，什么都不做
            if (_stateTime <= _fallattackStepStart.attackAnimation.GetAnimationLength(Context.Animator))
                return;
            // 2. 状态：start -> loop
            if (_phase == FallAttackPhase.start)
            {
                Context.IsStateLocked = false;
                Context.Animator.ResetTrigger("FallAttackingStart");
                Context.Animator.SetTrigger("FallAttackingLoop");
                //同步攻击阶段信息
                Context.currentStep = _fallattackStepLoop;
                //御剑
                Context.attackLogic.StartFlyingSword(_fallattackStepLoop);
                _phase = FallAttackPhase.loop;
            }  // 3. 状态：loop -> end (着地)
            else if (_phase == FallAttackPhase.loop)
            {

                if (Context.movementLogic.CustomCheckGrounded())
                {
                    //落地特效
                    Context.attackLogic.CheckEffect(_fallattackStepEnd);
                    _endStateTime = 0f;
                    Context.Animator.ResetTrigger("FallAttackingLoop");
                    Context.Animator.SetTrigger("FallAttackingEnd");
                    //同步攻击阶段信息
                    Context.currentStep = _fallattackStepEnd;
                    _phase = FallAttackPhase.end;
                }
            }  // 4. 状态：end -> over (计时)
            else if (_phase == FallAttackPhase.end)
            {
                _endStateTime += Time.deltaTime;
                if (_endStateTime > _fallattackStepEnd.attackAnimation.GetAnimationLength(Context.Animator))
                {
                    _phase = FallAttackPhase.over;
                }
            }
        }
        // 2.常态重力判断
        private void HandleGroundForce()
        {
            if(_phase== FallAttackPhase.start&& _stateTime>=0.3f)
            {
                Context.movementLogic.ApplyGroundingForce();
            }
            else if (_phase == FallAttackPhase.loop)
            {
                Context.movementLogic.ApplyGroundingForce();
            }
            else if (_phase == FallAttackPhase.end)
            {

                Context.movementLogic.ResetVerticalVelocity();// 落地垂直速度重置
            }
            else if (_phase == FallAttackPhase.over)
            {
                Context.movementLogic.ApplyGroundingForce();
            }
        }
        // 3. 更新下落攻击状态动画
        private void FallAttackingUpdateAnimation()
        {
            if (_stateTime * 1.5f > _fallattackStepEnd.attackAnimation.GetAnimationLength(Context.Animator)&&!hasUpdateFallAttackingAnimation)
            {
                hasUpdateFallAttackingAnimation = true;
                //显示装饰剑
                Context.manifestation.ShowDecorationSwordFade();
            }
        }
        #endregion

        public override void ExitState()
        {
            //1.退出下落攻击状态动画
            FallAttackingExitAnimation();
            //2.确保退出时重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子状态
        // 1. 退出下落攻击状态动画
        private void FallAttackingExitAnimation()
        {
            // 清理所有动画 Trigger
            Context.Animator.ResetTrigger("FallAttackingStart");
            Context.Animator.ResetTrigger("FallAttackingLoop");
            Context.Animator.ResetTrigger("FallAttackingEnd");
            //隐藏特效
            EffectService.DestroyAll();
        }
        #endregion

        private void CheckStateTransitions()
        {
            // 死亡状态
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                SwitchState(CharacterState.Dead);
                return;
            }
            //爆发状态
            if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.QBurst);
                return;
            }
            // 受击状态
            if (Context.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.Hit);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            //冲刺状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.Dashing);
                return;
            }
            // 空中冲刺状态
            if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsAirDashAvailable() && _phase == FallAttackPhase.loop)
            {
                SwitchState(CharacterState.AirDashing);
                return;
            }
            
            // 跳跃状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over)&& Context.CheckAndConsumeJumpRequest() && Context.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.Jumping);
                return;
            }
            //攻击状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable())
            {
                SwitchState(CharacterState.Attacking);
                return;
            }
            //移动状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && Context.MoveInput.magnitude > Context.movementLogic.moveThreshold)
            {
                SwitchState(CharacterState.Moving);
                return;
            }
            // 待机状态
            if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold && _phase == FallAttackPhase.over)
            {
                SwitchState(CharacterState.Idle);
                return;
            }
        }
    }
    #endregion

    #region 御空攻击状态
    // 御空攻击状态
    public class CharacterAirAttackingState : CharacterBaseState
    {
        private enum AirAttackPhase
        {
            Execution, // 攻击执行阶段：不可打断，造成伤害
            Recovery   // 攻击恢复阶段：可打断，开启连击窗口
        }
        private enum isGrouded
        {
            T,//是否在地面
            F   
        }
        private AirAttackPhase _phase;
        private AttackStep _attackStep;
        private isGrouded _isGrouded;
        private float _stateTime;
        private bool _hasUpdatedExitAnimation;

        public CharacterAirAttackingState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            //1. 初始化御空攻击数据
            InitializeAttackData();
            //2. 初始化攻击状态
            InitializeAttackState();
            //3. 播放攻击动画与表现
            AirAttackingEnterAnimation();
        }

        #region EnterState子方法
        //1. 初始化御空攻击数据
        private void InitializeAttackData()
        {
            // 御空攻击使用SkillAttackSteps配置
            if(Context.movementLogic.CustomCheckGrounded())
            {
                //御空攻击(地面模组)
                _attackStep = Context.attackLogic.InitializeAirSteps(Context.attackLogic.SkillAttackSteps);
                _isGrouded = isGrouded.T;
            }
            else
            {
                //御空攻击(空中模组)
                _attackStep = Context.attackLogic.InitializeAirSteps(Context.attackLogic.SkillAirAttackSteps);
                _isGrouded = isGrouded.F;
            }
            
            Context.currentStep = _attackStep;
        }
        //2. 初始化攻击状态
        private void InitializeAttackState()
        {
            // 清理攻击缓存
            Context.CleanWantsToAirAttackRequest();
            // 状态锁定
            Context.IsStateLocked = true;
            // 初始化阶段与计时
            _phase = AirAttackPhase.Execution;
            _stateTime = 0f;
            _hasUpdatedExitAnimation = false;
        }
        //3. 播放攻击动画与表现
        private void AirAttackingEnterAnimation()
        {
            // 装饰剑隐藏
            Context.manifestation.HideDecorationSwordFade();
            // 播放攻击动画
            Context.Animator.SetTrigger(_attackStep.attackAnimation.attackAnimationName);
            // 御剑/龙/特效表现
            Context.attackLogic.StartFlyingSword(_attackStep);
            Context.attackLogic.CheckDragonAnimation(_attackStep);
            Context.attackLogic.CheckEffect(_attackStep);
        }
        #endregion

        public override void UpdateState()
        {
            //1. 更新状态阶段与锁定
            UpdateStateTimeAndChangePhase();
            //2. 动画参数同步
            AirAttackingUpdateAnimation();

            //4. 状态切换判断
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1. 更新状态阶段与锁定
        private void UpdateStateTimeAndChangePhase()
        {
            _stateTime += Time.deltaTime;
            if (_phase == AirAttackPhase.Execution && _stateTime > _attackStep.attackAnimation.executionAttackCostTime)
            {
                // 进入恢复阶段，开启连击窗口
                Context.attackLogic.StartAirComboWindow();
                Context.IsStateLocked = false;
                _phase = AirAttackPhase.Recovery;
            }
        }
        //2. 动画参数同步
        private void AirAttackingUpdateAnimation()
        {
            // 防止动画出错
            Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
            // 动画快结束时显示装饰剑
            if (_stateTime * 1.2f > _attackStep.attackAnimation.GetAnimationLength(Context.Animator) && !_hasUpdatedExitAnimation)
            {
                _hasUpdatedExitAnimation = true;
                Context.manifestation.ShowDecorationSwordFade();
            }
        }
        #endregion

        public override void ExitState()
        {
            //1.退出动画相关
            AirAttackingExitAnimation();
            //2.重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出动画相关
        private void AirAttackingExitAnimation()
        {
            //清理动画触发器
            Context.Animator.ResetTrigger(_attackStep.attackAnimation.attackAnimationName);
            //结束御剑表现
            Context.attackLogic.EndFlyingSword();
            //隐藏龙
            Context.attackLogic.HidedragonInstantly();
            //隐藏特效
            EffectService.DestroyAll();

        }
        #endregion

        private void CheckStateTransitions()
        {
            //地面
            if(_isGrouded == isGrouded.T)
            {
                // 死亡状态
                if (Context._core.GetCharacterData().currentHealth <= 0)
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Dead);
                    return;
                }
                // 爆发状态
                if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.QBurst);
                    return;
                }
                // 受击状态
                if (Context.TryConsumeHitRequest())
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Hit);
                    return;
                }
                //战技状态
                if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.ESkill);
                    return;
                }
                // 冲刺状态
                if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsDashAvailable())
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Dashing);
                    return;
                }
                // 跳跃状态
                if (Context.CheckAndConsumeJumpRequest() && Context.movementLogic.IsJumpAvailable() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.Jumping);
                    return;
                }
                //御空攻击状态
                if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
                {
                    SwitchState(CharacterState.AirAttacking);
                    return;
                }
                // 攻击
                if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.Attacking);
                    return;
                }
                //移动状态
                if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold && Context.movementLogic.CustomCheckGrounded() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.Moving);
                    return;
                }
                // 待机状态
                if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold && _stateTime >= _attackStep.attackAnimation.GetAnimationLength(Context.Animator))
                {
                    SwitchState(CharacterState.Idle);
                    return;
                }
            }
            //空中
            if (_isGrouded == isGrouded.F)
            {
                // 死亡状态
                if (Context._core.GetCharacterData().currentHealth <= 0)
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Dead);
                    return;
                }
                // 爆发状态
                if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.QBurst);
                    return;
                }
                // 受击状态
                if (Context.TryConsumeHitRequest())
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Hit);
                    return;
                }
                //战技状态
                if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.ESkill);
                    return;
                }
                //御空冲刺状态
                if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsFloatDashAvailable())
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.FloatDashing);
                    return;
                }
                //下落状态
                if (!Context.movementLogic.CustomCheckGrounded() && _stateTime >= _attackStep.attackAnimation.GetAnimationLength(Context.Animator))
                {
                    SwitchState(CharacterState.Falling);
                    return;
                }
                //御空攻击状态
                if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
                {
                    SwitchState(CharacterState.AirAttacking);
                    return;
                }
                //移动状态
                if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold && Context.movementLogic.CustomCheckGrounded() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.Moving);
                    return;
                }
                // 待机状态
                if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold && _stateTime >= _attackStep.attackAnimation.GetAnimationLength(Context.Animator))
                {
                    SwitchState(CharacterState.Idle);
                    return;
                }
            }

        }
    }
    #endregion

    #region 冲刺状态
    //冲刺状态
    public class CharacterDashingState : CharacterBaseState
    {
        private enum DashPhase
        {
            Displacement, // 位移阶段：代码驱动移动，仅再次冲刺可打断
            Stopping       // 急停阶段：纯动画表演，任意动作可打断
        }
        private DashPhase _phase;//冲刺状态的两个阶段
        private  float DashLockTime ;//冲刺方向锁定时间
        private bool _dashDirection;//冲刺方向
        private float _stateTimer;//已处于冲刺状态的时间
        public CharacterDashingState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }
        public override void EnterState()
        {
            //1.初始化阶段 时长 时间 清理冲刺输入缓存 方向 
            InitializeDashData();
            if (!Context.movementLogic.TryConsumeDashStamina())
            {
                Context.IsStateLocked = false;
                SwitchState(Context.MoveInput.magnitude > Context.movementLogic.moveThreshold ? CharacterState.Moving : CharacterState.Idle);
                return;
            }
            //2.计算方向（调用CharacterMovement）
            _dashDirection = Context.movementLogic.CalculateDirection(Context.MoveInput);
            //3.计算上次冲刺间隔  重置连冲资格 消耗1次冲刺次数 启动内置 CD 并更新时间戳（调用CharacterMovement）
            Context.movementLogic.CalculateAndUpdateDashCounter();
            //4.检查并施加全局 CD 惩罚（调用CharacterMovement）
            Context.movementLogic.ApplyPenaltyIfNecessary();
            //5.进入冲刺状态动画
            DashingEnterAnimation();
            //6.重置连招
            Context.attackLogic.ResetCombo();
        }

        #region EnterState子方法  
        //1.初始化阶段 时长 时间 清理冲刺输入缓存 方向 
        private void InitializeDashData()
        {
            // 清理冲刺输入缓存
            Context.CleanWantsToDashRequest();
            //初始化阶段
            _phase = DashPhase.Displacement;
            //冲刺阶段状态锁定
            Context.IsStateLocked = true;
            //冲刺时长
            DashLockTime = Context.movementLogic.dashCostTime;
            //重置已处于冲刺状态的时间
            _stateTimer = 0f;
        }
        //5.进入冲刺状态动画
        private void DashingEnterAnimation()
        {
            if (_dashDirection)
                Context.Animator.SetTrigger("DashF");
            else
                Context.Animator.SetTrigger("DashB");
        }
        #endregion

        public override void UpdateState()
        { 
            //1.状态时间更新与解锁
            UpdateAndUnlocked();
            //2.更新冲刺状态动画
            DashingUpdateAnimation();
            //3.常态重力
            Context.movementLogic.ApplyGroundingForce();
            //4.状态转换判断
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1.状态时间更新与解锁
        private void UpdateAndUnlocked()
        {
            _stateTimer += Time.deltaTime;
            if(_stateTimer >=DashLockTime)
            {
                Context.IsStateLocked = false;
                _phase = DashPhase.Stopping;
            }
        }
        //2.更新冲刺状态动画
        private void DashingUpdateAnimation()
        {
            //1.传入参数防止错误
            Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
        }
        #endregion

        public override void ExitState()
        {
            //1.退出冲刺状态动画
            DashingExitAnimation();
            //2.确保退出时重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出冲刺状态动画
        private void DashingExitAnimation()
        {
            Context.Animator.ResetTrigger("DashF");
            Context.Animator.ResetTrigger("DashB");
        }
        #endregion

        private void CheckStateTransitions()
        {
            // 死亡状态
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                SwitchState(CharacterState.Dead);
                return;
            }
            //爆发状态
            if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.QBurst);
                return;
            }
            //受击状态
            if (Context.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.Hit);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            //冲刺状态
            if ((Context.WantsToDash && Context.movementLogic.IsDashAvailable()))
            {
                SwitchState(CharacterState.Dashing);
                return;
            }
            // 跳跃状态
            if (Context.CheckAndConsumeJumpRequest() && Context.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.Jumping);
                return;
            }
            //御空攻击状态
            if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
            {
                SwitchState(CharacterState.AirAttacking);
                return;
            }
            //攻击状态
            if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable())
            {
                SwitchState(CharacterState.Attacking);
                return;
            }
            // 移动状态
            if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold && Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Moving);
                return;
            }
            // 待机状态
            if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold &&_stateTimer>=1.33f && Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Idle);
                return;
            }
        }
    }
    #endregion

    #region 空中冲刺状态
    //空中冲刺状态
    public class CharacterAirDashingState : CharacterBaseState
    {
        private float _stateTime; //空中冲刺状态时长
        private float _airdashTimer=0.5f; // 空中冲刺持续时间计时器
        private bool _airDashDirection;// 冲刺朝向标识：true=向前 false=向后

        public CharacterAirDashingState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            //1.初始化空中冲刺数据 计算空中冲刺方向与朝向
            InitializeAirDashingData();
            //2.初始化空中冲刺状态
            InitializeAirDashingState();
            //3.进入空中冲刺状态动画
            AirDashingEnterAnimation();
            //4.重置连招
            Context.attackLogic.ResetCombo();
        }

        #region EnterState子方法
        //1.初始化空中冲刺数据 计算空中冲刺方向与朝向
        private void InitializeAirDashingData()
        {
            _airDashDirection = Context.movementLogic.CalculateDirection(Context.MoveInput);
        }
        //2.初始化空中冲刺状态
        private void InitializeAirDashingState()
        {
            // 清理冲刺输入请求，避免重复触发
            Context.CleanWantsToDashRequest();
            //初始化状态时间
            _stateTime = 0f;
            // 锁定状态：冲刺期间禁止切换其他状态
            Context.IsStateLocked = true;
            // 标记空中冲刺已使用（落地自动重置）
            Context.movementLogic.HasAirDashed = true;
        }
        //3.进入空中冲刺状态动画
        private void AirDashingEnterAnimation()
        {
            // 播放对应方向的空中冲刺动画
            if (_airDashDirection)
            {
                //Context.Animator.SetTrigger("AirDashF");
                Context.Animator.CrossFadeInFixedTime("Jump_Second_F", 0f, 0, 0);
            }
            else
            {
                //Context.Animator.SetTrigger("AirDashB");
                Context.Animator.CrossFadeInFixedTime("Jump_Second_B", 0f, 0, 0);
            }
        }
        #endregion

        public override void UpdateState()
        {
            //1.计时器更新
            UpdateAndUnlockStateTime();
            //2.状态转换
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1.计时器更新
        private void UpdateAndUnlockStateTime()
        {
            // 空中冲刺计时器更新
            _stateTime += Time.deltaTime;
            if (_stateTime >= _airdashTimer)
                Context.IsStateLocked = false;
        }
        #endregion
        public override void ExitState()
        {
            //1.退出空中冲刺状态动画
            AirDashingExitAnimation();
            //2.确保退出时重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出空中冲刺状态动画
        private void AirDashingExitAnimation()
        {
            // 清理空中冲刺动画 Trigger
            if (_airDashDirection)
            {
                //Context.Animator.ResetTrigger("AirDashF");
            }
            else
            {
                //Context.Animator.ResetTrigger("AirDashB");
            }
        }
        #endregion


        //状态转换
        private void CheckStateTransitions()
        {
            //爆发状态
            if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.QBurst);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            //御空攻击状态
            if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
            {
                SwitchState(CharacterState.AirAttacking);
                return;
            }
            //下落攻击状态
            if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsFallAttackable())
            {
                SwitchState(CharacterState.FallAttacking);
                return;
            }
            //下落状态
            if (!Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Falling);
                return;
            }
        }
    }
    #endregion

    #region 御空冲刺状态
    // 御空冲刺状态
    public class CharacterFloatDashingState : CharacterBaseState
    {
        private float _stateTime;//御空冲刺状态时长
        private float _floatDashTimer;//御空冲刺持续时间计时器
        private bool _floatDashDirection;//御空冲刺方向
        public CharacterFloatDashingState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }
        public override void EnterState()
        {
            //1.初始化御空冲刺数据
            InitializeFloatDashingData();
            //2.初始化御空冲刺状态
            InitializeFloatDashingState();
            //3.消耗体力
            if (!Context.movementLogic.TryConsumeFloatDashStamina())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.Falling);
                return;
            }
            //4.进入御空冲刺状态动画
            FloatDashingEnterAnimation();
            //5.重置连招
            Context.attackLogic.ResetCombo();
        }

        #region EnterState子方法
        //1.初始化御空冲刺数据
        private void InitializeFloatDashingData()
        {
            // 计算御空冲刺方向 + 旋转至八方向(前/后/左/右/四斜向)
            _floatDashDirection = Context.movementLogic.CalculateFloatDashDirection(Context.MoveInput);
            //初始化御空冲刺时长数据
            _floatDashTimer = Context.movementLogic.floatDashCostTime;
        }
        //2.初始化御空冲刺状态
        private void InitializeFloatDashingState()
        {
            // 清理冲刺输入请求，避免重复触发
            Context.CleanWantsToDashRequest();
            //初始化状态时间
            _stateTime = 0f;
            // 锁定状态：冲刺期间禁止切换其他状态
            Context.IsStateLocked = true;
        }
        //3.进入御空冲刺状态动画
        private void FloatDashingEnterAnimation()
        {
            if (_floatDashDirection)
            {
                Context.Animator.CrossFadeInFixedTime("SkillMove_F", 0f, 0, 0);
            }
            else
            {
                Context.Animator.CrossFadeInFixedTime("SkillMove_B", 0f, 0, 0);
            }
        }
        #endregion
        public override void UpdateState()
        {
            //1.计时器更新
            UpdateAndUnlockStateTime();
            //2.同步动画参数
            FloatDashingUpdateAnimation();
            //3.状态转换
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1.计时器更新
        private void UpdateAndUnlockStateTime()
        {
            _stateTime += Time.deltaTime;
            if (_stateTime >= _floatDashTimer)
            {
                Context.IsStateLocked = false;
            }
        }
        //2.同步动画参数
        private void FloatDashingUpdateAnimation()
        {
            Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
        }
        #endregion

        public override void ExitState()
        {
            //1.退出御空冲刺状态
            FloatDashingExitAnimation();
            //2.确保退出时重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出御空冲刺状态
        private void FloatDashingExitAnimation()
        {
        }
        #endregion

        //状态转换
        private void CheckStateTransitions()
        {
            // 死亡状态
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.Dead);
                return;
            }
            //爆发状态
            if (Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.QBurst);
                return;
            }
            //受击状态
            if (Context.TryConsumeHitRequest())
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.Hit);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            //御空冲刺状态
            if (Context.WantsToDash && Context.movementLogic.IsFloatDashAvailable()&& _stateTime >= 0.5f)
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.FloatDashing);
                return;
            }
            //御空攻击状态
            if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
            {
                SwitchState(CharacterState.AirAttacking);
                return;
            }
            //下落状态
            if (!Context.attackLogic.IsFloating|| _stateTime >= 1.9f)
            {
                SwitchState(CharacterState.Falling);
                return;
            }
        }
    }
    #endregion

    #region 闪避状态
    // 闪避状态
    public class CharacterDodgingState : CharacterBaseState
    {
        public CharacterDodgingState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            // 初始化：锁死状态+标记闪避+开启无敌+播放闪避动画+停止冲刺
            Context.IsStateLocked = true;

            Context.Animator.SetTrigger("Dodge"); // 需在Animator创建Dodge Trigger

        }

        public override void UpdateState()
        {

            // 仅判断动画结束，无其他逻辑（闪避期间禁止所有操作/状态切换）
            CheckStateTransitions();
        }



        public override void ExitState()
        {
            // 清理：解锁状态+清除标记+关闭无敌+重置触发器
            Context.IsStateLocked = false;

            Context.Animator.ResetTrigger("Dodge");
        }

        private void CheckStateTransitions()
        {
            // 唯一切换条件：闪避动画播放完成（无敌期间不处理任何死亡/受击）
            AnimatorStateInfo stateInfo = Context.Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsTag("Dodge") && stateInfo.normalizedTime >= 1f)
            {
                // 动画结束后，根据移动输入切回Idle/Moving
                if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold)
                {
                    SwitchState(CharacterState.Moving);
                }
                else
                {
                    SwitchState(CharacterState.Idle);
                }
            }
        }
    }
    #endregion

    #region 御空闪避状态
    // 御空闪避状态
    public class CharacterFloatDodgingState : CharacterBaseState
    {
        public CharacterFloatDodgingState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }
        public override void EnterState() { }
        public override void UpdateState() { }

        public override void ExitState() { }
    }
    #endregion

    #region 战技状态（流光夕影）（神霓飞芒）（逐天取月）（乘岁凌霄）
    // 战技状态
    public class CharacterESkillState : CharacterBaseState
    {
        public CharacterESkillState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }
        private enum ESkillType
        {
            ESkill1,//普通战技（无前置，CD好了就能用）流光夕影
            ESkill2,//普攻第四段解锁的战技（地面空中都能用，进御空状态）神霓飞芒
            ESkill3,//御空战技（Skill2后窗口内可用）逐天取月
            ESkill4//御空强化战技（御空普攻第四段后窗口内可用）惊龙破空
        }
        private enum ESkillStatePhase
        {
            Execution, // 技能执行阶段（不可打断）
            Recovery   // 技能恢复阶段（可打断）
        }

        private ESkillType _eSkillType;
        private ESkillStatePhase _statePhase;
        private AttackStep _currentSkillStep;
        private float _stateTime;
        private float _executionDuration; // 执行阶段时长

        public override void EnterState()
        {
            //1.初始化战技状态数据
            InitializeESkillData();
            //2.初始化战技状态
            InitializeState();
            //3. 播放技能动画与表现
            ESkillEnterAnimation();
        }

        #region EnterState子方法
        //1.初始化战技状态数据
        private void InitializeESkillData()
        {
            //初始化攻击阶段
            _currentSkillStep = Context.attackLogic.InitializeESkillSteps();
            // 初始化执行阶段时长
            _executionDuration = _currentSkillStep.attackAnimation.executionAttackCostTime;
            //初始化战技类型
            if (_currentSkillStep== Context.attackLogic.ESkillAttackSteps[3])
            {
                _eSkillType = ESkillType.ESkill4;
                Context.attackLogic.OnSkill4Used();
                return;
            }
            if (_currentSkillStep == Context.attackLogic.ESkillAttackSteps[2])
            {
                _eSkillType = ESkillType.ESkill3;
                Context.attackLogic.OnSkill3Used();
                return;
            }
            if (_currentSkillStep == Context.attackLogic.ESkillAttackSteps[1])
            {
                _eSkillType = ESkillType.ESkill2;
                Context.attackLogic.OnSkill2Used();
                return;
            }
            if (_currentSkillStep == Context.attackLogic.ESkillAttackSteps[0])
            {
                _eSkillType = ESkillType.ESkill1;
                Context.attackLogic.OnSkill1Used();
                return;
            }
        }
        //2.初始化战技状态
        private void InitializeState()
        {
            //1. 消费E技能请求
            Context.CleanWantsToESkilltRequest();
            //2.数据初始化
            _statePhase = ESkillStatePhase.Execution;
            _stateTime = 0f;
            //3.切换锁定
            Context.IsStateLocked = true;
        }
        //3. 播放技能动画与表现
        private void ESkillEnterAnimation()
        {
            // 播放角色动画
            Context.Animator.SetTrigger(_currentSkillStep.attackAnimation.attackAnimationName);
            // 御剑/龙/特效表现
            Context.attackLogic.StartFlyingSword(_currentSkillStep);
            Context.attackLogic.CheckDragonAnimation(_currentSkillStep);
            Context.attackLogic.CheckEffect(_currentSkillStep);
            // 隐藏装饰剑
            Context.manifestation.HideDecorationSwordFade();
        }
        #endregion

        public override void UpdateState()
        {
            // 1. 更新状态阶段
            UpdateStatePhase();
            //2. 更新技能动画与表现
            ESkillUpdateAnimation();
            // 3. 状态切换判断
            CheckStateTransitions();
        }

        #region UpdateState子方法
        // 1. 更新状态阶段
        private void UpdateStatePhase()
        {
            _stateTime += Time.deltaTime;
            if (_statePhase == ESkillStatePhase.Execution && _stateTime >= _executionDuration)
            {
                _statePhase = ESkillStatePhase.Recovery;
                Context.IsStateLocked = false;
            }
        }
        //2. 更新技能动画与表现
        private void ESkillUpdateAnimation()
        {
            // 动画参数同步
            Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);
        }

        private void ApplyDifferentGravity()
        {
            if(_eSkillType == ESkillType.ESkill3)
            {

            }
        }
        #endregion

        public override void ExitState()
        {
            //1. 退出技能动画
            ESkillExitAnimation();
            ////2.重置御空状态
            //if (_currentSkillStep == Context.attackLogic.ESkillAttackSteps[3])
            //Context.attackLogic.SetFloating(false);
            //3.重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();

        }

        #region ExitState子方法
        //1. 退出技能动画
        private void ESkillExitAnimation()
        {
            // 清理动画触发器
            Context.Animator.ResetTrigger(_currentSkillStep.attackAnimation.attackAnimationName);
            // 结束御剑表现
            Context.attackLogic.EndFlyingSword();
            // 结束龙
            Context.attackLogic.HidedragonInstantly();
            //隐藏特效
            EffectService.DestroyAll();
        }
        #endregion

        private void CheckStateTransitions()
        {
            //惊龙破空
            if (_eSkillType == ESkillType.ESkill4)
            {
                // 死亡状态
                if (Context._core.GetCharacterData().currentHealth <= 0 && _statePhase == ESkillStatePhase.Recovery)
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Dead);
                    return;
                }
                // 爆发状态
                if (_statePhase == ESkillStatePhase.Recovery && Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.QBurst);
                    return;
                }
                // 受击状态
                if (Context.TryConsumeHitRequest() && _statePhase == ESkillStatePhase.Recovery)
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Hit);
                    return;
                }
                //战技状态
                if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.ESkill);
                    return;
                }
                //御空冲刺状态
                if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsFloatDashAvailable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.FloatDashing);
                    return;
                }
                 //御空攻击状态
                if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
                {
                    SwitchState(CharacterState.AirAttacking);
                    return;
                }
                // 攻击状态
                if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.Attacking);
                }
                //下落状态
                if (!Context.movementLogic.CustomCheckGrounded() && _stateTime >=6.3f)
                {
                    SwitchState(CharacterState.Falling);
                    return;
                }
                //移动状态
                if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold && Context.movementLogic.CustomCheckGrounded() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.Moving);
                    return;
                }
                // 待机状态
                if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold && _stateTime >= _currentSkillStep.attackAnimation.GetAnimationLength(Context.Animator))
                {
                    SwitchState(CharacterState.Idle);
                    return;
                }
            }
            //逐天取月
            if (_eSkillType == ESkillType.ESkill3)
            {
                // 死亡状态
                if (Context._core.GetCharacterData().currentHealth <= 0 && _statePhase == ESkillStatePhase.Recovery)
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Dead);
                    return;
                }
                // 爆发状态
                if (_statePhase == ESkillStatePhase.Recovery && Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.QBurst);
                    return;
                }
                // 受击状态
                if (Context.TryConsumeHitRequest() && _statePhase == ESkillStatePhase.Recovery)
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Hit);
                    return;
                }
                //战技状态
                if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.ESkill);
                    return;
                }
                //御空冲刺状态
                if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsFloatDashAvailable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.FloatDashing);
                    return;
                }
                //御空攻击状态
                if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
                {
                    SwitchState(CharacterState.AirAttacking);
                    return;
                }
                // 攻击状态
                if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.Attacking);
                    return;
                }
                //下落状态
                if (!Context.movementLogic.CustomCheckGrounded() &&_stateTime>=2.3f)
                {
                    SwitchState(CharacterState.Falling);
                    return;
                }
                // 待机状态
                if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold && _stateTime >= _currentSkillStep.attackAnimation.GetAnimationLength(Context.Animator))
                {
                    SwitchState(CharacterState.Idle);
                    return;
                }
            }
            //神霓飞芒
            if (_eSkillType == ESkillType.ESkill2)
            {
                // 死亡状态
                if (Context._core.GetCharacterData().currentHealth <= 0 && _statePhase == ESkillStatePhase.Recovery)
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Dead);
                    return;
                }
                // 爆发状态
                if (_statePhase == ESkillStatePhase.Recovery && Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.QBurst);
                    return;
                }
                // 受击状态
                if (Context.TryConsumeHitRequest() && _statePhase == ESkillStatePhase.Recovery)
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Hit);
                    return;
                }
                //战技状态
                if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.ESkill);
                    return;
                }
                //御空冲刺状态
                if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsFloatDashAvailable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.FloatDashing);
                    return;
                }
                //御空攻击状态
                if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
                {
                    SwitchState(CharacterState.AirAttacking);
                    return;
                }
                // 攻击
                if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.Attacking);
                    return;
                }
                //下落状态
                if (!Context.movementLogic.CustomCheckGrounded() && Context.MoveInput.magnitude > Context.movementLogic.moveThreshold && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.Falling);
                    return;
                }
                // 待机状态
                if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold && _stateTime >= _currentSkillStep.attackAnimation.GetAnimationLength(Context.Animator))
                {
                    SwitchState(CharacterState.Idle);
                    return;
                }
            }
            //流光夕影
            if (_eSkillType == ESkillType.ESkill1)
            {
                // 死亡状态
                if (Context._core.GetCharacterData().currentHealth <= 0 && _statePhase == ESkillStatePhase.Recovery)
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Dead);
                    return;
                }
                // 爆发状态
                if (_statePhase == ESkillStatePhase.Recovery && Context.attackLogic.IsQBurstable() && Context.CheckAndConsumeQBurstRequest())
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.QBurst);
                    return;
                }
                // 受击状态
                if (Context.TryConsumeHitRequest() && _statePhase == ESkillStatePhase.Recovery)
                {
                    Context.IsStateLocked = false;
                    SwitchState(CharacterState.Hit);
                    return;
                }
                //战技状态
                if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.ESkill);
                    return;
                }
                // 冲刺状态
                if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsDashAvailable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.Dashing);
                    return;
                }
                // 跳跃状态
                if (Context.CheckAndConsumeJumpRequest() && Context.movementLogic.IsJumpAvailable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.Jumping);
                    return;
                }
                //下落状态
                if (!Context.movementLogic.CustomCheckGrounded() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.Falling);
                    return;
                }
                //御空攻击状态
                if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
                {
                    SwitchState(CharacterState.AirAttacking);
                    return;
                }
                // 攻击
                if (Context.CheckAndConsumeAttackRequest() &&Context.attackLogic.IsAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.Attacking);
                    return;
                }
                //移动状态
                if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold && Context.movementLogic.CustomCheckGrounded() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.Moving);
                    return;
                }
                // 待机状态
                if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold && _stateTime >= _currentSkillStep.attackAnimation.GetAnimationLength(Context.Animator))
                {
                    SwitchState(CharacterState.Idle);
                    return;
                }
            }
        }
    }
    #endregion

    #region 爆发状态
    // 爆发状态
    public class CharacterQBurstState : CharacterBaseState
    {
        private float _stateTime;//处于爆发状态的时长
        private float lockTime=3f;//处于锁定的时长
        private AttackStep _step;
        private bool hasUpateQBurstAnimation;//判断是否更新爆发动画
        public CharacterQBurstState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }
        public override void EnterState()
        { 
            //1.初始化爆发数据
            InitializeQBurstData();
            //2.初始化爆发状态
            InitialazeQBurstState();
            //3.进入爆发状态动画
            QBurstEnterAnimation();
            //4.重置连招
            Context.attackLogic.ResetCombo();
        }

        #region EnterState子方法
        //1.初始化爆发数据
        private void InitializeQBurstData()
        {
            _step = Context.attackLogic.QBurstAttackSteps[0];
            Context.currentStep = _step;
            Context.attackLogic.OnQBurstUsed();
        }
        //2.初始化爆发状态
        private void InitialazeQBurstState()
        {
            //数据初始化
            _stateTime = 0f;
            //状态锁定
            Context.IsStateLocked = true;
            //数据初始化
            hasUpateQBurstAnimation = false;
        }
        //3.进入爆发状态动画
        private void QBurstEnterAnimation()
        {
            //动画
            Context.Animator.SetTrigger("QBurst");
            //龙动画
            Context.attackLogic.CheckDragonAnimation(Context.attackLogic.QBurstAttackSteps[0]);
            //隐藏剑
            Context.manifestation.HideDecorationSwordFade();
            //特效
            Context.attackLogic.CheckEffect(_step);
        }
        #endregion

        public override void UpdateState()
        {
            //1.更新状态锁定
            UpdateStateTimeAndChangePhase();
            //2. 更新爆发动画
            FallAttackingUpdateAnimation();
            //3.状态转换
            CheckStateTransitions();
        }

        #region UpdateState子状态
        //1.更新状态锁定
        private void UpdateStateTimeAndChangePhase()
        {
            _stateTime += Time.deltaTime;
            if (_stateTime > lockTime)
            {
                Context.IsStateLocked = false;
            }
        }
        // 2. 更新爆发动画
        private void FallAttackingUpdateAnimation()
        {
            //防止动画出错
            Context.movementLogic.UpdateFreeMoveAnimation(Context.MoveInput, Context.IsHoldingRun);

            //if (_stateTime > 2f&&!hasUpateQBurstAnimation)
            //{
            //    hasUpateQBurstAnimation = true;
            //    //运镜
            //    Context._core.CameraMovement();
            //}

        }
        #endregion
        public override void ExitState()
        {
            //1.退出爆发状态动画
            QBurstExitAnimation();
            //2.确保退出时重置垂直速度
            Context.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子状态
        //1.退出爆发状态动画
        private void QBurstExitAnimation()
        {
            //动画
            Context.Animator.ResetTrigger("QBurst");
            //龙隐藏
            Context.attackLogic.HidedragonInstantly();
            //隐藏特效
            EffectService.DestroyAll();
        }
        #endregion

        private void CheckStateTransitions()
        {
            //死亡状态
            if (Context._core.GetCharacterData().currentHealth <= 0)
            {
                SwitchState(CharacterState.Dead);
                return;
            }
            //受击状态
            if (Context.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.Hit);
                return;
            }
            //战技状态
            if (Context.CheckAndConsumeESkillRequest() && Context.attackLogic.IsESkillable())
            {
                SwitchState(CharacterState.ESkill);
                return;
            }
            //御空冲刺状态
            if (Context.WantsToDash && Context.movementLogic.IsFloatDashAvailable() && _stateTime >= 2f)
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.FloatDashing);
                return;
            }
            //冲刺状态
            if (Context.CheckAndConsumeDashRequest() && Context.movementLogic.IsDashAvailable() && _stateTime >= 2f)
            {
                Context.IsStateLocked = false;
                SwitchState(CharacterState.Dashing);
                return;
            }
            //下落状态
            if (!Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Falling);
                return;
            }
            // 跳跃状态
            if (Context.CheckAndConsumeJumpRequest() && Context.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.Jumping);
                return;
            }
            //御空攻击状态
            if (Context.CheckAndConsumeAirAttackRequest() && Context.attackLogic.IsAirAttackable())
            {
                SwitchState(CharacterState.AirAttacking);
                return;
            }
            //攻击状态
            if (Context.CheckAndConsumeAttackRequest() && Context.attackLogic.IsAttackable())
            {
                SwitchState(CharacterState.Attacking);
                return;
            }
            //移动状态
            if (Context.MoveInput.magnitude > Context.movementLogic.moveThreshold && Context.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.Moving);
                return;
            }
            // 待机状态
            if (Context.MoveInput.magnitude < Context.movementLogic.moveThreshold && _stateTime >= 6f)
            {
                SwitchState(CharacterState.Idle);
                return;
            }
        }
    }
    #endregion

    #region 受击状态
    //受击状态
    public class CharacterHitState : CharacterBaseState
    {
        public CharacterHitState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            // 进入受击：播放受击动画/扣血/强制位移/锁定状态
            Context.IsStateLocked = true;
        }

        public override void UpdateState()
        {
            // 受击帧逻辑：判断动画是否结束
            CheckStateTransitions();
        }


        public override void ExitState()
        {
            // 退出受击：重置受击参数/解锁状态
            Context.IsStateLocked = false;
        }

        private void CheckStateTransitions()
        {
            // 受击动画结束 → 闲置/死亡
            AnimatorStateInfo stateInfo = Context.Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsTag("Hit") && stateInfo.normalizedTime >= 1f)
            {
                if (Context._core.GetCharacterData().currentHealth <= 0)
                {
                    SwitchState(CharacterState.Dead);
                }
                else
                {
                    SwitchState(CharacterState.Idle);
                }
            }
        }
    }
    #endregion

    #region 死亡状态
    //死亡状态
    public class CharacterDeadState : CharacterBaseState
    {
        public CharacterDeadState(CharacterStateMachine context, CharacterStateFactory factory) : base(context, factory) { }

        public override void EnterState()
        {
            // 进入死亡：播放死亡动画/禁用输入/禁用物理/销毁组件等
            Context.ResetInputState(); // 重置输入
            Context.characterController.enabled = false;
            Context.IsStateLocked = true;
        }

        public override void UpdateState()
        {
            // 死亡状态：无帧逻辑，或处理死亡后特效/销毁等
        }


        public override void ExitState()
        {
            // 死亡状态不可退出，留空
        }

        // 死亡状态无状态切换判断
    }
    #endregion

    #endregion
}



