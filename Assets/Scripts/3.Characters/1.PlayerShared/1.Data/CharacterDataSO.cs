using UnityEngine;

namespace WutheringWaves
{
    [CreateAssetMenu(menuName = "WutheringWaves/CharacterDataSO", fileName = "CharacterDataSO", order = 0)]
    public class CharacterDataSO : ScriptableObject
    {
        #region 角色信息
        [Header("===角色信息===")]
        [SerializeField] public CharacterName characterName;
        [Header("基础攻击力")]
        [SerializeField] public float baseAttack = 100f;
        [Header("最大生命值")]
        [SerializeField] public float maxHealth = 1000f;
        #endregion

        #region UI图标配置
        [Header("=== UI图标配置 ===")]
        [SerializeField] public CharacterUIConfigSO characterUIConfigSO;
        #endregion

        #region 动画相关
        [Header("=== 动画配置 ===")]
        [Tooltip("角色动画配置（动画语义键到 Animator 参数/剪辑映射）")]
        [SerializeField] public AnimationConfigSO animationConfigSO;
        #endregion

        #region 战斗相关
        [Header("=== 战斗配置 ===")]
        [Tooltip("角色战斗配置（攻击段、技能段等）")]
        [SerializeField] public CombatConfigSO combatConfig;
        #endregion

        #region 位移相关
        [Header("=== 位移配置 ===")]
        [Tooltip("角色位移配置（位移速率、消耗时间）")]
        [SerializeField] public MovementConfigSO movementConfigSO;
        #endregion

        #region 武器相关
        [Header("=== 武器配置 ===")]
        [Tooltip("角色武器配置（御剑轨迹、武器表现等）")]
        [SerializeField] public WeaponConfigSO weaponConfigSO;
        #endregion

        #region 特效相关
        [Header("=== 特效配置 ===")]
        [Tooltip("角色攻击特效配置（AttackId 到特效表现映射）")]
        [SerializeField] public EffectActionConfigSO effectActionConfigSO;
        #endregion

        #region 音效相关
        [Header("=== 音效配置 ===")]
        [Tooltip("角色音效配置（AttackId / LocomotionAnimationId 到音效表现映射）")]
        [SerializeField] public AudioActionConfigSO audioActionConfigSO;
        #endregion

        #region 缓冲相关
        [Header("=== 急停缓冲配置 ===")]
        [Header("移动缓冲的总滑行距离（米）")]
        [SerializeField] public float moveStoppingDistance = 0.7f;
        [Header("移动缓冲的总时长（秒）")]
        [SerializeField] public float moveStoppingTime = 1f;
        [Header("奔跑缓冲的总滑行距离（米）")]
        [SerializeField] public float runStoppingDistance = 1f;
        [Header("奔跑缓冲的总时长（秒）")]
        [SerializeField] public float runStoppingTime = 1f;
        [Header("急停缓冲的总滑行距离（米）")]
        [SerializeField] public float dashStoppingDistance = 1f;
        [Header("急停缓冲的总时长（秒）")]
        [SerializeField] public float dashStoppingTime = 0.66f;
        #endregion

        #region 地面监测相关
        [Header("重力加速度")]
        [SerializeField] public float gravity = -30f;
        [Header("坠落检测延迟时间（秒）")]
        [SerializeField] public float fallCheckDelay = 0.3f;
        [Header("地面检测球体半径")]
        [SerializeField] public float groundCheckRadius = 0.2f;
        [Header("地面检测起点偏移")]
        [SerializeField] public float groundCheckOriginOffset = 0.2f;
        [Header("地面检测最大距离")]
        [SerializeField] public float groundCheckDistance = 0.2f;
        #endregion
    }
}
