using UnityEngine;

/// <summary>
/// 地面移动：按输入设速度，松开或撞墙回待机。
/// </summary>
public class Player_MoveState : Player_GroundState
{
    /// <summary>绑定玩家与状态机。</summary>
    public Player_MoveState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
    }

    /// <summary>更新移动速度与状态切换。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;

        if (player.moveInput.x == 0 || player.wallDetected)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        player.SetVelocity(player.moveInput.x * player.moveSpeed, rb.linearVelocity.y);
    }
}
