using System.Collections;
using UnityEngine;

/// <summary>
/// 元素异常状态：冰冻减速、燃烧跳伤、感电蓄能与落雷。
/// </summary>
public class Entity_StatusHandler : MonoBehaviour
{
    /// <summary>所属实体（用于减速等）。</summary>
    private Entity entity;
    /// <summary>状态闪烁特效。</summary>
    private Entity_VFX entityVfx;
    /// <summary>抗性等属性。</summary>
    private Entity_Stats entityStats;
    /// <summary>生命（直接扣血）。</summary>
    private Entity_Health entityHealth;
    /// <summary>当前异常元素；None 表示无状态。</summary>
    private ElementType currentEffect = ElementType.None;

    [Header("感电效果")]
    /// <summary>落雷特效预制体。</summary>
    [SerializeField] private GameObject lightningStrikeVfx;
    /// <summary>当前感电蓄能。</summary>
    [SerializeField] private float currentCharge;
    /// <summary>触发落雷所需蓄能上限。</summary>
    [SerializeField] private float maximumCharge = 1;
    /// <summary>感电持续时间协程。</summary>
    private Coroutine shockCo;

    /// <summary>缓存组件引用。</summary>
    private void Awake()
    {
        entity = GetComponent<Entity>();
        entityVfx = GetComponent<Entity_VFX>();
        entityStats = GetComponent<Entity_Stats>();
        entityHealth = GetComponent<Entity_Health>();
    }

    /// <summary>按元素类型施加对应异常效果。</summary>
    public void ApplyStatusEffect(ElementType element, ElementalEffectData effectData)
    {
        if (element == ElementType.Ice && CanBeApplied(ElementType.Ice))
            ApplyChillEffect(effectData.chillDuration, effectData.chillSlowMultiplier);

        if (element == ElementType.Fire && CanBeApplied(ElementType.Fire))
            ApplyBurnEffect(effectData.burnDuration, effectData.totalBurnDamage);

        if (element == ElementType.Lightning && CanBeApplied(ElementType.Lightning))
            ApplyShockEffect(effectData.shockDuration, effectData.shockDamage, effectData.shockCharge);
    }

    /// <summary>
    /// 感电：受击积蓄电荷，满额触发落雷；电抗会减少本次蓄能量。
    /// </summary>
    public void ApplyShockEffect(float duration, float damage, float charge)
    {
        float lightningResistance = entityStats.GetElementalResistance(ElementType.Lightning);
        float finalCharge = charge * (1 - lightningResistance);

        currentCharge = currentCharge + finalCharge;
        if (currentCharge >= maximumCharge)
        {
            DoLightningStrike(damage);
            StopShockEffect();
            return;
        }

        if (shockCo != null) StopCoroutine(shockCo);
        shockCo = StartCoroutine(ShockEffectCo(duration));
    }

    /// <summary>清除感电状态与蓄能，并停止相关特效。</summary>
    private void StopShockEffect()
    {
        currentEffect = ElementType.None;
        currentCharge = 0;
        entityVfx.StopAllVfx();
    }

    /// <summary>生成落雷特效并对自身造成伤害。</summary>
    private void DoLightningStrike(float damage)
    {
        if (lightningStrikeVfx != null) Instantiate(lightningStrikeVfx, transform.position, Quaternion.identity);
        entityHealth.ReduceHealth(damage);
    }

    /// <summary>感电持续期间播放闪电色闪烁，到期清除。</summary>
    private IEnumerator ShockEffectCo(float duration)
    {
        currentEffect = ElementType.Lightning;
        entityVfx.PlayOnStatusVfx(duration, ElementType.Lightning);
        yield return new WaitForSeconds(duration);
        StopShockEffect();
    }

    /// <summary>燃烧：火抗减伤后，在持续时间内分段扣血。</summary>
    public void ApplyBurnEffect(float duration, float fireDamage)
    {
        float fireResistance = entityStats.GetElementalResistance(ElementType.Fire);
        float finalDamage = fireDamage * (1 - fireResistance);
        StartCoroutine(BurnEffectCo(duration, finalDamage));
    }

    /// <summary>燃烧协程：按 tick 扣血并播放火焰色闪烁。</summary>
    private IEnumerator BurnEffectCo(float duration, float totalDamage)
    {
        currentEffect = ElementType.Fire;
        entityVfx.PlayOnStatusVfx(duration, ElementType.Fire);

        int ticksPerSecond = 2;
        int tickCount = Mathf.Max(1, Mathf.RoundToInt(ticksPerSecond * duration));
        float damagePerTick = totalDamage / tickCount;
        float tickInterval = 1f / ticksPerSecond;

        for (int i = 0; i < tickCount; i++)
        {
            entityHealth.ReduceHealth(damagePerTick);
            yield return new WaitForSeconds(tickInterval);
        }
        currentEffect = ElementType.None;
    }

    /// <summary>冰冻：冰抗缩短时长，并对实体施加减速。</summary>
    public void ApplyChillEffect(float duration, float slowMultiplier)
    {
        float iceResistance = entityStats.GetElementalResistance(ElementType.Ice);
        float finalDuration = duration * (1 - iceResistance);
        StartCoroutine(ChilledEffectCo(finalDuration, slowMultiplier));
    }

    /// <summary>冰冻协程：减速 + 冰色闪烁。</summary>
    private IEnumerator ChilledEffectCo(float duration, float slowMultiplier)
    {
        entity.SlowDownEntity(duration, slowMultiplier);
        currentEffect = ElementType.Ice;
        entityVfx.PlayOnStatusVfx(duration, ElementType.Ice);
        yield return new WaitForSeconds(duration);
        currentEffect = ElementType.None;
    }

    /// <summary>
    /// 是否可施加该异常：无状态时可施加；感电可叠加蓄能。
    /// </summary>
    public bool CanBeApplied(ElementType element)
    {
        if (element == ElementType.Lightning && currentEffect == ElementType.Lightning) return true;
        return currentEffect == ElementType.None;
    }
}
