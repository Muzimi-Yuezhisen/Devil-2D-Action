using UnityEngine;

public class Player_GroundState : PlayerState
{
    public Player_GroundState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();
        //解决角色走路时遇到坑，坠落后仍会向前滑行 && 防止下坡时触发
        if (rb.linearVelocity.y < 0 && !player.groundDetected) stateMachine.ChangeState(player.fallState);
        if (input.Player.Jump.WasPressedThisFrame()) stateMachine.ChangeState(player.jumpState);
        if (input.Player.Attack.WasPressedThisFrame()) stateMachine.ChangeState(player.basicAttackState);

        if (input.Player.CounterAttack.WasPressedThisFrame()) stateMachine.ChangeState(player.counterAttackState);
    }
}
