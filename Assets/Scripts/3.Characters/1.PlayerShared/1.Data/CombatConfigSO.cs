using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    #region AttackStep
    // 受击反应等级：底层仍使用0、1、2，Inspector中显示明确名称
    public enum HitReactionLevel
    {
        None = 0, // 只造成数值伤害，不触发受击动作
        Light = 1, // 轻受击反应
        Heavy = 2 // 重受击反应
    }

    // 攻击范围类型：未启用范围叠加时选择单一范围
    public enum AttackRangeType
    {
        Sphere, // 球形范围
        Sector, // 扇形范围
        Box // 矩形范围
    }

    [System.Serializable]
    public class AttackStep
    {
        [Header("攻击步骤ID")]
        public AttackId attackId = AttackId.None;

        [Header("攻击命中配置")]
        public AttackHitConfig hitConfig = new AttackHitConfig();

        [Header("攻击范围配置")]
        public AttackRangeConfig rangeConfig = new AttackRangeConfig();

        [Header("攻击时序配置")]
        public AttackTimingConfig timingConfig = new AttackTimingConfig();
    }

    [System.Serializable]
    // 攻击命中配置：只负责命中后的伤害、削韧和受击反应
    public class AttackHitConfig
    {
        [Header("=== 攻击伤害配置 ===")]
        [Header("伤害倍率")]
        [Min(0f)]
        public float damageMultiplier = 1f;

        [Header("削韧倍率")]
        [Min(0f)]
        public float poiseDamageMultiplier = 1f;

        [Header("受击反应等级")]
        public HitReactionLevel hitReaction = HitReactionLevel.None;
    }

    [System.Serializable]
    // 攻击范围配置：负责单一范围或多个范围的叠加检测
    public class AttackRangeConfig
    {
        [Header("=== 范围模式配置 ===")]
        [Tooltip("开启后合并所有已启用范围；关闭后只使用单一范围类型")]
        public bool enableRangeCombination = true;

        [Header("未开启范围叠加时使用的范围")]
        public AttackRangeType singleRangeType = AttackRangeType.Sector;

        [Header("=== 球形范围配置 ===")]
        public bool useSphereRange = false;

        [Header("球形范围半径")]
        [Min(0f)]
        public float sphereRadius = 0f;

        [Header("球形范围本地偏移")]
        public Vector3 sphereOffset = new Vector3(0f, 1f, 0.5f);

        [Header("=== 扇形范围配置 ===")]
        public bool useSectorRange = true;

        [Header("扇形范围半径")]
        [Min(0f)]
        public float sectorRadius = 0f;

        [Header("扇形范围角度")]
        [Range(0f, 360f)]
        public float sectorAngle = 0f;

        [Header("扇形范围本地偏移")]
        public Vector3 sectorOffset = new Vector3(0f, 1f, 0.5f);

        [Header("=== 矩形范围配置 ===")]
        public bool useBoxRange = false;

        [Header("矩形范围完整尺寸")]
        public Vector3 boxSize = Vector3.zero;

        [Header("矩形范围本地偏移")]
        public Vector3 boxOffset = new Vector3(0f, 1f, 0.5f);
    }

    [System.Serializable]
    // 攻击时序配置
    public class AttackTimingConfig
    {
        [Header("攻击执行所需时长")]
        public float executionAttackCostTime = 0f;

        [Header("连击窗口持续时间（秒）")]
        public float comboWindowDuration = 1.5f;
    }
    #endregion

    [CreateAssetMenu(menuName = "WutheringWaves/CombatConfig", fileName = "CombatConfig", order = 1)]
    public class CombatConfigSO : ScriptableObject
    {
        [Header("===角色信息===")]
        [SerializeField] internal CharacterName characterName;

        [Header("轻攻击攻击段的核心列表")]
        [SerializeField] internal List<AttackStep> attackSteps = new List<AttackStep>();

        [Header("下落攻击段的核心列表")]
        [SerializeField] internal List<AttackStep> fallAttackSteps = new List<AttackStep>();
        [Header("重攻击段的核心列表")]
        [SerializeField] internal List<AttackStep> heavyAttackSteps = new List<AttackStep>();

        [Header("御空攻击段的核心列表（空中）")]
        [SerializeField] internal List<AttackStep> skillAirAttackSteps = new List<AttackStep>();

        [Header("御空攻击段的核心列表（近地）")]
        [SerializeField] internal List<AttackStep> skillAttackSteps = new List<AttackStep>();

        [Header("技能攻击段的核心列表")]
        [SerializeField] internal List<AttackStep> eSkillAttackSteps = new List<AttackStep>();
        [Header("爆发攻击段的核心列表")]
        [SerializeField] internal List<AttackStep> qBurstAttackSteps = new List<AttackStep>();
        [Header("延奏QTE攻击段的核心列表")]
        [SerializeField] internal List<AttackStep> qteSkillAttackSteps = new List<AttackStep>();

        [Header(" ===技能倍率===")]
        [Header("共鸣技能倍率")]
        [SerializeField] internal float resonanceESkillMagnification = 2f;       
        [Header("共鸣技能倍率")]
        [SerializeField] internal float resonanceQBurstMagnification = 5f;
    }
}
