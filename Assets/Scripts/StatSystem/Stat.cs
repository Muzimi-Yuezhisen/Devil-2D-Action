using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Stat
{
    [SerializeField] private float baseValue;
    [SerializeField] private List<StatModifier> modifiers = new List<StatModifier>();

    private bool needToCalculate = true;
    private float finalValue;
    [Serializable]
    public class StatModifier
    {
        public float value;
        public string source;

        public StatModifier(float value,string source)
        {
            this.value = value;
            this.source = source;
        }
    }

    public void AddModifier(float value,string source)
    {
        StatModifier modToAdd = new StatModifier(value, source);
        modifiers.Add(modToAdd);
        needToCalculate = true;
    }

    //移除所有同 source 的条目
    public void RemoveModifier(string source)
    {
        modifiers.RemoveAll(modifier => modifier.source == source);
        needToCalculate = true;
    }

    private float GetFinalValue()
    {
        float finalValue = baseValue;

        foreach(var modifier in modifiers)
        {
            finalValue += modifier.value;
        }
        return finalValue;
    }

    public float GetValue()
    {
        //优化性能，不发生属性改变时就不计算
        if (needToCalculate)
        {
            finalValue = GetFinalValue();
            needToCalculate = false;
        }
        return finalValue;
    }

    public void SetBaseValue(float value) => baseValue = value;
}
