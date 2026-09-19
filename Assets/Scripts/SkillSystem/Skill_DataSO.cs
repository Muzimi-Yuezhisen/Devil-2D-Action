using System;
using UnityEngine;

/// <summary>
/// 技能树节点用的技能配置资源。
/// </summary>
[CreateAssetMenu(menuName = "RPG Setup/Skill Data", fileName = "Skill data - ")]
public class Skill_DataSO : ScriptableObject
{
    [Header("技能描述")]
    /// <summary>显示名称。</summary>
    public string displayName;
    /// <summary>技能说明文本。</summary>
    [TextArea]
    public string description;
    /// <summary>技能图标。</summary>
    public Sprite icon;

    [Header("解锁与升级")]
    /// <summary>解锁消耗的技能点。</summary>
    public int cost;
    /// <summary>是否开局自动解锁。</summary>
    public bool unlockedByDefault;
    /// <summary>对应玩家身上的哪类技能组件。</summary>
    public SkillType skillType;
    /// <summary>解锁后写入运行时的升级包。</summary>
    public UpgradeData upgradeData;
}

/// <summary>
/// 解锁时传给技能组件的升级数据。
/// </summary>
[Serializable]
public class UpgradeData
{
    /// <summary>升级分支类型。</summary>
    public SkillUpgradeType upgradeType;
    /// <summary>解锁后的冷却时间。</summary>
    public float cooldown;
    /// <summary>伤害与元素效果缩放。</summary>
    public DamageScaleData damageScaleData;
}
