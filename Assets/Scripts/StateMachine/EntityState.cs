using UnityEngine;
/// <summary>
/// 实体状态基类：动画开关、计时器、动画事件与攻速同步。
/// </summary>
public abstract class EntityState
{
    /// <summary>所属状态机。</summary>
    protected StateMachine stateMachine;
    /// <summary>对应 Animator 布尔参数名。</summary>
    protected string animBoolName;
    /// <summary>刚体引用。</summary>
    protected Rigidbody2D rb;
    /// <summary>动画控制器。</summary>
    protected Animator anim;
    /// <summary>属性组件。</summary>
    protected Entity_Stats stats;
    /// <summary>状态计时器（冲刺时长、等待等）。</summary>
    protected float stateTimer;
    /// <summary>动画事件是否已触发（用于判定攻击段结束等）。</summary>
    protected bool triggerCalled;
    /// <summary>该实例是否仍是状态机的当前状态。</summary>
    protected bool IsActiveState => stateMachine.currentState == this;
    /// <summary>构造状态并绑定状态机与动画参数名。</summary>
    public EntityState(StateMachine stateMachine, string animBoolName)
    {
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }
    /// <summary>进入状态：打开对应动画，重置触发标记。</summary>
    public virtual void Enter()
    {
        anim.SetBool(animBoolName, true);
        triggerCalled = false;
    }
    /// <summary>每帧更新：递减计时器并刷新动画参数。</summary>
    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters();
    }
    /// <summary>离开状态：关闭对应动画。</summary>
    public virtual void Exit()
    {
        anim.SetBool(animBoolName, false);
    }
    /// <summary>供动画事件调用，标记本段动画触发点已到达。</summary>
    public void AnimationTrigger()
    {
        triggerCalled = true;
    }
    /// <summary>子类可重写以写入额外动画参数。</summary>
    public virtual void UpdateAnimationParameters()
    {
    }
    /// <summary>把进攻攻速写入 Animator 的 attackSpeedMultiplier。</summary>
    public void SyncAttackSpeed()
    {
        float attackSpeed = stats.offense.attackSpeed.GetValue();
        anim.SetFloat("attackSpeedMultiplier", attackSpeed);
    }
}
