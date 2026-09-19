using UnityEngine;

/// <summary>
/// 死亡：关闭输入与刚体模拟。
/// </summary>
public class Player_DeadState : PlayerState
{
    /// <summary>绑定玩家与状态机。</summary>
    public Player_DeadState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    /// <summary>进入时禁用操控与物理。</summary>
    public override void Enter()
    {
        base.Enter();
        input.Disable();
        rb.simulated = false;
    }
}
