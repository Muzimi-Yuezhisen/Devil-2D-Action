using UnityEngine;

/// <summary>
/// 待机：速度清零，有移动输入则切入奔跑。
/// </summary>
public class Player_IdleState : Player_GroundState
{
    /// <summary>绑定玩家与状态机。</summary>
    public Player_IdleState(Player player, StateMachine stateMachine, string stateName) : base(player, stateMachine, stateName)
    {
    }

    /// <summary>进入时停止水平移动。</summary>
    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(0, rb.linearVelocity.y);
    }

    /// <summary>有水平输入且未正面撞墙则进入移动。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;
        // 朝向墙壁时不切入移动，避免贴墙抖动
        if (player.moveInput.x == player.facingDir && player.wallDetected) return;
        if (player.moveInput.x != 0) stateMachine.ChangeState(player.moveState);
    }
}
