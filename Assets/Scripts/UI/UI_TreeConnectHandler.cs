using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 一条连线配置：子节点、方向、长度与旋转微调。
/// </summary>
[Serializable]
public class UI_TreeConnectDetails
{
    /// <summary>子节点上的连线处理器。</summary>
    public UI_TreeConnectHandler childNode;
    /// <summary>连线方向。</summary>
    public NodeDirectionType direction;
    /// <summary>连线长度。</summary>
    [Range(100f, 350f)] public float length;
    /// <summary>角度微调。</summary>
    [Range(-25f, 25f)] public float rotation;
}

/// <summary>
/// 管理从一个节点连出去的线，并自动摆好子节点位置。
/// </summary>
public class UI_TreeConnectHandler : MonoBehaviour
{
    /// <summary>自身矩形。</summary>
    private RectTransform rect => GetComponent<RectTransform>();
    /// <summary>连线细节配置。</summary>
    [SerializeField] private UI_TreeConnectDetails[] connectionDetails;
    /// <summary>实际连线组件。</summary>
    [SerializeField] private UI_TreeConnection[] connections;
    /// <summary>指向本节点的连线 Image（由父节点传入）。</summary>
    private Image connectionImage;
    /// <summary>连线原始颜色。</summary>
    private Color originalColor;
#if UNITY_EDITOR
    private bool validationQueued;
#endif

    /// <summary>缓存原始连线色。</summary>
    private void Awake()
    {
        if (connectionImage != null) originalColor = connectionImage.color;
    }

    /// <summary>返回所有直接子技能节点。</summary>
    public UI_TreeNode[] GetChildNodes()
    {
        List<UI_TreeNode> childrenToReturn = new List<UI_TreeNode>();

        foreach (var node in connectionDetails)
        {
            if (node.childNode != null)
            {
                childrenToReturn.Add(node.childNode.GetComponent<UI_TreeNode>());
            }
        }
        return childrenToReturn.ToArray();
    }

    /// <summary>按配置摆线、摆子节点，并把线的 Image 交给子节点。</summary>
    private void UpdateConnections()
    {
        if (connectionDetails == null || connections == null) return;
        for (int i = 0; i < connectionDetails.Length; i++)
        {
            var detail = connectionDetails[i];
            var connection = connections[i];
            if (detail == null || connection == null) continue;
            Image connectionImage = connection.GetConnectionImage();
            connection.DirectConnection(detail.direction, detail.length, detail.rotation);

            Vector2 targetPosition = connection.GetConnectionPoint(rect);
            if (detail.childNode == null) continue;

            detail.childNode.SetPosition(targetPosition);
            detail.childNode.SetConnectionImage(connectionImage);
            detail.childNode.transform.SetAsLastSibling();
        }
    }

    /// <summary>更新本节点连线，并递归更新所有子节点。</summary>
    public void UpdateAllConnections()
    {
        UpdateConnections();
        foreach (var node in connectionDetails)
        {
            if (node.childNode == null) continue;
            node.childNode.UpdateConnections();
        }
    }

    /// <summary>解锁时把连线提亮为白色，退还时恢复原色。</summary>
    public void UnlockConnectionImage(bool unlocked)
    {
        if (connectionImage == null) return;
        connectionImage.color = unlocked ? Color.white : originalColor;
    }

    /// <summary>设置指向本节点的连线 Image。</summary>
    public void SetConnectionImage(Image image) => connectionImage = image;

    /// <summary>设置本节点锚点位置。</summary>
    public void SetPosition(Vector2 position) => rect.anchoredPosition = position;

    /// <summary>编辑器校验数量并刷新布局。</summary>
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (validationQueued) return;
        validationQueued = true;
        EditorApplication.delayCall += ValidateAndUpdate;
#endif
    }

#if UNITY_EDITOR
    /// <summary>把层级调整延迟到 OnValidate 结束后，避免 Unity 禁止的嵌套 SendMessage。</summary>
    private void ValidateAndUpdate()
    {
        if (this == null) return;
        validationQueued = false;

        if (connectionDetails == null || connections == null || connectionDetails.Length <= 0) return;
        if (connectionDetails.Length != connections.Length)
        {
            Debug.Log("details的数量和connections的数量不匹配。-" + gameObject.name);
            return;
        }
        UpdateAllConnections();
    }
#endif
}
