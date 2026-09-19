using UnityEngine;

/// <summary>
/// 空中状态基类：空中水平移动与跳攻输入。
/// </summary>
public class Player_AiredState : PlayerState
{
    /// <summary>绑定玩家与状态机。</summary>
    public Player_AiredState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    /// <summary>空中按输入移动；按攻击进入跳攻。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;
        if (player.moveInput.x != 0)
        {
            player.SetVelocity(player.moveInput.x * player.moveSpeed * player.inAirMoveMultiplier, rb.linearVelocity.y);
        }

        if (input.Player.Attack.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.jumpAttackState);
        }
    }
}
