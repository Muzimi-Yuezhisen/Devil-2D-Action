using System.Collections;
using UnityEngine;

/// <summary>
/// 时间碎片技能：按升级分支放置、追踪、多重充能、传送或生命回溯。
/// </summary>
public class Skill_Shard : Skill_Base
{
    /// <summary>当前由本技能创建、用于传送/回溯的碎片。</summary>
    private SkillObject_Shard currentShard;
    /// <summary>玩家生命组件，用于生命回溯。</summary>
    private Entity_Health playerHealth;

    /// <summary>碎片预制体。</summary>
    [SerializeField] private GameObject shardPrefab;
    /// <summary>普通分支的爆炸延迟（秒）。</summary>
    [SerializeField] private float detinateTime = 2;

    [Header("追踪碎片升级")]
    /// <summary>碎片飞向敌人的速度。</summary>
    [SerializeField] private float shardSpeed = 7;

    [Header("多重碎片升级")]
    /// <summary>最大充能次数。</summary>
    [SerializeField] private int maxCharges = 3;
    /// <summary>当前剩余充能。</summary>
    [SerializeField] private int currentCharges;
    /// <summary>是否正在回复充能。</summary>
    [SerializeField] private bool isReCharging;

    [Header("传送碎片升级")]
    /// <summary>传送类碎片存在时间（秒）。</summary>
    [SerializeField] private float shardExistDuration = 10;

    [Header("生命回溯碎片升级")]
    /// <summary>放置碎片时记录的生命百分比。</summary>
    [SerializeField] private float savedHealthPercent;

    /// <summary>初始化充能与生命组件引用。</summary>
    protected override void Awake()
    {
        base.Awake();
        currentCharges = maxCharges;
        playerHealth = GetComponentInParent<Entity_Health>();
    }

    /// <summary>按当前升级分支尝试释放碎片技能。</summary>
    public override void TryUseSkill()
    {
        if (CanUseSkill() == false) return;

        if (Unlocked(SkillUpgradeType.Shard)) HandleShardRegular();

        if (Unlocked(SkillUpgradeType.Shard_MoveToEnemy)) HandleShardMoving();

        if (Unlocked(SkillUpgradeType.Shard_Multicast)) HandleShardMulticast();

        if (Unlocked(SkillUpgradeType.Shard_Teleport)) HandleShardTeleport();

        if (Unlocked(SkillUpgradeType.Shard_TeleportHpRewind)) HandleShardHealthRewind();
    }

    /// <summary>生命回溯：无碎片则放置并记血量；有碎片则换位并回血。</summary>
    private void HandleShardHealthRewind()
    {
        if (currentShard == null)
        {
            CreateShard();
            savedHealthPercent = playerHealth.GetHealthPercent();
        }
        else
        {
            SwapPlayerAndShard();
            playerHealth.SetHealthToPercent(savedHealthPercent);
            SetSkillOnCooldown();
        }
    }

    /// <summary>传送：无碎片则放置；有碎片则与玩家换位并进入冷却。</summary>
    private void HandleShardTeleport()
    {
        if (currentShard == null) CreateShard();
        else
        {
            SwapPlayerAndShard();
            SetSkillOnCooldown();
        }
    }

    /// <summary>玩家与碎片交换位置，碎片在原玩家位置爆炸。</summary>
    private void SwapPlayerAndShard()
    {
        Vector3 shardPosition = currentShard.transform.position;
        Vector3 playerPosition = player.transform.position;

        currentShard.transform.position = playerPosition;
        currentShard.Explode();
        player.TeleportPlayer(shardPosition);
    }

    /// <summary>多重充能：扣一次充能生成追踪碎片，并启动回复协程。</summary>
    private void HandleShardMulticast()
    {
        if (currentCharges <= 0) return;
        CreateShard();
        currentShard.MoveTowardsClosestTarget(shardSpeed);
        currentCharges--;

        if (isReCharging == false) StartCoroutine(ShardRechargeCo());
    }

    /// <summary>按冷却间隔逐个回复充能，直到满充能。</summary>
    private IEnumerator ShardRechargeCo()
    {
        isReCharging = true;

        while (currentCharges < maxCharges)
        {
            yield return new WaitForSeconds(cooldown);
            currentCharges++;
        }

        isReCharging = false;
    }

    /// <summary>追踪分支：生成碎片并飞向最近敌人，然后进入冷却。</summary>
    private void HandleShardMoving()
    {
        CreateShard();
        currentShard.MoveTowardsClosestTarget(shardSpeed);

        SetSkillOnCooldown();
    }

    /// <summary>基础分支：生成碎片并进入冷却。</summary>
    private void HandleShardRegular()
    {
        CreateShard();
        SetSkillOnCooldown();
    }

    /// <summary>生成由 Spell 管理的碎片；传送类会在爆炸时强制进入冷却。</summary>
    public void CreateShard()
    {
        float detonateTime = GetDetonateTime();

        GameObject shard = Instantiate(shardPrefab, transform.position, Quaternion.identity);
        currentShard = shard.GetComponent<SkillObject_Shard>();
        currentShard.SetupShard(this);

        if (Unlocked(SkillUpgradeType.Shard_Teleport) || Unlocked(SkillUpgradeType.Shard_TeleportHpRewind))
        {
            currentShard.OnExplode += ForceCooldown;
        }
    }

    /// <summary>生成不被 currentShard 追踪的碎片（供冲刺升级使用）。</summary>
    public void CreateRawShard()
    {
        bool canMove = Unlocked(SkillUpgradeType.Shard_MoveToEnemy) || Unlocked(SkillUpgradeType.Shard_Multicast);

        GameObject shard = Instantiate(shardPrefab, transform.position, Quaternion.identity);
        shard.GetComponent<SkillObject_Shard>().SetupShard(this, detinateTime, canMove, shardSpeed);
    }

    /// <summary>取得爆炸/存在时间：传送类用较长存在时间，否则用默认延时。</summary>
    public float GetDetonateTime()
    {
        if (Unlocked(SkillUpgradeType.Shard_Teleport) || Unlocked(SkillUpgradeType.Shard_TeleportHpRewind)) return shardExistDuration;
        return detinateTime;
    }

    /// <summary>碎片爆炸时若尚未冷却则进入冷却，并取消事件订阅。</summary>
    private void ForceCooldown()
    {
        if (OnCooldowm() == false)
        {
            SetSkillOnCooldown();
            currentShard.OnExplode -= ForceCooldown;
        }
    }
}
