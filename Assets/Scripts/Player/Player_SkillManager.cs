using UnityEngine;

/// <summary>
/// 玩家技能管理器：缓存冲刺与碎片技能，并按类型查找。
/// </summary>
public class Player_SkillManager : MonoBehaviour
{
    /// <summary>冲刺技能组件。</summary>
    public Skill_Dash dash { get; private set; }
    /// <summary>时间碎片技能组件。</summary>
    public Skill_Shard shard { get; private set; }
    public Skill_SwordThrow swordThrow { get; private set; }

    /// <summary>在子物体中查找技能组件。</summary>
    private void Awake()
    {
        dash = GetComponentInChildren<Skill_Dash>();
        shard = GetComponentInChildren<Skill_Shard>();
        swordThrow = GetComponentInChildren<Skill_SwordThrow>();
    }

    /// <summary>
    /// 按技能类型返回对应技能基类；未实现的类型返回 null。
    /// </summary>
    public Skill_Base GetSkillByType(SkillType type)
    {
        switch (type)
        {
            case SkillType.Dash: return dash;
            case SkillType.TimeShard: return shard;
            case SkillType.SwordThrow: return swordThrow;
            default:
                Debug.Log($"Skill type{type} is not implemented yet.");
                return null;
        }
    }
}
