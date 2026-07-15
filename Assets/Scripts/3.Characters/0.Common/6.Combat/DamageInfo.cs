using UnityEngine;

namespace WutheringWaves
{
    #region 受击反应等级
    // 受击反应等级：只描述普通受击动作强度，不负责失衡判断
    public enum HitReactionLevel
    {
        None = 0, // 不触发普通受击动作，但仍然可以造成削韧
        Light = 1, // 触发轻受击反应
        Heavy = 2 // 触发重受击反应
    }
    #endregion

    [System.Serializable]
    // 伤害信息：用于描述一次攻击命中的完整战斗数据
    public class DamageInfo
    {
        [Header("=== 伤害基础信息 ===")]
        public float damage; // 最终生命伤害值
        public float poiseDamage; // 最终削韧值

        [Header("=== 受击反应信息 ===")]
        public HitReactionLevel hitReaction = HitReactionLevel.None; // 普通受击反应等级

        [Header("=== 攻击来源信息 ===")]
        public GameObject attacker; // 发起攻击的对象

        [Header("=== 命中位置信息 ===")]
        public Vector3 hitPoint; // 命中点
        public Vector3 hitDirection; // 从攻击者指向受击目标的方向

        [Header("=== 攻击段信息 ===")]
        public AttackId attackId = AttackId.None; // 当前攻击段ID

        // 构造函数：创建一次完整的伤害信息
        public DamageInfo(
            float damage,
            float poiseDamage,
            HitReactionLevel hitReaction,
            GameObject attacker,
            Vector3 hitPoint,
            Vector3 hitDirection,
            AttackId attackId)
        {
            // 1.生命伤害容错，避免传入负数伤害
            this.damage = Mathf.Max(0f, damage);

            // 2.削韧值容错，避免负数削韧反向恢复敌人韧性
            this.poiseDamage = Mathf.Max(0f, poiseDamage);

            // 3.记录普通受击反应等级
            // None只代表不触发普通受击动作，不会取消削韧
            this.hitReaction = hitReaction;

            // 4.记录攻击来源，方便后续处理仇恨、击退和掉落归属
            this.attacker = attacker;

            // 5.记录命中位置和方向，方便后续处理受击朝向和击退方向
            this.hitPoint = hitPoint;
            this.hitDirection = hitDirection.sqrMagnitude > 0.001f
                ? hitDirection.normalized
                : Vector3.forward;

            // 6.记录攻击段ID，方便后续区分普攻、重击和技能表现
            this.attackId = attackId;
        }
    }
}