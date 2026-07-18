using UnityEngine;

//发出的攻击可以被反击
public interface ICounterable
{
    public bool CanBeCountered { get;}
    public void HandleCounter();
}
