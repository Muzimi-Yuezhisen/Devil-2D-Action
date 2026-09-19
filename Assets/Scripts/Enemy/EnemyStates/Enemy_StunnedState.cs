using UnityEngine;

/// <summary>
/// 敌人眩晕：关闭弹反窗口，击飞一段时间后回待机。
/// </summary>
public class Enemy_StunnedState : EnemyState
{
    /// <summary>敌人特效（关闭警示）。</summary>
    private Enemy_VFX vfx;

    /// <summary>缓存特效组件。</summary>
    public Enemy_StunnedState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
        vfx = enemy.GetComponent<Enemy_VFX>();
    }

    /// <summary>关闭弹反、施加眩晕击飞。</summary>
    public override void Enter()
    {
        base.Enter();
        vfx?.EnableAttackAlert(false);
        enemy.EnableCounterWindow(false);
        stateTimer = enemy.stunnedDuration;
        rb.linearVelocity = new Vector2(enemy.stunnedVelocity.x * -enemy.facingDir, enemy.stunnedVelocity.y);
    }

    /// <summary>眩晕结束后回待机。</summary>
    public override void Update()
    {
        base.Update();

        if (stateTimer < 0) stateMachine.ChangeState(enemy.idleState);
    }
}
