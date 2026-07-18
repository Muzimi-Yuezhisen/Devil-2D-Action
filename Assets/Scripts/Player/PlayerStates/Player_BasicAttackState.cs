using UnityEngine;

public class Player_BasicAttackState : PlayerState
{
    //玩家触发攻击后，有一小段时间调整位置
    private float attackVelocityTimer;
    //连击
    private const int FirstComboIndex = 1;
    private int comboIndex = 1;
    private int comboLimit = 3;
    //连击中断时间
    private float lastTimeAttacked;
    //攻击过程中转向
    private int attackDir;

    //预输入，玩家连续攻击就直接连击，不切换回idle状态
    private bool comboAttackQueued;
    
    public Player_BasicAttackState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
        if(comboLimit != player.attackVelocity.Length)
        {
            comboLimit = player.attackVelocity.Length;
            Debug.LogWarning("comboLimit has been adjusted, according to attack velocity array!");
        }
    }

    public override void Enter()
    {
        base.Enter();
        comboAttackQueued = false;
        ResetComboIndexIfNeeded();
        SyncAttackSpeed();

        //攻击中转向
        attackDir = player.moveInput.x != 0 ? (int)player.moveInput.x : player.facingDir;

        anim.SetInteger("basicAttackIndex", comboIndex);
        ApplyAttackVelocity();
    }


    public override void Update()
    {
        base.Update();
        HandleAttackVelocity();

        if (input.Player.Attack.WasPressedThisFrame()) QueueNextAttack();

        //动画播放结束，触发事件
        if (triggerCalled)
        {
            HandleStateExit();
        }
            
    }

    public override void Exit()
    {
        base.Exit();
        lastTimeAttacked = Time.time;
        comboIndex++;
    }

    private void HandleStateExit()
    {
        if (comboAttackQueued)
        {
            //人为地制造了一个“时间差”，强行给 Animator 留出一帧的时间去读取那个 false，然后再切入新状态把它变回 true
            anim.SetBool(animBoolName, false);
            player.EnterAttackStateWithDelay();
        }
        else stateMachine.ChangeState(player.idleState);
    }

    private void QueueNextAttack()
    {
        if (comboIndex < comboLimit) comboAttackQueued = true;
    }

    private void HandleAttackVelocity()
    {
        attackVelocityTimer -= Time.deltaTime;

        if(attackVelocityTimer < 0) player.SetVelocity(0, rb.linearVelocity.y);
    }

    private void ApplyAttackVelocity()
    {
        Vector2 attackVelocity = player.attackVelocity[comboIndex - 1];
        attackVelocityTimer = player.attackVelocityDuration;
        player.SetVelocity(attackVelocity.x * attackDir, attackVelocity.y);
    }

    private void ResetComboIndexIfNeeded()
    {
        if (Time.time > lastTimeAttacked + player.comboResetTime) comboIndex = FirstComboIndex;
        if (comboIndex > comboLimit) comboIndex = FirstComboIndex;
    }
}
