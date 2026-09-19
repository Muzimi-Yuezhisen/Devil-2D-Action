using UnityEngine;
/// <summary>
/// 敌人状态基类：绑定敌人组件，并同步移动/战斗动画速度。
/// </summary>
public class EnemyState : EntityState
{
    /// <summary>所属敌人。</summary>
    protected Enemy enemy;
    /// <summary>绑定敌人刚体、动画与属性。</summary>
    public EnemyState(Enemy enemy, StateMachine stateMachine, string animBoolName) : base(stateMachine, animBoolName)
    {
        this.enemy = enemy;
        rb = enemy.rb;
        anim = enemy.anim;
        stats = enemy.stats;
    }
    /// <summary>按战斗移速比例与水平速度更新动画参数，避免滑步感。</summary>
    public override void UpdateAnimationParameters()
    {
        base.UpdateAnimationParameters();
        float battleAnimSpeedMultiplier = enemy.battleMoveSpeed / enemy.moveSpeed;
        anim.SetFloat("battleAnimSpeedMultiplier", battleAnimSpeedMultiplier);
        anim.SetFloat("moveAnimSpeedMultiplier", enemy.moveAnimSpeedMultiplier);
        anim.SetFloat("xVelocity", rb.linearVelocity.x);
    }
}
