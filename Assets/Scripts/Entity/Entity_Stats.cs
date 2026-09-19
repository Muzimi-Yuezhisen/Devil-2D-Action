using UnityEngine;

/// <summary>
/// 实体属性中心：物理/元素伤害、护甲、闪避、生命，以及按类型取 Stat。
/// </summary>
public class Entity_Stats : MonoBehaviour
{
    /// <summary>默认属性配置资源，可一键应用到本组件。</summary>
    public Stat_SetupSO defaultStatSetup;

    /// <summary>资源类属性（生命、回复）。</summary>
    public Stat_ResourceGroup resources;
    /// <summary>进攻类属性。</summary>
    public Stat_offenseGroup offense;
    /// <summary>防御类属性。</summary>
    public Stat_DefenseGroup defense;
    /// <summary>主属性（力敏智体）。</summary>
    public Stat_MajorGroup major;

    private bool defaultSetupApplied;

    private void Awake() => InitializeFromDefaultSetup();

    /// <summary>让 ScriptableObject 成为唯一基础数值来源，并保证一次生命周期只应用一次。</summary>
    public void InitializeFromDefaultSetup()
    {
        if (defaultSetupApplied) return;
        ApplyDefaultStatSetup();
        GetComponent<Enemy>()?.ApplyArchetypeStats(this);
        defaultSetupApplied = true;
    }

    /// <summary>按伤害缩放数据生成一次攻击结算数据。</summary>
    public AttackData GetAttackData(DamageScaleData scaleData)
    {
        return new AttackData(this, scaleData);
    }

    /// <summary>
    /// 计算元素伤害：取最高元素为主，其余元素各贡献一半，并加智力加成。
    /// </summary>
    public float GetElementalDamage(out ElementType element, float scaleFactor = 1)
    {
        float fireDamage = offense.fireDamage.GetValue();
        float iceDamage = offense.iceDamage.GetValue();
        float lightningDamage = offense.lightningDamage.GetValue();
        float bonusElementalDamage = major.intelligence.GetValue();

        float highestDamage = fireDamage;
        element = ElementType.Fire;

        if (iceDamage > highestDamage)
        {
            highestDamage = iceDamage;
            element = ElementType.Ice;
        }
        if (lightningDamage > highestDamage)
        {
            highestDamage = lightningDamage;
            element = ElementType.Lightning;
        }

        if (highestDamage <= 0)
        {
            element = ElementType.None;
            return 0;
        }

        float bonusFire = (element == ElementType.Fire) ? 0 : fireDamage * 0.5f;
        float bonusIce = (element == ElementType.Ice) ? 0 : iceDamage * 0.5f;
        float bonusLightning = (element == ElementType.Lightning) ? 0 : lightningDamage * 0.5f;

        float weakerElementsDamage = bonusFire + bonusIce + bonusLightning;
        float finalDamage = highestDamage + bonusElementalDamage + weakerElementsDamage;

        return finalDamage * scaleFactor;
    }

    /// <summary>按元素取抗性（含智力加成），返回 0~0.75 的减免比例。</summary>
    public float GetElementalResistance(ElementType element)
    {
        float baseResistance = 0;
        float bonusResistance = major.intelligence.GetValue() * 0.5f;

        switch (element)
        {
            case ElementType.Fire:
                baseResistance = defense.fireRes.GetValue();
                break;
            case ElementType.Ice:
                baseResistance = defense.iceRes.GetValue();
                break;
            case ElementType.Lightning:
                baseResistance = defense.lightningRes.GetValue();
                break;
        }

        float resistance = baseResistance + bonusResistance;
        float resistanceCap = 75f;
        float finalResistance = Mathf.Clamp(resistance, 0, resistanceCap) / 100;

        return finalResistance;
    }

    /// <summary>
    /// 计算物理伤害与是否暴击；力量加伤与暴伤，敏捷加暴击率。
    /// </summary>
    public float GetPhysicalDamage(out bool isCrit, float scaleFactor = 1)
    {
        // 基础伤害 = 武器伤 + 力量
        float baseDamage = offense.damage.GetValue();
        float bonusDamage = major.strength.GetValue();
        float totalBaseDamage = baseDamage + bonusDamage;

        // 暴击率：基础 + 敏捷 * 0.3
        float baseCritChance = offense.critChance.GetValue();
        float bonusCritChance = major.agility.GetValue() * 0.3f;
        float critChance = baseCritChance + bonusCritChance;

        // 暴击倍率（百分比转倍数）
        float baseCritPower = offense.critPower.GetValue();
        float bonusCritPower = major.strength.GetValue() * 0.5f;
        float critPower = (baseCritPower + bonusCritPower) / 100;

        isCrit = Random.Range(0, 100) < critChance;
        float finalDamage = isCrit ? totalBaseDamage * critPower : totalBaseDamage;

        return finalDamage * scaleFactor;
    }

    /// <summary>
    /// 计算护甲减伤比例（考虑敌方穿甲），上限 85%。
    /// </summary>
    public float GetArmorMitigation(float armorReduction)
    {
        float baseArmor = defense.armor.GetValue();
        float bonusArmor = major.vitality.GetValue(); // 活力 1:1 加护甲
        float totalArmor = baseArmor + bonusArmor;

        float reductionMutliplier = Mathf.Clamp(1 - armorReduction, 0, 1);
        float effectiveArmor = totalArmor * reductionMutliplier;

        // 护甲公式：effective/(effective+100)，边际收益递减
        float mitigation = effectiveArmor / (effectiveArmor + 100);
        float mitigationCap = 0.85f;
        float finalMitigation = Mathf.Clamp(mitigation, 0, mitigationCap);

        return finalMitigation;
    }

    /// <summary>自身穿甲比例（0~1）。</summary>
    public float GetArmorReduction()
    {
        return Mathf.Clamp01(offense.armorReduction.GetValue() / 100);
    }

    /// <summary>闪避率（%），含敏捷加成，上限 85。</summary>
    public float GetEvasion()
    {
        float baseEvasion = defense.evasion.GetValue();
        float bonusEvasion = major.agility.GetValue() * 0.5f;

        float totalEvasion = baseEvasion + bonusEvasion;
        float evasionCap = 85;
        float finalEvasion = Mathf.Clamp(totalEvasion, 0, evasionCap);

        return finalEvasion;
    }

    /// <summary>最大生命 = 基础 + 活力 * 5。</summary>
    public float GetMaxHealth()
    {
        float baseMaxHealth = resources.maxHealth.GetValue();
        float bonusMaxHealth = major.vitality.GetValue() * 5;
        float finalMaxHealth = baseMaxHealth + bonusMaxHealth;
        return finalMaxHealth;
    }

    /// <summary>按枚举类型返回对应 Stat 引用。</summary>
    public Stat GetStatByType(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHealth:
                return resources.maxHealth;
            case StatType.HealthRegen:
                return resources.healthRegen;
            case StatType.Strength:
                return major.strength;
            case StatType.Agility:
                return major.agility;
            case StatType.Intelligence:
                return major.intelligence;
            case StatType.Vitality:
                return major.vitality;
            case StatType.AttackSpeed:
                return offense.attackSpeed;
            case StatType.Damage:
                return offense.damage;
            case StatType.CritChange:
                return offense.critChance;
            case StatType.CritPower:
                return offense.critPower;
            case StatType.ArmorReduction:
                return offense.armorReduction;
            case StatType.FireDamage:
                return offense.fireDamage;
            case StatType.IceDamage:
                return offense.iceDamage;
            case StatType.LightningDamage:
                return offense.lightningDamage;
            case StatType.Armor:
                return defense.armor;
            case StatType.Evasion:
                return defense.evasion;
            case StatType.IceResistance:
                return defense.iceRes;
            case StatType.FireResistance:
                return defense.fireRes;
            case StatType.LightningResistance:
                return defense.lightningRes;
            default:
                Debug.LogWarning($"StatType {type} not implement yet.");
                return null;
        }
    }

    /// <summary>编辑器菜单：把默认配置写入各组属性基础值。</summary>
    [ContextMenu("Update Default Stat Setup")]
    public void ApplyDefaultStatSetup()
    {
        if (defaultStatSetup == null)
        {
            Debug.Log("No default Stat Setup assigned");
            return;
        }

        resources.maxHealth.SetBaseValue(defaultStatSetup.maxHealth);
        resources.healthRegen.SetBaseValue(defaultStatSetup.healthRegen);

        major.strength.SetBaseValue(defaultStatSetup.strength);
        major.agility.SetBaseValue(defaultStatSetup.agility);
        major.intelligence.SetBaseValue(defaultStatSetup.intelligence);
        major.vitality.SetBaseValue(defaultStatSetup.vitality);

        offense.attackSpeed.SetBaseValue(defaultStatSetup.attackSpeed);
        offense.damage.SetBaseValue(defaultStatSetup.damage);
        offense.critChance.SetBaseValue(defaultStatSetup.critChance);
        offense.critPower.SetBaseValue(defaultStatSetup.critPower);
        offense.armorReduction.SetBaseValue(defaultStatSetup.armorReduction);

        offense.iceDamage.SetBaseValue(defaultStatSetup.iceDamage);
        offense.fireDamage.SetBaseValue(defaultStatSetup.fireDamage);
        offense.lightningDamage.SetBaseValue(defaultStatSetup.lightningDamage);

        defense.armor.SetBaseValue(defaultStatSetup.armor);
        defense.evasion.SetBaseValue(defaultStatSetup.evasion);

        defense.iceRes.SetBaseValue(defaultStatSetup.iceResistance);
        defense.fireRes.SetBaseValue(defaultStatSetup.fireResistance);
        defense.lightningRes.SetBaseValue(defaultStatSetup.lightningResistance);
    }
}
