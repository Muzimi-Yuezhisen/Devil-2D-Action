using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家实体：输入、各战斗/移动状态、技能与减速处理。
/// </summary>
public class Player : Entity
{
    /// <summary>玩家死亡时广播。</summary>
    public static event Action OnPlayerDeath;
    /// <summary>场景 UI（技能树开关等）。</summary>
    private UI ui;

    /// <summary>技能管理器。</summary>
    public Player_SkillManager skillManager { get; private set; }
    /// <summary>玩家专属特效（残影等）。</summary>
    public Player_VFX vfx { get; private set; }
    public Entity_Health health { get; private set; }
    public Entity_StatusHandler statusHandler { get; private set; }

    #region 状态变量
    /// <summary>输入动作集。</summary>
    public PlayerInputSet input { get; private set; }
    /// <summary>待机状态。</summary>
    public Player_IdleState idleState { get; private set; }
    /// <summary>移动状态。</summary>
    public Player_MoveState moveState { get; private set; }
    /// <summary>跳跃状态。</summary>
    public Player_JumpState jumpState { get; private set; }
    /// <summary>下落状态。</summary>
    public Player_FallState fallState { get; private set; }
    /// <summary>滑墙状态。</summary>
    public Player_WallSlideState wallSlideState { get; private set; }
    /// <summary>墙跳状态。</summary>
    public Player_WallJumpState wallJumpState { get; private set; }
    /// <summary>冲刺状态。</summary>
    public Player_DashState dashState { get; private set; }
    /// <summary>地面普攻状态。</summary>
    public Player_BasicAttackState basicAttackState { get; private set; }
    /// <summary>空中攻击状态（仅在空中状态中触发）。</summary>
    public Player_JumpAttackState jumpAttackState { get; private set; }
    /// <summary>死亡状态。</summary>
    public Player_DeadState deadState { get; private set; }
    /// <summary>弹反状态。</summary>
    public Player_CounterAttackState counterAttackState { get; private set; }

    public Player_SwordThrowState swordThrowState { get; private set; }
    #endregion

    [Header("攻击设置")]
    /// <summary>连招各段攻击位移。</summary>
    public Vector2[] attackVelocity;
    /// <summary>跳攻位移。</summary>
    public Vector2 jumpAttackVelocity;
    /// <summary>攻击位移持续时间。</summary>
    public float attackVelocityDuration = 0.1f;
    /// <summary>连招重置等待时间。</summary>
    public float comboResetTime = 1;
    /// <summary>延迟进入攻击状态的协程。</summary>
    private Coroutine queuedAttackCo;

    /// <summary>第一、二段使用普通击退，地面连招最后一段才使用重击击退。</summary>
    public AttackImpact CurrentAttackImpact =>
        stateMachine != null && stateMachine.currentState == basicAttackState && basicAttackState != null
            ? GetAttackImpactForComboIndex(basicAttackState.CurrentComboIndex)
            : AttackImpact.Normal;

    /// <summary>把连段阶段映射为受击力度，只有最后一段是重击。</summary>
    public AttackImpact GetAttackImpactForComboIndex(int comboIndex)
    {
        int comboLimit = basicAttackState != null ? basicAttackState.ComboLimit : attackVelocity?.Length ?? 3;
        return comboIndex >= Mathf.Max(1, comboLimit) ? AttackImpact.Heavy : AttackImpact.Normal;
    }

    [Header("移动设置")]
    /// <summary>地面移动速度。</summary>
    public float moveSpeed;
    /// <summary>跳跃初速度。</summary>
    public float jumpForce = 5;
    /// <summary>墙跳冲量。</summary>
    public Vector2 wallJumpForce;
    /// <summary>空中水平移动倍率。</summary>
    [Range(0f, 1f)]
    public float inAirMoveMultiplier = 0.7f;
    /// <summary>滑墙下落减速倍率。</summary>
    [Range(0f, 1f)]
    public float wallSlideSlowMultiplier = 0.3f;
    [Space]
    /// <summary>冲刺持续时间。</summary>
    public float dashDuration = 0.25f;
    /// <summary>冲刺速度。</summary>
    public float dashSpeed = 20;

    /// <summary>当前移动输入（-1~1）。</summary>
    public Vector2 moveInput { get; private set; }
    public Vector2 mousePositon { get; private set; }

    /// <summary>创建输入与全部状态实例。</summary>
    protected override void Awake()
    {
        base.Awake();

        ui = FindAnyObjectByType<UI>();
        vfx = GetComponent<Player_VFX>();
        input = new PlayerInputSet();
        health = GetComponent<Entity_Health>();
        skillManager = GetComponent<Player_SkillManager>();
        statusHandler = GetComponent<Entity_StatusHandler>();

        idleState = new Player_IdleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        jumpState = new Player_JumpState(this, stateMachine, "JumpFall");
        fallState = new Player_FallState(this, stateMachine, "JumpFall");
        wallSlideState = new Player_WallSlideState(this, stateMachine, "wallSlide");
        wallJumpState = new Player_WallJumpState(this, stateMachine, "JumpFall");
        dashState = new Player_DashState(this, stateMachine, "dash");
        basicAttackState = new Player_BasicAttackState(this, stateMachine, "basicAttack");
        jumpAttackState = new Player_JumpAttackState(this, stateMachine, "jumpAttack");
        deadState = new Player_DeadState(this, stateMachine, "dead");
        counterAttackState = new Player_CounterAttackState(this, stateMachine, "counterAttack");
        swordThrowState = new Player_SwordThrowState(this, stateMachine, "swordThrow");
    }

    /// <summary>进入待机作为初始状态。</summary>
    protected override void Start()
    {
        base.Start();
        stateMachine.Init(idleState);
    }

    /// <summary>传送玩家到指定世界坐标。</summary>
    public void TeleportPlayer(Vector3 position) => transform.position = position;

    /// <summary>冰冻等减速：临时降低移速、跳跃、动画与攻击位移。</summary>
    protected override IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
    {
        float originalMoveSpeed = moveSpeed;
        float originalJumpForce = jumpForce;
        float originalAnimSpeed = anim.speed;
        Vector2 originalWallJump = wallJumpForce;
        Vector2 originalJumpAttack = jumpAttackVelocity;
        Vector2[] originalAttackVelocity = new Vector2[attackVelocity.Length];
        Array.Copy(attackVelocity, originalAttackVelocity, attackVelocity.Length);

        float speedMultiplier = 1 - slowMultiplier;

        moveSpeed *= speedMultiplier;
        jumpForce *= speedMultiplier;
        anim.speed *= speedMultiplier;
        wallJumpForce *= speedMultiplier;
        jumpAttackVelocity *= speedMultiplier;
        for (int i = 0; i < attackVelocity.Length; i++)
        {
            attackVelocity[i] *= speedMultiplier;
        }

        yield return new WaitForSeconds(duration);

        moveSpeed = originalMoveSpeed;
        jumpForce = originalJumpForce;
        anim.speed = originalAnimSpeed;
        wallJumpForce = originalWallJump;
        jumpAttackVelocity = originalJumpAttack;
        for (int i = 0; i < attackVelocity.Length; i++)
        {
            attackVelocity[i] = originalAttackVelocity[i];
        }
    }

    /// <summary>死亡：广播事件并切到死亡状态。</summary>
    public override void EntityDeath()
    {
        base.EntityDeath();
        OnPlayerDeath?.Invoke();
        stateMachine.ChangeState(deadState);
    }

    /// <summary>本帧结束后再进入普攻状态（避免同帧输入冲突）。</summary>
    public void EnterAttackStateWithDelay()
    {
        if (queuedAttackCo != null) StopCoroutine(queuedAttackCo);
        queuedAttackCo = StartCoroutine(EnterAttackStateWithDelayCo());
    }

    /// <summary>延迟一帧切入普攻。</summary>
    private IEnumerator EnterAttackStateWithDelayCo()
    {
        yield return new WaitForEndOfFrame();
        queuedAttackCo = null;
        if (health != null && health.isDead) yield break;
        stateMachine.ReenterState(basicAttackState);
    }

    /// <summary>启用输入并绑定移动、技能树、施法。</summary>
    private void OnEnable()
    {
        input.Enable();

        input.Player.Mouse.performed += HandleMouseInput;
        input.Player.Movement.performed += HandleMovementInput;
        input.Player.Movement.canceled += HandleMovementCanceled;
        input.Player.ToggleSkillTreeUI.performed += HandleSkillTreeInput;
        input.Player.Spell.performed += HandleSpellInput;
    }

    /// <summary>禁用输入。</summary>
    private void OnDisable()
    {
        input.Player.Mouse.performed -= HandleMouseInput;
        input.Player.Movement.performed -= HandleMovementInput;
        input.Player.Movement.canceled -= HandleMovementCanceled;
        input.Player.ToggleSkillTreeUI.performed -= HandleSkillTreeInput;
        input.Player.Spell.performed -= HandleSpellInput;
        input.Disable();
    }

    private void HandleMouseInput(InputAction.CallbackContext context) => mousePositon = context.ReadValue<Vector2>();

    private void HandleMovementInput(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();

    private void HandleMovementCanceled(InputAction.CallbackContext _) => moveInput = Vector2.zero;

    private void HandleSkillTreeInput(InputAction.CallbackContext _)
    {
        if (ui != null) ui.ToggleSkillTreeUI();
    }

    private void HandleSpellInput(InputAction.CallbackContext _)
    {
        skillManager?.shard?.TryUseSkill();
    }
}
