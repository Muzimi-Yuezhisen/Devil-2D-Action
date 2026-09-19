using UnityEngine;

/// <summary>
/// 默认属性配置资源，可一键应用到 Entity_Stats。
/// </summary>
[CreateAssetMenu(menuName = "RPG Setup/Default Stat Setup", fileName = "Default Stat Setup")]
public class Stat_SetupSO : ScriptableObject
{
    [Header("平衡目标（仅作设计说明）")]
    [TextArea]
    public string balanceNote;

    [Header("资源属性")]
    /// <summary>最大生命。</summary>
    public float maxHealth = 100;
    /// <summary>生命回复。</summary>
    public float healthRegen;

    [Header("进攻-物理伤害")]
    /// <summary>攻击速度。</summary>
    public float attackSpeed = 1;
    /// <summary>基础伤害。</summary>
    public float damage = 10;
    /// <summary>暴击率。</summary>
    public float critChance;
    /// <summary>暴击倍率。</summary>
    public float critPower = 150;
    /// <summary>护甲穿透。</summary>
    public float armorReduction;

    [Header("进攻-元素伤害")]
    /// <summary>火焰伤害。</summary>
    public float fireDamage;
    /// <summary>冰霜伤害。</summary>
    public float iceDamage;
    /// <summary>闪电伤害。</summary>
    public float lightningDamage;

    [Header("防御-物理")]
    /// <summary>护甲。</summary>
    public float armor;
    /// <summary>闪避。</summary>
    public float evasion;

    [Header("防御-元素")]
    /// <summary>火抗。</summary>
    public float fireResistance;
    /// <summary>冰抗。</summary>
    public float iceResistance;
    /// <summary>电抗。</summary>
    public float lightningResistance;

    [Header("主属性")]
    /// <summary>力量。</summary>
    public float strength;
    /// <summary>敏捷。</summary>
    public float agility;
    /// <summary>智力。</summary>
    public float intelligence;
    /// <summary>活力。</summary>
    public float vitality;
}
