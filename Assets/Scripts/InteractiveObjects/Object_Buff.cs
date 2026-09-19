using System.Collections;
using UnityEngine;

/// <summary>
/// 一条属性增益：类型与数值。
/// </summary>
[System.Serializable]
public class Buff
{
    /// <summary>要修改的属性类型。</summary>
    public StatType type;
    /// <summary>修正数值。</summary>
    public float value;
}

/// <summary>
/// 可拾取增益物：碰撞后临时加属性，并上下漂浮。
/// </summary>
public class Object_Buff : MonoBehaviour
{
    /// <summary>供 HUD 显示拾取来源与持续时间。</summary>
    public static event System.Action<string, float> OnBuffPickedUp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => OnBuffPickedUp = null;

    /// <summary>自身精灵（生效时透明）。</summary>
    private SpriteRenderer sr;
    /// <summary>被修改属性的实体。</summary>
    private Entity_Stats statsToModify;

    [Header("增益设置")]
    /// <summary>本物体提供的增益列表。</summary>
    [SerializeField] private Buff[] buffs;
    /// <summary>增益持续时间。</summary>
    [SerializeField] private float buffDuration = 4;
    /// <summary>是否还可被拾取（生效中关闭防重复）。</summary>
    [SerializeField] private bool canBeUsed = true;
    /// <summary>修正来源名（移除时用）。</summary>
    [SerializeField] private string buffName;

    [Header("漂浮移动")]
    /// <summary>上下漂浮速度。</summary>
    [SerializeField] private float floatSpeed = 1f;
    /// <summary>漂浮振幅。</summary>
    [SerializeField] private float floatRange = 0.1f;
    /// <summary>初始位置。</summary>
    private Vector3 startPosition;

    /// <summary>缓存精灵与起点。</summary>
    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        startPosition = transform.position;
    }

    /// <summary>正弦上下漂浮。</summary>
    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatRange;
        transform.position = startPosition + new Vector3(0, yOffset);
    }

    /// <summary>玩家碰触后开始增益协程。</summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (canBeUsed == false) return;
        Player player = collision.GetComponent<Player>();
        if (player == null) player = collision.GetComponentInParent<Player>();
        if (player == null) return;

        statsToModify = player.GetComponent<Entity_Stats>();
        if (statsToModify == null) return;
        StartCoroutine(BuffCo(buffDuration));
    }

    /// <summary>添加修正，到期移除并销毁物体。</summary>
    private IEnumerator BuffCo(float duration)
    {
        canBeUsed = false;
        sr.color = Color.clear;

        foreach (var buff in buffs)
        {
            Stat stat = statsToModify.GetStatByType(buff.type);
            stat?.AddModifier(buff.value, buffName);
        }

        OnBuffPickedUp?.Invoke(string.IsNullOrWhiteSpace(buffName) ? "神秘增益" : buffName, buffDuration);
        AudioManager.PlaySfx(AudioCue.MenuOpen);

        yield return new WaitForSeconds(duration);

        foreach (var buff in buffs)
        {
            Stat stat = statsToModify.GetStatByType(buff.type);
            stat?.RemoveModifier(buffName);
        }
        Destroy(gameObject);
    }
}
