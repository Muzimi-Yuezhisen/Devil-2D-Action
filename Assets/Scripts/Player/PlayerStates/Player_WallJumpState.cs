using UnityEngine;

/// <summary>
/// 墙跳：离开墙壁施加冲量后立即转入下落。
/// </summary>
public class Player_WallJumpState : PlayerState
{
    /// <summary>绑定玩家与状态机。</summary>
    public Player_WallJumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    /// <summary>施加反向墙跳力并切入下落。</summary>
    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(player.wallJumpForce.x * -player.facingDir, player.wallJumpForce.y);
        stateMachine.ChangeState(player.fallState);
    }

    /// <summary>若再次贴墙则进入滑墙。</summary>
    public override void Update()
    {
        base.Update();
        if (player.wallDetected) stateMachine.ChangeState(player.wallSlideState);
    }
}
