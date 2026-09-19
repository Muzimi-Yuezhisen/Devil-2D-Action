using UnityEngine;

/// <summary>
/// 敌人巡逻：向前走，遇墙或无地面则回待机并翻面。
/// </summary>
public class Enemy_MoveState : Enemy_GroundedState
{
    /// <summary>绑定敌人与状态机。</summary>
    public Enemy_MoveState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    /// <summary>进入时若面前无路则翻面。</summary>
    public override void Enter()
    {
        base.Enter();
        if (enemy.groundDetected == false || enemy.wallDetected) enemy.Flip();
    }

    /// <summary>按朝向移动；无路可走回待机。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;
        enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, rb.linearVelocity.y);

        if (enemy.groundDetected == false || enemy.wallDetected)
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
