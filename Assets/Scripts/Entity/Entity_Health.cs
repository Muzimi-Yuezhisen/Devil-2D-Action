using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 实体生命：受伤、治疗、回复、死亡与受击击退。
/// </summary>
public class Entity_Health : MonoBehaviour, IDamagable
{
    /// <summary>生命归零且死亡状态完成切换后触发一次。</summary>
    public event Action<Entity_Health> OnDied;
    /// <summary>受击闪白等特效。</summary>
    private Entity_VFX entityVfx;
    /// <summary>所属实体。</summary>
    private Entity entity;
    /// <summary>属性（最大生命、护甲、闪避等）。</summary>
    private Entity_Stats entityStats;
    /// <summary>血条 UI（子物体 Slider）。</summary>
    private Slider healthBar;

    /// <summary>当前生命值。</summary>
    [SerializeField] protected float currentHealth;
    /// <summary>是否已死亡。</summary>
    public bool isDead {  get; private set; }
    public float CurrentHealth => currentHealth;
    public float MaxHealth => entityStats != null ? entityStats.GetMaxHealth() : 0;
    public Vector2 NormalKnockbackPower => knockbackPower;
    public Vector2 HeavyKnockbackPower => heavyKnockbackpower;

    [Header("生命回复")]
    /// <summary>回复间隔（秒）。</summary>
    [SerializeField] private float regenInterval = 1;
    /// <summary>是否允许自然回复。</summary>
    [SerializeField] private bool canRegenerateHealth = true;

    [Header("受击击退")]
    /// <summary>普通受击击退力度。</summary>
    [SerializeField] private Vector2 knockbackPower = new Vector2(1.2f, 0.8f);
    /// <summary>重击击退力度。</summary>
    [SerializeField] private Vector2 heavyKnockbackpower = new Vector2(4f, 3f);
    /// <summary>普通击退持续时间。</summary>
    [SerializeField] private float knockbackDuration = 0.12f;
    /// <summary>重击击退持续时间。</summary>
    [SerializeField] private float heavyKnockbackDuration = 0.25f;

    [Header("重击设置")]
    /// <summary>单次伤害占最大生命比例超过此值视为重击（默认 30%）。</summary>
    [SerializeField] private float heavyDamageThreshold = 0.3f;

    /// <summary>缓存组件、初始化生命并开启定时回复。</summary>
    private void Awake()
    {
        entityVfx = GetComponent<Entity_VFX>();
        entity = GetComponent<Entity>();
        healthBar = GetComponentInChildren<Slider>();
        entityStats = GetComponent<Entity_Stats>();

        entityStats.InitializeFromDefaultSetup();
        currentHealth = entityStats.GetMaxHealth();
        UpdateHealthBar();

        InvokeRepeating(nameof(RegenerateHealth), 0, regenInterval);
    }

    /// <summary>
    /// 受伤结算：闪避、护甲/抗性减免、击退与扣血。命中返回 true。
    /// </summary>
    public virtual bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer,
        AttackImpact impact = AttackImpact.Auto)
    {
        if (isDead) return false;

        if (AttackEvaded())
        {
            Debug.Log($"{gameObject.name} evaded the attack");
            return false;
        }

        Entity_Stats attackerStats = damageDealer != null ? damageDealer.GetComponent<Entity_Stats>() : null;
        float armorReduction = attackerStats != null ? attackerStats.GetArmorReduction() : 0;

        // 计算物穿后的物理承伤
        float mitigation = entityStats.GetArmorMitigation(armorReduction);
        float physicalDamageTaken = damage * (1 - mitigation);

        // 计算元素抗性后的元素承伤
        float resistance = entityStats.GetElementalResistance(element);
        float elementalDamageTaken = elementalDamage * (1 - resistance);

        TakeKnockback(damageDealer, physicalDamageTaken, impact);
        ReduceHealth(physicalDamageTaken + elementalDamageTaken);

        return true;
    }

    /// <summary>按闪避率判定是否躲开本次攻击。</summary>
    private bool AttackEvaded()
    {
        return UnityEngine.Random.Range(0, 100) < entityStats.GetEvasion();
    }

    /// <summary>定时回复生命。</summary>
    private void RegenerateHealth()
    {
        if (canRegenerateHealth == false || isDead) return;

        float regenAmount = entityStats.resources.healthRegen.GetValue();
        IncreaseHealth(regenAmount);
    }

    /// <summary>治疗，不超过最大生命。</summary>
    public void IncreaseHealth(float healAmount)
    {
        if (isDead) return;

        float newHealth = currentHealth + healAmount;
        float maxHealth = entityStats.GetMaxHealth();

        currentHealth = Mathf.Min(newHealth, maxHealth);
        UpdateHealthBar();
    }

    /// <summary>扣血、播受击特效，归零则死亡。</summary>
    public void ReduceHealth(float damage)
    {
        if (isDead || damage <= 0) return;
        entityVfx?.PlayOnDamageVfx();
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();
        if (currentHealth <= 0) Die();
        else AudioManager.PlaySfxAt(AudioCue.Hurt, transform.position);
    }

    /// <summary>按最大生命百分比扣血；地形伤害不受护甲影响。</summary>
    public void ReduceHealthPercent(float percent)
    {
        ReduceHealth(MaxHealth * Mathf.Clamp01(percent));
    }

    /// <summary>标记死亡并通知实体。</summary>
    protected void Die()
    {
        if (isDead) return;
        isDead = true;
        CancelInvoke(nameof(RegenerateHealth));
        AudioManager.PlaySfxAt(AudioCue.Death, transform.position);
        entity?.EntityDeath();
        OnDied?.Invoke(this);
    }

    /// <summary>当前生命百分比（0~1）。</summary>
    public float GetHealthPercent()
    {
        float maxHealth = entityStats.GetMaxHealth();
        return maxHealth > 0 ? currentHealth / maxHealth : 0;
    }

    /// <summary>将生命设为最大生命的指定比例。</summary>
    public void SetHealthToPercent(float percent)
    {
        currentHealth = entityStats.GetMaxHealth() * Mathf.Clamp01(percent);
        UpdateHealthBar();
    }

    /// <summary>刷新血条显示。</summary>
    private void UpdateHealthBar()
    {
        if (healthBar == null) return;
        healthBar.value = GetHealthPercent();
    }

    /// <summary>根据最终物理伤害向攻击来源反方向击退。</summary>
    private void TakeKnockback(Transform damageDealer, float finalDamage, AttackImpact impact)
    {
        Vector2 knockback = CalculateKnockback(finalDamage, damageDealer, impact);
        float duration = CalculateDuration(finalDamage, impact);
        entity?.ReceiveKnockback(knockback, duration);
    }

    /// <summary>计算击退向量（含方向与重击判定）。</summary>
    private Vector2 CalculateKnockback(float damage, Transform damageDealer, AttackImpact impact)
    {
        if (damageDealer == null) return Vector2.zero;
        int direction = transform.position.x > damageDealer.position.x ? 1 : -1;
        Vector2 knockback = IsHeavyImpact(damage, impact) ? heavyKnockbackpower : knockbackPower;
        knockback.x *= direction;
        return knockback;
    }

    /// <summary>按是否重击返回击退时长。</summary>
    private float CalculateDuration(float damage, AttackImpact impact) =>
        IsHeavyImpact(damage, impact) ? heavyKnockbackDuration : knockbackDuration;

    /// <summary>显式攻击等级优先；Auto 才回退到旧的伤害占比规则。</summary>
    private bool IsHeavyImpact(float damage, AttackImpact impact)
    {
        if (impact == AttackImpact.Heavy) return true;
        if (impact == AttackImpact.Normal) return false;
        return IsHeavyDamage(damage);
    }

    /// <summary>伤害是否达到重击阈值。</summary>
    private bool IsHeavyDamage(float damage) => damage / entityStats.GetMaxHealth() > heavyDamageThreshold;
}
