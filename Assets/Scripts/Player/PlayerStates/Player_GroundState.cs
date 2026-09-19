using UnityEngine;

/// <summary>
/// 地面状态基类：处理下落、跳跃、普攻与弹反输入。
/// </summary>
public class Player_GroundState : PlayerState
{
    /// <summary>绑定玩家与状态机。</summary>
    public Player_GroundState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    /// <summary>离地下落、跳跃、攻击、弹反切换。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;

        // 角色已离地且在下落时切入下落（避免落地瞬间误判）
        if (rb.linearVelocity.y < 0 && !player.groundDetected)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }
        if (input.Player.Jump.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.jumpState);
            return;
        }
        if (input.Player.Attack.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.basicAttackState);
            return;
        }
        if (input.Player.CounterAttack.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.counterAttackState);
            return;
        }
        if (input.Player.RangeAttack.WasPressedThisFrame() && skillManager.swordThrow.CanUseSkill())
            stateMachine.ChangeState(player.swordThrowState);
    }
}
