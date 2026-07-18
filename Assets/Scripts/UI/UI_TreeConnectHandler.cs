using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//连接点的细节：方向和长度
[Serializable]
public class UI_TreeConnectDetails
{
    public UI_TreeConnectHandler childNode;
    public NodeDirectionType direction;
    [Range(100f, 350f)] public float length;
    [Range(-25f, 25f)] public float rotation;
}

/// <summary>
/// 管理技能树里「从一个节点连出去」的线，并自动摆好子节点位置
/// </summary>
public class UI_TreeConnectHandler : MonoBehaviour
{
    private RectTransform rect => GetComponent<RectTransform>();
    [SerializeField] private UI_TreeConnectDetails[] connectionDetails;
    [SerializeField] private UI_TreeConnection[] connections;

    //连接线和颜色
    private Image connectionImage;
    private Color originalColor;

    private void Awake()
    {
        if (connectionImage != null) originalColor = connectionImage.color;
    }

    //返回子节点
    public UI_TreeNode[] GetChildNodes()
    {
        List<UI_TreeNode> childrenToReturn = new List<UI_TreeNode>();
        
        foreach(var node in connectionDetails)
        {
            if(node.childNode != null)
            {
                childrenToReturn.Add(node.childNode.GetComponent<UI_TreeNode>());
            }

        }

        return childrenToReturn.ToArray();
    }

    /// <summary>
    /// 自动摆线、摆子节点、把线的 Image 交给子节点
    /// </summary>
    private void UpdateConnections()
    {
        for(int i = 0; i < connectionDetails.Length; i++)
        {
            var detail = connectionDetails[i];
            var connection = connections[i];

            Image connectionImage = connection.GetConnectionImage();

            connection.DirectConnection(detail.direction, detail.length, detail.rotation);
            Vector2 targetPosition = connection.GetConnectionPoint(rect);

            if (detail.childNode == null) continue;
            detail.childNode.SetPosition(targetPosition);
            detail.childNode.SetConnectionImage(connectionImage);
            detail.childNode.transform.SetAsLastSibling(); //让节点按照父子架构设置层级
        }
    }

    /// <summary>
    /// 更新这个节点的位置以及所有子节点的位置
    /// </summary>
    public void UpdateAllConnections()
    {
        UpdateConnections();

        foreach(var node in connectionDetails)
        {
            if (node.childNode == null) continue;
            node.childNode.UpdateConnections();
        }
    }

    public void UnlockConnectionImage(bool unlocked)
    {
        if(connectionImage == null) return;
        connectionImage.color = unlocked ? Color.white : originalColor;
    }

    //替换连接线
    public void SetConnectionImage(Image image) => connectionImage = image;
    public void SetPosition(Vector2 position) => rect.anchoredPosition = position;

    private void OnValidate()
    {
        if (connectionDetails.Length <= 0) return;

        if (connectionDetails.Length != connections.Length)
        {
            Debug.Log("details的数量和connections的数量不匹配。-" + gameObject.name);
            return;
        }
        UpdateAllConnections();
    }
}
