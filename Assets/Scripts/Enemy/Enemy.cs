using System.Collections;
using UnityEngine;

/// <summary>三种敌人种族；AI 状态机复用，外观、节奏与数值各自独立。</summary>
public enum EnemyArchetype
{
    BoneSoldier = 0,
    NightSorrow = 1,
    UnderworldBrute = 2
}

/// <summary>
/// 敌人基类：巡逻/战斗参数、玩家探测、减速与进入战斗。
/// </summary>
public class Enemy : Entity
{
    private const string ArchetypeStatSource = "EnemyArchetype";

    public Enemy_Health health;

    [Header("敌人类型")]
    [SerializeField] private EnemyArchetype archetype = EnemyArchetype.BoneSoldier;
    public EnemyArchetype Archetype => archetype;
    public string DisplayName => archetype switch
    {
        EnemyArchetype.NightSorrow => "夜哀",
        EnemyArchetype.UnderworldBrute => "冥界蛮兵",
        _ => "骸骨士兵"
    };
    public int SkillPointReward => archetype == EnemyArchetype.UnderworldBrute ? 2 : 1;

    /// <summary>待机状态。</summary>
    public Enemy_IdleState idleState;
    /// <summary>巡逻移动状态。</summary>
    public Enemy_MoveState moveState;
    /// <summary>攻击状态。</summary>
    public Enemy_AttackState attackState;
    /// <summary>战斗追击状态。</summary>
    public Enemy_BattleState battleState;
    /// <summary>死亡状态。</summary>
    public Enemy_DeadState deadState;
    /// <summary>被弹反眩晕状态。</summary>
    public Enemy_StunnedState stunnedState;

    [Header("战斗设置")]
    /// <summary>战斗中移动速度。</summary>
    public float battleMoveSpeed = 3;
    /// <summary>可发起攻击的距离。</summary>
    public float attackDistance = 2;
    /// <summary>丢失玩家后保持战斗的时长。</summary>
    public float battleTimeDuration = 5;
    /// <summary>过近时后撤的距离阈值。</summary>
    public float minRetreatDistance = 1;
    /// <summary>后撤冲量。</summary>
    public Vector2 retreatVelocity;

    [Header("眩晕状态")]
    /// <summary>眩晕持续时间。</summary>
    public float stunnedDuration = 1;
    /// <summary>被弹反时击飞速度。</summary>
    public Vector2 stunnedVelocity = new Vector2(7, 7);
    /// <summary>当前是否处于可弹反窗口。</summary>
    protected bool canBeStunned;

    [Header("移动设置")]
    /// <summary>巡逻移速。</summary>
    public float moveSpeed = 1.4f;
    /// <summary>待机停留时间。</summary>
    public float idleTime = 2;
    /// <summary>移动动画播放倍率。</summary>
    [Range(0,2)]
    public float moveAnimSpeedMultiplier = 1;

    [Header("玩家检测")]
    /// <summary>玩家所在层级。</summary>
    [SerializeField] private LayerMask whatIsPlayer;
    /// <summary>探测射线起点。</summary>
    [SerializeField] private Transform playerCheck;
    /// <summary>探测距离。</summary>
    [SerializeField] private float playerCheckDistance = 10;
    /// <summary>当前锁定的玩家 Transform。</summary>
    public Transform player { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        ApplyArchetypeBehaviour();
        stats?.InitializeFromDefaultSetup();
        health = GetComponent<Enemy_Health>();
    }

    /// <summary>按种族设置追击节奏，并装配各自的 CC0 动画覆盖控制器。</summary>
    private void ApplyArchetypeBehaviour()
    {
        switch (archetype)
        {
            case EnemyArchetype.NightSorrow:
                battleMoveSpeed = 4.4f;
                attackDistance = 1.7f;
                battleTimeDuration = 7f;
                minRetreatDistance = 0.65f;
                moveSpeed = 2.25f;
                idleTime = 0.8f;
                moveAnimSpeedMultiplier = 1.3f;
                playerCheckDistance = 8f;
                ApplyVisualIdentity("EnemyVisuals/NightSorrow", 1.38f, -0.58f, 1.18f,
                    new Vector2(0.62f, 1.45f), new Vector2(0, -0.27f));
                break;
            case EnemyArchetype.UnderworldBrute:
                battleMoveSpeed = 2.15f;
                attackDistance = 2.45f;
                battleTimeDuration = 8f;
                minRetreatDistance = 1.1f;
                moveSpeed = 0.9f;
                idleTime = 2.4f;
                moveAnimSpeedMultiplier = 0.82f;
                playerCheckDistance = 6f;
                ApplyVisualIdentity("EnemyVisuals/UnderworldBrute", 0.94f, -0.48f, 1.38f,
                    new Vector2(0.86f, 1.8f), new Vector2(0, -0.1f));
                break;
            default:
                battleMoveSpeed = 3f;
                attackDistance = 2f;
                battleTimeDuration = 5f;
                minRetreatDistance = 1f;
                moveSpeed = 1.4f;
                idleTime = 2f;
                moveAnimSpeedMultiplier = 1f;
                playerCheckDistance = 7f;
                ApplyVisualIdentity(null, 1f, 0f, 0.82f,
                    new Vector2(0.6f, 1.5f), new Vector2(0, -0.237f));
                break;
        }
    }

    /// <summary>由 Entity_Stats 在基础配置应用后叠加类型差异。</summary>
    public void ApplyArchetypeStats(Entity_Stats targetStats)
    {
        if (targetStats == null) return;

        foreach (StatType type in new[] { StatType.MaxHealth, StatType.Damage, StatType.Armor, StatType.AttackSpeed })
            targetStats.GetStatByType(type)?.RemoveModifier(ArchetypeStatSource);

        switch (archetype)
        {
            case EnemyArchetype.NightSorrow:
                AddStatModifier(targetStats, StatType.MaxHealth, -10f);
                AddStatModifier(targetStats, StatType.Damage, -3f);
                AddStatModifier(targetStats, StatType.Armor, -1f);
                AddStatModifier(targetStats, StatType.AttackSpeed, 0.35f);
                break;
            case EnemyArchetype.UnderworldBrute:
                AddStatModifier(targetStats, StatType.MaxHealth, 20f);
                AddStatModifier(targetStats, StatType.Damage, 2f);
                AddStatModifier(targetStats, StatType.Armor, 10f);
                AddStatModifier(targetStats, StatType.AttackSpeed, -0.12f);
                break;
        }
    }

    private static void AddStatModifier(Entity_Stats targetStats, StatType type, float value)
    {
        targetStats.GetStatByType(type)?.AddModifier(value, ArchetypeStatSource);
    }

    private void ApplyVisualIdentity(string controllerResource, float visualScale, float visualOffsetY,
        float healthBarHeight,
        Vector2 colliderSize, Vector2 colliderOffset)
    {
        SpriteRenderer body = GetComponentInChildren<SpriteRenderer>(true);
        if (body == null) return;
        // 种族身份不再靠染色，完整保留 SpriteRenderer.color 给受击/元素反馈。
        body.color = Color.white;
        body.transform.localScale = Vector3.one * visualScale;
        Vector3 visualPosition = body.transform.localPosition;
        visualPosition.y = visualOffsetY;
        body.transform.localPosition = visualPosition;

        Animator bodyAnimator = anim != null ? anim : GetComponentInChildren<Animator>(true);
        if (bodyAnimator != null && !string.IsNullOrEmpty(controllerResource))
        {
            AnimatorOverrideController controller =
                Resources.Load<AnimatorOverrideController>(controllerResource);
            if (controller != null) bodyAnimator.runtimeAnimatorController = controller;
        }

        CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            capsule.size = colliderSize;
            capsule.offset = colliderOffset;
        }

        foreach (Canvas healthCanvas in GetComponentsInChildren<Canvas>(true))
        {
            if (healthCanvas.renderMode != RenderMode.WorldSpace) continue;
            RectTransform rect = healthCanvas.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, healthBarHeight);
        }
    }

    /// <summary>编辑器关卡构建器设置类型；运行时重新套用完整配置。</summary>
    public void ConfigureArchetype(EnemyArchetype value)
    {
        archetype = value;
        ApplyArchetypeBehaviour();
        if (Application.isPlaying) stats?.InitializeFromDefaultSetup();
    }

    /// <summary>减速：临时降低巡逻/战斗移速与动画速度。</summary>
    protected override IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
    {
        float originalMoveSpeed = moveSpeed;
        float originalBattleSpeed = battleMoveSpeed;
        float originalAnimSpeed = anim.speed;
        float speedMultiplier = 1 - slowMultiplier;

        moveSpeed *= speedMultiplier;
        battleMoveSpeed *= speedMultiplier;
        anim.speed *= speedMultiplier;

        yield return new WaitForSeconds(duration);

        moveSpeed = originalMoveSpeed;
        battleMoveSpeed = originalBattleSpeed;
        anim.speed = originalAnimSpeed;
    }

    /// <summary>开关可弹反窗口。</summary>
    public void EnableCounterWindow(bool enable) => canBeStunned = enable;

    /// <summary>死亡时切入死亡状态。</summary>
    public override void EntityDeath()
    {
        base.EntityDeath();
        stateMachine.ChangeState(deadState);
    }

    /// <summary>玩家死亡后敌人回到待机。</summary>
    private void HandlePlayerDeath()
    {
        stateMachine.ChangeState(idleState);
    }

    /// <summary>受击或发现玩家时尝试进入战斗（已在战斗/攻击中则忽略）。</summary>
    public void TryEnterBattleState(Transform player)
    {
        if (stateMachine.currentState == battleState || stateMachine.currentState == attackState) return;
        this.player = player;
        stateMachine.ChangeState(battleState);
    }

    /// <summary>获取玩家引用；为空则用探测结果。</summary>
    public Transform GetPlayerReference()
    {
        if (player == null) player = PlayerDetected().transform;
        return player;
    }

    /// <summary>朝向方向射线探测玩家（需命中 Player 层）。</summary>
    public RaycastHit2D PlayerDetected()
    {
        RaycastHit2D hit =
            Physics2D.Raycast(playerCheck.position, Vector2.right * facingDir, playerCheckDistance, whatIsPlayer | whatIsGround);
        if (hit.collider == null || hit.collider.gameObject.layer != LayerMask.NameToLayer("Player")) return default;
        return hit;
    }

    /// <summary>绘制玩家探测与攻击距离辅助线。</summary>
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(playerCheck.position, new Vector3(playerCheck.position.x + (facingDir * playerCheckDistance), playerCheck.position.y));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(playerCheck.position, new Vector3(playerCheck.position.x + (facingDir * attackDistance), playerCheck.position.y));
    }

    /// <summary>订阅玩家死亡事件。</summary>
    private void OnEnable()
    {
        Player.OnPlayerDeath += HandlePlayerDeath;
    }

    /// <summary>取消订阅。</summary>
    private void OnDisable()
    {
        Player.OnPlayerDeath -= HandlePlayerDeath;
    }
}
