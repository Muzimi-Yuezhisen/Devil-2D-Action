using UnityEngine;

//状态机的管理
public class StateMachine
{
    public EntityState currentState {  get; private set; }
    public bool canChangeState;

    public void Init(EntityState entityState)
    {
        canChangeState = true;
        currentState = entityState;
        currentState.Enter();
    }

    public void ChangeState(EntityState entityState)
    {
        if (canChangeState == false) return;
        currentState.Exit();
        currentState = entityState;
        currentState.Enter();
    }

    public void UpdateActiveState()
    {
        currentState.Update();
    }

    public void SwitchOffStateMachine() => canChangeState = false;
}
