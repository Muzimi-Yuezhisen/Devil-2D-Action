using UnityEngine;

public class Player_IdleState : Player_GroundState
{
    public Player_IdleState(Player player, StateMachine stateMachine, string stateName) : base(player,stateMachine, stateName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(0, rb.linearVelocity.y);
    }
    public override void Update()
    {
        base.Update();

        //×²Ç½¾ÍÍ£Ö¹ÒÆ¶¯
        if (player.moveInput.x == player.facingDir && player.wallDetected) return;

        if (player.moveInput.x != 0) stateMachine.ChangeState(player.moveState);

    }
}
