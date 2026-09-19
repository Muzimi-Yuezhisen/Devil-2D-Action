using UnityEngine;

/// <summary>
/// ??????????????/???????????
/// </summary>
public class Skill_Dash : Skill_Base
{
    /// <summary>?????????????</summary>
    public void OnStartEffect()
    {
        if (Unlocked(SkillUpgradeType.Dash_CloneOnStart) || Unlocked(SkillUpgradeType.Dash_CloneOnStartAndArrival))
        {
            CreateClone();
        }

        if (Unlocked(SkillUpgradeType.Dash_ShardOnStart) || Unlocked(SkillUpgradeType.Dash_ShardOnStartAndArrival))
        {
            CreateShard();
        }
    }

    /// <summary>?????????????</summary>
    public void OnEndEffect()
    {
        if (Unlocked(SkillUpgradeType.Dash_CloneOnStartAndArrival))
        {
            CreateClone();
        }
        if (Unlocked(SkillUpgradeType.Dash_ShardOnStartAndArrival))
        {
            CreateShard();
        }
    }

    /// <summary>??????????? Spell ??????</summary>
    private void CreateShard()
    {
        skillManager.shard.CreateRawShard();
    }

    /// <summary>???????????????</summary>
    private void CreateClone()
    {
        Debug.Log("Create time echo");
    }
}
