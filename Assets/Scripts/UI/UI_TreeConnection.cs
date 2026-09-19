using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能树连线的八个方向。
/// </summary>
public enum NodeDirectionType
{
    /// <summary>无连线。</summary>
    None,
    /// <summary>左上。</summary>
    UpLeft,
    /// <summary>正上。</summary>
    Up,
    /// <summary>右上。</summary>
    UpRight,
    /// <summary>正左。</summary>
    Left,
    /// <summary>正右。</summary>
    Right,
    /// <summary>左下。</summary>
    DownLeft,
    /// <summary>正下。</summary>
    Down,
    /// <summary>右下。</summary>
    DownRight
}

/// <summary>
/// 单条技能树连线：按方向、长度、旋转摆放端点。
/// </summary>
public class UI_TreeConnection : MonoBehaviour
{
    /// <summary>旋转支点。</summary>
    [SerializeField] private RectTransform rotationPoint;
    /// <summary>控制长度的矩形（带 Image）。</summary>
    [SerializeField] private RectTransform connectionLength;
    /// <summary>子节点应放置的端点。</summary>
    [SerializeField] private RectTransform childNodeConnectionPoint;

    /// <summary>按方向与长度摆放连线；None 时长度为 0。</summary>
    public void DirectConnection(NodeDirectionType direction, float length, float offset)
    {
        bool shouldBeActive = direction != NodeDirectionType.None;
        float finalLength = shouldBeActive ? length : 0;
        float angle = GetDirectionAngle(direction);

        rotationPoint.localRotation = Quaternion.Euler(0, 0, angle + offset);
        connectionLength.sizeDelta = new Vector2(finalLength, connectionLength.sizeDelta.y);
    }

    /// <summary>返回连线 Image。</summary>
    public Image GetConnectionImage() => connectionLength.GetComponent<Image>();

    /// <summary>把端点世界坐标转为父节点下的本地坐标。</summary>
    public Vector2 GetConnectionPoint(RectTransform rect)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect.parent as RectTransform,
            childNodeConnectionPoint.position,
            null,
            out var localPosition
            );
        return localPosition;
    }

    /// <summary>方向枚举转角度。</summary>
    private float GetDirectionAngle(NodeDirectionType type)
    {
        switch (type)
        {
            case NodeDirectionType.UpLeft: return 135f;
            case NodeDirectionType.Up: return 90f;
            case NodeDirectionType.UpRight: return 45f;
            case NodeDirectionType.Left: return 180f;
            case NodeDirectionType.Right: return 0f;
            case NodeDirectionType.DownLeft: return -135f;
            case NodeDirectionType.Down: return -90f;
            case NodeDirectionType.DownRight: return -45f;
            default: return 0f;
        }
    }
}
