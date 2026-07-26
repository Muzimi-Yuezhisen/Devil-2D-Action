using UnityEngine;

public class Skill_Base : MonoBehaviour
{
    public Player_SkillManager skillManager {  get; private set; }
    public Player player {  get; private set; }

    public DamageScaleData damageScaleData {  get; private set; }

    [Header("General details")]
    [SerializeField] protected SkillType skillType;
    [SerializeField] protected SkillUpgradeType upgradeType;
    [SerializeField] protected float cooldown;
    private float lastTimeUsed;

    protected virtual void Awake()
    {
        skillManager = GetComponentInParent<Player_SkillManager>();
        player = GetComponentInParent<Player>();
        lastTimeUsed = lastTimeUsed - cooldown;
    }

    public virtual void TryUseSkill()
    {

    }

    public void SetSkillUpgrade(UpgradeData upgrade)
    {
        upgradeType = upgrade.upgradeType;
        cooldown = upgrade.cooldown;
        damageScaleData = upgrade.damageScaleData;
    }

    public bool CanUseSkill()
    {
        if (upgradeType == SkillUpgradeType.None) return false;

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
    protected bool OnCooldowm() => Time.time < lastTimeUsed + cooldown;

    /// <summary>
    /// 记录上次释放时机
    /// </summary>
    public void SetSkillOnCooldown() => lastTimeUsed = Time.time;

    public void RestCooldownBy(float cooldownReducton) => lastTimeUsed += cooldownReducton;
    public void RestCooldown() => lastTimeUsed = Time.time;

}
