#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>按整张地图的可行走表面分区布置遭遇，并把宝箱、Buff、技能点纳入奖励循环。</summary>
public static class LevelEncounterBuilder
{
    public const int ExpectedEnemyCount = 10;
    public const int ExpectedChestCount = 1;
    public const int ExpectedPlacedBuffCount = 1;
    public const int ExpectedSpikeHazardCount = 1;
    public const int ExpectedArchetypeCount = 3;
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string EnemyPrefabPath = "Assets/Prefabs/Enemy_Skeleton.prefab";
    private const string ChestPrefabPath = "Assets/Prefabs/LevelObjects/Object_Chest.prefab";
    private const string BuffPrefabPath = "Assets/Prefabs/LevelObjects/Object_Buff.prefab";
    private const string EncounterRootName = "EncounterLayout_重点掌握";
    private const float EnemyRootOffsetFromSurface = 1.58f;

    private readonly struct SpawnPoint
    {
        public readonly string name;
        public readonly Vector3 position;
        public readonly EnemyArchetype archetype;

        public SpawnPoint(string name, EnemyArchetype archetype, float x, float surfaceY)
        {
            this.name = name;
            this.archetype = archetype;
            position = new Vector3(x, surfaceY + EnemyRootOffsetFromSurface, -0.136f);
        }
    }

    private static readonly SpawnPoint[] SpawnPoints =
    {
        // Ground Tilemap 实测边界：X=-41..66，Y=-31..5；但 X=-41..-19 是左侧地刺边界，不属于可玩区域。
        // 参数最后一项是平台表面 worldY，不再直接填写容易出错的角色根节点 Y。
        // 可玩入口从 X=-18 开始；玩家出生点与第一场教学战都避开左侧地刺。
        new SpawnPoint("Enemy_01_BoneSoldier_WestBridge", EnemyArchetype.BoneSoldier, -8.5f, -2f),
        // 中央低台与上层小平台各放一只，形成上下错落而非同层排队。
        new SpawnPoint("Enemy_02_NightSorrow_CentralLower", EnemyArchetype.NightSorrow, 6.5f, -4f),
        new SpawnPoint("Enemy_03_BoneSoldier_CentralUpper", EnemyArchetype.BoneSoldier, 9.5f, 3f),
        // 上层屋顶：使用慢速重型敌人和快速敌人形成节奏差异。
        new SpawnPoint("Enemy_04_UnderworldBrute_CentralRoof", EnemyArchetype.UnderworldBrute, 21f, 4f),
        new SpawnPoint("Enemy_05_NightSorrow_EastRoof", EnemyArchetype.NightSorrow, 41.5f, 5f),
        // 中层下降路线：左右两段平台各承担一场遭遇。
        new SpawnPoint("Enemy_06_NightSorrow_DescentWest", EnemyArchetype.NightSorrow, 18.5f, -7f),
        new SpawnPoint("Enemy_07_UnderworldBrute_DescentEast", EnemyArchetype.UnderworldBrute, 41.5f, -7f),
        // 最底层大厅保留两只，而不是把全关敌人都塞进这里。
        new SpawnPoint("Enemy_08_BoneSoldier_LowerHallWest", EnemyArchetype.BoneSoldier, 7.5f, -23f),
        new SpawnPoint("Enemy_09_UnderworldBrute_LowerHallEast", EnemyArchetype.UnderworldBrute, 24.5f, -23f),
        // 最右侧出口平台作为收尾遭遇。
        new SpawnPoint("Enemy_10_NightSorrow_EastExit", EnemyArchetype.NightSorrow, 63.5f, -6f)
    };

    [MenuItem("Tools/Devil/Rebuild Level Encounters")]
    public static void Build()
    {
        EnemyVisualAssetBuilder.Build();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        GameObject chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChestPrefabPath);
        GameObject buffPrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(BuffPrefabPath);
        if (enemyPrefab == null)
            throw new System.InvalidOperationException($"Enemy prefab not found: {EnemyPrefabPath}");
        if (chestPrefab == null || buffPrefabObject == null)
            throw new System.InvalidOperationException("Chest or buff prefab is missing from Assets/Prefabs/LevelObjects.");

        Object_Buff buffPrefab = buffPrefabObject.GetComponent<Object_Buff>();
        if (buffPrefab == null)
            throw new System.InvalidOperationException("Object_Buff prefab has no Object_Buff component.");

        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            Object.DestroyImmediate(enemy.gameObject);
        foreach (Object_Chest chest in Object.FindObjectsByType<Object_Chest>(FindObjectsSortMode.None))
            Object.DestroyImmediate(chest.gameObject);
        foreach (Object_Buff buff in Object.FindObjectsByType<Object_Buff>(FindObjectsSortMode.None))
            Object.DestroyImmediate(buff.gameObject);

        GameObject oldRoot = GameObject.Find(EncounterRootName);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        GameObject root = new GameObject(EncounterRootName);

        GameObject spikeHazard = new GameObject("Hazard_LeftBoundarySpikes");
        spikeHazard.transform.SetParent(root.transform, false);
        spikeHazard.transform.position = new Vector3(-30f, -4.55f, 0f);
        BoxCollider2D spikeTrigger = spikeHazard.AddComponent<BoxCollider2D>();
        spikeTrigger.isTrigger = true;
        spikeTrigger.size = new Vector2(22f, 1.2f);
        Object_Spikes spikes = spikeHazard.AddComponent<Object_Spikes>();
        spikes.Configure(0.2f, 1f, new Vector2(9f, 7f), 0.25f);

        foreach (SpawnPoint spawn in SpawnPoints)
        {
            GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
            enemy.name = spawn.name;
            enemy.transform.SetParent(root.transform, true);
            enemy.transform.position = spawn.position;
            Enemy enemyController = enemy.GetComponent<Enemy>();
            enemyController.ConfigureArchetype(spawn.archetype);
            EditorUtility.SetDirty(enemyController);
        }

        GameObject placedBuff = (GameObject)PrefabUtility.InstantiatePrefab(buffPrefabObject, scene);
        placedBuff.name = "Buff_01_Tempest_CentralRoute";
        placedBuff.transform.SetParent(root.transform, true);
        placedBuff.transform.position = new Vector3(2.5f, -3.42f, -0.2f);

        GameObject chestObject = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab, scene);
        chestObject.name = "Chest_01_EastRoofReward";
        chestObject.transform.SetParent(root.transform, true);
        chestObject.transform.position = new Vector3(46f, 5.15f, -0.15f);
        Object_Chest rewardChest = chestObject.GetComponent<Object_Chest>();
        rewardChest.ConfigureReward(buffPrefab, 2);
        EditorUtility.SetDirty(rewardChest);

        Player player = Object.FindAnyObjectByType<Player>();
        if (player != null)
            player.transform.position = new Vector3(-15.5f, 0.37f, -1f);

        UI_SkillTree skillTree = Object.FindAnyObjectByType<UI_SkillTree>();
        if (skillTree != null)
        {
            skillTree.ConfigureInitialSkillPoints(1);
            EditorUtility.SetDirty(skillTree);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LevelEncounterBuilder] Built {SpawnPoints.Length} enemies across west, roof, descent, lower-hall and east-exit zones; 3 species, 1 chest, 1 placed buff and a 20% spike boundary.");
    }
}
#endif
