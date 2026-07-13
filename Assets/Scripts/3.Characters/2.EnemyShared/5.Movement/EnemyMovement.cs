using UnityEngine;
using UnityEngine.AI;

namespace WutheringWaves
{
    // 敌人移动组件只能存在一个，并要求根物体拥有CharacterController和NavMeshAgent
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 敌人移动核心引用 ===")]
        [SerializeField] private EnemyContext context; // 敌人上下文
        [SerializeField] private CharacterController characterController; // 敌人实体移动控制器
        [SerializeField] private NavMeshAgent navMeshAgent; // 敌人导航路径计算组件
        [SerializeField] private Transform target; // 当前追击目标，由Boss战控制器指定
        #endregion

        #region 移动配置
        [Header("=== Boss移动配置 ===")]
        [SerializeField] private float stopDistance = 3f; // Boss停止靠近距离
        [SerializeField] private float resumeMovePadding = 0.5f; // 恢复追击缓冲距离
        [SerializeField] private float moveSpeed = 2f; // Boss最大移动速度
        [SerializeField] private float rotateSpeed = 360f; // Boss每秒最大旋转角度
        [SerializeField] private float pathUpdateInterval = 0.1f; // 重新计算目标路径的时间间隔
        [SerializeField] private float navMeshSampleDistance = 2f; // Boss出生点附近的NavMesh查找距离
        #endregion

        #region 运行时数据
        private bool isStoppingNearTarget; // 是否已经停在目标附近
        private float pathUpdateTimer; // 路径刷新计时器
        private bool hasLoggedNavigationFailure; // 是否已经输出导航异常，避免每帧重复警告
        #endregion

        #region 对外只读属性
        public Transform Target => target; // 当前目标
        public float StopDistance => stopDistance; // 停止距离
        public bool HasTarget => target != null; // 是否存在目标
        #endregion

        #region 生命周期
        private void OnDisable()
        {
            // 1.对象禁用时清空目标和路径，避免Boss重用后保留旧目标
            ClearTarget();
        }
        #endregion

        #region 初始化
        // 敌人移动初始化：由EnemyContext统一调用
        public void Initialize(EnemyContext context)
        {
            //1.验证获取组件
            GatAndValidateComponent();

            //2.配置NavMeshAgent的移动控制方式
            InitializeNavMeshAgent();

            //3.将Boss放置并绑定到附近的NavMesh
            if (!TryPlaceOnNavMesh())
            {
                return;
            }

            // 4.初始化时清理旧目标和旧路径
            ClearTarget();
        }
        //1.验证获取组件
        private void  GatAndValidateComponent()
        {
            // 1.缓存敌人上下文
            this.context = context;

            // 2.获取根物体上的实体移动组件
            characterController = GetComponent<CharacterController>();

            // 3.获取根物体上的导航组件
            navMeshAgent = GetComponent<NavMeshAgent>();

            // 4.CharacterController为空时无法执行实体移动
            if (characterController == null)
            {
                Debug.LogError($"敌人 {name} 缺少 CharacterController 组件，无法执行实体移动。", this);
                return;
            }

            // 5.NavMeshAgent为空时无法计算导航路径
            if (navMeshAgent == null)
            {
                Debug.LogError($"敌人 {name} 缺少 NavMeshAgent 组件，无法执行导航寻路。", this);
                return;
            }
        }

        //2.初始化NavMeshAgent配置
        private void InitializeNavMeshAgent()
        {
            // 1.NavMeshAgent只负责路径计算，不直接修改Transform位置
            navMeshAgent.updatePosition = false;

            // 2.关闭自动旋转，由EnemyMovement按当前状态手动控制朝向
            navMeshAgent.updateRotation = false;

            // 3.同步移动配置，确保导航计算与实体移动使用相同参数
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.angularSpeed = rotateSpeed;
            navMeshAgent.stoppingDistance = stopDistance;
            navMeshAgent.autoBraking = true;
        }

        //3.尝试将Boss放置到附近的NavMesh
        private bool TryPlaceOnNavMesh()
        {
            // 1.NavMeshAgent不可用时不能查找NavMesh
            if (navMeshAgent == null || !navMeshAgent.enabled)
            {
                return false;
            }

            // 2.只查找当前Agent Type可以使用的NavMesh
            NavMeshQueryFilter queryFilter = new NavMeshQueryFilter
            {
                agentTypeID = navMeshAgent.agentTypeID,
                areaMask = navMeshAgent.areaMask
            };

            // 3.查找Boss出生点附近的可行走位置
            if (!NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit navMeshHit,
                navMeshSampleDistance,
                queryFilter))
            {
                Debug.LogError(
                    $"敌人 {name} 出生点附近 {navMeshSampleDistance} 米内没有可用NavMesh。",
                    this
                );
                return false;
            }

            // 4.先将导航模拟位置放到有效NavMesh上
            if (!navMeshAgent.Warp(navMeshHit.position))
            {
                Debug.LogError($"敌人 {name} 无法绑定到NavMesh。", this);
                return false;
            }

            // 5.临时关闭CharacterController，安全同步Boss实体位置
            bool wasCharacterControllerEnabled = characterController.enabled;
            characterController.enabled = false;
            transform.position = navMeshHit.position;
            characterController.enabled = wasCharacterControllerEnabled;

            // 6.同步导航模拟位置与Boss实体位置
            navMeshAgent.nextPosition = transform.position;
            hasLoggedNavigationFailure = false;

            return true;
        }
        #endregion

        #region 判断方法

        #endregion

        #region 当前目标
        // 设置当前目标：Boss战开始时由BossBattleController调用
        public void SetTarget(CharacterContext characterContext)
        {
            // 1.目标为空时清空当前目标
            if (characterContext == null)
            {
                ClearTarget();
                return;
            }

            // 2.缓存玩家根节点作为当前导航目标
            target = characterContext.transform;

            // 3.重置停止状态和路径刷新计时
            isStoppingNearTarget = false;
            pathUpdateTimer = pathUpdateInterval;
            hasLoggedNavigationFailure = false;
        }

        // 清空当前目标：Boss战结束、目标离开或目标无效时调用
        public void ClearTarget()
        {
            // 1.清空当前目标和运行时状态
            target = null;
            isStoppingNearTarget = false;
            pathUpdateTimer = 0f;
            hasLoggedNavigationFailure = false;

            // 2.NavMeshAgent不可用时不操作路径
            if (!CanUseNavMeshAgent())
            {
                return;
            }

            // 3.停止导航并清除旧路径
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();

            // 4.同步导航模拟位置，避免重新追击时出现位置跳变
            navMeshAgent.nextPosition = transform.position;
        }
        #endregion

        #region 移动执行
        // 靠近当前目标
        public void MoveToTarget()
        {
            // 1.没有目标时不执行移动
            if (target == null)
            {
                StopMove();
                return;
            }

            // 2.NavMeshAgent不可用时不执行移动
            if (!CanUseNavMeshAgent())
            {
                LogNavigationFailure();
                return;
            }

            hasLoggedNavigationFailure = false;

            // 3.同步当前实体位置，避免导航模拟位置与CharacterController分离
            navMeshAgent.nextPosition = transform.position;

            // 4.到达停止距离后停止，不在追击状态中原地旋转
            if (IsInStopDistance())
            {
                StopMove();
                return;
            }

            // 5.恢复NavMeshAgent路径计算
            navMeshAgent.isStopped = false;

            // 6.按时间间隔刷新玩家目标位置
            UpdateTargetDestination();

            // 7.路径仍在计算或不存在时暂不移动
            if (navMeshAgent.pathPending || !navMeshAgent.hasPath)
            {
                return;
            }

            // 8.读取NavMeshAgent计算出的路径期望速度
            Vector3 moveVelocity = navMeshAgent.desiredVelocity;

            if (moveVelocity.sqrMagnitude <= 0.001f)
            {
                return;
            }

            // 9.限制实体移动速度，避免导航参数异常时突然加速
            moveVelocity = Vector3.ClampMagnitude(moveVelocity, moveSpeed);

            // 10.由CharacterController执行实体移动和碰撞处理
            characterController.Move(moveVelocity * Time.deltaTime);

            // 11.移动后同步NavMeshAgent内部位置
            navMeshAgent.nextPosition = transform.position;

            // 12.只有实际移动时才沿路径方向平滑旋转
            RotateToDirection(moveVelocity);
        }

        // 按固定间隔刷新导航目标位置
        private void UpdateTargetDestination()
        {
            // 1.累计路径刷新计时
            pathUpdateTimer += Time.deltaTime;

            // 2.路径存在且未到刷新时间时继续沿旧路径移动
            if (navMeshAgent.hasPath && pathUpdateTimer < pathUpdateInterval)
            {
                return;
            }

            // 3.重置刷新计时
            pathUpdateTimer = 0f;

            // 4.更新玩家位置并重新计算路径
            if (navMeshAgent.SetDestination(target.position))
            {
                return;
            }

            // 5.路径设置失败时只输出一次警告
            LogNavigationFailure();
        }

        // 停止移动：保留当前路径，方便目标离开恢复距离后继续追击
        public void StopMove()
        {
            // 1.NavMeshAgent不可用时不处理
            if (!CanUseNavMeshAgent())
            {
                return;
            }

            // 2.暂停导航模拟
            navMeshAgent.isStopped = true;

            // 3.保持导航模拟位置与实体位置一致
            navMeshAgent.nextPosition = transform.position;
        }

        // 朝向当前目标：预留给后续攻击准备状态调用
        public void RotateToTarget()
        {
            // 1.没有目标时不转向
            if (target == null)
            {
                return;
            }

            // 2.计算目标水平方向
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            RotateToDirection(direction);
        }

        // 朝指定方向平滑转向
        private void RotateToDirection(Vector3 direction)
        {
            // 1.旋转只使用水平方向
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            // 2.计算目标朝向
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

            // 3.按每秒最大旋转角度平滑转向，避免瞬间改变朝向
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
        }
        #endregion

        #region 距离判断
        // 判断是否已经到达停止距离
        public bool IsInStopDistance()
        {
            // 1.没有目标时不能判定
            if (target == null)
            {
                isStoppingNearTarget = false;
                return false;
            }

            // 2.计算Boss与目标的水平直线距离
            Vector3 targetDirection = target.position - transform.position;
            targetDirection.y = 0f;
            float horizontalDistance = targetDirection.magnitude;

            // 3.已经停止时使用恢复缓冲，避免边界附近频繁启停
            if (isStoppingNearTarget)
            {
                if (horizontalDistance <= stopDistance + resumeMovePadding)
                {
                    return true;
                }

                // 4.玩家离开恢复距离后重新追击，并立即刷新路径
                isStoppingNearTarget = false;
                pathUpdateTimer = pathUpdateInterval;
                return false;
            }

            // 5.优先使用导航路径剩余距离，避免隔墙时按直线距离错误停止
            float currentDistance = GetCurrentNavigationDistance(horizontalDistance);

            // 6.第一次进入停止距离时停止
            if (currentDistance <= stopDistance)
            {
                isStoppingNearTarget = true;
                return true;
            }

            return false;
        }

        // 获取当前导航距离
        private float GetCurrentNavigationDistance(float fallbackDistance)
        {
            // 1.路径无效或正在计算时使用水平距离兜底
            if (!CanUseNavMeshAgent()
                || navMeshAgent.pathPending
                || !navMeshAgent.hasPath)
            {
                return fallbackDistance;
            }

            // 2.剩余距离无效时使用水平距离兜底
            float remainingDistance = navMeshAgent.remainingDistance;
            if (float.IsInfinity(remainingDistance) || float.IsNaN(remainingDistance))
            {
                return fallbackDistance;
            }

            // 3.返回沿NavMesh路径计算的剩余距离
            return remainingDistance;
        }
        #endregion

        #region 导航校验
        // 判断NavMeshAgent当前是否可以正常使用
        private bool CanUseNavMeshAgent()
        {
            return navMeshAgent != null
                && navMeshAgent.isActiveAndEnabled
                && navMeshAgent.isOnNavMesh;
        }

        // 输出导航异常：防止追击状态每帧重复输出
        private void LogNavigationFailure()
        {
            // 1.已经输出过时不重复输出
            if (hasLoggedNavigationFailure)
            {
                return;
            }

            hasLoggedNavigationFailure = true;
            Debug.LogWarning($"敌人 {name} 当前无法获得有效NavMesh路径。", this);
        }
        #endregion
    }
}