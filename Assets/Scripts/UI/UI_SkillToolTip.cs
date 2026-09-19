using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 技能提示：显示名称、描述与解锁条件（含颜色）。
/// </summary>
public class UI_SkillToolTip : UI_ToolTip
{
    /// <summary>主 UI。</summary>
    private UI ui;
    /// <summary>技能树（查点数）。</summary>
    private UI_SkillTree skillTree;

    /// <summary>技能名称文本。</summary>
    [SerializeField] private TextMeshProUGUI skillName;
    /// <summary>技能描述文本。</summary>
    [SerializeField] private TextMeshProUGUI skillDescription;
    /// <summary>解锁条件文本。</summary>
    [SerializeField] private TextMeshProUGUI skillRequirements;

    [Space]
    /// <summary>条件已满足时的颜色。</summary>
    [SerializeField] private string metConditionHex;
    /// <summary>条件未满足时的颜色。</summary>
    [SerializeField] private string notMetConditionHex;
    /// <summary>重要提示颜色（锁死说明等）。</summary>
    [SerializeField] private string importantInfoHex;
    /// <summary>示例色（编辑器用）。</summary>
    [SerializeField] private Color exampleColor;
    /// <summary>技能被互斥锁死后的说明文案。</summary>
    [SerializeField] private string lockedSkillText = "你已经选择了另一条路线，此技能已被锁定。";

    /// <summary>锁死闪烁协程。</summary>
    private Coroutine textEffectCo;

    /// <summary>缓存 UI 与技能树。</summary>
    protected override void Awake()
    {
        base.Awake();
        ui = GetComponentInParent<UI>();
        if (ui != null) skillTree = ui.GetComponentInChildren<UI_SkillTree>(true);
    }

    /// <summary>基类显示接口。</summary>
    public override void ShowToolTip(bool show, RectTransform targetRect)
    {
        base.ShowToolTip(show, targetRect);
    }

    /// <summary>按节点填充名称、描述与条件列表。</summary>
    public void ShowToolTip(bool show, RectTransform targetRect, UI_TreeNode node)
    {
        base.ShowToolTip(show, targetRect);

        if (show == false) return;

        skillName.text = node.skillData.displayName;
        skillDescription.text = node.skillData.description;

        string skillLockedText = $"<color={importantInfoHex}> {lockedSkillText}</color>";
        string requirements = node.isLocked ? skillLockedText : GetRequirements(node.skillData.cost, node.neededNodes, node.conflictNodes);

        skillRequirements.text = requirements;
    }

    /// <summary>锁死技能被点击时闪烁提示文字。</summary>
    public void LockedSkillEffect()
    {
        if (textEffectCo != null) StopCoroutine(textEffectCo);

        textEffectCo = StartCoroutine(TextBlinkEffectCo(skillRequirements, 0.15f, 3));
    }

    /// <summary>条件文字颜色闪烁。</summary>
    private IEnumerator TextBlinkEffectCo(TextMeshProUGUI text, float blinkInterval, int blinkCount)
    {
        for (int i = 0; i < blinkCount; i++)
        {
            text.text = GetColoredText(notMetConditionHex, lockedSkillText);
            yield return new WaitForSeconds(blinkInterval);
            text.text = GetColoredText(importantInfoHex, lockedSkillText);
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    /// <summary>拼接点耗、前置与互斥说明。</summary>
    private string GetRequirements(int skillCost, UI_TreeNode[] neededNodes, UI_TreeNode[] conflictNodes)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("解锁条件：");

        string costColor = skillTree.EnoughSkillPoints(skillCost) ? metConditionHex : notMetConditionHex;
        sb.AppendLine($"<color={costColor}> - 需要 {skillCost} 个技能点</color>");

        foreach (var node in neededNodes)
        {
            if (node == null) continue;

            string nodeColor = node.isUnlocked ? metConditionHex : notMetConditionHex;
            sb.AppendLine($"<color={nodeColor}> - {node.skillData.displayName}</color>");
        }

        if (conflictNodes.Length <= 0) return sb.ToString();

        sb.AppendLine();
        sb.AppendLine($"<color={importantInfoHex}>解锁后将锁定：</color>");

        foreach (var node in conflictNodes)
        {
            if (node == null) continue;

            sb.AppendLine($"<color={importantInfoHex}> - {node.skillData.displayName}</color>");
        }

        return sb.ToString();
    }
}
