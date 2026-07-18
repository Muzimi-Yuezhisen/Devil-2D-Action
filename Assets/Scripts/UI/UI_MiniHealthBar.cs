using UnityEngine;

public class UI_MiniHealthBar : MonoBehaviour
{
    private Entity entity;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
    }
    private void OnEnable()
    {
        entity.OnFlipped += HandleFlip;
    }

    private void OnDisable()
    {
        entity.OnFlipped -= HandleFlip;
    }
    /// <summary>
    /// 确保UI不会随着GameObject的旋转而旋转
    /// </summary>
    private void HandleFlip() => transform.rotation = Quaternion.identity;
}
