using UnityEngine;

/// <summary>
/// 玩家战斗：在实体战斗基础上增加弹反检测。
/// </summary>
public class Player_Combat : Entity_Combat
{
    [Header("弹反设置")]
    /// <summary>弹反结束后的硬直/恢复时间。</summary>
    [SerializeField] private float counterRecovery = 0.1f;

    /// <summary>
    /// 对检测范围内可弹反目标执行弹反；有任一成功则返回 true。
    /// </summary>
    public bool CounterAttackPerformed()
    {
        bool hasPerformedCounter = false;

        foreach (var target in GetDetectedColliders())
        {
            ICounterable counterable = target.GetComponent<ICounterable>();
            if (counterable == null) continue;

            if (counterable.CanBeCountered)
            {
                counterable.HandleCounter();
                hasPerformedCounter = true;
            }
        }
        return hasPerformedCounter;
    }

    /// <summary>获取弹反恢复时长。</summary>
    public float GetCounterRecoveryDuration() => counterRecovery;
}
