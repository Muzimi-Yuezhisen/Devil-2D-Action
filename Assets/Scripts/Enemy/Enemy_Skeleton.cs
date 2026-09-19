using UnityEngine;

/// <summary>
/// 骷髅敌人：创建状态机并实现可弹反接口。
/// </summary>
public class Enemy_Skeleton : Enemy, ICounterable
{
    /// <summary>当前是否可被弹反（弹反窗口开启时）。</summary>
    public bool CanBeCountered { get => canBeStunned; }

    /// <summary>创建各状态实例。</summary>
    protected override void Awake()
    {
        base.Awake();
        idleState = new Enemy_IdleState(this, stateMachine, "idle");
        moveState = new Enemy_MoveState(this, stateMachine, "move");
        attackState = new Enemy_AttackState(this, stateMachine, "attack");
        battleState = new Enemy_BattleState(this, stateMachine, "battle");
        deadState = new Enemy_DeadState(this, stateMachine, "idle");
        stunnedState = new Enemy_StunnedState(this, stateMachine, "stunned");
    }

    /// <summary>以待机作为初始状态。</summary>
    protected override void Start()
    {
        base.Start();
        stateMachine.Init(idleState);
    }

    /// <summary>被弹反时切入眩晕状态。</summary>
    public void HandleCounter()
    {
        if (CanBeCountered == false) return;
        stateMachine.ChangeState(stunnedState);
    }
}
