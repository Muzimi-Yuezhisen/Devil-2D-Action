using UnityEngine;

[System.Serializable]
public class ParallaxLayer
{
    [SerializeField]
    private Transform background;   //图层
    [SerializeField]
    private float parallaxMultiplier; //不同图层的移动速度倍率
    [SerializeField]
    private float imageWidthOffset = 10;

    //图片宽度
    private float imageWidth;
    private float imageHalfWidth;

    public void CalculateImageWidth()
    {
        imageWidth = background.GetComponent<SpriteRenderer>().bounds.size.x;
        imageHalfWidth = imageWidth / 2;
    }
    public void Move(float distanceToMove)
    {
        background.position += Vector3.right * (distanceToMove * parallaxMultiplier);
    }

    public void LoopBackground(float cameraLeftEdge,float cameraRightEdge)
    {
        float imageRightEdge = (background.position.x + imageHalfWidth) - imageWidthOffset;
        float imageLeftEdge = (background.position.x - imageHalfWidth) + imageWidthOffset;

        if (imageRightEdge < cameraLeftEdge) background.position += Vector3.right * imageWidth;
        else if (imageLeftEdge > cameraRightEdge) background.position += Vector3.right * -imageWidth;
    }
}
