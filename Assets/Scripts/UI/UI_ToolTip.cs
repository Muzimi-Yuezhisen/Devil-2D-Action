using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 提示面板基类：跟随目标并避免超出屏幕。
/// </summary>
public class UI_ToolTip : MonoBehaviour
{
    /// <summary>自身 RectTransform。</summary>
    private RectTransform rect;
    /// <summary>相对目标的偏移。</summary>
    [SerializeField] private Vector2 offset = new Vector2(300, 20);

    /// <summary>缓存 RectTransform。</summary>
    protected virtual void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    /// <summary>显示时贴到目标旁；隐藏时移到远处。</summary>
    public virtual void ShowToolTip(bool show, RectTransform targetRect)
    {
        if (show == false)
        {
            rect.position = new Vector2(9999, 9999);
            return;
        }
        UpdatePosition(targetRect);
    }

    /// <summary>按屏幕左右与上下边界调整提示位置。</summary>
    private void UpdatePosition(RectTransform targetRect)
    {
        float screenCenterX = Screen.width / 2;
        float screenTop = Screen.height;
        float screenBottom = 0;

        Vector2 targetPosition = targetRect.position;

        targetPosition.x = targetPosition.x > screenCenterX ? targetPosition.x - offset.x : targetPosition.x + offset.x;

        float verticalHalf = rect.sizeDelta.y / 2f;
        float topY = targetPosition.y + verticalHalf;
        float bottomY = targetPosition.y - verticalHalf;

        if (topY > screenTop) targetPosition.y = screenTop - verticalHalf - offset.y;
        else if (bottomY < screenBottom) targetPosition.y = screenBottom + verticalHalf + offset.y;

        rect.position = targetPosition;
    }

    /// <summary>生成带颜色标签的富文本。</summary>
    protected string GetColoredText(string color, string text)
    {
        return $"<color={color}>{text}</color>";
    }
}
