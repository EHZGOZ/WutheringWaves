using UnityEngine;

namespace WutheringWaves
{
    public class EnemyContext : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 敌人核心数据 ===")]
        [Header("敌人基础数据：手动填入")]
        [SerializeField] private EnemyDataSO enemyDataSO; // 敌人静态配置

        [Header("=== 敌人核心组件（自动获取） ===")]
        [SerializeField] private EnemyAttributes enemyAttributes; // 敌人属性组件
        [SerializeField] private EnemyStateMachine stateMachine; // 敌人状态机
        [SerializeField] private EnemyMovement movementLogic; // 敌人移动逻辑

        [SerializeField] private Animator animator; // 敌人动画控制器
        [SerializeField] private Collider[] enemyColliders; // 敌人碰撞体列表
        #endregion

        #region 死亡配置
        [Header("=== 敌人死亡配置 ===")]
        [SerializeField] private bool disableObjectOnDead = false; // 死亡后是否隐藏整个敌人对象
        [SerializeField] private float deadDisableDelay = 2f; // 死亡后延迟隐藏时间
        #endregion

        #region 运行时状态
        private bool hasEnteredDead; // 是否已经进入死亡流程
        #endregion

        #region 对外只读属性
        public EnemyDataSO EnemyDataSO => enemyDataSO; // 敌人基础数据
        public EnemyAttributes EnemyAttributes => enemyAttributes; // 敌人属性组件
        public EnemyRuntimeData RuntimeData => enemyAttributes != null ? enemyAttributes.RuntimeData : null; // 敌人运行时数据
        public EnemyStateMachine StateMachine => stateMachine; // 敌人状态机
        public EnemyMovement MovementLogic => movementLogic; // 敌人移动逻辑
        public Animator Animator => animator; // 敌人动画控制器
        public Collider[] EnemyColliders => enemyColliders; // 敌人碰撞体列表

        public bool IsDead => enemyAttributes != null && enemyAttributes.IsDead; // 是否已经死亡
        #endregion

        #region 生命周期
        private void Awake()
        {
            // 1.敌人生成时初始化
            Initialize();
        }
        #endregion

        #region 初始化
        // 敌人初始化入口：统一初始化组件、属性、移动和状态机
        public void Initialize()
        {
            // 1.重置死亡流程标记，避免对象池复用时状态残留
            hasEnteredDead = false;

            // 2.自动获取敌人核心组件
            AutoGetCoreComponents();

            // 3.初始化敌人属性组件
            InitializeEnemyAttributes();

            // 4.初始化敌人移动组件
            InitializeEnemyMovement();

            // 5.初始化敌人状态机
            InitializeStateMachine();
        }

        // 自动获取敌人核心组件
        private void AutoGetCoreComponents()
        {
            // 1.获取敌人属性组件
            if (enemyAttributes == null)
            {
                enemyAttributes = GetComponent<EnemyAttributes>();
            }

            // 2.获取敌人状态机
            if (stateMachine == null)
            {
                stateMachine = GetComponent<EnemyStateMachine>();
            }

            // 3.获取敌人移动逻辑
            if (movementLogic == null)
            {
                movementLogic = GetComponent<EnemyMovement>();
            }

            // 4.获取敌人动画控制器
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            // 5.获取敌人所有碰撞体，死亡时统一关闭
            if (enemyColliders == null || enemyColliders.Length == 0)
            {
                enemyColliders = GetComponentsInChildren<Collider>(true);
            }
        }

        // 初始化敌人属性组件
        private void InitializeEnemyAttributes()
        {
            // 1.属性组件为空时提示，敌人将无法被 IDamageable 正常命中
            if (enemyAttributes == null)
            {
                Debug.LogError($"敌人 {name} 缺少 EnemyAttributes 组件。", this);
                return;
            }

            // 2.由 EnemyAttributes 接管生命值、受伤、恢复和死亡判定
            enemyAttributes.Initialize(this);
        }
        // 初始化敌人移动组件
        private void InitializeEnemyMovement()
        {
            // 1.移动组件为空时提示，敌人将无法追击玩家
            if (movementLogic == null)
            {
                Debug.LogError($"敌人 {name} 缺少 EnemyMovement 组件。", this);
                return;
            }

            // 2.由 EnemyMovement 接管发现目标、追击、停止和转向
            movementLogic.Initialize(this);
        }
        // 初始化敌人状态机
        private void InitializeStateMachine()
        {
            // 1.状态机为空时，只输出提示，不阻断敌人基础属性流程
            if (stateMachine == null)
            {
                Debug.LogError($"敌人 {name} 缺少 EnemyStateMachine 组件。", this);
                return;
            }

            // 2.由状态机接管敌人状态生命周期
            stateMachine.Initialize(this);
        }
        #endregion

        #region 死亡表现
        // 进入死亡状态：由 EnemyDeadState 调用
        public void OnDeadStateEntered()
        {
            // 1.防止死亡逻辑重复执行
            if (hasEnteredDead)
            {
                return;
            }

            hasEnteredDead = true;

            // 2.当前阶段先输出日志，确认死亡流程能被触发
            Debug.Log($"敌人 {name} 已死亡。");

            // 3.关闭碰撞体，避免死亡后继续被命中或阻挡角色
            DisableEnemyColliders();

            // 4.后续这里可以补死亡动画、掉落、经验、事件通知
            if (disableObjectOnDead)
            {
                Invoke(nameof(DisableEnemyObject), deadDisableDelay);
            }
        }

        // 关闭敌人碰撞体
        private void DisableEnemyColliders()
        {
            // 1.碰撞体列表为空时不处理
            if (enemyColliders == null)
            {
                return;
            }

            // 2.逐个关闭碰撞体
            for (int i = 0; i < enemyColliders.Length; i++)
            {
                Collider enemyCollider = enemyColliders[i];
                if (enemyCollider == null)
                {
                    continue;
                }

                enemyCollider.enabled = false;
            }
        }

        // 隐藏敌人对象
        private void DisableEnemyObject()
        {
            gameObject.SetActive(false);
        }
        #endregion
    }
}