using UnityEngine;

/// <summary>
/// 投剑瞄准与出手：确认后只投一次，等动画结束再回待机。
/// </summary>
public class Player_SwordThrowState : PlayerState
{
    private Camera mainCamera;

    /// <summary>
    /// 本轮是否已经确认出手。为 true 时不再预览轨迹，也不因按住右键而反复进入出手动画。
    /// </summary>
    private bool hasThrown;

    public Player_SwordThrowState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // 每次进入瞄准都重置，否则上次投掷的标记会挡住下一次
        hasThrown = false;
        skillManager.swordThrow.EnableDots(true);

        if (mainCamera != Camera.main) mainCamera = Camera.main;
    }

    public override void Update()
    {
        base.Update();
        if (IsActiveState == false) return;

        player.SetVelocity(0, rb.linearVelocity.y);

        if (hasThrown == false)
        {
            Vector2 dirToMouse = DirectionToMouse();
            player.HandleFlip(dirToMouse.x);
            skillManager.swordThrow.PredictTrajectory(dirToMouse);

            // 左键确认方向，切到出手动画；之后不再接受第二次确认
            if (input.Player.Attack.WasPressedThisFrame())
            {
                hasThrown = true;
                // 兜底：出手动画若没有结束事件，超时后也必须退出，否则 Animator 会循环投掷
                stateTimer = 0.4f;
                anim.SetBool("swordThrowPerformed", true);
                skillManager.swordThrow.EnableDots(false);
                skillManager.swordThrow.ConfirmTrajectory(dirToMouse);
            }

            // 尚未出手时，松开右键视为取消瞄准
            if (input.Player.RangeAttack.WasReleasedThisFrame())
            {
                stateMachine.ChangeState(player.idleState);
            }
        }
        else
        {
            // 已出手：忽略右键按住。等 ThrowSword 事件（或超时）再回待机，关掉 swordThrow。
            if (triggerCalled || stateTimer < 0)
            {
                stateMachine.ChangeState(player.idleState);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        // base.Exit 会关掉 swordThrow，父层不会再把子状态机拉回来
        anim.SetBool("swordThrowPerformed", false);
        skillManager.swordThrow.EnableDots(false);
    }

    private Vector2 DirectionToMouse()
    {
        if (mainCamera == null) return Vector2.right * player.facingDir;
        Vector2 playerPosition = player.transform.position;
        Vector2 worldMousePosition = mainCamera.ScreenToWorldPoint(player.mousePositon);

        Vector2 direction = worldMousePosition - playerPosition;

        return direction.normalized;
    }
}
