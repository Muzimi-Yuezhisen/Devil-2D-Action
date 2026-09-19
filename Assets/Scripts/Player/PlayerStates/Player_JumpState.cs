using UnityEngine;

/// <summary>
/// 跳跃上升：赋予跳跃初速度，速度转负后进入下落。
/// </summary>
public class Player_JumpState : Player_AiredState
{
    /// <summary>绑定玩家与状态机。</summary>
    public Player_JumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    /// <summary>进入时施加向上速度。</summary>
    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(rb.linearVelocity.x, player.jumpForce);
    }

    /// <summary>开始下落且未在跳攻时切入下落状态。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;
        if (rb.linearVelocity.y < 0 && stateMachine.currentState != player.jumpAttackState)
        {
            stateMachine.ChangeState(player.fallState);
        }
    }
}
