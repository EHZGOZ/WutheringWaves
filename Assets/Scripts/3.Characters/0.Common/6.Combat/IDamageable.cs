namespace WutheringWaves
{
    // 可受伤接口：所有可以被攻击命中的对象都实现这个接口
    // 例如：普通敌人、Boss、可破坏物，后续也可以扩展到玩家受击
    public interface IDamageable
    {
        // 是否已经死亡或失效，攻击逻辑可以用它过滤无效目标
        bool IsDead { get; }

        // 受到伤害的统一入口
        // 使用 DamageInfo 而不是 float，方便后续携带攻击者、命中点、击退方向、攻击段ID等信息
        void TakeDamage(DamageInfo damageInfo);
    }
}