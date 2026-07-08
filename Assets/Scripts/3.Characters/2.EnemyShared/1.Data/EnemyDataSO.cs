using UnityEngine;

namespace WutheringWaves
{
    [CreateAssetMenu(menuName = "WutheringWaves/EnemyDataSO", fileName = "EnemyDataSO", order = 10)]
    public class EnemyDataSO : ScriptableObject
    {
        #region 敌人基础信息
        [Header("=== 敌人基础信息 ===")]
        [Header("敌人名称")]
        [SerializeField] public string enemyName = "Enemy"; // 敌人名称，用于调试、UI显示和后续掉落归属判断

        [Header("最大生命值")]
        [SerializeField] public float maxHealth = 100f; // 敌人最大生命值

        [Header("基础攻击力")]
        [SerializeField] public float baseAttack = 10f; // 敌人基础攻击力，后续敌人攻击玩家时使用
        #endregion

        #region 动画相关
        [Header("=== 动画配置 ===")]
        [Tooltip("敌人动画配置（动画语义键到动画剪辑映射）")]
        [SerializeField] public EnemyAnimationConfigSO animationConfigSO; // 敌人动画配置
        #endregion

        #region 掉落相关
        [Header("=== 掉落相关 ===")]
        [Header("死亡后是否生成掉落物")]
        [SerializeField] public bool canDropItem = true; // 是否允许该敌人死亡后生成掉落物
        #endregion
    }
}