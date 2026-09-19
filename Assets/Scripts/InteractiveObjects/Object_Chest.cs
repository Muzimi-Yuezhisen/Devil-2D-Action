using UnityEngine;

/// <summary>
/// 宝箱：受到伤害时播放开启动画并被击飞旋转。
/// </summary>
public class Object_Chest : MonoBehaviour, IDamagable
{
    /// <summary>子物体刚体。</summary>
    private Rigidbody2D rb => GetComponentInChildren<Rigidbody2D>();
    /// <summary>子物体动画。</summary>
    private Animator anim => GetComponentInChildren<Animator>();
    /// <summary>受击特效。</summary>
    private Entity_VFX fx => GetComponent<Entity_VFX>();

    [Header("开启设置")]
    /// <summary>被打开时的击退速度。</summary>
    [SerializeField] private Vector2 knockback;
    /// <summary>开箱后掉落的增益物。</summary>
    [SerializeField] private Object_Buff rewardBuffPrefab;
    /// <summary>开箱直接奖励的技能点。</summary>
    [SerializeField, Min(0)] private int skillPointReward = 2;
    /// <summary>防止一次攻击的多碰撞或后续攻击重复领奖。</summary>
    [SerializeField] private bool isOpened;

    public bool IsOpened => isOpened;
    public int SkillPointReward => skillPointReward;
    public Object_Buff RewardBuffPrefab => rewardBuffPrefab;

    /// <summary>受伤即开启：播特效、开动画并施加冲量。</summary>
    public bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer,
        AttackImpact impact = AttackImpact.Auto)
    {
        if (isOpened) return false;
        isOpened = true;

        fx?.PlayOnDamageVfx();
        if (anim != null) anim.SetBool("chestOpen", true);
        if (rb != null)
        {
            rb.linearVelocity = knockback;
            rb.angularVelocity = Random.Range(-200f, 200f);
        }

        UI_SkillTree skillTree = FindAnyObjectByType<UI_SkillTree>();
        skillTree?.AddSkillPoints(skillPointReward, "古老宝箱");

        if (rewardBuffPrefab != null)
            Instantiate(rewardBuffPrefab, transform.position + Vector3.up * 1.15f, Quaternion.identity);

        AudioManager.PlaySfxAt(AudioCue.UiClick, transform.position);
        return true;
    }

    /// <summary>关卡构建器配置宝箱奖励，避免奖励逻辑散落在场景 YAML 中。</summary>
    public void ConfigureReward(Object_Buff buffPrefab, int skillPoints)
    {
        rewardBuffPrefab = buffPrefab;
        skillPointReward = Mathf.Max(0, skillPoints);
        isOpened = false;
    }
}
