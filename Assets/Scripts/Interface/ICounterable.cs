using UnityEngine;
/// <summary>
/// 可被弹反的对象接口：在攻击可弹反窗口内被玩家弹反时调用。
/// </summary>
public interface ICounterable
{
    /// <summary>当前是否处于可被弹反的状态。</summary>
    public bool CanBeCountered { get; }
    /// <summary>被成功弹反时执行的逻辑。</summary>
    public void HandleCounter();
}
