using UnityEngine;

/// <summary>
/// 下落：落地回待机，撞墙进滑墙。
/// </summary>
public class Player_FallState : Player_AiredState
{
    /// <summary>绑定玩家与状态机。</summary>
    public Player_FallState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    /// <summary>落地或贴墙时切换状态。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;
        if (player.groundDetected)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }
        if (player.wallDetected) stateMachine.ChangeState(player.wallSlideState);
    }
}
