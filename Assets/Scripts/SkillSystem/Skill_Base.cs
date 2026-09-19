using UnityEngine;
/// <summary>
/// 技能基类：冷却、解锁分支、伤害缩放等通用逻辑。
/// </summary>
public class Skill_Base : MonoBehaviour
{
    /// <summary>玩家技能管理器。</summary>
    public Player_SkillManager skillManager { get; private set; }
    /// <summary>所属玩家。</summary>
    public Player player { get; private set; }
    /// <summary>当前技能的伤害与元素效果缩放（由解锁写入）。</summary>
    public DamageScaleData damageScaleData { get; private set; }
    [Header("通用设置")]
    /// <summary>技能大类（冲刺 / 时间碎片等）。</summary>
    [SerializeField] protected SkillType skillType;
    /// <summary>当前激活的升级分支；None 表示未解锁。</summary>
    [SerializeField] protected SkillUpgradeType upgradeType;
    /// <summary>冷却时间（秒）。</summary>
    [SerializeField] protected float cooldown;
    /// <summary>上次进入冷却时的时间戳。</summary>
    private float lastTimeUsed;
    /// <summary>初始化引用，并让技能开局即可使用。</summary>
    protected virtual void Awake()
    {
        skillManager = GetComponentInParent<Player_SkillManager>();
        player = GetComponentInParent<Player>();
        lastTimeUsed = lastTimeUsed - cooldown;
        damageScaleData = new DamageScaleData();
    }
    /// <summary>尝试释放技能（子类重写具体逻辑）。</summary>
    public virtual void TryUseSkill()
    {
    }
    /// <summary>
    /// 技能树解锁时写入升级数据。
    /// </summary>
    /// <param name="upgrade">升级类型、冷却与伤害缩放。</param>
    public void SetSkillUpgrade(UpgradeData upgrade)
    {
        upgradeType = upgrade.upgradeType;
        cooldown = upgrade.cooldown;
        damageScaleData = upgrade.damageScaleData;
        ResetCooldown();
    }
    /// <summary>
    /// 是否可以使用技能（已解锁且不在冷却中）。
    /// </summary>
    public virtual bool CanUseSkill()
    {
        if (upgradeType == SkillUpgradeType.None) return false;
        if (OnCooldowm())
        {
            Debug.Log("On Cooldown");
            return false;
        }
        return true;
    }
    /// <summary>当前升级分支是否等于指定类型。</summary>
    protected bool Unlocked(SkillUpgradeType upgradeToCheck) => upgradeType == upgradeToCheck;
    /// <summary>是否仍在冷却中。</summary>
    protected bool OnCooldowm() => Time.time < lastTimeUsed + cooldown;
    /// <summary>记录释放时间，开始冷却。</summary>
    public void SetSkillOnCooldown() => lastTimeUsed = Time.time;
    /// <summary>
    /// 减少剩余冷却（增大 lastTimeUsed，让冷却更早结束）。
    /// </summary>
    /// <param name="cooldownReducton">提前结束的秒数。</param>
    public void RestCooldownBy(float cooldownReducton) => lastTimeUsed += cooldownReducton;
    /// <summary>将冷却基准重置为当前时间。</summary>
    public void ResetCooldown() => lastTimeUsed = Time.time - cooldown;
}
