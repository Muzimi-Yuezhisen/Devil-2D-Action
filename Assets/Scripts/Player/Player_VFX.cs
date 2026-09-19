using System.Collections;
using UnityEngine;

/// <summary>
/// 玩家特效：冲刺残影等。
/// </summary>
public class Player_VFX : Entity_VFX
{
    [Header("残影特效")]
    /// <summary>生成残影的间隔。</summary>
    [Range(0.01f, 0.2f)]
    [SerializeField] private float imageEchoInterval = 0.5f;
    /// <summary>残影预制体。</summary>
    [SerializeField] private GameObject imageEchoPrefab;
    /// <summary>残影协程引用。</summary>
    private Coroutine imageEchoCo;

    /// <summary>在指定时长内按间隔生成残影。</summary>
    public void DoImageEchoEffect(float duration)
    {
        if (imageEchoCo != null) StopCoroutine(imageEchoCo);
        imageEchoCo = StartCoroutine(ImageEchoEffectCo(duration));
    }

    /// <summary>残影生成循环。</summary>
    private IEnumerator ImageEchoEffectCo(float duration)
    {
        float time = 0;
        while (time < duration)
        {
            CreateImageEcho();
            yield return new WaitForSeconds(imageEchoInterval);
            time += imageEchoInterval;
        }
    }

    /// <summary>在当前位置复制当前帧精灵作为残影。</summary>
    private void CreateImageEcho()
    {
        GameObject imageEcho = Instantiate(imageEchoPrefab, transform.position, transform.rotation);
        imageEcho.GetComponentInChildren<SpriteRenderer>().sprite = sr.sprite;
    }
}
