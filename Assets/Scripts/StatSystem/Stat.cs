using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单条属性：基础值 + 修正列表，惰性计算最终值。
/// </summary>
[Serializable]
public class Stat
{
    /// <summary>基础数值。</summary>
    [SerializeField] private float baseValue;
    /// <summary>来自 Buff 等的修正项。</summary>
    [SerializeField] private List<StatModifier> modifiers = new List<StatModifier>();
    /// <summary>是否需要重新求和。</summary>
    private bool needToCalculate = true;
    /// <summary>缓存的最终值。</summary>
    private float finalValue;

    /// <summary>一条属性修正（数值 + 来源标记）。</summary>
    [Serializable]
    public class StatModifier
    {
        /// <summary>修正数值。</summary>
        public float value;
        /// <summary>来源标识，用于成批移除。</summary>
        public string source;

        /// <summary>创建修正项。</summary>
        public StatModifier(float value, string source)
        {
            this.value = value;
            this.source = source;
        }
    }

    /// <summary>添加一条修正并标记需重算。</summary>
    public void AddModifier(float value, string source)
    {
        StatModifier modToAdd = new StatModifier(value, source);
        modifiers.Add(modToAdd);
        needToCalculate = true;
    }

    /// <summary>移除所有指定来源的修正。</summary>
    public void RemoveModifier(string source)
    {
        modifiers.RemoveAll(modifier => modifier.source == source);
        needToCalculate = true;
    }

    /// <summary>基础值加全部修正求和。</summary>
    private float GetFinalValue()
    {
        float finalValue = baseValue;
        foreach (var modifier in modifiers)
        {
            finalValue += modifier.value;
        }
        return finalValue;
    }

    /// <summary>获取最终属性值（有缓存，仅在变更后重算）。</summary>
    public float GetValue()
    {
        if (needToCalculate)
        {
            finalValue = GetFinalValue();
            needToCalculate = false;
        }
        return finalValue;
    }

    /// <summary>设置基础值。</summary>
    public void SetBaseValue(float value) => baseValue = value;
}
