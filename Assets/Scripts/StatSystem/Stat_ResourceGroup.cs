using System;
using UnityEngine;

/// <summary>
/// 资源属性组：最大生命与生命回复。
/// </summary>
[Serializable]
public class Stat_ResourceGroup
{
    /// <summary>最大生命基础值。</summary>
    public Stat maxHealth;
    /// <summary>每秒生命回复。</summary>
    public Stat healthRegen;
}
