using UnityEngine;

namespace WutheringWaves
{
    public class EnemyContext : MonoBehaviour, IDamageable
    {
        #region 核心引用
        [Header("=== 敌人核心数据 ===")]
        [Header("敌人基础数据：手动填入")]
        [SerializeField] private EnemyDataSO enemyDataSO; // 敌人静态配置

        [Header("=== 敌人运行时数据 ===")]
        [SerializeField] private EnemyRuntimeData runtimeData = new(); // 敌人运行时数据
        #endregion

        #region 对外只读属性
        public EnemyDataSO EnemyDataSO => enemyDataSO; // 敌人基础数据
        public EnemyRuntimeData RuntimeData => runtimeData; // 敌人运行时数据

        public bool IsDead => runtimeData != null && runtimeData.IsDead; // 是否已经死亡
        #endregion

        #region 生命周期
        private void Awake()
        {
            // 1.敌人生成时初始化运行时数据
            Initialize();
        }
        #endregion

        #region 初始化
        // 敌人初始化入口：后续 AI、表现、掉落模块也可以从这里统一初始化
        public void Initialize()
        {
            // 1.确保运行时数据对象存在
            if (runtimeData == null)
            {
                runtimeData = new EnemyRuntimeData();
            }

            // 2.从敌人静态配置中初始化生命值
            runtimeData.Initialize(enemyDataSO);
        }
        #endregion

        #region 受伤与死亡
        // 受到伤害：IDamageable接口入口
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

            // 3.扣除生命值
            runtimeData.TakeDamage(damageInfo.damage);

            // 4.调试输出，方便先验证伤害链路是否打通
            Debug.Log($"敌人 {name} 受到伤害：{damageInfo.damage}，当前生命值：{runtimeData.currentHealth}");

            // 5.扣血后如果死亡，进入死亡处理
            if (runtimeData.IsDead)
            {
                Die();
            }
        }

        // 死亡处理：第一版只负责标记和调试，后续再接死亡动画与掉落
        private void Die()
        {
            // 1.当前阶段先只输出日志，确认死亡流程能被触发
            Debug.Log($"敌人 {name} 已死亡。");

            // 2.临时禁用敌人对象，后续可以替换为死亡动画、掉落事件、延迟销毁
            gameObject.SetActive(false);
        }
        #endregion
    }
}