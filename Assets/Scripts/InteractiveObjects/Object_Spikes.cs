using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地刺边界：每次触发扣除玩家最大生命的固定百分比，并把玩家推回安全区域。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Object_Spikes : MonoBehaviour
{
    [Header("地刺伤害")]
    [SerializeField, Range(0.01f, 1f)] private float maxHealthDamagePercent = 0.2f;
    [SerializeField, Min(0.1f)] private float damageCooldown = 1f;
    [SerializeField] private Vector2 returnKnockback = new Vector2(9f, 7f);
    [SerializeField, Min(0.05f)] private float knockbackDuration = 0.25f;

    private readonly Dictionary<int, float> nextDamageTimes = new Dictionary<int, float>();

    public float DamagePercent => maxHealthDamagePercent;
    public float DamageCooldown => damageCooldown;

    private void Reset()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryDamage(other);
    private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

    private void TryDamage(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.health == null) return;
        TryDamage(player.health);
    }

    /// <summary>执行一次地刺结算；公开入口也供自动回归验证百分比规则。</summary>
    public bool TryDamage(Entity_Health targetHealth)
    {
        if (targetHealth == null || targetHealth.isDead) return false;
        Player player = targetHealth.GetComponent<Player>();
        if (player == null) return false;

        int targetId = targetHealth.GetInstanceID();
        if (nextDamageTimes.TryGetValue(targetId, out float nextTime) && Time.unscaledTime < nextTime)
            return false;

        nextDamageTimes[targetId] = Time.unscaledTime + damageCooldown;
        targetHealth.ReduceHealthPercent(maxHealthDamagePercent);
        if (!targetHealth.isDead)
            player.ReceiveKnockback(returnKnockback, knockbackDuration);
        return true;
    }

    /// <summary>由关卡构建器统一写入边界伤害参数。</summary>
    public void Configure(float damagePercent, float cooldown, Vector2 knockback, float duration)
    {
        maxHealthDamagePercent = Mathf.Clamp01(damagePercent);
        damageCooldown = Mathf.Max(0.1f, cooldown);
        returnKnockback = knockback;
        knockbackDuration = Mathf.Max(0.05f, duration);
        nextDamageTimes.Clear();
    }
}
