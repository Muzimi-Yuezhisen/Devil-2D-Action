#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// 可从命令行或菜单运行的核心战斗冒烟测试。
/// 它只修改 Play Mode 内存，不会保存测试造成的场景变化。
/// </summary>
[InitializeOnLoad]
public static class CombatPlayModeSmokeRunner
{
    private const string IsRunningKey = "Devil.CombatSmoke.IsRunning";
    private const string HasFailedKey = "Devil.CombatSmoke.HasFailed";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

    private enum TestPhase
    {
        MainMenu,
        LoadingGameplay,
        Gameplay,
        Finished
    }

    private static double phaseStartTime;
    private static TestPhase phase;

    static CombatPlayModeSmokeRunner()
    {
        if (SessionState.GetBool(IsRunningKey, false)) AttachCallbacks();
    }

    [MenuItem("Tools/Devil/Run Combat Smoke Test")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        SessionState.SetBool(IsRunningKey, true);
        SessionState.SetBool(HasFailedKey, false);
        AttachCallbacks();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void AttachCallbacks()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        Application.logMessageReceived -= HandleLog;
        Application.logMessageReceived += HandleLog;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            phaseStartTime = EditorApplication.timeSinceStartup;
            phase = TestPhase.MainMenu;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
        else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(IsRunningKey, false))
        {
            Complete();
        }
    }

    private static void Tick()
    {
        double elapsed = EditorApplication.timeSinceStartup - phaseStartTime;

        if (phase == TestPhase.MainMenu && elapsed >= 2)
        {
            MainMenuController menu = ValidateMainMenu();
            if (menu != null)
            {
                menu.StartGame();
                phase = TestPhase.LoadingGameplay;
                phaseStartTime = EditorApplication.timeSinceStartup;
                return;
            }

            FinishPlayMode();
            return;
        }

        if (phase == TestPhase.LoadingGameplay)
        {
            if (SceneManager.GetActiveScene().name == MainMenuController.GameplaySceneName)
            {
                phase = TestPhase.Gameplay;
                phaseStartTime = EditorApplication.timeSinceStartup;
            }
            else if (elapsed > 8)
            {
                Require(false, "Start Game did not load the gameplay scene.");
                FinishPlayMode();
            }
            return;
        }

        if (phase != TestPhase.Gameplay || elapsed < 2) return;
        ValidateGameplay();
        FinishPlayMode();
    }

    private static MainMenuController ValidateMainMenu()
    {
        Require(SceneManager.GetActiveScene().name == MainMenuController.SceneName,
            "The project did not start in the MainMenu scene.");
        Require(SceneUtility.GetBuildIndexByScenePath(MenuScenePath) == 0,
            "MainMenu is not build index 0.");
        Require(SceneUtility.GetBuildIndexByScenePath(GameplayScenePath) == 1,
            "Gameplay scene is not build index 1.");

        MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
        Require(menu != null, "MainMenuController did not initialize.");
        Require(AudioManager.Instance != null, "AudioManager did not initialize in the main menu.");
        Require(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1,
            "Main menu must contain exactly one AudioListener.");
        Require(Camera.main != null && menu != null && menu.MenuCamera == Camera.main && Camera.main.enabled,
            "Main menu must have one enabled MainCamera so the Game view never shows 'No cameras rendering'.");
        Require(AudioManager.IsMusicPlaying, "Main-menu BGM is not playing.");
        Require(AudioManager.CurrentMusicCue == AudioCue.MainMenuMusic,
            "Main menu is playing the wrong music cue.");
        Require(AudioManager.CurrentMusicClipName == "menu_gothic_piano",
            "Main menu is not using the refreshed gothic music.");

        if (menu == null) return null;
        Require(menu.MainPanel != null && menu.MainPanel.activeSelf, "Main-menu options are not visible.");
        Require(menu.SettingsPanel != null && !menu.SettingsPanel.activeSelf,
            "Settings panel should be closed initially.");
        Require(menu.StartButton != null && menu.SettingsButton != null && menu.ExitButton != null,
            "One or more main-menu buttons are missing.");
        Require(GameObject.Find("BloodMoon") != null && GameObject.Find("CastleKeep") != null,
            "Gothic menu background is missing its blood moon or castle silhouette.");

        menu.OpenSettings();
        Require(menu.SettingsPanel.activeSelf && !menu.MainPanel.activeSelf,
            "Settings button did not switch panels.");
        menu.CloseSettings();
        Require(!menu.SettingsPanel.activeSelf && menu.MainPanel.activeSelf,
            "Back did not return to the main menu.");
        return menu;
    }

    private static void ValidateGameplay()
    {

        Player player = Object.FindAnyObjectByType<Player>();
        CombatFlowController flow = Object.FindAnyObjectByType<CombatFlowController>();
        UI gameUI = Object.FindAnyObjectByType<UI>();
        Enemy_Health[] enemies = Object.FindObjectsByType<Enemy_Health>(FindObjectsSortMode.None);
        Enemy_Health enemy = enemies.Length > 0 ? enemies[0] : null;

        Require(player != null, "Player did not initialize in Play Mode.");
        Require(player != null && player.health != null, "Player health did not initialize.");
        Require(flow != null, "CombatFlowController bootstrap did not run.");
        Require(enemies.Length == LevelEncounterBuilder.ExpectedEnemyCount,
            $"Expected {LevelEncounterBuilder.ExpectedEnemyCount} level enemies, found {enemies.Length}.");
        Require(AudioManager.Instance != null, "AudioManager bootstrap did not run.");
        Require(AudioManager.IsMusicPlaying, "Gameplay BGM is not playing.");
        Require(AudioManager.CurrentMusicCue == AudioCue.GameplayMusic,
            "Gameplay scene is playing the wrong music cue.");
        Require(AudioManager.CurrentMusicClipName == "gameplay_gothic_loop",
            "Gameplay is not using the refreshed gothic pixel loop.");
        Require(gameUI != null, "Main UI did not initialize.");

        ValidatePlayerModule(player);
        foreach (Enemy_Health enemyHealth in enemies)
            ValidateEnemyModule(enemyHealth);
        ValidateSceneModule();
        ValidateSkillModule(player, gameUI);
        ValidateStatModule(player);
        ValidateLevelBalance(player, enemies);
        ValidateHazardModule(player);
        ValidateRewardModule(player, gameUI);

        if (gameUI != null)
        {
            Require(!gameUI.IsSkillTreeOpen, "Skill tree should be closed when entering the scene.");
            CanvasGroup skillTreeGroup = gameUI.SkillTreeCanvasGroup;
            Require(skillTreeGroup != null, "Skill tree has no CanvasGroup visibility controller.");
            Require(skillTreeGroup != null && Mathf.Approximately(skillTreeGroup.alpha, 0),
                "Closed skill tree should be visually hidden.");
            Require(skillTreeGroup != null && !skillTreeGroup.blocksRaycasts,
                "Closed skill tree should not block pointer input.");
            Require(gameUI.transform.Find("SkillTree_ClosedHint") != null,
                "Closed skill tree has no L-key hint.");
            Require(gameUI.SkillTreeOverlay != null &&
                    gameUI.SkillTreeOverlay.transform.Find("SkillTree_Frame/Inner/SkillTree_Header") != null,
                "Open skill tree has no header or close prompt.");
            Require(gameUI.SkillTreeOverlay != null &&
                    gameUI.SkillTreeOverlay.transform.Find("SkillTree_Frame") != null,
                "Skill tree has no background frame.");
            Require(gameUI.SkillTreeOverlay != null &&
                    gameUI.SkillTreeOverlay.transform.Find("SkillTree_Frame/Inner/Cathedral_Background") != null,
                "Skill tree did not load its illustrated cathedral background.");
            Require(gameUI.SkillTreeOverlay != null &&
                    gameUI.SkillTreeOverlay.transform.Find("SkillTree_Frame/Inner/NodeLegend") != null,
                "Skill tree does not use its lower area for node guidance.");

            gameUI.OpenSkillTree();
            Require(gameUI.IsSkillTreeOpen, "Skill tree did not open.");
            Require(Mathf.Approximately(Time.timeScale, 0), "Opening the skill tree did not pause gameplay.");
            Require(skillTreeGroup != null && skillTreeGroup.blocksRaycasts,
                "Open skill tree did not enable pointer input.");
            Require(gameUI.TryCloseSkillTree(), "Open skill tree did not consume the close request.");
            Require(!gameUI.IsSkillTreeOpen, "Skill tree did not close.");
            Require(Mathf.Approximately(Time.timeScale, 1), "Closing the skill tree did not resume gameplay.");
            Require(skillTreeGroup != null && !skillTreeGroup.blocksRaycasts,
                "Closing the skill tree did not release pointer input.");
            Require(!gameUI.TryCloseSkillTree(), "Closed skill tree incorrectly consumed another close request.");
        }

        foreach (AudioCue cue in System.Enum.GetValues(typeof(AudioCue)))
            Require(AudioManager.IsCueAvailable(cue), $"Audio cue has no imported clip: {cue}");

        ValidateStateReentry();

        if (flow != null)
        {
            flow.PauseCombat();
            Require(Mathf.Approximately(Time.timeScale, 0), "Pause did not stop game time.");
            flow.ResumeCombat();
            Require(Mathf.Approximately(Time.timeScale, 1), "Resume did not restore game time.");
        }

        if (enemies.Length > 0)
        {
            int pointsBeforeKills = gameUI != null && gameUI.skillTree != null
                ? gameUI.skillTree.CurrentSkillPoints
                : 0;
            int expectedEnemyRewards = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                Enemy enemyController = enemies[i].GetComponent<Enemy>();
                if (enemyController != null) expectedEnemyRewards += enemyController.SkillPointReward;
                enemies[i].ReduceHealth(enemies[i].MaxHealth + 1);
                Require(enemies[i].isDead, $"Lethal damage did not kill enemy {i + 1}.");
                if (i < enemies.Length - 1)
                    Require(Mathf.Approximately(Time.timeScale, 1),
                        "Combat ended before every level enemy was defeated.");
            }
            Require(Mathf.Approximately(Time.timeScale, 0), "Defeating the final enemy did not finish combat.");
            if (gameUI != null && gameUI.skillTree != null)
                Require(gameUI.skillTree.CurrentSkillPoints == pointsBeforeKills + expectedEnemyRewards,
                    "Enemy deaths did not grant the configured skill-point rewards.");
        }

        Time.timeScale = 1;
    }

    private static void FinishPlayMode()
    {
        if (phase == TestPhase.Finished) return;
        phase = TestPhase.Finished;
        Time.timeScale = 1;
        EditorApplication.ExitPlaymode();
    }

    /// <summary>玩家、输入、战斗组件与三段连招所需引用。</summary>
    private static void ValidatePlayerModule(Player player)
    {
        if (player == null) return;

        Require(player.anim != null, "Player Animator did not initialize.");
        Require(player.rb != null, "Player Rigidbody2D did not initialize.");
        Require(player.stats != null, "Player stats did not initialize.");
        Require(player.health != null && player.health.CurrentHealth > 0, "Player health is not playable.");
        Require(player.GetComponent<Player_Combat>() != null, "Player combat component is missing.");
        Require(player.statusHandler != null, "Player status-effect handler is missing.");
        Require(player.vfx != null, "Player VFX component is missing.");
        Require(player.input != null && player.input.Player.enabled, "Player input map is not enabled.");
        Require(player.attackVelocity != null && player.attackVelocity.Length >= 3,
            "Player combo has fewer than three configured attack stages.");
        Require(player.GetAttackImpactForComboIndex(1) == AttackImpact.Normal &&
                player.GetAttackImpactForComboIndex(2) == AttackImpact.Normal &&
                player.GetAttackImpactForComboIndex(3) == AttackImpact.Heavy,
            "Basic combo impact mapping must keep stages 1/2 normal and stage 3 heavy.");
        Require(player.health.NormalKnockbackPower.magnitude < player.health.HeavyKnockbackPower.magnitude,
            "Normal attack knockback is not weaker than the combo finisher.");
        Require(player.health.NormalKnockbackPower.x <= 2f && player.health.NormalKnockbackPower.y <= 1.5f,
            "Normal attack knockback is too strong for the basic combo.");

        Require(player.idleState != null && player.moveState != null && player.jumpState != null &&
                player.fallState != null && player.wallSlideState != null && player.wallJumpState != null &&
                player.dashState != null && player.basicAttackState != null && player.jumpAttackState != null &&
                player.counterAttackState != null && player.swordThrowState != null && player.deadState != null,
            "One or more player states failed to initialize.");
    }

    /// <summary>敌人生命、状态机状态与战斗依赖。</summary>
    private static void ValidateEnemyModule(Enemy_Health enemyHealth)
    {
        if (enemyHealth == null) return;

        Enemy enemy = enemyHealth.GetComponent<Enemy>();
        Require(enemy != null, "Enemy entity component is missing.");
        if (enemy == null) return;

        Require(enemy.anim != null && enemy.rb != null && enemy.stats != null,
            "Enemy core components did not initialize.");
        Require(enemy.health == enemyHealth && enemyHealth.CurrentHealth > 0,
            "Enemy health is not playable.");
        Require(enemy.GetComponent<Entity_Combat>() != null, "Enemy combat component is missing.");
        Require(enemy.GetComponent<Entity_StatusHandler>() != null, "Enemy status-effect handler is missing.");
        Require(!string.IsNullOrWhiteSpace(enemy.DisplayName) && enemy.SkillPointReward > 0,
            "Enemy archetype has no display name or skill-point reward.");
        if (enemy.Archetype != EnemyArchetype.BoneSoldier)
            Require(enemy.anim.runtimeAnimatorController is AnimatorOverrideController,
                $"{enemy.DisplayName} is still using the skeleton artwork instead of its own animation override.");

        SpriteRenderer body = enemy.anim.GetComponent<SpriteRenderer>();
        Require(body != null && body.color == Color.white,
            $"{enemy.DisplayName} uses body tint as identity, which conflicts with elemental status readability.");
        Entity_VFX vfx = enemy.GetComponent<Entity_VFX>();
        if (vfx != null && body != null)
        {
            vfx.PlayOnStatusVfx(0.25f, ElementType.Fire);
            Require(enemy.GetComponentInChildren<StatusEffectPresenter>() != null,
                $"{enemy.DisplayName} did not create an independent elemental status indicator.");
            Require(body.color == Color.white,
                $"{enemy.DisplayName} elemental status overwrote its identity artwork color.");
            vfx.StopAllVfx();
        }
        Require(enemy.idleState != null && enemy.moveState != null && enemy.attackState != null &&
                enemy.battleState != null && enemy.stunnedState != null && enemy.deadState != null,
            "One or more enemy states failed to initialize.");
    }

    /// <summary>摄像机、视差、事件系统等场景级依赖。</summary>
    private static void ValidateSceneModule()
    {
        Require(Camera.main != null, "Main camera is missing or not tagged MainCamera.");
        Require(GameObject.Find("CinemachineCamera") != null, "Cinemachine camera is missing.");
        ParallaxBackground parallax = Object.FindAnyObjectByType<ParallaxBackground>();
        Require(parallax != null, "Parallax background is missing.");
        if (parallax != null && Camera.main != null)
        {
            Vector3 originalCameraPosition = Camera.main.transform.position;
            Camera.main.transform.position = originalCameraPosition + Vector3.up * 12f;
            parallax.SyncToCameraNow();
            Require(parallax.PrimaryLayerCoversCameraVertically(),
                "Sky background no longer covers the camera after an upward jump.");
            Camera.main.transform.position = originalCameraPosition;
            parallax.SyncToCameraNow();
        }
        Require(Object.FindAnyObjectByType<EventSystem>() != null, "UI EventSystem is missing.");
    }

    /// <summary>技能组件、技能树数据以及默认解锁写入。</summary>
    private static void ValidateSkillModule(Player player, UI gameUI)
    {
        if (player == null) return;

        Player_SkillManager manager = player.skillManager;
        Require(manager != null, "Player skill manager did not initialize.");
        if (manager == null) return;

        Require(manager.dash != null, "Dash skill component is missing.");
        Require(manager.shard != null, "Shard skill component is missing.");
        Require(manager.swordThrow != null, "Sword throw skill component is missing.");
        Require(manager.dash == null || manager.dash.damageScaleData != null, "Dash damage data did not initialize.");
        Require(manager.shard == null || manager.shard.damageScaleData != null, "Shard damage data did not initialize.");
        Require(manager.swordThrow == null || manager.swordThrow.damageScaleData != null,
            "Sword throw damage data did not initialize.");

        if (gameUI == null || gameUI.skillTree == null) return;
        Require(gameUI.skillTree.skillManager == manager, "Skill tree is not wired to the player skill manager.");
        Require(gameUI.skillTree.CurrentSkillPoints == 1,
            "The demo should start with exactly one spendable skill point.");

        UI_TreeNode[] nodes = gameUI.skillTree.GetComponentsInChildren<UI_TreeNode>(true);
        Require(nodes.Length > 0, "Skill tree contains no skill nodes.");
        bool foundDefaultNode = false;
        foreach (UI_TreeNode node in nodes)
        {
            Require(node.skillData != null, $"Skill node '{node.name}' has no Skill_DataSO.");
            if (node.skillData == null || !node.skillData.unlockedByDefault) continue;
            foundDefaultNode = true;
            Require(node.isUnlocked, $"Default skill '{node.skillData.displayName}' was not unlocked at startup.");
        }
        Require(foundDefaultNode, "Skill tree has no default unlocked node.");
    }

    /// <summary>属性表完整性与关键数值范围。</summary>
    private static void ValidateStatModule(Player player)
    {
        if (player == null || player.stats == null) return;

        Entity_Stats stats = player.stats;
        Require(stats.GetMaxHealth() > 0, "Player maximum health is not positive.");
        Require(stats.GetArmorMitigation(0) >= 0 && stats.GetArmorMitigation(0) <= 0.85f,
            "Armor mitigation is outside its designed range.");
        Require(stats.GetEvasion() >= 0 && stats.GetEvasion() <= 85,
            "Evasion is outside its designed range.");

        foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            Require(stats.GetStatByType(statType) != null, $"Stat lookup is not implemented: {statType}");
    }

    /// <summary>验证全地图分布、三种敌人种族和每类敌人的击杀/承伤节奏。</summary>
    private static void ValidateLevelBalance(Player player, Enemy_Health[] enemies)
    {
        if (player == null || player.stats == null || player.health == null || enemies.Length == 0) return;

        HashSet<EnemyArchetype> archetypes = new HashSet<EnemyArchetype>();
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        int westCount = 0;
        int centerCount = 0;
        int eastCount = 0;
        int upperCount = 0;
        int middleCount = 0;
        int lowerHallCount = 0;
        foreach (Enemy_Health enemyHealth in enemies)
        {
            Enemy enemy = enemyHealth.GetComponent<Enemy>();
            if (enemy != null) archetypes.Add(enemy.Archetype);

            Vector3 position = enemyHealth.transform.position;
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxY = Mathf.Max(maxY, position.y);

            if (position.x < 0f) westCount++;
            else if (position.x <= 32f) centerCount++;
            else eastCount++;

            if (position.y > 3f) upperCount++;
            else if (position.y < -20f) lowerHallCount++;
            else middleCount++;
        }
        Require(archetypes.Count == LevelEncounterBuilder.ExpectedArchetypeCount,
            "Level must contain Bone Soldier, Night Sorrow and Underworld Brute.");
        Require(minX >= -18f,
            $"An enemy is inside the left spike boundary: minimum X is {minX:0.##}.");
        Require(maxX - minX >= 70f,
            $"Enemy layout does not cover the map width: X span is only {maxX - minX:0.##}.");
        Require(maxY - minY >= 25f,
            $"Enemy layout does not cover the map height: Y span is only {maxY - minY:0.##}.");
        Require(westCount >= 1 && centerCount >= 5 && eastCount >= 3,
            $"Enemy regions are unbalanced: west={westCount}, center={centerCount}, east={eastCount}.");
        Require(upperCount >= 2 && middleCount >= 5 && lowerHallCount >= 2,
            $"Enemy height tiers are unbalanced: upper={upperCount}, middle={middleCount}, lower={lowerHallCount}.");
        Require(player.transform.position.x >= -18f && player.transform.position.x <= -13f &&
                player.transform.position.y > -2f,
            "Player should start on the safe west platform, outside the left spike boundary.");

        int groundMask = LayerMask.GetMask("Ground");
        foreach (Enemy_Health enemyHealth in enemies)
        {
            RaycastHit2D ground = Physics2D.Raycast(enemyHealth.transform.position, Vector2.down, 4f, groundMask);
            Require(ground.collider != null && ground.normal.y > 0.45f,
                $"Enemy '{enemyHealth.name}' is not placed over a walkable ground surface.");
        }

        float playerRawDamage = player.stats.offense.damage.GetValue() + player.stats.major.strength.GetValue();
        foreach (Enemy_Health enemyHealth in enemies)
        {
            Enemy enemy = enemyHealth.GetComponent<Enemy>();
            Entity_Stats enemyStats = enemyHealth.GetComponent<Entity_Stats>();
            Require(enemy != null && enemyStats != null, "Enemy stats are missing for balance validation.");
            if (enemy == null || enemyStats == null) continue;

            float playerHitDamage = playerRawDamage *
                                    (1 - enemyStats.GetArmorMitigation(player.stats.GetArmorReduction()));
            int hitsToKillEnemy = Mathf.CeilToInt(enemyHealth.MaxHealth / playerHitDamage);

            float enemyRawDamage = enemyStats.offense.damage.GetValue() + enemyStats.major.strength.GetValue();
            float enemyHitDamage = enemyRawDamage *
                                   (1 - player.stats.GetArmorMitigation(enemyStats.GetArmorReduction()));
            int hitsToKillPlayer = Mathf.CeilToInt(player.health.MaxHealth / enemyHitDamage);

            int expectedEnemyMin = enemy.Archetype == EnemyArchetype.UnderworldBrute ? 6 : 4;
            int expectedEnemyMax = enemy.Archetype == EnemyArchetype.UnderworldBrute ? 8 : 6;
            Require(hitsToKillEnemy >= expectedEnemyMin && hitsToKillEnemy <= expectedEnemyMax,
                $"{enemy.DisplayName} durability is outside target: {hitsToKillEnemy} hits.");
            Require(hitsToKillPlayer >= 7 && hitsToKillPlayer <= 12,
                $"Player survivability against {enemy.DisplayName} is outside target: {hitsToKillPlayer} hits.");
        }
    }

    /// <summary>验证左侧地刺是边界触发器，单次固定扣除玩家 20% 最大生命。</summary>
    private static void ValidateHazardModule(Player player)
    {
        Object_Spikes[] spikes = Object.FindObjectsByType<Object_Spikes>(FindObjectsSortMode.None);
        Require(spikes.Length == LevelEncounterBuilder.ExpectedSpikeHazardCount,
            $"Expected {LevelEncounterBuilder.ExpectedSpikeHazardCount} spike hazard, found {spikes.Length}.");
        if (spikes.Length == 0 || player == null || player.health == null) return;

        Object_Spikes hazard = spikes[0];
        Require(Mathf.Approximately(hazard.DamagePercent, 0.2f),
            "Spike boundary damage is not configured to 20% max health.");
        BoxCollider2D trigger = hazard.GetComponent<BoxCollider2D>();
        Require(trigger != null && trigger.isTrigger,
            "Spike boundary is missing its trigger collider.");

        player.health.SetHealthToPercent(1f);
        float healthBefore = player.health.CurrentHealth;
        Require(hazard.TryDamage(player.health), "Spike hazard did not damage the player.");
        float expectedHealth = healthBefore - player.health.MaxHealth * 0.2f;
        Require(Mathf.Abs(player.health.CurrentHealth - expectedHealth) < 0.01f,
            $"Spike damage should remove 20% max health; remaining={player.health.CurrentHealth:0.##}.");
        Require(!hazard.TryDamage(player.health),
            "Spike hazard ignored its contact cooldown and damaged twice immediately.");
        player.health.SetHealthToPercent(1f);
    }

    /// <summary>验证初始 Buff、宝箱掉落和技能点奖励形成可玩的奖励闭环。</summary>
    private static void ValidateRewardModule(Player player, UI gameUI)
    {
        Object_Chest[] chests = Object.FindObjectsByType<Object_Chest>(FindObjectsSortMode.None);
        Object_Buff[] buffs = Object.FindObjectsByType<Object_Buff>(FindObjectsSortMode.None);
        Require(chests.Length == LevelEncounterBuilder.ExpectedChestCount,
            $"Expected {LevelEncounterBuilder.ExpectedChestCount} reward chest, found {chests.Length}.");
        Require(buffs.Length == LevelEncounterBuilder.ExpectedPlacedBuffCount,
            $"Expected {LevelEncounterBuilder.ExpectedPlacedBuffCount} placed buff, found {buffs.Length}.");
        if (chests.Length == 0 || player == null || gameUI == null || gameUI.skillTree == null) return;

        Object_Chest chest = chests[0];
        Require(chest.RewardBuffPrefab != null && chest.SkillPointReward == 2,
            "Reward chest is not configured with its buff and two skill points.");

        int pointsBefore = gameUI.skillTree.CurrentSkillPoints;
        Require(chest.TakeDamage(1, 0, ElementType.None, player.transform),
            "The reward chest could not be opened by a player hit.");
        Require(chest.IsOpened, "Chest did not enter its opened state.");
        Require(gameUI.skillTree.CurrentSkillPoints == pointsBefore + chest.SkillPointReward,
            "Opening the chest did not grant skill points.");
        Require(Object.FindObjectsByType<Object_Buff>(FindObjectsSortMode.None).Length == buffs.Length + 1,
            "Opening the chest did not spawn its buff reward.");
        Require(!chest.TakeDamage(1, 0, ElementType.None, player.transform),
            "An opened chest granted its reward more than once.");
    }

    private static void ValidateStateReentry()
    {
        StateMachine machine = new StateMachine();
        ReentryProbeState state = new ReentryProbeState(machine);
        machine.Init(state);

        Require(machine.ChangeState(state) == false,
            "ChangeState should reject an accidental transition to the current state.");
        Require(machine.ReenterState(state), "ReenterState rejected the active state.");
        Require(state.enterCount == 2 && state.exitCount == 1,
            "ReenterState did not execute one Exit/Enter cycle.");
    }

    private sealed class ReentryProbeState : EntityState
    {
        public int enterCount;
        public int exitCount;

        public ReentryProbeState(StateMachine machine) : base(machine, string.Empty)
        {
        }

        public override void Enter() => enterCount++;
        public override void Exit() => exitCount++;
    }

    private static void Require(bool condition, string message)
    {
        if (condition) return;
        SessionState.SetBool(HasFailedKey, true);
        Debug.LogError($"[CombatSmoke] {message}");
    }

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        SessionState.SetBool(HasFailedKey, true);
    }

    private static void Complete()
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        Application.logMessageReceived -= HandleLog;

        bool failed = SessionState.GetBool(HasFailedKey, false);
        SessionState.EraseBool(IsRunningKey);
        SessionState.EraseBool(HasFailedKey);

        if (failed) Debug.LogError("[CombatSmoke] FAILED");
        else Debug.Log("[CombatSmoke] PASSED");

        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }
}
#endif
