using UnityEngine;
using UnityEngine.AI;

namespace WutheringWaves
{
    // 敌人根运动组件挂在Animator所在物体上，负责将动画根运动转交给敌人根物体
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class EnemyRootMotion : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 敌人根运动核心引用 ===")]
        [SerializeField] private EnemyContext context; // 敌人上下文
        [SerializeField] private Animator animator; // 当前物体上的动画控制器
        [SerializeField] private CharacterController characterController; // 敌人根物体角色控制器
        [SerializeField] private NavMeshAgent navMeshAgent; // 敌人根物体导航组件
        #endregion

        #region 调试配置
        [Header("=== 敌人Root Motion调试 ===")]
        [SerializeField] private bool enableRootMotionDebug = false; // 是否开启根运动调试日志
        [SerializeField] private bool onlyLogWhenMoved = true; // 是否只在产生位移或旋转时输出
        #endregion

        #region 初始化
        // 敌人根运动初始化：由EnemyContext统一调用
        public void Initialize(EnemyContext context)
        {
            // 1.缓存敌人上下文
            this.context = context;

            // 2.获取当前模型物体上的Animator
            animator = GetComponent<Animator>();

            // 3.获取敌人根物体上的CharacterController
            characterController = context != null
                ? context.GetComponent<CharacterController>()
                : GetComponentInParent<CharacterController>();

            // 4.获取敌人根物体上的NavMeshAgent
            navMeshAgent = context != null
                ? context.GetComponent<NavMeshAgent>()
                : GetComponentInParent<NavMeshAgent>();

            // 5.验证根运动依赖
            ValidateRequiredComponents();
        }

        // 验证敌人根运动必需组件
        private void ValidateRequiredComponents()
        {
            // 1.敌人上下文为空时无法判断当前状态
            if (context == null)
            {
                Debug.LogError($"敌人根运动组件 {name} 没有获取到 EnemyContext。", this);
            }

            // 2.Animator为空时无法读取动画根运动
            if (animator == null)
            {
                Debug.LogError($"敌人根运动组件 {name} 缺少 Animator。", this);
            }

            // 3.CharacterController为空时无法移动敌人根物体
            if (characterController == null)
            {
                Debug.LogError($"敌人根运动组件 {name} 没有获取到 CharacterController。", this);
            }

            // 4.NavMeshAgent为空时无法同步导航内部位置
            if (navMeshAgent == null)
            {
                Debug.LogError($"敌人根运动组件 {name} 没有获取到 NavMeshAgent。", this);
            }
        }
        #endregion

        #region Animator根运动回调
        private void OnAnimatorMove()
        {
            // 1.初始化未完成时不处理根运动
            if (context == null || animator == null || characterController == null)
            {
                return;
            }

            // 2.获取当前敌人状态机
            EnemyStateMachine stateMachine = context.StateMachine;
            if (stateMachine == null)
            {
                return;
            }

            // 3.当前状态不使用Root Motion时直接忽略
            if (ShouldIgnoreRootMotion(stateMachine.CurrentStateType))
            {
                return;
            }

            // 4.读取动画文件提供的完整根运动数据
            // 位移轴和旋转是否存在，由动画文件中的Bake Into Pose配置决定
            Vector3 rootMotionDeltaMove = animator.deltaPosition;
            Quaternion rootMotionDeltaRotation = animator.deltaRotation;

            // 5.输出根运动调试信息
            LogRootMotionDebug(
                stateMachine.CurrentStateType,
                rootMotionDeltaMove,
                rootMotionDeltaRotation
            );

            // 6.将动画旋转应用到敌人根物体，而不是Animator模型子物体
            context.transform.rotation *= rootMotionDeltaRotation;

            // 7.通过CharacterController移动敌人根物体，使模型与碰撞箱一起移动
            if (characterController.enabled)
            {
                characterController.Move(rootMotionDeltaMove);
            }

            // 8.同步NavMeshAgent内部位置，避免受击后导航仍使用旧位置
            SyncNavMeshAgentPosition();
        }

        // 判断当前状态是否需要忽略Root Motion
        private bool ShouldIgnoreRootMotion(EnemyState currentState)
        {
            // 1.待机、追击由代码和NavMesh管理，不应用动画根运动
            if (currentState == EnemyState.Idle
                || currentState == EnemyState.Chase)
            {
                return true;
            }

            // 2.死亡流程会关闭CharacterController，第一版暂不应用死亡根运动
            if (currentState == EnemyState.Dead)
            {
                return true;
            }

            // 3.其他动作状态默认允许使用动画文件提供的Root Motion
            return false;
        }

        // 同步NavMeshAgent内部位置
        private void SyncNavMeshAgentPosition()
        {
            // 1.NavMeshAgent不可用或不在NavMesh上时不处理
            if (navMeshAgent == null
                || !navMeshAgent.isActiveAndEnabled
                || !navMeshAgent.isOnNavMesh)
            {
                return;
            }

            // 2.将导航内部位置同步到敌人根物体的实际位置
            navMeshAgent.nextPosition = context.transform.position;
        }
        #endregion

        #region 调试输出
        // 输出敌人Root Motion调试信息
        private void LogRootMotionDebug(
            EnemyState currentState,
            Vector3 rootMotionDeltaMove,
            Quaternion rootMotionDeltaRotation)
        {
            // 1.未开启调试时不输出
            if (!enableRootMotionDebug)
            {
                return;
            }

            // 2.判断当前帧是否产生了实际根运动
            bool hasMove = rootMotionDeltaMove.sqrMagnitude > 0.000001f;
            bool hasRotation = Quaternion.Angle(
                Quaternion.identity,
                rootMotionDeltaRotation
            ) > 0.01f;

            // 3.只记录移动帧时，静止帧不输出
            if (onlyLogWhenMoved && !hasMove && !hasRotation)
            {
                return;
            }

            // 4.输出当前状态与动画根运动数据
            Debug.Log(
                $"[EnemyRootMotion] 敌人={context.name} 状态={currentState} " +
                $"deltaMove={rootMotionDeltaMove} " +
                $"deltaRotation={rootMotionDeltaRotation.eulerAngles}",
                this
            );
        }
        #endregion
    }
}