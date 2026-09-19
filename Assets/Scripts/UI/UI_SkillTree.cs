using UnityEngine;

/// <summary>
/// 技能树：管理技能点，并刷新根节点连线布局。
/// </summary>
public class UI_SkillTree : MonoBehaviour
{
    /// <summary>点数变化：当前值、变化量、来源。</summary>
    public event System.Action<int, int, string> SkillPointsChanged;

    /// <summary>当前可用技能点。</summary>
    [SerializeField] private int skillPoints;
    /// <summary>连线布局用的根节点处理器。</summary>
    [SerializeField] private UI_TreeConnectHandler[] parentNodes;
    /// <summary>玩家技能管理器（解锁时写入升级）。</summary>
    public Player_SkillManager skillManager { get; private set; }
    /// <summary>当前可消费技能点，供 HUD、奖励系统与测试读取。</summary>
    public int CurrentSkillPoints => skillPoints;

    /// <summary>查找场景中的技能管理器。</summary>
    private void Awake()
    {
        skillManager = FindAnyObjectByType<Player_SkillManager>();
    }

    /// <summary>开局刷新全部连线位置。</summary>
    private void Start()
    {
        UpdateAllConnections();
        NotifySkillPointsChanged(0, "初始技能点");
    }

    /// <summary>退还全部已解锁技能（编辑器菜单）。</summary>
    [ContextMenu("Reset Skill Tree")]
    public void RefundAllSkills()
    {
        UI_TreeNode[] skillNodes = GetComponentsInChildren<UI_TreeNode>();
        foreach (var node in skillNodes) node.Refund();
    }

    /// <summary>技能点是否足够支付开销。</summary>
    public bool EnoughSkillPoints(int cost) => skillPoints >= cost;
    /// <summary>扣除技能点。</summary>
    public void RemoveSkillPoints(int cost) => TrySpendSkillPoints(cost, "技能");
    /// <summary>增加技能点。</summary>
    public void AddSkillPoints(int points) => AddSkillPoints(points, "奖励");

    /// <summary>带来源地增加技能点，让 HUD 能解释奖励来自哪里。</summary>
    public void AddSkillPoints(int points, string source)
    {
        if (points <= 0) return;
        skillPoints += points;
        NotifySkillPointsChanged(points, source);
    }

    /// <summary>原子化消费技能点，避免检查与扣除之间出现负数。</summary>
    public bool TrySpendSkillPoints(int cost, string source)
    {
        cost = Mathf.Max(0, cost);
        if (!EnoughSkillPoints(cost)) return false;

        skillPoints -= cost;
        NotifySkillPointsChanged(-cost, source);
        return true;
    }

    /// <summary>关卡构建器设置本次试玩的初始点数。</summary>
    public void ConfigureInitialSkillPoints(int points)
    {
        skillPoints = Mathf.Max(0, points);
    }

    private void NotifySkillPointsChanged(int delta, string source)
    {
        SkillPointsChanged?.Invoke(skillPoints, delta, string.IsNullOrWhiteSpace(source) ? "未知来源" : source);
    }

    /// <summary>更新所有根节点及其子树连线布局。</summary>
    [ContextMenu("Update All Connections")]
    public void UpdateAllConnections()
    {
        foreach (var node in parentNodes) node.UpdateAllConnections();
    }
}
