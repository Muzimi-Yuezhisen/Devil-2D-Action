using UnityEngine;

/// <summary>
/// 单个视差图层：按倍率移动，并在相机两侧循环拼贴。
/// </summary>
[System.Serializable]
public class ParallaxLayer
{
    /// <summary>图层 Transform。</summary>
    [SerializeField]
    private Transform background;
    /// <summary>相对相机移动的倍率（越小越远）。</summary>
    [SerializeField]
    private float parallaxMultiplier;
    /// <summary>循环判定时的边缘余量。</summary>
    [SerializeField]
    private float imageWidthOffset = 10;
    /// <summary>精灵世界宽度。</summary>
    private float imageWidth;
    /// <summary>半宽。</summary>
    private float imageHalfWidth;

    /// <summary>根据 SpriteRenderer 计算图片宽度。</summary>
    public void CalculateImageWidth()
    {
        imageWidth = background.GetComponent<SpriteRenderer>().bounds.size.x;
        imageHalfWidth = imageWidth / 2;
    }

    /// <summary>按视差倍率跟随相机的水平与垂直位移。</summary>
    public void Move(Vector2 cameraDelta)
    {
        if (background == null) return;
        background.position += new Vector3(cameraDelta.x, cameraDelta.y, 0) * parallaxMultiplier;
    }

    /// <summary>图层精灵是否覆盖指定的纵向世界范围。</summary>
    public bool CoversVerticalRange(float bottom, float top, float margin)
    {
        if (background == null) return false;
        SpriteRenderer renderer = background.GetComponent<SpriteRenderer>();
        if (renderer == null) return false;
        Bounds bounds = renderer.bounds;
        return bounds.min.y <= bottom - margin && bounds.max.y >= top + margin;
    }

    /// <summary>图层完全离开相机一侧时平移一整幅宽度实现循环。</summary>
    public void LoopBackground(float cameraLeftEdge, float cameraRightEdge)
    {
        float imageRightEdge = (background.position.x + imageHalfWidth) - imageWidthOffset;
        float imageLeftEdge = (background.position.x - imageHalfWidth) + imageWidthOffset;

        if (imageRightEdge < cameraLeftEdge) background.position += Vector3.right * imageWidth;
        else if (imageLeftEdge > cameraRightEdge) background.position += Vector3.right * -imageWidth;
    }
}
