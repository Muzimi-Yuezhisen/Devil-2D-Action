using UnityEngine;
/// <summary>
/// 状态机：管理当前状态的进入、更新与切换。
/// </summary>
public class StateMachine
{
    /// <summary>当前激活的状态。</summary>
    public EntityState currentState { get; private set; }
    /// <summary>是否允许切换状态（死亡后可关闭）。</summary>
    public bool canChangeState;
    /// <summary>初始化并进入第一个状态。</summary>
    public void Init(EntityState entityState)
    {
        canChangeState = true;
        currentState = entityState;
        currentState.Enter();
    }
    /// <summary>退出当前状态并进入新状态。</summary>
    public bool ChangeState(EntityState entityState)
    {
        if (canChangeState == false || entityState == null || currentState == entityState) return false;
        currentState?.Exit();
        currentState = entityState;
        currentState.Enter();
        return true;
    }
    /// <summary>
    /// 退出并重新进入当前状态。用于连击等需要同类型状态重新初始化的场景，
    /// 与 ChangeState 分开，避免普通调用意外重复执行 Enter/Exit。
    /// </summary>
    public bool ReenterState(EntityState entityState)
    {
        if (canChangeState == false || entityState == null || currentState != entityState) return false;
        currentState.Exit();
        currentState.Enter();
        return true;
    }
    /// <summary>每帧更新当前状态逻辑。</summary>
    public void UpdateActiveState()
    {
        currentState?.Update();
    }
    /// <summary>关闭状态切换（例如死亡后）。</summary>
    public void SwitchOffStateMachine() => canChangeState = false;
}
