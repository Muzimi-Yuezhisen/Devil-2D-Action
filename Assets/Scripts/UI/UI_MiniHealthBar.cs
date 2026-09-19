using UnityEngine;

/// <summary>
/// 头顶小血条：实体翻转时保持 UI 不跟着旋转。
/// </summary>
public class UI_MiniHealthBar : MonoBehaviour
{
    /// <summary>所属实体。</summary>
    private Entity entity;

    /// <summary>在父物体上查找实体。</summary>
    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
    }

    /// <summary>订阅翻转向事件。</summary>
    private void OnEnable()
    {
        entity.OnFlipped += HandleFlip;
    }

    /// <summary>取消订阅。</summary>
    private void OnDisable()
    {
        entity.OnFlipped -= HandleFlip;
    }

    /// <summary>翻转后把血条旋转重置为世界朝向。</summary>
    private void HandleFlip() => transform.rotation = Quaternion.identity;
}
