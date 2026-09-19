using UnityEngine;
/// <summary>
/// 玩家状态基类：共享输入与技能管理器，并统一处理冲刺打断。
/// </summary>
public abstract class PlayerState : EntityState
{
    /// <summary>玩家实体。</summary>
    protected Player player;
    /// <summary>玩家输入。</summary>
    protected PlayerInputSet input;
    /// <summary>技能管理器。</summary>
    protected Player_SkillManager skillManager;
    /// <summary>绑定玩家组件引用。</summary>
    public PlayerState(Player player, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.player = player;
        anim = player.anim;
        rb = player.rb;
        input = player.input;
        stats = player.stats;
        skillManager = player.skillManager;
    }
    /// <summary>更新逻辑，并在可冲刺时响应冲刺键。</summary>
    public override void Update()
    {
        base.Update();
        if (input.Player.Dash.WasPerformedThisFrame() && CanDash())
        {
            skillManager.dash.SetSkillOnCooldown();
            stateMachine.ChangeState(player.dashState);
        }
    }
    /// <summary>同步竖直速度到动画。</summary>
    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }
    /// <summary>是否允许冲刺：技能可用、未贴墙、且当前不在冲刺状态。</summary>
    private bool CanDash()
    {
        if (skillManager.dash.CanUseSkill() == false) return false;
        if (player.wallDetected) return false;
        if (stateMachine.currentState == player.dashState) return false;
        return true;
    }
}
