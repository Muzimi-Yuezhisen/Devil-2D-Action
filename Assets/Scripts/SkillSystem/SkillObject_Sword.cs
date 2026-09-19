using UnityEngine;

public class SkillObject_Sword : SkillObject_Base
{
    protected Skill_SwordThrow swordManager;
    protected Rigidbody2D rb;

    protected Transform playerTransform;
    protected bool shouldComeback;
    protected float comebackSpeed = 20;
    protected float maxAllowedDistance = 25;

    protected virtual void Update()
    {
        transform.right = rb.linearVelocity;
        HandleComeback();
    }

    public virtual void SetupSword(Skill_SwordThrow swordManager, Vector2 direction)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction;

        this.swordManager = swordManager;

        playerTransform = swordManager.transform.root;
        playerStats = swordManager.player.stats;
        damageDealer = swordManager.player.transform;
        damageScaleData = swordManager.damageScaleData;
    }

    public void GetSwordBackToPlayer()
    {
        shouldComeback = true;
        transform.SetParent(null, true);
        if (rb != null) rb.simulated = false;
    }

    protected void HandleComeback()
    {
        if (playerTransform == null) return;
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance > maxAllowedDistance) GetSwordBackToPlayer();

        if (shouldComeback == false) return;
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, comebackSpeed * Time.deltaTime);

        if (distance < 0.5f) Destroy(gameObject);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        StopSword(collision);
        DamageEnemiesInRadius(transform, 1);
    }

    protected void StopSword(Collider2D collision)
    {
        rb.simulated = false;
        //设为碰撞体的子物体，实现跟随碰撞体移动
        transform.parent = collision.transform;
    }
}

