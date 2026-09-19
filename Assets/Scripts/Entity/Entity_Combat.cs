using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 实体战斗组件：检测目标并执行普攻结算。
/// </summary>
public class Entity_Combat : MonoBehaviour
{
    /// <summary>特效组件。</summary>
    private Entity_VFX vfx;
    /// <summary>属性组件。</summary>
    private Entity_Stats stats;
    /// <summary>普攻的伤害与元素效果缩放。</summary>
    public DamageScaleData basicAttackScale;
    [Header("目标检测")]
    /// <summary>攻击检测圆心。</summary>
    [SerializeField] private Transform targetCheck;
    /// <summary>攻击检测半径。</summary>
    [SerializeField] private float targetCheckRadius = 1;
    /// <summary>可攻击目标层级。</summary>
    [SerializeField] private LayerMask whatIsTarget;
    /// <summary>缓存特效与属性组件。</summary>
    private void Awake()
    {
        vfx = GetComponent<Entity_VFX>();
        stats = GetComponent<Entity_Stats>();
    }
    /// <summary>
    /// 对检测范围内目标造成伤害、施加元素状态，并播放命中特效。
    /// </summary>
    public void PerformAttack()
    {
        if (stats == null || targetCheck == null) return;

        bool hitAnyTarget = false;
        Player attackingPlayer = GetComponent<Player>();
        AttackImpact impact = attackingPlayer != null
            ? attackingPlayer.CurrentAttackImpact
            : AttackImpact.Normal;
        HashSet<IDamagable> damagedTargets = new HashSet<IDamagable>();
        foreach (var target in GetDetectedColliders())
        {
            IDamagable damagable = target.GetComponentInParent<IDamagable>();
            if (damagable == null || damagedTargets.Add(damagable) == false) continue;

            AttackData attackData = stats.GetAttackData(basicAttackScale ?? new DamageScaleData());
            Entity_StatusHandler statusHandler = target.GetComponentInParent<Entity_StatusHandler>();
            float elementalDamage = attackData.elementalDamage;
            float physDamage = attackData.physicalDamage;
            ElementType element = attackData.element;
            bool targetGotHit = damagable.TakeDamage(physDamage, elementalDamage, element, transform, impact);
            if (targetGotHit)
            {
                hitAnyTarget = true;
                if (element != ElementType.None) statusHandler?.ApplyStatusEffect(element, attackData.effectData);
                vfx?.CreateOnHitVFX(target.transform, attackData.isCrit, element);
            }
        }

        AudioManager.PlaySfxAt(hitAnyTarget ? AudioCue.AttackHit : AudioCue.AttackSwing, transform.position);
    }
    /// <summary>获取攻击范围内的碰撞体。</summary>
    protected Collider2D[] GetDetectedColliders()
    {
        if (targetCheck == null) return System.Array.Empty<Collider2D>();
        return Physics2D.OverlapCircleAll(targetCheck.position, targetCheckRadius, whatIsTarget);
    }
    /// <summary>在编辑器中绘制攻击范围。</summary>
    private void OnDrawGizmos()
    {
        if (targetCheck == null) return;
        Gizmos.DrawWireSphere(targetCheck.position, targetCheckRadius);
    }
}
