using UnityEngine;

//集合idle和movement的地面大状态
public class Enemy_GroundedState : EnemyState
{
    public Enemy_GroundedState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        if (enemy.PlayerDetected() == true) stateMachine.ChangeState(enemy.battleState);
    }
}
