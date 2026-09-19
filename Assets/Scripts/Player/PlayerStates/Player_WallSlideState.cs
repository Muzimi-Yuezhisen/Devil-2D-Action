using UnityEngine;

/// <summary>
/// 滑墙：减速下滑，可墙跳；离墙下落，落地待机。
/// </summary>
public class Player_WallSlideState : PlayerState
{
    /// <summary>绑定玩家与状态机。</summary>
    public Player_WallSlideState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    /// <summary>处理滑墙速度与状态切换。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;
        HandleWallSlide();

        if (input.Player.Jump.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.wallJumpState);
            return;
        }

        if (player.wallDetected == false)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        if (player.groundDetected)
        {
            stateMachine.ChangeState(player.idleState);
            if (player.moveInput.x != 0 && player.moveInput.x != player.facingDir) player.Flip();
        }
    }

    /// <summary>下键不减速；否则按倍率减缓竖直速度。</summary>
    private void HandleWallSlide()
    {
        if (player.moveInput.y < 0) player.SetVelocity(player.moveInput.x, rb.linearVelocity.y);
        else player.SetVelocity(player.moveInput.x, rb.linearVelocity.y * player.wallSlideSlowMultiplier);
    }
}
