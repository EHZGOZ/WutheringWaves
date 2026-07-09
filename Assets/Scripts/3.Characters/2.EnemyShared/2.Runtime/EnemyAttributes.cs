using System;
using UnityEngine;

namespace WutheringWaves
{
    // 敌人属性组件：负责敌人的生命值、受伤、恢复、死亡判定，并向外发出属性事件
    public class EnemyAttributes : MonoBehaviour, IDamageable
    {
        #region 核心引用
        [Header("=== 敌人属性核心引用 ===")]
        [SerializeField] private EnemyContext context; // 敌人上下文
        [SerializeField] private EnemyStateMachine stateMachine; // 敌人状态机
        [SerializeField] private EnemyRuntimeData runtimeData = new(); // 敌人运行时数据
        #endregion

        #region 属性事件
        public event Action<EnemyAttributes, float, float, float> OnHealthChanged; // 生命值变化事件：当前生命值、最大生命值、生命值百分比
        public event Action<EnemyAttributes, DamageInfo> OnDamaged; // 受伤事件：后续受击特效、音效、飘字可以监听
        public event Action<EnemyAttributes, float> OnHealed; // 恢复事件：后续治疗表现可以监听
        public event Action<EnemyAttributes> OnDead; // 死亡事件：后续掉落、经验、任务计数可以监听
        #endregion

        #region 对外只读属性
        public EnemyRuntimeData RuntimeData => runtimeData; // 敌人运行时数据
        public bool IsDead => runtimeData != null && runtimeData.IsDead; // 是否已经死亡
        #endregion

        #region 初始化
        // 敌人属性初始化：由 EnemyContext 统一调用
        public void Initialize(EnemyContext context)
        {
            // 1.缓存敌人上下文
            this.context = context;

            // 2.确保运行时数据对象存在
            if (runtimeData == null)
            {
                runtimeData = new EnemyRuntimeData();
            }

            // 3.从敌人静态配置中初始化运行时数据
            runtimeData.Initialize(context != null ? context.EnemyDataSO : null);

            // 4.缓存敌人状态机，受伤后由这里通知状态切换
            stateMachine = context != null ? context.StateMachine : null;

            // 5.初始化完成后主动通知一次生命值，方便后续血条绑定时刷新初始显示
            NotifyHealthChanged();
        }
        #endregion

        #region 受伤与恢复
        // 受到伤害：IDamageable接口入口，玩家攻击命中后会调用这里
        public void TakeDamage(DamageInfo damageInfo)
        {
            // 1.空值校验，避免无效伤害信息
            if (damageInfo == null || runtimeData == null)
            {
                return;
            }

            // 2.已经死亡时不再重复处理伤害
            if (runtimeData.IsDead)
            {
                return;
            }

            // 3.记录扣血前是否存活，用于判断是否刚刚死亡
            bool wasAlive = !runtimeData.IsDead;

            // 4.扣除生命值
            runtimeData.TakeDamage(damageInfo.damage);

            // 5.通知生命值变化和受伤事件
            NotifyHealthChanged();
            OnDamaged?.Invoke(this, damageInfo);

            // 6.调试输出，方便验证伤害链路
            Debug.Log($"敌人 {name} 受到伤害：{damageInfo.damage}，当前生命值：{runtimeData.currentHealth}");

            // 7.如果本次伤害导致死亡，先发死亡事件
            if (wasAlive && runtimeData.IsDead)
            {
                OnDead?.Invoke(this);
            }

            // 8.通知状态机进入受击或死亡状态，当前阶段状态机仍由 EnemyAttributes 直接调用
            if (stateMachine != null)
            {
                stateMachine.RequestHit(damageInfo);
                return;
            }

            // 9.没有状态机时兜底处理死亡
            if (runtimeData.IsDead)
            {
                context?.OnDeadStateEntered();
            }
        }

        // 恢复生命值：后续治疗、回血、重置敌人时可以调用
        public void Heal(float amount)
        {
            // 1.空值校验
            if (runtimeData == null)
            {
                return;
            }

            // 2.死亡后不恢复，避免死亡敌人被普通治疗拉起
            if (runtimeData.IsDead)
            {
                return;
            }

            // 3.恢复生命值
            runtimeData.Heal(amount);

            // 4.通知生命值变化和恢复事件
            NotifyHealthChanged();
            OnHealed?.Invoke(this, amount);

            // 5.调试输出，方便验证恢复逻辑
            Debug.Log($"敌人 {name} 恢复生命值：{amount}，当前生命值：{runtimeData.currentHealth}");
        }
        #endregion

        #region 事件通知
        // 通知生命值变化
        private void NotifyHealthChanged()
        {
            // 1.空值校验
            if (runtimeData == null)
            {
                return;
            }

            // 2.向外广播当前生命值状态
            OnHealthChanged?.Invoke(this, runtimeData.currentHealth, runtimeData.maxHealth, runtimeData.NormalizedHealth);
        }
        #endregion
    }
}