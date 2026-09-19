using UnityEngine;

/// <summary>
/// 投剑技能：抛物线预览、确认方向，并在动画事件中出手。
/// </summary>
public class Skill_SwordThrow : Skill_Base
{
    private SkillObject_Sword currentSword;
    private float currentThrowPower;

    [Header("Regular Sword Upgrade")]
    [SerializeField] private GameObject swordPrefab;
    /// <summary>投掷力度（再乘 10 作为初速度）。</summary>
    [Range(0, 10)]
    [SerializeField] private float regularThrowPower = 5;

    [Header("Pierce Sword Upgrade")]
    [SerializeField] private GameObject pierceSwordPrefab;
    public int amountToPierce = 2;
    [Range(0, 10)]
    [SerializeField] private float pierceThrowPower = 5;

    [Header("Spin Sword Upgrade")]
    [SerializeField] private GameObject spinSwordPrefab;
    public int maxDistance = 5;
    public float attacksPerSecond = 6;
    public float maxSpinDuration = 3;
    [Range(0, 10)]
    [SerializeField] private float spinThrowPower = 5;

    [Header("Bounce Sword Upgrade")]
    [SerializeField] private GameObject bounceSwordPrefab;
    public int bounceCount = 5;
    public float bounceSpeed = 12;
    [Range(0, 10)]
    [SerializeField] private float bounceThrowPower = 5;

    [Header("轨迹预览")]
    /// <summary>轨迹点预制体（请拖 Project 里的 Prefab，不要拖场景物体）。</summary>
    [SerializeField] private GameObject predictionDot;
    /// <summary>预览点数量。</summary>
    [SerializeField] private int numberOfDots = 20;
    /// <summary>相邻预览点的时间间隔。</summary>
    [SerializeField] private float spaceBetweenDots = 0.05f;


    private float swordGravity;
    /// <summary>运行时生成的预览点。</summary>
    private Transform[] dots;
    /// <summary>玩家确认投掷时的方向。</summary>
    private Vector2 confirmedDirection;

    protected override void Awake()
    {
        base.Awake();
        Rigidbody2D swordBody = swordPrefab != null ? swordPrefab.GetComponent<Rigidbody2D>() : null;
        swordGravity = swordBody != null ? swordBody.gravityScale : 1;
    }
    /// <summary>生成预览点并默认隐藏。</summary>
    private void Start()
    {
        dots = GenerateDots();
    }

    public override bool CanUseSkill()
    {
        UpdateThrowPower();

        if (currentSword != null)
        {
            currentSword.GetSwordBackToPlayer();
            return false;
        }
            
        return base.CanUseSkill();
    }

    public void ThrowSword()
    {
        // 动画若因状态未退出而重播，避免同一轮再生成第二把剑
        if (currentSword != null) return;

        GameObject selectedSwordPrefab = GetSwordPrefab();
        if (selectedSwordPrefab == null) return;

        Vector3 spawnPosition = dots != null && dots.Length > 1 ? dots[1].position : player.transform.position;
        GameObject newSword = Instantiate(selectedSwordPrefab, spawnPosition, Quaternion.identity);

        currentSword = newSword.GetComponent<SkillObject_Sword>();
        if (currentSword == null)
        {
            Destroy(newSword);
            return;
        }
        currentSword.SetupSword(this, GetThrowPower());
        AudioManager.PlaySfxAt(AudioCue.SwordThrow, player.transform.position);
        SetSkillOnCooldown();

        // 没有单独的 CurrentStateTrigger 时，用这次出手事件通知状态机本轮结束，
        // 否则 swordThrow 一直为 true，子状态机会反复重进 Performed 并连发。
        player.CurrentStateAnimationTrigger();
    }

    private GameObject GetSwordPrefab()
    {
        if (Unlocked(SkillUpgradeType.SwordThrow)) return swordPrefab;

        if (Unlocked(SkillUpgradeType.SwordThrow_Pierce)) return pierceSwordPrefab;

        if (Unlocked(SkillUpgradeType.SwordThrow_Spin)) return spinSwordPrefab;

        if (Unlocked(SkillUpgradeType.SwordThrow_Bounce)) return bounceSwordPrefab;

        Debug.Log("No valied sword upgrade selected!");
        return null;
    }

    private void UpdateThrowPower()
    {
        switch (upgradeType)
        {
            case SkillUpgradeType.SwordThrow:
                currentThrowPower = regularThrowPower;
                break;
            case SkillUpgradeType.SwordThrow_Spin:
                currentThrowPower = spinThrowPower;
                break;
            case SkillUpgradeType.SwordThrow_Pierce:
                currentThrowPower = pierceThrowPower;
                break;
            case SkillUpgradeType.SwordThrow_Bounce:
                currentThrowPower = bounceThrowPower;
                break;
        }
    }

    private Vector2 GetThrowPower() => confirmedDirection * (currentThrowPower * 10);

    public void PredictTrajectory(Vector2 direction)
    {
        for(int i = 0; i < dots.Length; i++)
        {
            dots[i].position = GetTrajectoryPoint(direction, i * spaceBetweenDots);
        }
    }

    private Vector2 GetTrajectoryPoint(Vector2 direction, float t)
    {
        float scaledThrowPower = currentThrowPower * 10;
        // the starting speed  and direction of the throw
        Vector2 initialVelocity = direction * scaledThrowPower;

        //重力计算：1/2 g t^2
        Vector2 gravityEffect = 0.5f * Physics2D.gravity * swordGravity * (t * t);

        Vector2 predictedPoint = (initialVelocity * t) + gravityEffect;

        Vector2 playerPositon = transform.root.position;

        return playerPositon + predictedPoint;

    }

    public void ConfirmTrajectory(Vector2 direction) => confirmedDirection = direction;

    public void EnableDots(bool enable)
    {
        if (dots == null) return;
        foreach (Transform t in dots) t.gameObject.SetActive(enable);
    }

    private Transform[] GenerateDots()
    {
        if (predictionDot == null || numberOfDots <= 0) return System.Array.Empty<Transform>();
        Transform[] newDots = new Transform[numberOfDots];
        for(int i = 0; i < numberOfDots; i++)
        {
            newDots[i] = Instantiate(predictionDot, transform.position, Quaternion.identity, transform).transform;
            newDots[i].gameObject.SetActive(false);
        }
        return newDots;
    }
}
