using UnityEngine;

/// <summary>
/// 敌人生命：受伤后尝试进入战斗状态。
/// </summary>
public class Enemy_Health : Entity_Health
{
    /// <summary>所属敌人。</summary>
    private Enemy enemy;

    /// <summary>缓存敌人组件。</summary>
    private void Start()
    {
        enemy = GetComponent<Enemy>();
    }

    /// <summary>结算伤害；若命中且来源是玩家则进入战斗。</summary>
    public override bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer,
        AttackImpact impact = AttackImpact.Auto)
    {
        bool wasHit = base.TakeDamage(damage, elementalDamage, element, damageDealer, impact);

        if (wasHit == false) return false;

        if (damageDealer != null && damageDealer.GetComponent<Player>() != null) enemy?.TryEnterBattleState(damageDealer);

        return true;
    }
}
