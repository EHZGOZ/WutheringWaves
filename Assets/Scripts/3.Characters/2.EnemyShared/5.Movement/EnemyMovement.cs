using UnityEngine;

namespace WutheringWaves
{
    // 敌人移动组件：负责敌人发现目标、追击目标、停止移动和转向
    public class EnemyMovement : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 敌人移动核心引用 ===")]
        [SerializeField] private EnemyContext context; // 敌人上下文
        [SerializeField] private CharacterController characterController; // 敌人角色控制器
        [SerializeField] private Transform target; // 当前追击目标
        [SerializeField] private CharacterController targetCharacterController; // 当前目标角色控制器
        #endregion

        #region 移动配置
        [Header("=== 敌人移动配置 ===")]
        [SerializeField] private float detectRange = 8f; // 发现玩家范围
        [SerializeField] private float stopDistance = 2f; // 停止追击距离，后续可作为攻击距离参考
        [SerializeField] private float collisionStopPadding = 0.8f; // 碰撞预留距离，避免敌人贴到玩家身上
        [SerializeField] private float resumeMovePadding = 0.35f; // 重新追击缓冲距离，避免停止边界反复抖动
        [SerializeField] private float moveSpeed = 2f; // 移动速度
        [SerializeField] private float rotateSpeed = 10f; // 转向速度
        [SerializeField] private bool ignoreTargetBodyCollision = true; // 是否忽略敌人与目标的实体碰撞
        #endregion

        #region 运行时数据
        private bool isStoppingNearTarget; // 是否已经停在目标附近
        private CharacterController ignoredTargetCharacterController; // 当前已忽略碰撞的目标控制器
        #endregion

        #region 对外只读属性
        public Transform Target => target; // 当前目标
        public float DetectRange => detectRange; // 发现范围
        public float StopDistance => stopDistance; // 停止距离
        public float SafeStopDistance => stopDistance + collisionStopPadding; // 实际停止距离：攻击距离 + 碰撞预留距离
        public bool HasTarget => target != null; // 是否存在目标
        #endregion

        #region 生命周期
        private void OnDisable()
        {
            // 1.对象禁用时恢复忽略碰撞，避免对象池复用时残留
            ClearTargetCollisionIgnore();
        }
        #endregion

        #region 初始化
        // 敌人移动初始化：由 EnemyContext 统一调用
        public void Initialize(EnemyContext context)
        {
            // 1.缓存敌人上下文
            this.context = context;

            // 2.自动获取 CharacterController
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            // 3.初始化时先尝试获取当前玩家角色
            RefreshTarget();
        }
        #endregion

        #region 目标检测
        // 刷新当前目标：第一版直接追当前受控角色
        public void RefreshTarget()
        {
            // 1.玩家控制器不存在时，不处理
            if (PlayerController.Instance == null || PlayerController.Instance.CurrentCharacterContext == null)
            {
                target = null;
                targetCharacterController = null;
                ClearTargetCollisionIgnore();
                return;
            }

            // 2.缓存当前受控角色作为敌人目标
            CharacterContext characterContext = PlayerController.Instance.CurrentCharacterContext;
            target = characterContext.transform;
            targetCharacterController = characterContext.CharacterController;

            // 3.忽略敌人与当前目标的实体碰撞，避免玩家和敌人互相推动
            ApplyTargetCollisionIgnore();
        }

        // 判断目标是否在发现范围内
        public bool IsTargetInDetectRange()
        {
            // 1.每次检测前刷新目标，避免切人后敌人还追旧角色
            RefreshTarget();

            // 2.目标为空时，返回 false
            if (target == null)
            {
                return false;
            }

            // 3.只计算水平距离，避免角色模型高度差影响敌人追击判断
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            return direction.sqrMagnitude <= detectRange * detectRange;
        }

        // 判断是否已经到达停止距离
        public bool IsInStopDistance()
        {
            // 1.没有目标时不能判定
            if (target == null)
            {
                isStoppingNearTarget = false;
                return false;
            }

            // 2.只计算水平距离，避免敌人因为 Y 轴差异继续贴脸移动
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            float currentDistance = direction.magnitude;

            // 3.如果已经停住，使用更大的恢复距离，避免边界来回切换造成抖动
            if (isStoppingNearTarget)
            {
                if (currentDistance <= SafeStopDistance + resumeMovePadding)
                {
                    return true;
                }

                isStoppingNearTarget = false;
                return false;
            }

            // 4.第一次进入安全停止距离时停住
            if (currentDistance <= SafeStopDistance)
            {
                isStoppingNearTarget = true;
                return true;
            }

            return false;
        }
        #endregion

        #region 移动执行
        // 追击当前目标
        public void MoveToTarget()
        {
            // 1.移动前刷新目标，避免切人后继续追旧目标
            RefreshTarget();

            // 2.没有目标时不移动
            if (target == null)
            {
                return;
            }

            // 3.到达停止距离后不继续贴脸，只保持朝向
            if (IsInStopDistance())
            {
                StopMove();
                RotateToTarget();
                return;
            }

            // 4.计算水平移动方向
            Vector3 moveDirection = target.position - transform.position;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude <= 0.001f)
            {
                return;
            }

            // 5.计算当前水平距离
            float currentDistance = moveDirection.magnitude;
            moveDirection.Normalize();

            // 6.移动前先朝向目标
            RotateToDirection(moveDirection);

            // 7.限制本帧最大移动距离，避免一步移动直接穿进目标
            float moveDistance = moveSpeed * Time.deltaTime;
            float remainDistance = currentDistance - SafeStopDistance;
            moveDistance = Mathf.Min(moveDistance, remainDistance);

            if (moveDistance <= 0f)
            {
                StopMove();
                return;
            }

            // 8.使用 CharacterController 移动；没有组件时使用 Transform 兜底
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(moveDirection * moveDistance);
            }
            else
            {
                transform.position += moveDirection * moveDistance;
            }
        }

        // 停止移动：第一版先预留，后续可在这里清动画参数
        public void StopMove()
        {

        }

        // 朝向当前目标
        public void RotateToTarget()
        {
            // 1.没有目标时不转向
            if (target == null)
            {
                return;
            }

            // 2.计算目标方向
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            RotateToDirection(direction.normalized);
        }

        // 朝指定方向平滑转向
        private void RotateToDirection(Vector3 direction)
        {
            // 1.方向无效时不处理
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            // 2.平滑旋转到目标方向
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
        #endregion

        #region 碰撞处理
        // 忽略敌人与当前目标的实体碰撞
        private void ApplyTargetCollisionIgnore()
        {
            // 1.配置关闭时恢复旧忽略关系
            if (!ignoreTargetBodyCollision)
            {
                ClearTargetCollisionIgnore();
                return;
            }

            // 2.敌人或目标控制器为空时不处理
            if (characterController == null || targetCharacterController == null)
            {
                return;
            }

            // 3.目标没有变化时不重复设置
            if (ignoredTargetCharacterController == targetCharacterController)
            {
                return;
            }

            // 4.目标变化时先恢复旧目标碰撞
            ClearTargetCollisionIgnore();

            // 5.忽略敌人与当前玩家的 CharacterController 实体碰撞
            Physics.IgnoreCollision(characterController, targetCharacterController, true);
            ignoredTargetCharacterController = targetCharacterController;
        }

        // 恢复敌人与旧目标的实体碰撞
        private void ClearTargetCollisionIgnore()
        {
            // 1.没有旧目标时不处理
            if (ignoredTargetCharacterController == null)
            {
                return;
            }

            // 2.敌人控制器存在时恢复碰撞关系
            if (characterController != null)
            {
                Physics.IgnoreCollision(characterController, ignoredTargetCharacterController, false);
            }

            // 3.清空旧目标记录
            ignoredTargetCharacterController = null;
        }
        #endregion
    }
}