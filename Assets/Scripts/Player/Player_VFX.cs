using System.Collections;
using UnityEngine;

/// <summary>
/// 创建冲刺时产生的残影
/// </summary>
public class Player_VFX : Entity_VFX
{
    [Header("Image Echo VFX")]
    [Range(0.01f, 0.2f)]
    [SerializeField] private float imageEchoInterval = 0.5f;
    [SerializeField] private GameObject imageEchoPrefab;
    private Coroutine imageEchoCo;

    public void DoImageEchoEffect(float duration)
    {
        if (imageEchoCo != null) StopCoroutine(imageEchoCo);
        imageEchoCo = StartCoroutine(ImageEchoEffectCo(duration));
    }

    //每imageEchoInterval创建一个残影
    private IEnumerator ImageEchoEffectCo(float duration)
    {
        float time = 0;

        while(time < duration)
        {
            CreateImageEcho();

            yield return new WaitForSeconds(imageEchoInterval);
            time += imageEchoInterval;
        }
    }

    private void CreateImageEcho()
    {
        GameObject imageEcho = Instantiate(imageEchoPrefab, transform.position, transform.rotation);
        imageEcho.GetComponentInChildren<SpriteRenderer>().sprite = sr.sprite;
    }
}
