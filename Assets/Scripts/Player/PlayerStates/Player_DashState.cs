using UnityEngine;

/// <summary>
/// 冲刺：关闭重力、沿方向高速移动、播残影与技能起止效果。
/// </summary>
public class Player_DashState : PlayerState
{
    /// <summary>冲刺前重力，退出时还原。</summary>
    private float originalGravityScale;
    /// <summary>冲刺方向（进入时锁定，防止中途转向）。</summary>
    private int dashDir;

    /// <summary>绑定玩家与状态机。</summary>
    public Player_DashState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    /// <summary>开启冲刺效果、残影，锁定方向并关闭重力。</summary>
    public override void Enter()
    {
        base.Enter();
        AudioManager.PlaySfxAt(AudioCue.Dash, player.transform.position);
        skillManager.dash.OnStartEffect();
        player.vfx.DoImageEchoEffect(player.dashDuration);

        dashDir = player.moveInput.x != 0 ? (int)player.moveInput.x : player.facingDir;
        stateTimer = player.dashDuration;
        originalGravityScale = rb.gravityScale;
        rb.gravityScale = 0;
    }

    /// <summary>维持冲刺速度；撞墙或时间到则退出。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;
        CancelDashIfNeeded();
        if (IsActiveState == false) return;
        player.SetVelocity(player.dashSpeed * dashDir, 0);

        if (stateTimer < 0)
        {
            if (player.groundDetected) stateMachine.ChangeState(player.idleState);
            else stateMachine.ChangeState(player.fallState);
        }
    }

    /// <summary>结束冲刺效果并恢复重力。</summary>
    public override void Exit()
    {
        base.Exit();
        skillManager.dash.OnEndEffect();
        player.SetVelocity(0, 0);
        rb.gravityScale = originalGravityScale;
    }

    /// <summary>冲刺撞墙时取消：地面回待机，否则滑墙。</summary>
    private void CancelDashIfNeeded()
    {
        if (player.wallDetected)
        {
            if (player.groundDetected) stateMachine.ChangeState(player.idleState);
            else stateMachine.ChangeState(player.wallSlideState);
        }
    }
}
