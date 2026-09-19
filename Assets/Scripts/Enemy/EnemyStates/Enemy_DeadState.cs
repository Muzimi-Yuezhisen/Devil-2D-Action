using UnityEngine;

/// <summary>
/// 敌人死亡：关闭碰撞与动画，上抛后关闭状态机。
/// </summary>
public class Enemy_DeadState : EnemyState
{
    /// <summary>绑定敌人与状态机。</summary>
    public Enemy_DeadState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(enemy, stateMachine, animBoolName)
    {
    }

    /// <summary>播放死亡物理表现并锁定状态机。</summary>
    public override void Enter()
    {
        anim.enabled = false;
        foreach (Collider2D collider in enemy.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        enemy.EnableCounterWindow(false);
        enemy.GetComponent<Enemy_VFX>()?.EnableAttackAlert(false);
        rb.gravityScale = 12;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 15);

        stateMachine.SwitchOffStateMachine();
    }
}
