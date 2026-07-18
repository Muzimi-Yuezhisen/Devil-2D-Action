using UnityEngine;

public enum SkillUpgradeType
{
    None,
    Dash,
    Dash_CloneOnStart,  //开始时创建克隆
    Dash_CloneOnStartAndArrival,    //开始和结束时创建克隆
    Dash_ShardOnStart,  //冲刺开始时创建碎片
    Dash_ShardOnStartAndArrival, //冲刺开始和结束时创建碎片

    Shard,  //碎片在有敌人接近/到时间会产生爆炸
    Shard_MoveToEnemy,  //碎片会朝着敌人移动
    Shard_TripleCast,   //碎片会有n次充能
    Shard_Teleport, //和最后一个创建的碎片交换位置
    Shard_TeleportAndHeal,  //交换位置，并且可以回复状态到创建碎片的状态
}
