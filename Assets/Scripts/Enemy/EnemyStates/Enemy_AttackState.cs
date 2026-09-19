using UnityEngine;

/// <summary>
/// 敌人攻击：同步攻速，动画结束后回战斗。
/// </summary>
public class Enemy_AttackState : EnemyState
{
    /// <summary>绑定敌人与状态机。</summary>
    public Enemy_AttackState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    /// <summary>进入时同步攻击速度到动画。</summary>
    public override void Enter()
    {
        base.Enter();
        SyncAttackSpeed();
        enemy.SetVelocity(0, rb.linearVelocity.y);
        float attackSpeed = Mathf.Max(0.01f, stats.offense.attackSpeed.GetValue());
        stateTimer = 1.5f / attackSpeed;
    }

    /// <summary>攻击动画触发结束后回到战斗追击。</summary>
    public override void Update()
    {
        base.Update();
        if (triggerCalled || stateTimer < 0) stateMachine.ChangeState(enemy.battleState);
    }

    /// <summary>无论如何离开攻击，都关闭弹反窗口和攻击提示。</summary>
    public override void Exit()
    {
        base.Exit();
        enemy.EnableCounterWindow(false);
        enemy.GetComponent<Enemy_VFX>()?.EnableAttackAlert(false);
    }
}
