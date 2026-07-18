using UnityEngine;

public class Enemy_Health : Entity_Health
{
    private Enemy enemy;

    private void Start()
    {
        enemy = GetComponent<Enemy>();
    }

    public override bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        bool wasHit = base.TakeDamage(damage, elementalDamage, element, damageDealer);

        if (wasHit == false) return false;

        //if (damageDealer.CompareTag("Player")) enemy.TryEnterBattleState(damageDealer);
        if(damageDealer.GetComponent<Player>() != null) enemy.TryEnterBattleState(damageDealer);

        return true;
    }
}
