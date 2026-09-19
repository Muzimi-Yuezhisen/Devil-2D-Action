using UnityEngine;

/// <summary>
/// 地面普攻连招：位移、连段索引、预输入下一段。
/// </summary>
public class Player_BasicAttackState : PlayerState
{
    /// <summary>本段攻击位移剩余时间。</summary>
    private float attackVelocityTimer;
    /// <summary>连招起始段索引。</summary>
    private const int FirstComboIndex = 1;
    /// <summary>当前连招段（从 1 开始）。</summary>
    private int comboIndex = 1;
    /// <summary>最大连招段数。</summary>
    private int comboLimit = 3;
    /// <summary>上次攻击结束时间，用于超时重置连招。</summary>
    private float lastTimeAttacked;
    /// <summary>本段攻击朝向。</summary>
    private int attackDir;
    /// <summary>是否已预输入下一段攻击。</summary>
    private bool comboAttackQueued;
    public int CurrentComboIndex => comboIndex;
    public int ComboLimit => comboLimit;

    /// <summary>按攻击位移数组长度校准连招上限。</summary>
    public Player_BasicAttackState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
        int configuredComboCount = player.attackVelocity?.Length ?? 0;
        if (configuredComboCount > 0 && comboLimit != configuredComboCount)
        {
            comboLimit = configuredComboCount;
            Debug.LogWarning("comboLimit has been adjusted, according to attack velocity array!");
        }
    }

    /// <summary>进入：重置预输入、同步攻速、设段索引并施加位移。</summary>
    public override void Enter()
    {
        base.Enter();
        comboAttackQueued = false;
        ResetComboIndexIfNeeded();
        SyncAttackSpeed();
        float attackSpeed = Mathf.Max(0.01f, stats.offense.attackSpeed.GetValue());
        stateTimer = 1.25f / attackSpeed;

        attackDir = player.moveInput.x != 0 ? (int)player.moveInput.x : player.facingDir;
        anim.SetInteger("basicAttackIndex", comboIndex);
        ApplyAttackVelocity();
    }

    /// <summary>维持攻击位移；接收连招预输入；动画结束时退出。</summary>
    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;
        HandleAttackVelocity();
        if (input.Player.Attack.WasPressedThisFrame()) QueueNextAttack();
        if (triggerCalled || stateTimer < 0)
        {
            HandleStateExit();
        }
    }

    /// <summary>离开时记录时间并推进连招索引。</summary>
    public override void Exit()
    {
        base.Exit();
        lastTimeAttacked = Time.time;
        comboIndex++;
    }

    /// <summary>有预输入则延迟再进攻击态，否则回待机。</summary>
    private void HandleStateExit()
    {
        if (comboAttackQueued)
        {
            // 先关掉布尔再延迟进入，避免 Animator 同帧读不到切换
            anim.SetBool(animBoolName, false);
            player.EnterAttackStateWithDelay();
        }
        else stateMachine.ChangeState(player.idleState);
    }

    /// <summary>未满连招上限时标记预输入。</summary>
    private void QueueNextAttack()
    {
        if (comboIndex < comboLimit) comboAttackQueued = true;
    }

    /// <summary>攻击位移结束后水平速度归零。</summary>
    private void HandleAttackVelocity()
    {
        attackVelocityTimer -= Time.deltaTime;
        if (attackVelocityTimer < 0) player.SetVelocity(0, rb.linearVelocity.y);
    }

    /// <summary>按当前段施加攻击位移。</summary>
    private void ApplyAttackVelocity()
    {
        if (player.attackVelocity == null || player.attackVelocity.Length == 0)
        {
            player.SetVelocity(0, rb.linearVelocity.y);
            return;
        }
        Vector2 attackVelocity = player.attackVelocity[comboIndex - 1];
        attackVelocityTimer = player.attackVelocityDuration;
        player.SetVelocity(attackVelocity.x * attackDir, attackVelocity.y);
    }

    /// <summary>超时或越界时把连招重置为第一段。</summary>
    private void ResetComboIndexIfNeeded()
    {
        if (Time.time > lastTimeAttacked + player.comboResetTime) comboIndex = FirstComboIndex;
        if (comboIndex > comboLimit) comboIndex = FirstComboIndex;
    }
}
