using UnityEngine;

namespace WutheringWaves
{
    [System.Serializable]
    // 伤害信息：用于描述一次攻击命中的基础数据
    public class DamageInfo
    {
        [Header("=== 伤害基础信息 ===")]
        public float damage; // 最终伤害值

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
            GameObject attacker,
            Vector3 hitPoint,
            Vector3 hitDirection,
            AttackId attackId)
        {
            // 1.伤害值容错，避免传入负数伤害
            this.damage = Mathf.Max(0f, damage);

            // 2.记录攻击来源，方便后续做仇恨、击退、掉落归属
            this.attacker = attacker;

            // 3.记录命中位置和方向，方便后续做受击朝向、击退方向
            this.hitPoint = hitPoint;
            this.hitDirection = hitDirection.sqrMagnitude > 0.001f ? hitDirection.normalized : Vector3.forward;

            // 4.记录攻击段ID，方便后续区分普攻、重击、技能等表现
            this.attackId = attackId;
        }
    }
}