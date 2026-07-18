using UnityEngine;

public abstract class EntityState
{
    protected StateMachine stateMachine;
    protected string animBoolName;
    protected Rigidbody2D rb;
    protected Animator anim;
    protected Entity_Stats stats;

    //冲刺的计时器
    protected float stateTimer;
    //攻击的记录
    protected bool triggerCalled;

    public EntityState(StateMachine stateMachine,string animBoolName)
    {
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        anim.SetBool(animBoolName, true);
        triggerCalled = false;
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters();
    }
    public virtual void Exit()
    {
        anim.SetBool(animBoolName, false);
    }

    //提供动画结束调用
    public void AnimationTrigger()
    {
        triggerCalled = true;
    }

    //设置额外参数
    public virtual void UpdateAnimationParameters()
    {

    }

    public void SyncAttackSpeed()
    {
        float attackSpeed = stats.offense.attackSpeed.GetValue();
        anim.SetFloat("attackSpeedMultiplier", attackSpeed);
    }
}
