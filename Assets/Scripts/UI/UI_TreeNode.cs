using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 每个技能图标节点
/// </summary>
public class UI_TreeNode : MonoBehaviour , IPointerEnterHandler , IPointerExitHandler , IPointerDownHandler
{
    private UI ui;
    private RectTransform rect;
    private UI_SkillTree skillTree;
    private UI_TreeConnectHandler connectHandler;

    [Header("Unlock details")]
    public UI_TreeNode[] neededNodes; //前置技能
    public UI_TreeNode[] conflictNodes; //互斥分支
    public bool isUnlocked; //是否已解锁
    public bool isLocked;   //是否被互斥锁死（不可再解锁）

    [Header("Skill details")]
    public Skill_DataSO skillData;
    [SerializeField] private string skillName;
    [SerializeField] private Image skillIcon;
    [SerializeField] private int skillCost;
    [SerializeField] private string lockedColorHex = "#9F9797";

    private Color defaultIconColor;
    private static readonly Color HoverIconColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    private void Awake()
    {
        ui = GetComponentInParent<UI>();
        rect = GetComponent<RectTransform>();
        skillTree = GetComponentInParent<UI_SkillTree>();
        connectHandler = GetComponent<UI_TreeConnectHandler>();

        defaultIconColor = GetColorByHex(lockedColorHex);
        SetIconColor(defaultIconColor);
    }

    private void Start()
    {
        if (skillData.unlockedByDefault) Unlock();
    }

    //取消技能的选择
    public void Refund()
    {
        isUnlocked = false;
        isLocked = false;
        SetIconColor(GetColorByHex(lockedColorHex));

        skillTree.AddSkillPoints(skillData.cost);
        connectHandler.UnlockConnectionImage(false);
    }

    //解锁技能，标记解锁、扣点、锁互斥、连线变白；调用 SetSkillUpgrade 改 Player 技能
    private void Unlock()
    {
        isUnlocked = true;
        defaultIconColor = Color.white;
        SetIconColor(Color.white);
        skillTree.RemoveSkillPoints(skillData.cost);
        LockConflictNodes();
        connectHandler.UnlockConnectionImage(true);

        skillTree.skillManager.GetSkillByType(skillData.skillType).SetSkillUpgrade(skillData.upgradeData);
    }

    private bool CanBeUnlocked()
    {
        if (isUnlocked || isLocked) return false;

        if (skillTree.EnoughSkillPoints(skillData.cost) == false) return false;

        foreach(var node in neededNodes)
        {
            if (node.isUnlocked == false) return false;
        }

        foreach(var node in conflictNodes)
        {
            if (node.isUnlocked) return false;
        }

        return true;
    }

    private void LockConflictNodes()
    {
        foreach(var node in conflictNodes)
        {
            node.isLocked = true;
            node.LockChildNodes();
        }
    }

    //锁定当前节点以及所有子节点
    public void LockChildNodes()
    {
        isLocked = true;
        foreach(var node in connectHandler.GetChildNodes())
        {
            node.LockChildNodes();
        }
    }

    private void SetIconColor(Color color)
    {
        if (skillIcon == null) return;
        skillIcon.color = color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (CanBeUnlocked()) Unlock();
        else if (isLocked) ui.skillToolTip.LockedSkillEffect();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUnlocked == false && isLocked == false)
            SetIconColor(HoverIconColor);

        if (ui != null && ui.skillToolTip != null)
            ui.skillToolTip.ShowToolTip(true, rect, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ui != null && ui.skillToolTip != null)
            ui.skillToolTip.ShowToolTip(false, rect);

        if (isUnlocked == false && isLocked == false)
            SetIconColor(defaultIconColor);
    }

    private Color GetColorByHex(string hexNumber)
    {
        ColorUtility.TryParseHtmlString(hexNumber, out Color color);
        return color;
    }

    private void OnDisable()
    {
        if (isLocked) SetIconColor(GetColorByHex(lockedColorHex));
        if (isUnlocked) SetIconColor(Color.white);
    }

    private void OnValidate()
    {
        if (skillData == null) return;
        skillName = skillData.displayName;
        skillIcon.sprite = skillData.icon;
        skillCost = skillData.cost;
        gameObject.name = "UI_TreeNode - " + skillData.displayName;
    }
}
