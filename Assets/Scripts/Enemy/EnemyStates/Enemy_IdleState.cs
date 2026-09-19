using UnityEngine;

/// <summary>
/// 敌人待机：计时结束后进入巡逻。
/// </summary>
public class Enemy_IdleState : Enemy_GroundedState
{
    /// <summary>绑定敌人与状态机。</summary>
    public Enemy_IdleState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    /// <summary>设置待机计时。</summary>
    public override void Enter()
    {
        base.Enter();
        stateTimer = enemy.idleTime;
    }

    /// <summary>计时结束切入移动。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;
        if (stateTimer < 0) stateMachine.ChangeState(enemy.moveState);
    }
}
