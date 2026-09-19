using System.Collections;
using UnityEngine;

/// <summary>
/// 一次性特效：可选淡出、随机偏移/旋转，到时自动销毁。
/// </summary>
public class VFX_AutoController : MonoBehaviour
{
    /// <summary>精灵渲染器。</summary>
    private SpriteRenderer sr;

    /// <summary>是否自动销毁。</summary>
    [SerializeField] private bool autoDestroy = true;
    /// <summary>销毁延迟（秒）。</summary>
    [SerializeField] private float destroyDelay = 1;
    [Space]
    /// <summary>是否随机位置偏移。</summary>
    [SerializeField] private bool randomOffset = true;
    /// <summary>是否随机旋转。</summary>
    [SerializeField] private bool randomRotation = true;

    [Header("淡出效果")]
    /// <summary>是否开启淡出。</summary>
    [SerializeField] private bool canFade;
    /// <summary>透明度每秒减少量。</summary>
    [SerializeField] private float fadeSpeed = 1;

    [Header("随机旋转")]
    /// <summary>随机旋转最小角度。</summary>
    [SerializeField] private float minRotation = 0;
    /// <summary>随机旋转最大角度。</summary>
    [SerializeField] private float maxRotation = 360;

    [Header("随机位置")]
    /// <summary>X 偏移下限。</summary>
    [SerializeField] private float xMinOffset = -0.3f;
    /// <summary>X 偏移上限。</summary>
    [SerializeField] private float xMaxOffset = 0.3f;
    [Space]
    /// <summary>Y 偏移下限。</summary>
    [SerializeField] private float yMinOffset = -0.3f;
    /// <summary>Y 偏移上限。</summary>
    [SerializeField] private float yMaxOffset = 0.3f;

    /// <summary>缓存精灵。</summary>
    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>启动淡出、随机变换与定时销毁。</summary>
    private void Start()
    {
        if (canFade) StartCoroutine(FadeCo());
        ApplyRandomOffset();
        ApplyRandomRotation();
        if (autoDestroy) Destroy(gameObject, destroyDelay);
    }

    /// <summary>逐帧降低透明度。</summary>
    private IEnumerator FadeCo()
    {
        Color targetColor = Color.white;
        while (targetColor.a > 0)
        {
            targetColor.a -= fadeSpeed * Time.deltaTime;
            sr.color = targetColor;
            yield return null;
        }
        sr.color = targetColor;
    }

    /// <summary>在当前位置叠加随机偏移。</summary>
    private void ApplyRandomOffset()
    {
        if (randomOffset == false) return;
        float xOffset = Random.Range(xMinOffset, xMaxOffset);
        float yOffset = Random.Range(yMinOffset, yMaxOffset);
        transform.position = transform.position + new Vector3(xOffset, yOffset);
    }

    /// <summary>绕 Z 轴随机旋转。</summary>
    private void ApplyRandomRotation()
    {
        if (randomRotation == false) return;
        float zRotation = Random.Range(minRotation, maxRotation);
        transform.Rotate(0, 0, zRotation);
    }
}
