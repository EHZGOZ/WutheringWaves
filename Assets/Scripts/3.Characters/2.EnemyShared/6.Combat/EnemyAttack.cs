using UnityEngine;

namespace WutheringWaves
{
    // 敌人攻击组件：负责攻击距离、攻击冷却和攻击开始逻辑
    public class EnemyAttack : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 敌人攻击核心引用 ===")]
        [SerializeField] private EnemyContext context; // 敌人上下文
        #endregion

        #region 攻击配置
        [Header("=== 敌人攻击配置 ===")]
        [SerializeField] private float attackRange = 3f; // 攻击判定距离，建议略大于 EnemyMovement 的 SafeStopDistance
        [SerializeField] private float attackCooldown = 1.5f; // 攻击冷却时间
        [SerializeField] private float attackStateDuration = 0.6f; // 攻击状态持续时间，后续可替换为动画长度
        #endregion

        #region 运行时数据
        private float nextAttackTime; // 下一次允许攻击的时间
        #endregion

        #region 对外只读属性
        public float AttackRange => attackRange; // 攻击距离
        public float AttackStateDuration => attackStateDuration; // 攻击状态持续时间
        #endregion

        #region 初始化
        // 敌人攻击初始化：由 EnemyContext 统一调用
        public void Initialize(EnemyContext context)
        {
            // 1.缓存敌人上下文
            this.context = context;

            // 2.初始化攻击时间，允许敌人进入攻击范围后立刻攻击
            nextAttackTime = 0f;
        }
        #endregion

        #region 攻击判断
        // 判断当前是否可以攻击目标
        public bool CanAttackTarget()
        {
            // 1.上下文为空时不能攻击
            if (context == null)
            {
                return false;
            }

            // 2.敌人死亡后不能攻击
            if (context.IsDead)
            {
                return false;
            }

            // 3.移动组件或目标为空时不能攻击
            if (context.MovementLogic == null || context.MovementLogic.Target == null)
            {
                return false;
            }

            // 4.攻击冷却未结束时不能攻击
            if (Time.time < nextAttackTime)
            {
                return false;
            }

            // 5.只计算水平距离，避免高度差影响攻击距离判断
            Vector3 direction = context.MovementLogic.Target.position - transform.position;
            direction.y = 0f;

            return direction.sqrMagnitude <= attackRange * attackRange;
        }
        #endregion

        #region 攻击执行
        // 开始攻击：当前阶段只记录冷却和输出日志，后续再接动画事件与伤害判定
        public void BeginAttack()
        {
            // 1.记录下一次可攻击时间
            nextAttackTime = Time.time + attackCooldown;

            // 2.当前先用日志确认攻击流程是否能跑通
            Debug.Log($"敌人 {name} 开始攻击。");
        }
        #endregion
    }
}