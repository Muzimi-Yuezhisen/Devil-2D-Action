using UnityEngine;

/// <summary>
/// 敌人地面大状态：发现玩家则进入战斗。
/// </summary>
public class Enemy_GroundedState : EnemyState
{
    /// <summary>绑定敌人与状态机。</summary>
    public Enemy_GroundedState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    /// <summary>探测到玩家时切入战斗。</summary>
    public override void Update()
    {
        base.Update();
        if (enemy.PlayerDetected() == true) stateMachine.ChangeState(enemy.battleState);
    }
}
