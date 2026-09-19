using UnityEngine;

/// <summary>
/// 敌人动画事件：开关弹反窗口与攻击警示。
/// </summary>
public class EnemyAnnimationTriggers : Entity_AnimationTriggers
{
    /// <summary>所属敌人。</summary>
    private Enemy enemy;
    /// <summary>敌人特效。</summary>
    private Enemy_VFX enemyVfx;

    /// <summary>缓存敌人与特效组件。</summary>
    protected override void Awake()
    {
        base.Awake();
        enemy = GetComponentInParent<Enemy>();
        enemyVfx = GetComponentInParent<Enemy_VFX>();
    }

    /// <summary>动画事件：打开弹反窗口。</summary>
    private void EnableCounterWindow()
    {
        enemy.EnableCounterWindow(true);
        enemyVfx.EnableAttackAlert(true);
    }

    /// <summary>动画事件：关闭弹反窗口。</summary>
    private void DisableCounterWindow()
    {
        enemy.EnableCounterWindow(false);
        enemyVfx.EnableAttackAlert(false);
    }
}
