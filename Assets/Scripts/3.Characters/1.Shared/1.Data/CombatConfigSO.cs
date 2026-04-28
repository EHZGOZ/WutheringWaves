using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    #region AttackStep
    [System.Serializable]
    public class AttackStep
    {
        [Header("攻击步骤ID")]
        public AttackId attackId = AttackId.None;
        [Header("攻击命中配置")]
        public AttackHitConfig hitConfig = new AttackHitConfig();
        [Header("攻击时序配置")]
        public AttackTimingConfig timingConfig = new AttackTimingConfig();
    }

    [System.Serializable]
    //攻击命中配置
    public class AttackHitConfig
    {
        [Header("=== 攻击伤害配置 ===")]
        [Header("伤害倍率")]
        public float damageMultiplier = 1f;
        [Header("体力消耗")]
        public float staminaCost = 0f;

        [Header("=== 攻击判定配置 ===")]
        [Header("伤害球形判定半径")]
        public float attackRadius = 0f;
        [Header("伤害扇形判定角度")]
        public float attackAngle = 0f;
        [Header("伤害判定偏移量")]
        public Vector3 attackOffset = new Vector3(0f, 1f, 0.5f);
    }

    [System.Serializable]
    //攻击时序配置
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
