using UnityEngine;

/// <summary>
/// 敌人战斗：追击玩家、过近后撤、进入攻击距离则攻击。
/// </summary>
public class Enemy_BattleState : EnemyState
{
    /// <summary>锁定的玩家。</summary>
    private Transform player;
    /// <summary>上次仍处于战斗（探测到玩家）的时间。</summary>
    private float lastTimeWasInBattle;

    /// <summary>绑定敌人与状态机。</summary>
    public Enemy_BattleState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    /// <summary>刷新计时；过近则后撤并对准玩家。</summary>
    public override void Enter()
    {
        base.Enter();
        UpdateBattleTimer();
        player ??= enemy.GetPlayerReference();

        if (ShouldRetreat())
        {
            rb.linearVelocity = new Vector2(enemy.retreatVelocity.x * -DirectionToPlayer(), enemy.retreatVelocity.y);
            enemy.HandleFlip(DirectionToPlayer());
        }
    }

    /// <summary>追击/攻击/超时回待机。</summary>
    public override void Update()
    {
        base.Update();

        if (enemy.PlayerDetected() == true) UpdateBattleTimer();

        if (player == null || BattleTimeIsOver())
        {
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        if (WithinAttackRange() && enemy.PlayerDetected()) stateMachine.ChangeState(enemy.attackState);
        else enemy.SetVelocity(enemy.battleMoveSpeed * DirectionToPlayer(), rb.linearVelocity.y);
    }

    /// <summary>刷新战斗计时。</summary>
    private void UpdateBattleTimer() => lastTimeWasInBattle = Time.time;

    /// <summary>是否超过战斗持续时间。</summary>
    private bool BattleTimeIsOver() => Time.time > lastTimeWasInBattle + enemy.battleTimeDuration;

    /// <summary>是否进入攻击距离。</summary>
    private bool WithinAttackRange() => DistanceToPlayer() < enemy.attackDistance;

    /// <summary>是否过近需要后撤。</summary>
    private bool ShouldRetreat() => DistanceToPlayer() < enemy.minRetreatDistance;

    /// <summary>与玩家的水平距离。</summary>
    private float DistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Mathf.Abs(player.position.x - enemy.transform.position.x);
    }

    /// <summary>指向玩家的水平方向（1/-1）。</summary>
    private int DirectionToPlayer()
    {
        if (player == null) return 0;
        return player.position.x > enemy.transform.position.x ? 1 : -1;
    }
}
