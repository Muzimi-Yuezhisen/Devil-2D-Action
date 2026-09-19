using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 技能树节点：解锁条件、互斥、扣点，并写入玩家技能升级。
/// </summary>
public class UI_TreeNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    /// <summary>主 UI。</summary>
    private UI ui;
    /// <summary>自身矩形（提示定位用）。</summary>
    private RectTransform rect;
    /// <summary>所属技能树。</summary>
    private UI_SkillTree skillTree;
    /// <summary>连线与子节点处理器。</summary>
    private UI_TreeConnectHandler connectHandler;

    [Header("解锁设置")]
    /// <summary>需要先解锁的前置节点。</summary>
    public UI_TreeNode[] neededNodes;
    /// <summary>互斥分支节点。</summary>
    public UI_TreeNode[] conflictNodes;
    /// <summary>是否已解锁。</summary>
    public bool isUnlocked;
    /// <summary>是否被互斥锁死（不可再解锁）。</summary>
    public bool isLocked;

    [Header("技能设置")]
    /// <summary>节点对应的技能数据。</summary>
    public Skill_DataSO skillData;
    /// <summary>显示用名称（由数据同步）。</summary>
    [SerializeField] private string skillName;
    /// <summary>技能图标。</summary>
    [SerializeField] private Image skillIcon;
    /// <summary>解锁消耗（由数据同步）。</summary>
    [SerializeField] private int skillCost;
    /// <summary>未解锁时图标颜色（十六进制）。</summary>
    [SerializeField] private string lockedColorHex = "#9F9797";

    /// <summary>当前默认图标色。</summary>
    private Color defaultIconColor;
    /// <summary>悬停高亮色。</summary>
    private static readonly Color HoverIconColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    /// <summary>缓存引用并设置初始图标色。</summary>
    private void Awake()
    {
        ui = GetComponentInParent<UI>();
        rect = GetComponent<RectTransform>();
        skillTree = GetComponentInParent<UI_SkillTree>();
        connectHandler = GetComponent<UI_TreeConnectHandler>();

        defaultIconColor = GetColorByHex(lockedColorHex);
        SetIconColor(defaultIconColor);
    }

    /// <summary>默认解锁的技能开局直接解锁。</summary>
    private void Start()
    {
        if (skillData.unlockedByDefault) Unlock();
    }

    /// <summary>退还本节点：恢复锁定状态、退点、暗连线。</summary>
    public void Refund()
    {
        isUnlocked = false;
        isLocked = false;
        SetIconColor(GetColorByHex(lockedColorHex));

        skillTree.AddSkillPoints(skillData.cost);
        connectHandler.UnlockConnectionImage(false);
    }

    /// <summary>解锁：扣点、锁互斥、亮连线，并 SetSkillUpgrade。</summary>
    private void Unlock()
    {
        if (skillTree.TrySpendSkillPoints(skillData.cost, skillData.displayName) == false) return;

        isUnlocked = true;
        defaultIconColor = Color.white;
        SetIconColor(Color.white);
        LockConflictNodes();
        connectHandler.UnlockConnectionImage(true);

        skillTree.skillManager.GetSkillByType(skillData.skillType).SetSkillUpgrade(skillData.upgradeData);
    }

    /// <summary>是否满足点数、前置与互斥条件。</summary>
    private bool CanBeUnlocked()
    {
        if (isUnlocked || isLocked) return false;

        if (skillTree.EnoughSkillPoints(skillData.cost) == false) return false;

        foreach (var node in neededNodes)
        {
            if (node.isUnlocked == false) return false;
        }

        foreach (var node in conflictNodes)
        {
            if (node.isUnlocked) return false;
        }

        return true;
    }

    /// <summary>锁死互斥节点及其子树。</summary>
    private void LockConflictNodes()
    {
        foreach (var node in conflictNodes)
        {
            node.isLocked = true;
            node.LockChildNodes();
        }
    }

    /// <summary>递归锁死当前节点及所有子节点。</summary>
    public void LockChildNodes()
    {
        isLocked = true;
        foreach (var node in connectHandler.GetChildNodes())
        {
            node.LockChildNodes();
        }
    }

    /// <summary>设置图标颜色。</summary>
    private void SetIconColor(Color color)
    {
        if (skillIcon == null) return;
        skillIcon.color = color;
    }

    /// <summary>点击：可解锁则解锁，已锁死则闪提示。</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        AudioManager.PlaySfx(AudioCue.UiClick);
        if (CanBeUnlocked()) Unlock();
        else if (isLocked) ui.skillToolTip.LockedSkillEffect();
    }

    /// <summary>悬停：高亮未解锁图标并显示提示。</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUnlocked == false && isLocked == false)
            SetIconColor(HoverIconColor);

        if (ui != null && ui.skillToolTip != null)
            ui.skillToolTip.ShowToolTip(true, rect, this);
    }

    /// <summary>离开：隐藏提示并恢复图标色。</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (ui != null && ui.skillToolTip != null)
            ui.skillToolTip.ShowToolTip(false, rect);

        if (isUnlocked == false && isLocked == false)
            SetIconColor(defaultIconColor);
    }

    /// <summary>解析十六进制颜色。</summary>
    private Color GetColorByHex(string hexNumber)
    {
        ColorUtility.TryParseHtmlString(hexNumber, out Color color);
        return color;
    }

    /// <summary>禁用时按锁定/解锁状态恢复图标色。</summary>
    private void OnDisable()
    {
        if (isLocked) SetIconColor(GetColorByHex(lockedColorHex));
        if (isUnlocked) SetIconColor(Color.white);
    }

    /// <summary>编辑器：从 Skill_DataSO 同步名称、图标与消耗。</summary>
    private void OnValidate()
    {
        if (skillData == null) return;
        skillName = skillData.displayName;
        skillIcon.sprite = skillData.icon;
        skillCost = skillData.cost;
        gameObject.name = "UI_TreeNode - " + skillData.displayName;
    }
}
