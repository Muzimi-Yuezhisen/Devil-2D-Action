using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 实体基类：状态机、朝向、移动速度、地面/墙壁检测与击退。
/// </summary>
public class Entity : MonoBehaviour
{
    /// <summary>朝向翻转时触发。</summary>
    public event Action OnFlipped;
    /// <summary>动画控制器。</summary>
    public Animator anim { get; private set; }
    /// <summary>刚体。</summary>
    public Rigidbody2D rb { get; private set; }
    /// <summary>属性组件。</summary>
    public Entity_Stats stats { get; private set; }

    /// <summary>状态机实例。</summary>
    protected StateMachine stateMachine;

    /// <summary>当前是否朝右。</summary>
    private bool facingRight = true;
    /// <summary>朝向：1 右，-1 左。</summary>
    public int facingDir { get; private set; } = 1;

    [Header("碰撞检测")]
    /// <summary>地面检测射线长度。</summary>
    [SerializeField] private float groundCheckDistance;
    /// <summary>墙壁检测射线长度。</summary>
    [SerializeField] private float wallCheckDistance;
    /// <summary>地面/墙体所在层级。</summary>
    [SerializeField] protected LayerMask whatIsGround;
    /// <summary>地面检测起点。</summary>
    [SerializeField] private Transform groundCheck;
    /// <summary>主墙壁检测起点。</summary>
    [SerializeField] private Transform primaryWallCheck;
    /// <summary>副墙壁检测起点（可选，双点同时命中才算贴墙）。</summary>
    [SerializeField] private Transform secondaryWallCheck;
    /// <summary>是否检测到地面。</summary>
    public bool groundDetected { get; private set; }
    /// <summary>是否检测到墙壁。</summary>
    public bool wallDetected { get; private set; }

    /// <summary>是否处于击退中（期间忽略主动设速度）。</summary>
    private bool isKnocked;

    /// <summary>击退协程引用。</summary>
    private Coroutine knockbackCo;
    /// <summary>减速协程引用。</summary>
    private Coroutine slowDownCo;

    /// <summary>缓存组件并创建状态机。</summary>
    protected virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<Entity_Stats>();
        stateMachine = new StateMachine();
    }

    /// <summary>子类可在此做开局初始化。</summary>
    protected virtual void Start()
    {
    }

    /// <summary>每帧更新碰撞检测与当前状态。</summary>
    protected virtual void Update()
    {
        HandleCollisionDetection();
        stateMachine.UpdateActiveState();
    }

    /// <summary>把动画事件转发给当前状态。</summary>
    public void CurrentStateAnimationTrigger()
    {
        stateMachine.currentState.AnimationTrigger();
    }

    /// <summary>死亡时由生命组件调用，子类实现具体逻辑。</summary>
    public virtual void EntityDeath()
    {
    }

    /// <summary>在指定时长内按倍率减速（子类协程实现）。</summary>
    public virtual void SlowDownEntity(float duration, float slowMultiplier)
    {
        if (slowDownCo != null) StopCoroutine(slowDownCo);

        slowDownCo = StartCoroutine(SlowDownEntityCo(duration, slowMultiplier));
    }

    /// <summary>减速协程，基类为空实现。</summary>
    protected virtual IEnumerator SlowDownEntityCo(float duration, float slowMultiplier)
    {
        yield return null;
    }

    /// <summary>施加击退速度并锁定移动一段时间。</summary>
    public void ReceiveKnockback(Vector2 knockback, float duration)
    {
        if (knockbackCo != null) StopCoroutine(knockbackCo);
        knockbackCo = StartCoroutine(KnockbackCo(knockback, duration));
    }

    /// <summary>击退协程：写入速度，结束后清零并解除锁定。</summary>
    private IEnumerator KnockbackCo(Vector2 knockback, float duration)
    {
        isKnocked = true;
        rb.linearVelocity = knockback;
        yield return new WaitForSeconds(duration);
        rb.linearVelocity = Vector2.zero;
        isKnocked = false;
        knockbackCo = null;
    }

    /// <summary>设置水平/竖直速度，并根据水平速度处理翻转。</summary>
    public void SetVelocity(float xVelocity, float yVelocity)
    {
        if (isKnocked) return;
        rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        HandleFlip(xVelocity);
    }

    /// <summary>立即翻转朝向并触发事件。</summary>
    public void Flip()
    {
        transform.Rotate(0, 180, 0);
        facingRight = !facingRight;
        facingDir = facingDir * -1;

        OnFlipped?.Invoke();
    }

    /// <summary>按水平速度自动翻转。</summary>
    public void HandleFlip(float xVelocity)
    {
        if (xVelocity > 0 && facingRight == false) Flip();
        else if (xVelocity < 0 && facingRight == true) Flip();
    }

    /// <summary>射线检测地面与墙壁。</summary>
    private void HandleCollisionDetection()
    {
        groundDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
        if (secondaryWallCheck != null)
        {
            wallDetected = Physics2D.Raycast(primaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround)
            && Physics2D.Raycast(secondaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
        }
        else
        {
            wallDetected = Physics2D.Raycast(primaryWallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
        }
    }

    /// <summary>编辑器中绘制地面/墙壁检测线。</summary>
    protected virtual void OnDrawGizmos()
    {
        if (groundCheck == null || primaryWallCheck == null) return;
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + new Vector3(0, -groundCheckDistance));
        Gizmos.DrawLine(primaryWallCheck.position, primaryWallCheck.position + new Vector3(wallCheckDistance * facingDir, 0));
        if (secondaryWallCheck != null)
            Gizmos.DrawLine(secondaryWallCheck.position, secondaryWallCheck.position + new Vector3(wallCheckDistance * facingDir, 0));
    }
}
