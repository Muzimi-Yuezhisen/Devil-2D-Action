using System;
using UnityEngine;

[Serializable]
public class Stat_offenseGroup
{
    public Stat attackSpeed;//π•ÀŸ

    //Physical damage
    public Stat damage;
    public Stat critPower;
    public Stat critChance;
    public Stat armorReduction; //ŒÔ¿Ì¥©Õ∏

    //Elemental damage
    public Stat fireDamage;
    public Stat iceDamage;
    public Stat lightningDamage;
}
