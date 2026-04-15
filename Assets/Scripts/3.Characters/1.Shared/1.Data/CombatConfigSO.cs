using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    [CreateAssetMenu(menuName = "WutheringWaves/CombatConfig", fileName = "CombatConfig", order = 1)]
    public class CombatConfigSO : ScriptableObject
    {
        #region 1. 普通攻击配置
        [Header("=== 普通攻击配置 ===")]
        [Tooltip("轻攻击攻击段的核心列表")]
        [SerializeField] internal List<AttackStep> attackSteps = new List<AttackStep>();

        [Tooltip("下落攻击段的核心列表")]
        [SerializeField] internal List<AttackStep> fallAttackSteps = new List<AttackStep>();
        #endregion

        #region 2. 御空攻击配置
        [Header("=== 御空攻击配置 ===")]
        [Tooltip("御空攻击段的核心列表（空中）")]
        [SerializeField] internal List<AttackStep> skillAirAttackSteps = new List<AttackStep>();

        [Tooltip("御空攻击段的核心列表（近地）")]
        [SerializeField] internal List<AttackStep> skillAttackSteps = new List<AttackStep>();
        #endregion

        #region 3. 技能攻击配置
        [Header("=== 技能攻击配置 ===")]
        [Tooltip("技能攻击段的核心列表")]
        [SerializeField] internal List<AttackStep> eSkillAttackSteps = new List<AttackStep>();

        [Tooltip("爆发攻击段的核心列表")]
        [SerializeField] internal List<AttackStep> qBurstAttackSteps = new List<AttackStep>();
        #endregion

        #region 技能
        [Header("共鸣技能")]
        [Tooltip("共鸣技能CD")]
        [SerializeField] internal float resonanceESkillCD = 5;
        [Tooltip("共鸣技能倍率")]
        [SerializeField] internal float resonanceESkillMagnification = 2f;
        [Header("共鸣解放")]
        [Tooltip("共鸣解放CD")]
        [SerializeField] internal float resonanceQBurstCD = 10f;
        [Tooltip("共鸣技能倍率")]
        [SerializeField] internal float resonanceQBurstMagnification = 5f;
        #endregion
    }
}
