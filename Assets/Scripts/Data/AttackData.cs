using System;

/// <summary>
/// 攻击数据，封装一次攻击的物理/元素伤害与元素效果。
/// </summary>
[Serializable]
public class AttackData
{
    /// <summary>物理伤害。</summary>
    public float physicalDamage;
    /// <summary>元素伤害。</summary>
    public float elementalDamage;
    /// <summary>是否为暴击。</summary>
    public bool isCrit;
    /// <summary>元素类型。</summary>
    public ElementType element;

    /// <summary>元素效果数据。</summary>
    public ElementalEffectData effectData;

    /// <summary>
    /// 根据实体属性与伤害缩放配置计算攻击数据。
    /// </summary>
    /// <param name="entityStats">攻击者属性。</param>
    /// <param name="scaleData">伤害与元素效果缩放。</param>
    public AttackData(Entity_Stats entityStats, DamageScaleData scaleData)
    {
        physicalDamage = entityStats.GetPhysicalDamage(out isCrit, scaleData.physical);
        elementalDamage = entityStats.GetElementalDamage(out element, scaleData.elemental);

        effectData = new ElementalEffectData(entityStats, scaleData);
    }
}
