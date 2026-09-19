using UnityEngine;

/// <summary>
/// 可受伤对象接口，统一处理受击逻辑。
/// </summary>
public interface IDamagable
{
    /// <summary>
    /// 受到伤害。
    /// </summary>
    /// <param name="damage">物理伤害。</param>
    /// <param name="elementalDamage">元素伤害。</param>
    /// <param name="element">元素类型。</param>
    /// <param name="damageDealer">伤害来源的 Transform。</param>
    /// <param name="impact">本次攻击的击退等级；Auto 会保留按伤害比例判定的兼容行为。</param>
    /// <returns>是否成功造成伤害。</returns>
    public bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer,
        AttackImpact impact = AttackImpact.Auto);
}
