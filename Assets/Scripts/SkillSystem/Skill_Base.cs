using UnityEngine;

public class Skill_Base : MonoBehaviour
{
    [Header("General details")]
    [SerializeField] protected SkillType skillType;
    [SerializeField] protected SkillUpgradeType upgradeType;
    [SerializeField] private float cooldown;
    private float lastTimeUsed;

    protected virtual void Awake()
    {
        lastTimeUsed = lastTimeUsed - cooldown;
    }

    public void SetSkillUpgrade(UpgradeData upgrade)
    {
        upgradeType = upgrade.upgradeType;
        cooldown = upgrade.cooldown;
    }

    public bool CanUseSkill()
    {
        if (OnCooldowm())
        {
            Debug.Log("On Cooldown");
            return false;
        }
        return true;
    }

    protected bool Unlocked(SkillUpgradeType upgradeToCheck) => upgradeType == upgradeToCheck;

    /// <summary>
    /// 检查是否冷却
    /// </summary>
    /// <returns></returns>
    private bool OnCooldowm() => Time.time < lastTimeUsed + cooldown;

    /// <summary>
    /// 记录上次释放时机
    /// </summary>
    public void SetSkillOnCooldown() => lastTimeUsed = Time.time;

    public void RestCooldownBy(float cooldownReducton) => lastTimeUsed += cooldownReducton;
    public void RestCooldown() => lastTimeUsed = Time.time;

}
