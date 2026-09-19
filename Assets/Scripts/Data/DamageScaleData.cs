using System;
using UnityEngine;

/// <summary>
/// 伤害缩放与元素效果倍率配置，用于技能/攻击的数据驱动调整。
/// </summary>
[Serializable]
public class DamageScaleData
{
    [Header("伤害")]
    /// <summary>物理伤害倍率。</summary>
    public float physical = 1;
    /// <summary>元素伤害倍率。</summary>
    public float elemental = 1;

    [Header("冰冻")]
    /// <summary>冰冻持续时间（秒）。</summary>
    public float chillDuration = 3;
    /// <summary>冰冻减速倍率。</summary>
    public float chillSlowMultiplier = 0.2f;

    [Header("燃烧")]
    /// <summary>燃烧持续时间（秒）。</summary>
    public float burnDuration = 3;
    /// <summary>燃烧伤害倍率。</summary>
    public float burnDamageScale = 1;

    [Header("电击")]
    /// <summary>电击持续时间（秒）。</summary>
    public float shockDuration = 3;
    /// <summary>电击伤害倍率。</summary>
    public float shockDamageScale = 1;
    /// <summary>电击充能量。</summary>
    public float shockCharge = 0.4f;
}
