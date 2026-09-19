using System;
using UnityEngine;
/// <summary>
/// 时间碎片生成物：计时或碰到敌人后爆炸，可选追踪最近敌人。
/// </summary>
public class SkillObject_Shard : SkillObject_Base
{
    /// <summary>爆炸时通知（例如强制技能进入冷却）。</summary>
    public event Action OnExplode;
    /// <summary>创建此碎片的技能管理逻辑。</summary>
    private Skill_Shard shardManager;
    /// <summary>爆炸特效预制体。</summary>
    [SerializeField] private GameObject vfxPrefab;
    /// <summary>追踪目标。</summary>
    private Transform target;
    /// <summary>追踪移动速度。</summary>
    private float speed;
    private bool hasExploded;
    /// <summary>若有追踪目标则每帧靠近。</summary>
    private void Update()
    {
        if (target == null) return;
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }
    /// <summary>开始飞向最近的敌人。</summary>
    /// <param name="speed">移动速度。</param>
    public void MoveTowardsClosestTarget(float speed)
    {
        target = FindClosestTarget();
        this.speed = speed;
    }
    /// <summary>
    /// 由 Spell 创建的碎片初始化：注入属性与缩放，并按时爆炸。
    /// </summary>
    public void SetupShard(Skill_Shard shardManager)
    {
        this.shardManager = shardManager;
        playerStats = shardManager.player.stats;
        damageDealer = shardManager.player.transform;
        damageScaleData = shardManager.damageScaleData;
        float detonationTime = shardManager.GetDetonateTime();
        Invoke(nameof(Explode), detonationTime);
    }
    /// <summary>
    /// 由冲刺等外部创建的碎片初始化，可指定延时与是否追踪。
    /// </summary>
    public void SetupShard(Skill_Shard shardManager, float detonationTime, bool canMove, float shardSpeed)
    {
        this.shardManager = shardManager;
        playerStats = shardManager.player.stats;
        damageDealer = shardManager.player.transform;
        damageScaleData = shardManager.damageScaleData;
        Invoke(nameof(Explode), detonationTime);
        if (canMove) MoveTowardsClosestTarget(shardSpeed);
    }
    /// <summary>范围伤害、播放特效、触发爆炸事件并销毁自身。</summary>
    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        CancelInvoke(nameof(Explode));

        AudioManager.PlaySfxAt(AudioCue.ShardExplosion, transform.position);
        DamageEnemiesInRadius(transform, checkRadius);
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity);
            SpriteRenderer vfxRenderer = vfx.GetComponentInChildren<SpriteRenderer>();
            if (vfxRenderer != null) vfxRenderer.color = shardManager.player.vfx.GetElementColor(usedElement);
        }
        OnExplode?.Invoke();
        Destroy(gameObject);
    }
    /// <summary>碰到敌人时立即爆炸。</summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Enemy>() == null) return;
        Explode();
    }
}
