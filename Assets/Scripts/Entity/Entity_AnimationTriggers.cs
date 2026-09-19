using UnityEngine;

/// <summary>
/// 动画事件入口：把 Animator 事件转到状态机与战斗组件。
/// </summary>
public class Entity_AnimationTriggers : MonoBehaviour
{
    /// <summary>所属实体。</summary>
    private Entity entity;
    /// <summary>战斗组件（普攻结算）。</summary>
    private Entity_Combat entityCombat;

    /// <summary>在父物体上查找实体与战斗组件。</summary>
    protected virtual void Awake()
    {
        entity = GetComponentInParent<Entity>();
        entityCombat = GetComponentInParent<Entity_Combat>();
    }

    /// <summary>动画事件：通知当前状态到达触发点。</summary>
    private void CurrentStateTrigger()
    {
        entity.CurrentStateAnimationTrigger();
    }

    /// <summary>动画事件：执行一次攻击判定。</summary>
    private void AttackTrigger()
    {
        entityCombat.PerformAttack();
    }
}
