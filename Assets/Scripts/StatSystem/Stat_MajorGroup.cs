using System;
using UnityEngine;

[Serializable]
public class Stat_MajorGroup
{
    public Stat strength; //力量 1 -> 1 Physical Damage + 0.5% Crit Power
    public Stat agility; // 敏捷 1 -> 0.5% Evasion + 0.3% Crit Chance
    public Stat intelligence; //智力 1 -> 1 Magical Damage + 0.5% Elemental resistance
    public Stat vitality; //体力 1 -> 5 Max Health + 1 Armor
}
