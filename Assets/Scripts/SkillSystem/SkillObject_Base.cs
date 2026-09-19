using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 技能生成物基类：范围检测敌人并结算伤害与元素状态。
/// </summary>
public class SkillObject_Base : MonoBehaviour
{
    [SerializeField] private GameObject onHitVfx;
    [Space]
    /// <summary>敌人所在的层级。</summary>
    [SerializeField] protected LayerMask whatIsEnemy;
    /// <summary>检测圆心；为空则用自身位置。</summary>
    [SerializeField] protected Transform targetCheck;
    /// <summary>爆炸/伤害检测半径。</summary>
    [SerializeField] protected float checkRadius = 1;

    protected Animator anim;
    /// <summary>释放者属性，用于计算攻击数据。</summary>
    protected Entity_Stats playerStats;
    /// <summary>本次生成物使用的伤害缩放。</summary>
    protected DamageScaleData damageScaleData;
    /// <summary>真正的技能释放者，用于穿甲计算。</summary>
    protected Transform damageDealer;
    /// <summary>最近一次结算使用的元素类型（供特效染色）。</summary>
    protected ElementType usedElement;
    protected bool targetGotHit;

    protected virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// 对半径内敌人造成伤害并施加元素状态。
    /// </summary>
    /// <param name="t">检测中心。</param>
    /// <param name="radius">检测半径。</param>
    protected void DamageEnemiesInRadius(Transform t, float radius)
    {
        if (playerStats == null || t == null) return;

        HashSet<IDamagable> damagedTargets = new HashSet<IDamagable>();
        foreach (var target in GetEnemiesAround(t, radius))
        {
            IDamagable damagable = target.GetComponentInParent<IDamagable>();
            if (damagable == null || damagedTargets.Add(damagable) == false) continue;

            AttackData attackData = playerStats.GetAttackData(damageScaleData ?? new DamageScaleData());
            Entity_StatusHandler statusHandler = target.GetComponentInParent<Entity_StatusHandler>();
            float physDamage = attackData.physicalDamage;
            float elemDamage = attackData.elementalDamage;
            ElementType element = attackData.element;
            targetGotHit = damagable.TakeDamage(physDamage, elemDamage, element, damageDealer != null ? damageDealer : transform);
            if (targetGotHit && element != ElementType.None) statusHandler?.ApplyStatusEffect(element, attackData.effectData);

            if (targetGotHit && onHitVfx != null) Instantiate(onHitVfx, target.transform.position, Quaternion.identity);
            usedElement = element;
        }
    }
    /// <summary>在半径 10 内寻找最近的敌人 Transform。</summary>
    protected Transform FindClosestTarget()
    {
        Transform target = null;
        float closestDistance = Mathf.Infinity;
        foreach (var enemy in GetEnemiesAround(transform, 10))
        {
            Entity_Health health = enemy.GetComponentInParent<Entity_Health>();
            if (health != null && health.isDead) continue;
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                target = enemy.transform;
                closestDistance = distance;
            }
        }
        return target;
    }
    /// <summary>圆形检测指定层级上的碰撞体。</summary>
    protected Collider2D[] GetEnemiesAround(Transform t, float radius)
    {
        return Physics2D.OverlapCircleAll(t.position, radius, whatIsEnemy);
    }
    /// <summary>在 Scene 视图绘制检测范围。</summary>
    private void OnDrawGizmos()
    {
        if (targetCheck == null) targetCheck = transform;
        Gizmos.DrawWireSphere(targetCheck.position, checkRadius);
    }
}
