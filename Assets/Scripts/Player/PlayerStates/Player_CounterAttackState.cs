using UnityEngine;

/// <summary>
/// 弹反：进入时检测并执行弹反，随后硬直或结束回待机。
/// </summary>
public class Player_CounterAttackState : PlayerState
{
    /// <summary>玩家战斗组件。</summary>
    private Player_Combat combat;
    /// <summary>本次是否成功弹到目标。</summary>
    private bool counteredSomebody;

    /// <summary>缓存战斗组件。</summary>
    public Player_CounterAttackState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
        combat = player.GetComponent<Player_Combat>();
    }

    /// <summary>进入时立刻判定弹反并设置恢复计时。</summary>
    public override void Enter()
    {
        base.Enter();
        stateTimer = Mathf.Max(0.6f, combat != null ? combat.GetCounterRecoveryDuration() : 0);
        counteredSomebody = combat != null && combat.CounterAttackPerformed();
        anim.SetBool("counterAttackPerformed", counteredSomebody);
    }

    /// <summary>站桩；动画结束或未命中超时则回待机。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;
        player.SetVelocity(0, rb.linearVelocity.y);
        if (triggerCalled || stateTimer < 0) stateMachine.ChangeState(player.idleState);
    }
}
