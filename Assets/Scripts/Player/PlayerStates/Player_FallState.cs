using UnityEngine;

public class Player_FallState : Player_AiredState
{
    public Player_FallState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();
        if (player.groundDetected) stateMachine.ChangeState(player.idleState);
        //遇到墙进入滑行
        if (player.wallDetected) stateMachine.ChangeState(player.wallSlideState);
    }
}
