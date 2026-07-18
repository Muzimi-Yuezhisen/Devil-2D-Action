using UnityEngine;

public class EnemyAnnimationTriggers : Entity_AnimationTriggers
{
    private Enemy enemy;
    private Enemy_VFX enemyVfx;

    protected override void Awake()
    {
        base.Awake();
        enemy = GetComponentInParent<Enemy>();
        enemyVfx = GetComponentInParent<Enemy_VFX>();
    }

    private void EnableCounterWindow()
    {
        enemy.EnableCounterWindow(true);
        enemyVfx.EnableAttackAlert(true);
    }

    private void DisableCounterWindow()
    {
        enemy.EnableCounterWindow(false);
        enemyVfx.EnableAttackAlert(false);
    }

}
