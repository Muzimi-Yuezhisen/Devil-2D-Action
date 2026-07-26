using System.Collections;
using UnityEngine;

//受击替换空白材质
public class Entity_VFX : MonoBehaviour
{
    protected SpriteRenderer sr;
    private Entity entity;

    [Header("On Taking Damage VFX")]
    [SerializeField] private Material onDamageMaterial;
    [SerializeField] private float onDamageVfxDuration = 0.2f;
    private Material originalMaterial;
    private Coroutine onDamageVfxCoroutine;

    [Header("On Doing Damage VFX")]
    [SerializeField] private Color hitVfxColor = Color.white;
    [SerializeField] private GameObject hitVFX;
    [SerializeField] private GameObject critHitVFX;

    [Header("Element Colors")]
    [SerializeField] private Color chillVfx = Color.cyan;
    [SerializeField] private Color burnVfx = Color.red;
    [SerializeField] private Color shockVfx = Color.yellow;
    private Color originalHitVfxColor;
    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        entity = GetComponent<Entity>();
        originalMaterial = sr.material;
        originalHitVfxColor = hitVfxColor;
    }

    //陷入异常状态，闪烁
    public void PlayOnStatusVfx(float duration,ElementType element)
    {
        if(element == ElementType.Ice)
        {
            StartCoroutine(PlayStatusVfxCo(duration, chillVfx));
        }
        if(element == ElementType.Fire)
        {
            StartCoroutine(PlayStatusVfxCo(duration, burnVfx));
        }
        if(element == ElementType.Lightning)
        {
            StartCoroutine(PlayStatusVfxCo(duration, shockVfx));
        }
    }

    public void StopAllVfx()
    {
        StopAllCoroutines();
        sr.color = Color.white;
        sr.material = originalMaterial;
    }

    private IEnumerator PlayStatusVfxCo(float duration, Color effectColor)
    {
        float tickInterval = 0.25f;
        float timeHasPassed = 0;

        Color lightColor = effectColor * 1.2f;
        Color darkColor = effectColor * 0.8f;

        bool toggle = false;

        while(timeHasPassed < duration)
        {
            sr.color = toggle ? lightColor : darkColor;
            toggle = !toggle;

            yield return new WaitForSeconds(tickInterval);
            timeHasPassed += tickInterval;
        }

        sr.color = Color.white;
    }

    public void CreateOnHitVFX(Transform target, bool isCrit, ElementType element)
    {
        GameObject hitPrefab = isCrit ? hitVFX : critHitVFX;
        GameObject vfx = Instantiate(hitPrefab, target.position, Quaternion.identity);
        //vfx.GetComponentInChildren<SpriteRenderer>().color = GetElementColor(element);

        //旋转特效方向
        if (entity.facingDir == -1 && isCrit) vfx.transform.Rotate(0, 180, 0);
    }

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

    public void PlayOnDamageVfx()
    {
        //连续挨打，就刷新时间
        if (onDamageVfxCoroutine != null) StopCoroutine(onDamageVfxCoroutine); 
        onDamageVfxCoroutine = StartCoroutine(OnDamageVfxCo());
    }

    private IEnumerator OnDamageVfxCo()
    {
        sr.material = onDamageMaterial;
        yield return new WaitForSeconds(onDamageVfxDuration);
        sr.material = originalMaterial;
    }
}
