using UnityEngine;

namespace WutheringWaves
{
    #region 敌人状态枚举
    // 敌人核心状态枚举：当前先做待机、追击、受击、死亡
    public enum EnemyState
    {
        Idle, // 待机状态
        Chase, // 追击状态
        Hit,  // 受击状态
        Dead  // 死亡状态
    }
    #endregion

    public class EnemyStateMachine : MonoBehaviour
    {
        #region 1.核心引用 (由CharacterContext自动注入)
        [Header("=== 核心依赖（由CharacterContext自动注入，无需手动赋值）===")]
        [SerializeField] private EnemyContext context; // 敌人上下文
        [SerializeField] private Animator animator; // 敌人动画控制器
        #endregion

        #region 2. FSM核心组件
        public EnemyStateFactory StateFactory { get; private set; } // 敌人状态工厂
        public EnemyStateBase CurrentState { get; private set; } // 当前状态实例
        public EnemyState CurrentStateType { get; private set; } // 当前状态类型

        public EnemyState PreviousStateType { get; private set; } // 上一个状态类型
        public EnemyState PreviousPreviousStateType { get; private set; } // 上上一个状态类型

        public DamageInfo LastDamageInfo { get; private set; } // 最近一次受击信息
        #endregion

        #region 3. 状态共享数据
        public AttackStep currentStep;
        //是否状态锁定
        public bool IsStateLocked { get; set; } = false;
        public EnemyContext Context => context; // 敌人上下文
        public Animator Animator => animator; // 敌人动画控制器
        #endregion

        #region 4.初始化
        // 敌人状态机初始化：由 EnemyContext 统一调用
        public void Initialize(EnemyContext context)
        {
            // 1.缓存敌人上下文
            this.context = context;

            // 2.通过上下文获取动画控制器，不再持有EnemyRuntimeData引用
            animator = context != null ? context.Animator : null;

            // 3.创建状态工厂
            StateFactory = new EnemyStateFactory();

            // 4.注册敌人基础状态
            RegisterBaseStates();

            // 5.初始化上一状态和上上状态记录
            PreviousStateType = EnemyState.Idle;
            PreviousPreviousStateType = EnemyState.Idle;

            // 6.进入默认待机状态
            SwitchState(StateFactory.GetState(EnemyState.Idle));
        }

        // 注册敌人基础状态：第一版注册待机、追击、受击、死亡
        private void RegisterBaseStates()
        {
            // 1.空值保护，避免状态工厂未创建时空引用
            if (StateFactory == null)
            {
                return;
            }

            // 2.注册敌人基础状态
            StateFactory.RegisterState(EnemyState.Idle, new EnemyIdleState(this, StateFactory));
            StateFactory.RegisterState(EnemyState.Chase, new EnemyChaseState(this, StateFactory));
            StateFactory.RegisterState(EnemyState.Hit, new EnemyHitState(this, StateFactory));
            StateFactory.RegisterState(EnemyState.Dead, new EnemyDeadState(this, StateFactory));
        }
        #endregion

        #region  5. FSM核心方法：状态切换（唯一入口，自动执行生命周期）
        // 状态切换统一入口
        internal void SwitchState(EnemyStateBase newState)
        {
            // 1.目标状态为空时不切换
            if (newState == null)
            {
                return;
            }

            // 2.死亡后不允许切回其他状态
            if (CurrentStateType == EnemyState.Dead)
            {
                return;
            }

            // 3.退出旧状态
            CurrentState?.ExitState();

            // 4.将上一状态记录为上上状态
            PreviousPreviousStateType = PreviousStateType;

            // 5.将当前状态记录为上一状态
            PreviousStateType = CurrentStateType;

            // 6.切换当前状态
            CurrentState = newState;
            CurrentStateType = GetCurrentStateType(newState);

            // 7.进入新状态
            CurrentState.EnterState();
        }

        // 根据状态实例解析状态枚举
        private EnemyState GetCurrentStateType(EnemyStateBase state)
        {
            return state switch
            {
                EnemyIdleState => EnemyState.Idle,
                EnemyChaseState => EnemyState.Chase,
                EnemyHitState => EnemyState.Hit,
                EnemyDeadState => EnemyState.Dead,
                _ => EnemyState.Idle
            };
        }
        #endregion

        #region 6.生命周期
        private void Update()
        {
            // 1.当前状态为空时不执行
            if (CurrentState == null)
            {
                return;
            }

            // 2.死亡状态也允许自己保持空更新，避免后续死亡动画扩展时被拦截
            CurrentState.UpdateState();
        }
        #endregion

        #region 7.动画查询
        // 根据敌人动画ID获取动画名称
        public string GetEnemyAnimationName(EnemyAnimationId animationId)
        {
            // 1.空值检查
            if (context == null || context.EnemyDataSO == null || context.EnemyDataSO.animationConfigSO == null)
            {
                return string.Empty;
            }

            // 2.从敌人动画配置中获取动画名称
            return context.EnemyDataSO.animationConfigSO.GetEnemyAnimationName(animationId);
        }

        // 根据敌人动画ID获取动画长度
        public float GetEnemyAnimationLength(EnemyAnimationId animationId)
        {
            // 1.空值检查
            if (context == null || context.EnemyDataSO == null || context.EnemyDataSO.animationConfigSO == null)
            {
                return 0f;
            }

            // 2.从敌人动画配置中获取动画长度
            return context.EnemyDataSO.animationConfigSO.GetEnemyAnimationLength(animationId);
        }
        #endregion

        #region 8.Gizmo绘制（编辑器下显示当前状态）
        // 单个Gizmo文本的配置类
        [System.Serializable]
        public class GizmoLabelSettings
        {
            [Tooltip("是否显示此信息")]
            public bool show = true;

            [Tooltip("文字颜色")]
            public Color color = Color.yellow;

            [Tooltip("相对于敌人根物体的Y轴偏移")]
            public float yOffset = 0f;

            [Tooltip("相对于敌人的X轴偏移")]
            public float xOffset = 0f;

            [Tooltip("字体大小")]
            public int fontSize = 12;

            [Tooltip("字体样式")]
            public FontStyle fontStyle = FontStyle.Bold;
        }

        [Header("=== Gizmo总开关 ===")]
        [Tooltip("是否显示所有敌人状态Gizmo文字")]
        [SerializeField] private bool showAllGizmos = true;

        [Header("=== 各个Gizmo独立配置 ===")]
        [SerializeField]
        private GizmoLabelSettings currentStateGizmo =
            new GizmoLabelSettings { yOffset = 3.6f, color = Color.yellow };

        [SerializeField]
        private GizmoLabelSettings previousStateGizmo =
            new GizmoLabelSettings { yOffset = 3.3f, color = Color.cyan };

        [SerializeField]
        private GizmoLabelSettings previousPreviousStateGizmo =
            new GizmoLabelSettings { yOffset = 3.0f, color = Color.magenta };

        // 编辑器下实时绘制敌人状态Gizmo
        private void OnDrawGizmos()
        {
            // 1.总开关关闭时不绘制
            if (!showAllGizmos)
            {
                return;
            }

#if UNITY_EDITOR
            // 2.绘制当前状态
            if (currentStateGizmo.show)
            {
                DrawGizmoLabel(
                    $"当前状态：{CurrentStateType}",
                    currentStateGizmo
                );
            }

            // 3.绘制上一状态
            if (previousStateGizmo.show)
            {
                DrawGizmoLabel(
                    $"上一状态：{PreviousStateType}",
                    previousStateGizmo
                );
            }

            // 4.绘制上上状态
            if (previousPreviousStateGizmo.show)
            {
                DrawGizmoLabel(
                    $"上上状态：{PreviousPreviousStateType}",
                    previousPreviousStateGizmo
                );
            }
        }
#endif

        // 绘制单个Gizmo文字标签
        private void DrawGizmoLabel(string text, GizmoLabelSettings settings)
        {
#if UNITY_EDITOR
            // 1.计算文字绘制位置
            Vector3 basePosition = transform.position + Vector3.up * settings.yOffset;
            Vector3 finalPosition = basePosition + transform.right * settings.xOffset;

            // 2.设置文字样式
            GUIStyle style = new GUIStyle
            {
                fontSize = settings.fontSize,
                fontStyle = settings.fontStyle,
                normal = { textColor = settings.color }
            };

            // 3.在Scene视图绘制文字
            UnityEditor.Handles.color = settings.color;
            UnityEditor.Handles.Label(finalPosition, text, style);
#endif
        }
    #endregion

        #region 受击请求
        // 敌人受到伤害后，由 EnemyAttributes 调用此方法请求切换状态
        public void RequestHit(DamageInfo damageInfo)
        {
            // 1.缓存最近一次受击信息
            LastDamageInfo = damageInfo;

            // 2.敌人上下文为空时不能判断当前状态
            if (context == null)
            {
                return;
            }

            // 3.敌人已经死亡时进入死亡状态
            if (context.IsDead)
            {
                SwitchState(StateFactory.GetState(EnemyState.Dead));
                return;
            }

            // 4.敌人仍然存活时进入受击状态
            SwitchState(StateFactory.GetState(EnemyState.Hit));
        }
        #endregion
    }
}