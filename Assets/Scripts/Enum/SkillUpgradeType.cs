using UnityEngine;
/// <summary>
/// 技能升级类型，用于技能树分支与强化效果。
/// </summary>
public enum SkillUpgradeType
{
    /// <summary>无升级（未解锁）。</summary>
    None,
    /// <summary>基础冲刺。</summary>
    Dash,
    /// <summary>冲刺开始时创建克隆。</summary>
    Dash_CloneOnStart,
    /// <summary>冲刺开始和结束时都创建克隆。</summary>
    Dash_CloneOnStartAndArrival,
    /// <summary>冲刺开始时创建碎片。</summary>
    Dash_ShardOnStart,
    /// <summary>冲刺开始和结束时都创建碎片。</summary>
    Dash_ShardOnStartAndArrival,


    /// <summary>基础碎片：敌人靠近或到时爆炸。</summary>
    Shard,
    /// <summary>碎片会朝最近敌人移动。</summary>
    Shard_MoveToEnemy,
    /// <summary>碎片拥有多次充能。</summary>
    Shard_Multicast,
    /// <summary>与最后创建的碎片交换位置。</summary>
    Shard_Teleport,
    /// <summary>交换位置，并把生命回复到创建碎片时的比例。</summary>
    Shard_TeleportHpRewind,

    SwordThrow, //You can throw sword to damage enemies from range
    SwordThrow_Spin,    //Your sword will spin at one point and damage enemies. Like a chainsaw
    SwordThrow_Pierce,  //Pierce sword will pierce N targets
    SwordThrow_Bounce   //Bounce sword will bounce between enemies
}
