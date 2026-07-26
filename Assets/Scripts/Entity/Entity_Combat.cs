using UnityEngine;

public class Entity_Combat : MonoBehaviour
{
    private Entity_VFX vfx;
    private Entity_Stats stats;

    public DamageScaleData basicAttackScale;

    [Header("Target detection")]
    [SerializeField] private Transform targetCheck;
    [SerializeField] private float targetCheckRadius = 1;
    [SerializeField] private LayerMask whatIsTarget;

    private void Awake()
    {
        vfx = GetComponent<Entity_VFX>();
        stats = GetComponent<Entity_Stats>();
    }

    /// <summary>
    /// 执行攻击的效果，包括伤害、特效、效果等
    /// </summary>
    public void PerformAttack()
    {
        foreach(var target in GetDetectedColliders())
        {
            IDamagable damagable = target.GetComponent<IDamagable>();
            if (damagable == null) continue;

            AttackData attackData = stats.GetAttackData(basicAttackScale);
            Entity_StatusHandler statusHandler = target.GetComponent<Entity_StatusHandler>();

            float elementalDamage = attackData.elementalDamage;
            float physDamage = attackData.physicalDamage;
            ElementType element = attackData.element;

            bool targetGotHit = damagable.TakeDamage(physDamage, elementalDamage, element, transform);

            //if (element != ElementType.None) ApplyStatusEffect(target.transform, element);
            if (element != ElementType.None) statusHandler?.ApplyStatusEffect(element, attackData.effectData);

            if (targetGotHit)
            {
                vfx.CreateOnHitVFX(target.transform, attackData.isCrit, element);
            }
                

            //Entity_Health targetHealth = target.GetComponent<Entity_Health>();
            //targetHealth?.TakeDamage(damage,transform);
        }
    }

    //public void ApplyStatusEffect(Transform target, ElementType element, float scaleFactor = 1)
    //{
    //    Entity_StatusHandler statusHandler = target.GetComponent<Entity_StatusHandler>();

    //    if (statusHandler == null) return;

    //    if(element == ElementType.Ice && statusHandler.CanBeApplied(ElementType.Ice))
    //    {
    //        statusHandler.ApplyChillEffect(defaultDuration, chillSlowMultiplier);
    //    }

    //    if(element == ElementType.Fire && statusHandler.CanBeApplied(ElementType.Fire))
    //    {
    //        scaleFactor = fireScale;
    //        float fireDamage = stats.offense.fireDamage.GetValue() * scaleFactor;
    //        statusHandler.ApplyBurnEffect(defaultDuration, fireDamage);
    //    }
    //    if(element == ElementType.Lightning && statusHandler.CanBeApplied(ElementType.Lightning))
    //    {
    //        scaleFactor = lightningScale;
    //        float lightningDamage = stats.offense.lightningDamage.GetValue() * scaleFactor;
    //        statusHandler.ApplyElectrifyEffect(defaultDuration, lightningDamage, electrifyChargeBuildUp);
    //    }
    //}

    protected Collider2D[] GetDetectedColliders()
    {
        return Physics2D.OverlapCircleAll(targetCheck.position, targetCheckRadius, whatIsTarget);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(targetCheck.position, targetCheckRadius);
    }
}
