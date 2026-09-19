using System;
using UnityEngine;

/// <summary>
/// 主属性组：力量、敏捷、智力、活力。
/// </summary>
[Serializable]
public class Stat_MajorGroup
{
    /// <summary>力量：加物理伤害与暴击倍率。</summary>
    public Stat strength;
    /// <summary>敏捷：加闪避与暴击率。</summary>
    public Stat agility;
    /// <summary>智力：加元素伤害与元素抗性。</summary>
    public Stat intelligence;
    /// <summary>活力：加最大生命与护甲。</summary>
    public Stat vitality;
}
