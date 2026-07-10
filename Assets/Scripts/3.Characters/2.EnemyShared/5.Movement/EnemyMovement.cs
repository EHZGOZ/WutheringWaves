using UnityEngine;

namespace WutheringWaves
{
    // 敌人移动组件只能存在一个，并要求根物体拥有CharacterController
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class EnemyMovement : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 敌人移动核心引用 ===")]
        [SerializeField] private EnemyContext context; // 敌人上下文
        [SerializeField] private CharacterController characterController; // 敌人角色控制器
        [SerializeField] private Transform target; // 当前追击目标，由 Boss 战控制器指定
        [SerializeField] private CharacterController targetCharacterController; // 当前目标角色控制器
        #endregion

        #region 移动配置
        [Header("=== Boss 移动配置 ===")]
        [SerializeField] private float stopDistance = 2.5f; // Boss 停止靠近距离，后续可和攻击距离分开配置
        [SerializeField] private float resumeMovePadding = 0.35f; // 重新靠近缓冲距离，避免停止边界反复抖动
        [SerializeField] private float moveSpeed = 2f; // Boss 靠近目标速度
        [SerializeField] private float rotateSpeed = 10f; // Boss 转向速度
        [SerializeField] private bool ignoreTargetBodyCollision = true; // 是否忽略 Boss 与目标的实体碰撞
        #endregion

        #region 运行时数据
        private bool isStoppingNearTarget; // 是否已经停在目标附近
        private CharacterController ignoredTargetCharacterController; // 当前已忽略碰撞的目标控制器
        #endregion

        #region 对外只读属性
        public Transform Target => target; // 当前目标
        public float StopDistance => stopDistance; // 停止距离
        public bool HasTarget => target != null; // 是否存在目标
        #endregion

        #region 生命周期
        private void OnDisable()
        {
            // 1.对象禁用时恢复忽略碰撞，避免对象池或场景重开时残留
            ClearTargetCollisionIgnore();
        }
        #endregion

        #region 初始化
        // 敌人移动初始化：由 EnemyContext 统一调用
        public void Initialize(EnemyContext context)
        {
            // 1.缓存敌人上下文
            this.context = context;

            // 2.获取同一物体上的CharacterController
            characterController = GetComponent<CharacterController>();

            // 3.旧预制体可能是在添加RequireComponent之前创建的，因此仍然进行运行时校验
            if (characterController == null)
            {
                Debug.LogError($"敌人 {name} 缺少 CharacterController 组件，无法正常执行移动和碰撞处理。", this);
                return;
            }

            // 4.Boss目标由BossBattleController指定，初始化时清理旧目标
            ClearTarget();
        }
        #endregion

        #region 目标控制
        // 设置当前目标：Boss 战开始时由 BossBattleController 调用
        public void SetTarget(CharacterContext characterContext)
        {
            // 1.目标为空时清空当前目标
            if (characterContext == null)
            {
                ClearTarget();
                return;
            }

            // 2.缓存目标 Transform 与 CharacterController
            target = characterContext.transform;
            targetCharacterController = characterContext.CharacterController;

            // 3.重置停止状态，避免切换目标后 Boss 不重新判断距离
            isStoppingNearTarget = false;

            // 4.忽略 Boss 与当前目标的实体碰撞，避免玩家和 Boss 互相卡住
            ApplyTargetCollisionIgnore();
        }

        // 清空当前目标：Boss 战结束、目标离开或目标无效时调用
        public void ClearTarget()
        {
            // 1.清空目标前恢复旧目标碰撞
            ClearTargetCollisionIgnore();

            // 2.清空目标引用
            target = null;
            targetCharacterController = null;

            // 3.重置停止状态，避免下次重新追击时残留
            isStoppingNearTarget = false;
        }

        // 判断目标是否有效：保留这个方法，兼容 EnemyState 当前的调用
        public bool IsTargetInDetectRange()
        {
            // 1.Boss 版不做范围检测，只判断是否已经由外部指定目标
            return target != null;
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

            // 2.只计算水平距离，避免高度差影响 Boss 站位判断
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            float currentDistance = direction.magnitude;

            // 3.如果已经停住，使用更大的恢复距离，避免停止边界反复抖动
            if (isStoppingNearTarget)
            {
                if (currentDistance <= stopDistance + resumeMovePadding)
                {
                    return true;
                }

                isStoppingNearTarget = false;
                return false;
            }

            // 4.第一次进入停止距离时停住
            if (currentDistance <= stopDistance)
            {
                isStoppingNearTarget = true;
                return true;
            }

            return false;
        }
        #endregion

        #region 移动执行
        // 靠近当前目标
        public void MoveToTarget()
        {
            // 1.没有目标时不移动
            if (target == null)
            {
                return;
            }

            // 2.到达停止距离后不继续贴近，只保持朝向
            if (IsInStopDistance())
            {
                StopMove();
                RotateToTarget();
                return;
            }

            // 3.计算水平移动方向
            Vector3 moveDirection = target.position - transform.position;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude <= 0.001f)
            {
                return;
            }

            // 4.计算当前水平距离
            float currentDistance = moveDirection.magnitude;
            moveDirection.Normalize();

            // 5.移动前先朝向目标
            RotateToDirection(moveDirection);

            // 6.限制本帧最大移动距离，避免一步移动超过停止距离
            float moveDistance = moveSpeed * Time.deltaTime;
            float remainDistance = currentDistance - stopDistance;
            moveDistance = Mathf.Min(moveDistance, remainDistance);

            if (moveDistance <= 0f)
            {
                StopMove();
                return;
            }

            // 7.使用 CharacterController 移动；没有组件时使用 Transform 兜底
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(moveDirection * moveDistance);
            }
            else
            {
                transform.position += moveDirection * moveDistance;
            }
        }

        // 停止移动：第一版先预留，后续可在这里清 Boss 移动动画参数
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
        // 忽略 Boss 与当前目标的实体碰撞
        private void ApplyTargetCollisionIgnore()
        {
            // 1.配置关闭时恢复旧忽略关系
            if (!ignoreTargetBodyCollision)
            {
                ClearTargetCollisionIgnore();
                return;
            }

            // 2.Boss 或目标控制器为空时不处理
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

            // 5.忽略 Boss 与当前玩家的 CharacterController 实体碰撞
            Physics.IgnoreCollision(characterController, targetCharacterController, true);
            ignoredTargetCharacterController = targetCharacterController;
        }

        // 恢复 Boss 与旧目标的实体碰撞
        private void ClearTargetCollisionIgnore()
        {
            // 1.没有旧目标时不处理
            if (ignoredTargetCharacterController == null)
            {
                return;
            }

            // 2.Boss 控制器存在时恢复碰撞关系
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