using UnityEngine;

/// <summary>
/// 空中攻击：进入时冲刺位移，落地播触发并回待机。
/// </summary>
public class Player_JumpAttackState : PlayerState
{
    /// <summary>是否已经触地（用于只触发一次落地动画）。</summary>
    private bool touchedGround;

    /// <summary>绑定玩家与状态机。</summary>
    public Player_JumpAttackState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    /// <summary>施加跳攻速度。</summary>
    public override void Enter()
    {
        base.Enter();
        touchedGround = false;
        player.SetVelocity(player.jumpAttackVelocity.x * player.facingDir, player.jumpAttackVelocity.y);
    }

    /// <summary>落地触发动画；动画结束后回待机。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;

        if (player.groundDetected && touchedGround == false)
        {
            touchedGround = true;
            stateTimer = 0.75f;
            anim.SetTrigger("jumpAttackTrigger");
            player.SetVelocity(0, rb.linearVelocity.y);
        }

        if ((triggerCalled || stateTimer < 0) && player.groundDetected) stateMachine.ChangeState(player.idleState);
    }
}
