using UnityEngine;
using UnityEngine.AI;

namespace WutheringWaves
{
    // 敌人移动组件只能存在一个，并要求根物体拥有目标、实体移动和导航组件
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyTargeting))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 敌人移动核心引用 ===")]
        [SerializeField] private EnemyContext context; // 敌人共享上下文
        [SerializeField] private EnemyTargeting targetingLogic; // 敌人目标管理逻辑
        [SerializeField] private CharacterController characterController; // 敌人实体移动控制器
        [SerializeField] private NavMeshAgent navMeshAgent; // 敌人导航路径计算组件
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
        private Transform lastNavigationTarget; // 上一次用于计算路径的目标，仅用于检测目标变化
        #endregion

        #region 对外只读属性
        public float StopDistance => stopDistance; // 停止距离
        #endregion

        #region 生命周期
        private void OnDisable()
        {
            // 1.组件禁用时只清理导航运行状态
            // 目标本身由EnemyTargeting的OnDisable负责清理
            ResetNavigation();
        }
        #endregion

        #region 初始化
        // 敌人移动初始化：由EnemyContext统一调用
        public void Initialize(EnemyContext context)
        {
            // 1.缓存敌人共享上下文
            this.context = context;

            // 2.获取并验证敌人移动所需组件
            if (!GetAndValidateComponents())
            {
                return;
            }

            // 3.配置NavMeshAgent的移动控制方式
            InitializeNavMeshAgent();

            // 4.将Boss放置并绑定到附近的NavMesh
            if (!TryPlaceOnNavMesh())
            {
                return;
            }

            // 5.初始化导航运行状态，不清除EnemyTargeting中的目标
            ResetNavigation();
        }

        // 获取并验证EnemyMovement需要的组件
        private bool GetAndValidateComponents()
        {
            // 1.EnemyContext为空时无法完成统一初始化
            if (context == null)
            {
                Debug.LogError($"敌人 {name} 的EnemyMovement没有获得EnemyContext。", this);
                return false;
            }

            // 2.获取敌人目标管理组件
            targetingLogic = GetComponent<EnemyTargeting>();

            // 3.获取根物体上的实体移动组件
            characterController = GetComponent<CharacterController>();

            // 4.获取根物体上的导航组件
            navMeshAgent = GetComponent<NavMeshAgent>();

            // 5.记录组件验证结果，统一结束初始化
            bool isValid = true;

            if (targetingLogic == null)
            {
                Debug.LogError($"敌人 {name} 缺少 EnemyTargeting 组件，无法获取追击目标。", this);
                isValid = false;
            }

            if (characterController == null)
            {
                Debug.LogError($"敌人 {name} 缺少 CharacterController 组件，无法执行实体移动。", this);
                isValid = false;
            }

            if (navMeshAgent == null)
            {
                Debug.LogError($"敌人 {name} 缺少 NavMeshAgent 组件，无法执行导航寻路。", this);
                isValid = false;
            }

            return isValid;
        }

        // 初始化NavMeshAgent配置
        private void InitializeNavMeshAgent()
        {
            // 1.NavMeshAgent只负责路径计算，不直接修改Transform位置
            navMeshAgent.updatePosition = false;

            // 2.关闭自动旋转，由EnemyMovement手动控制朝向
            navMeshAgent.updateRotation = false;

            // 3.同步移动配置，保证导航计算与实体移动参数一致
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.angularSpeed = rotateSpeed;
            navMeshAgent.stoppingDistance = stopDistance;
            navMeshAgent.autoBraking = true;
        }

        // 尝试将Boss放置到附近的NavMesh
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

            return true;
        }
        #endregion

        #region 追击判断
        // 判断当前是否允许进入追击状态
        public bool IsChaseAvailable()
        {
            // 1.直接从EnemyTargeting读取当前目标
            Transform currentTarget =
                targetingLogic.TargetTransform;

            // 2.必须存在有效目标
            bool hasValidTarget =
                currentTarget != null;

            // 3.NavMeshAgent必须处于可用状态
            bool canUseNavigation =
                CanUseNavMeshAgent();

            // 4.目标或导航不可用时不能进入追击
            if (!hasValidTarget || !canUseNavigation)
            {
                return false;
            }

            // 5.计算Boss与目标的水平距离
            Vector3 targetDirection =
                currentTarget.position - transform.position;

            targetDirection.y = 0f;

            float horizontalDistance =
                targetDirection.magnitude;

            float resumeMoveDistance =
                stopDistance + resumeMovePadding;

            // 6.已经停近目标时使用实时水平距离
            // 停止期间旧路径可能没有继续更新
            float currentDistance = isStoppingNearTarget
                ? horizontalDistance
                : GetCurrentNavigationDistance(horizontalDistance);

            // 7.目标离开恢复距离后才允许重新追击
            return currentDistance > resumeMoveDistance;
        }
        #endregion

        #region 移动执行

        #region 移动
        // 靠近EnemyTargeting当前提供的目标
        // 此方法只负责组织移动流程，具体步骤由下方小方法完成
        public void MoveToTarget()
        {
            // 1.尝试获取当前移动目标
            if (!TryGetCurrentMoveTarget(
                out Transform currentTarget))
            {
                StopMove();
                return;
            }

            // 2.检查并准备NavMeshAgent进入移动状态
            if (!TryPrepareNavigationForMove())
            {
                return;
            }

            // 3.检测导航目标是否发生变化
            DetectNavigationTargetChanged(currentTarget);

            // 4.按固定间隔提交目标的最新位置
            UpdateTargetDestination(currentTarget);

            // 5.尝试从当前路径获得有效移动速度
            if (!TryGetNavigationVelocity(
                out Vector3 moveVelocity))
            {
                return;
            }

            // 6.应用本帧导航移动
            ApplyNavigationMovement(moveVelocity);
        }

        // 尝试获取EnemyTargeting当前提供的移动目标
        private bool TryGetCurrentMoveTarget(
            out Transform currentTarget)
        {
            // 1.从EnemyTargeting读取当前目标根节点
            currentTarget =
                targetingLogic.TargetTransform;

            // 2.目标存在时允许继续执行移动流程
            return currentTarget != null;
        }

        // 尝试让NavMeshAgent准备进入移动状态
        private bool TryPrepareNavigationForMove()
        {
            // 1.NavMeshAgent不可用时不能执行导航移动
            if (!CanUseNavMeshAgent())
            {
                return false;
            }

            // 2.移动前同步Agent内部位置与Boss实体位置
            // 避免关闭updatePosition后两者逐渐分离
            navMeshAgent.nextPosition =
                transform.position;

            // 3.恢复NavMeshAgent的路径跟随
            navMeshAgent.isStopped = false;

            return true;
        }

        // 尝试从当前导航路径获得有效移动速度
        private bool TryGetNavigationVelocity(
            out Vector3 moveVelocity)
        {
            // 1.默认没有可使用的移动速度
            moveVelocity = Vector3.zero;

            // 2.路径仍在计算时暂不移动
            if (navMeshAgent.pathPending)
            {
                return false;
            }

            // 3.当前没有路径时暂不移动
            if (!navMeshAgent.hasPath)
            {
                return false;
            }

            // 4.读取Agent根据当前路径计算出的期望速度
            moveVelocity =
                navMeshAgent.desiredVelocity;

            // 5.速度过小时不执行实体移动和转向
            if (moveVelocity.sqrMagnitude <= 0.001f)
            {
                moveVelocity = Vector3.zero;
                return false;
            }

            // 6.限制移动速度，避免导航参数异常时突然加速
            moveVelocity =
                Vector3.ClampMagnitude(
                    moveVelocity,
                    moveSpeed
                );

            return true;
        }

        // 将有效导航速度应用到Boss实体
        private void ApplyNavigationMovement(
            Vector3 moveVelocity)
        {
            // 1.由CharacterController执行实体移动和碰撞处理
            characterController.Move(
                moveVelocity * Time.deltaTime
            );

            // 2.移动后同步Agent内部位置与Boss实体位置
            navMeshAgent.nextPosition =
                transform.position;

            // 3.沿本帧实际移动方向平滑旋转Boss
            RotateToDirection(moveVelocity);
        }

        #endregion

        #region 停止
        // 停止移动：暂停导航但保留当前路径
        public void StopMove()
        {
            // 1.NavMeshAgent不可用时不处理
            if (!CanUseNavMeshAgent())
            {
                return;
            }

            // 2.暂停导航模拟
            navMeshAgent.isStopped = true;

            // 3.保持导航模拟位置与Boss实体位置一致
            navMeshAgent.nextPosition =
                transform.position;
            #endregion
        }
        #endregion

        #region 导航相关
        // 检测本次导航目标是否发生变化
        private void DetectNavigationTargetChanged(
            Transform currentTarget)
        {
            // 1.目标引用没有变化时继续使用正常刷新间隔
            if (lastNavigationTarget == currentTarget)
            {
                return;
            }

            // 2.记录新的导航目标
            // 这里只是路径刷新缓存，不拥有目标本身
            lastNavigationTarget = currentTarget;

            // 3.让下一次目标位置提交立即执行
            // 不清除旧路径，避免切人时出现短暂停顿
            pathUpdateTimer = pathUpdateInterval;
        }

        // 按固定间隔刷新导航目标位置
        private void UpdateTargetDestination(
            Transform currentTarget)
        {
            // 1.累计路径刷新计时
            pathUpdateTimer += Time.deltaTime;

            // 2.路径存在且未到刷新时间时继续沿旧路径移动
            if (navMeshAgent.hasPath
                && pathUpdateTimer < pathUpdateInterval)
            {
                return;
            }

            // 3.重置路径刷新计时
            pathUpdateTimer = 0f;

            // 4.提交当前目标的最新位置并请求路径
            if (navMeshAgent.SetDestination(
                currentTarget.position))
            {
                return;
            }
        }

        // 重置移动导航：不负责决定敌人的目标
        public void ResetNavigation()
        {
            // 1.重置EnemyMovement内部运行时数据
            isStoppingNearTarget = false;
            pathUpdateTimer = 0f;
            lastNavigationTarget = null;

            // 2.NavMeshAgent不可用时不继续操作
            if (!CanUseNavMeshAgent())
            {
                return;
            }

            // 3.停止导航并清除旧路径
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();

            // 4.同步导航模拟位置与Boss实体位置
            navMeshAgent.nextPosition = transform.position;
        }
        #endregion

        #region 转向执行
        // 朝向EnemyTargeting当前提供的目标
        public void RotateToTarget()
        {
            // 1.直接从EnemyTargeting读取当前目标
            Transform currentTarget =
                targetingLogic.TargetTransform;

            // 2.没有目标时不转向
            if (currentTarget == null)
            {
                return;
            }

            // 3.计算目标水平方向
            Vector3 direction =
                currentTarget.position - transform.position;

            direction.y = 0f;

            // 4.朝目标方向平滑旋转
            RotateToDirection(direction);
        }

        // 朝指定方向平滑转向
        private void RotateToDirection(Vector3 direction)
        {
            // 1.旋转只使用水平方向
            direction.y = 0f;

            // 2.方向过小时不执行旋转
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            // 3.计算目标朝向
            Quaternion targetRotation =
                Quaternion.LookRotation(direction.normalized);

            // 4.按每秒最大旋转角度平滑转向
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
            // 1.直接从EnemyTargeting读取当前目标
            Transform currentTarget =
                targetingLogic.TargetTransform;

            // 2.没有目标时不能判定
            if (currentTarget == null)
            {
                isStoppingNearTarget = false;
                return false;
            }

            // 3.计算Boss与目标的水平直线距离
            Vector3 targetDirection =
                currentTarget.position - transform.position;

            targetDirection.y = 0f;

            float horizontalDistance =
                targetDirection.magnitude;

            // 4.已经停止时使用恢复缓冲
            // 避免边界附近频繁切换追击状态
            if (isStoppingNearTarget)
            {
                if (horizontalDistance
                    <= stopDistance + resumeMovePadding)
                {
                    return true;
                }

                // 5.玩家离开恢复距离后重新追击
                // 同时要求下一次移动立即提交最新目的地
                isStoppingNearTarget = false;
                pathUpdateTimer = pathUpdateInterval;
                return false;
            }

            // 6.优先使用导航路径剩余距离
            // 避免隔墙时按照直线距离错误停止
            float currentDistance =
                GetCurrentNavigationDistance(
                    horizontalDistance
                );

            // 7.第一次进入停止距离时记录停止阶段
            if (currentDistance <= stopDistance)
            {
                isStoppingNearTarget = true;
                return true;
            }

            return false;
        }

        // 获取当前导航路径距离
        private float GetCurrentNavigationDistance(
            float fallbackDistance)
        {
            // 1.路径无效或正在计算时使用水平距离兜底
            if (!CanUseNavMeshAgent()
                || navMeshAgent.pathPending
                || !navMeshAgent.hasPath)
            {
                return fallbackDistance;
            }

            // 2.读取沿当前NavMesh路径计算的剩余距离
            float remainingDistance =
                navMeshAgent.remainingDistance;

            // 3.剩余距离无效时使用水平距离兜底
            if (float.IsInfinity(remainingDistance)
                || float.IsNaN(remainingDistance))
            {
                return fallbackDistance;
            }

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
        #endregion
    }
}