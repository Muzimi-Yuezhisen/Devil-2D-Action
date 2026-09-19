using UnityEngine;

/// <summary>
/// 敌人特效：攻击弹反提示图标开关。
/// </summary>
public class Enemy_VFX : Entity_VFX
{
    [Header("弹反窗口")]
    /// <summary>攻击可弹反时显示的警示物体。</summary>
    [SerializeField] private GameObject attackAlert;

    /// <summary>开局关闭警示。</summary>
    protected void Start()
    {
        EnableAttackAlert(false);
    }

    /// <summary>显示或隐藏弹反警示。</summary>
    public void EnableAttackAlert(bool enable)
    {
        if (attackAlert == null) return;
        attackAlert.SetActive(enable);
    }
}
