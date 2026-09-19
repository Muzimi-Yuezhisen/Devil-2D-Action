using System;

/// <summary>
/// 元素效果运行时数据，由属性与缩放配置计算得出。
/// </summary>
public class ElementalEffectData
{
    /// <summary>冰冻持续时间（秒）。</summary>
    public float chillDuration;
    /// <summary>冰冻减速倍率。</summary>
    public float chillSlowMultiplier;

    /// <summary>燃烧持续时间（秒）。</summary>
    public float burnDuration;
    /// <summary>燃烧总伤害。</summary>
    public float totalBurnDamage;

    /// <summary>电击持续时间（秒）。</summary>
    public float shockDuration;
    /// <summary>单次电击伤害。</summary>
    public float shockDamage;
    /// <summary>电击充能量。</summary>
    public float shockCharge;

    /// <summary>
    /// 根据实体属性与伤害缩放配置计算元素效果数值。
    /// </summary>
    /// <param name="entityStats">攻击者属性。</param>
    /// <param name="damageScale">元素效果缩放配置。</param>
    public ElementalEffectData(Entity_Stats entityStats, DamageScaleData damageScale)
    {
        chillDuration = damageScale.chillDuration;
        chillSlowMultiplier = damageScale.chillSlowMultiplier;

        burnDuration = damageScale.burnDuration;
        totalBurnDamage = entityStats.offense.fireDamage.GetValue() * damageScale.burnDamageScale;

        shockDuration = damageScale.shockDuration;
        shockDamage = entityStats.offense.lightningDamage.GetValue() * damageScale.shockDamageScale;
        shockCharge = damageScale.shockCharge;
    }
}
