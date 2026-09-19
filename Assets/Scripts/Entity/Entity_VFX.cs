using System.Collections;
using UnityEngine;

/// <summary>
/// 实体视觉反馈：受击材质、命中特效、异常状态颜色闪烁。
/// </summary>
public class Entity_VFX : MonoBehaviour
{
    /// <summary>主精灵渲染器。</summary>
    protected SpriteRenderer sr;
    /// <summary>所属实体（用于朝向等）。</summary>
    private Entity entity;

    [Header("受击特效")]
    /// <summary>受击时短暂替换的材质（如闪白）。</summary>
    [SerializeField] private Material onDamageMaterial;
    /// <summary>受击材质持续时间。</summary>
    [SerializeField] private float onDamageVfxDuration = 0.2f;
    /// <summary>原始材质缓存。</summary>
    private Material originalMaterial;
    /// <summary>原始精灵色；种族身份由独立精灵承担，此颜色只用于短时反馈。</summary>
    private Color originalSpriteColor = Color.white;
    /// <summary>受击材质协程。</summary>
    private Coroutine onDamageVfxCoroutine;

    [Header("造成伤害特效")]
    /// <summary>命中特效默认颜色。</summary>
    [SerializeField] private Color hitVfxColor = Color.white;
    /// <summary>普通命中特效预制体。</summary>
    [SerializeField] private GameObject hitVFX;
    /// <summary>暴击命中特效预制体。</summary>
    [SerializeField] private GameObject critHitVFX;

    [Header("元素颜色")]
    /// <summary>冰冻闪烁色。</summary>
    [SerializeField] private Color chillVfx = Color.cyan;
    /// <summary>燃烧闪烁色。</summary>
    [SerializeField] private Color burnVfx = Color.red;
    /// <summary>感电闪烁色。</summary>
    [SerializeField] private Color shockVfx = Color.yellow;
    /// <summary>命中颜色初始值备份。</summary>
    private Color originalHitVfxColor;
    /// <summary>当前世界空间异常状态徽记。</summary>
    private StatusEffectPresenter statusPresenter;
    /// <summary>异常状态显示计时。</summary>
    private Coroutine statusVfxCoroutine;

    /// <summary>缓存渲染器与材质。</summary>
    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        entity = GetComponent<Entity>();
        if (sr != null)
        {
            originalMaterial = sr.material;
            originalSpriteColor = sr.color;
        }
        originalHitVfxColor = hitVfxColor;
    }

    /// <summary>按元素显示徽记、脚下光环和粒子；不再覆盖怪物本体颜色。</summary>
    public void PlayOnStatusVfx(float duration, ElementType element)
    {
        if (statusVfxCoroutine != null) StopCoroutine(statusVfxCoroutine);
        ClearStatusPresenter();
        statusVfxCoroutine = StartCoroutine(PlayStatusVfxCo(duration, element, GetElementColor(element)));
    }

    /// <summary>停止全部特效协程并恢复默认外观。</summary>
    public void StopAllVfx()
    {
        StopAllCoroutines();
        statusVfxCoroutine = null;
        ClearStatusPresenter();
        if (sr == null) return;
        sr.color = originalSpriteColor;
        sr.material = originalMaterial;
    }

    /// <summary>异常状态持续期间保持独立世界空间提示，到时自动清理。</summary>
    private IEnumerator PlayStatusVfxCo(float duration, ElementType element, Color effectColor)
    {
        if (sr == null) yield break;
        statusPresenter = StatusEffectPresenter.Create(transform, sr, element, effectColor);
        yield return new WaitForSeconds(duration);
        ClearStatusPresenter();
        statusVfxCoroutine = null;
    }

    private void ClearStatusPresenter()
    {
        if (statusPresenter == null) return;
        Destroy(statusPresenter.gameObject);
        statusPresenter = null;
    }

    /// <summary>在目标位置生成命中/暴击特效，暴击时按朝向翻转。</summary>
    public void CreateOnHitVFX(Transform target, bool isCrit, ElementType element)
    {
        GameObject hitPrefab = isCrit ? critHitVFX : hitVFX;
        if (hitPrefab == null) return;
        GameObject vfx = Instantiate(hitPrefab, target.position, Quaternion.identity);
        if (entity != null && entity.facingDir == -1 && isCrit) vfx.transform.Rotate(0, 180, 0);
    }

    /// <summary>返回元素对应的特效颜色。</summary>
    public Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Ice:
                return chillVfx;
            case ElementType.Fire:
                return burnVfx;
            case ElementType.Lightning:
                return shockVfx;
            default:
                return Color.white;
        }
    }

    /// <summary>播放受击闪白；连续受击会刷新持续时间。</summary>
    public void PlayOnDamageVfx()
    {
        if (sr == null || onDamageMaterial == null) return;
        if (onDamageVfxCoroutine != null) StopCoroutine(onDamageVfxCoroutine);
        onDamageVfxCoroutine = StartCoroutine(OnDamageVfxCo());
    }

    /// <summary>受击材质替换协程。</summary>
    private IEnumerator OnDamageVfxCo()
    {
        sr.material = onDamageMaterial;
        yield return new WaitForSeconds(onDamageVfxDuration);
        sr.material = originalMaterial;
    }
}
