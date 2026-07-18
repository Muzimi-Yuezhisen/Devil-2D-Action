using UnityEngine;

public class Player_DashState : PlayerState
{
    //冲刺时需要去除重力的影响，保存重力
    private float originalGravityScale;
    //防止冲刺时转向
    private int dashDir;
    public Player_DashState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        skillManager.dash.OnStartEffect();
        player.vfx.DoImageEchoEffect(player.dashDuration);
        //冲刺的方向是玩家键盘输入的方向
        dashDir = player.moveInput.x != 0 ? (int)player.moveInput.x : player.facingDir;
        stateTimer = player.dashDuration;
        originalGravityScale = rb.gravityScale;
        rb.gravityScale = 0;
    }

    public override void Update()
    {
        base.Update();
        CancelDashIfNeeded();
        player.SetVelocity(player.dashSpeed * dashDir, 0);

        if (stateTimer < 0)
        {
            if (player.groundDetected) stateMachine.ChangeState(player.idleState);
            else stateMachine.ChangeState(player.fallState);
        }
            
    }

    public override void Exit()
    {
        base.Exit();
        skillManager.dash.OnEndEffect();

        player.SetVelocity(0, 0);
        rb.gravityScale = originalGravityScale;
    }

    //冲刺撞墙就取消冲刺
    private void CancelDashIfNeeded()
    {
        if (player.wallDetected)
        {
            if (player.groundDetected) stateMachine.ChangeState(player.idleState);
            else stateMachine.ChangeState(player.wallSlideState);
        }
    }
}
