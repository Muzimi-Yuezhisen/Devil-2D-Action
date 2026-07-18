using UnityEngine;

/// <summary>
/// 冲刺技能产生的碎片，检测到敌人时就产生爆炸
/// </summary>
public class SkillObject_Shard : SkillObject_Base
{
    [SerializeField] private GameObject vfxPrefab;

    //一段时间后自动销毁
    public void SetupShard(float detinationTime)
    {
        Invoke(nameof(Explode), detinationTime);
    }

    private void Explode()
    {
        DamageEnemiesInRadius(transform, checkRadius);
        Instantiate(vfxPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Enemy>() == null) return;

        Explode();
    }
}
