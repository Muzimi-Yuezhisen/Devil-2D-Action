using UnityEngine;

public class Player_CounterAttackState : PlayerState
{
    private Player_Combat combat;
    private bool counteredSomebody;
    public Player_CounterAttackState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
        combat = player.GetComponent<Player_Combat>();
    }

    public override void Enter()
    {
        base.Enter();
        stateTimer = combat.GetCounterRecoveryDuration();
        //在刚进入状态时进行弹反的判定
        counteredSomebody = combat.CounterAttackPerformed();
        anim.SetBool("counterAttackPerformed", counteredSomebody);
    }

    public override void Update()
    {
        base.Update();
        //停止移动
        player.SetVelocity(0, rb.linearVelocity.y);

        if(triggerCalled) stateMachine.ChangeState(player.idleState);

        if (stateTimer < 0 && counteredSomebody == false) stateMachine.ChangeState(player.idleState);
    }
}
