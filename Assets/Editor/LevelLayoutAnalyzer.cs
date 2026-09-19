#if UNITY_EDITOR
using System.Text;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>输出关卡可行走几何、角色位置与当前战斗数值，供布置遭遇使用。</summary>
public static class LevelLayoutAnalyzer
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string OverviewPath = "Logs/level-overview-distributed.png";

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Physics2D.SyncTransforms();

        StringBuilder report = new StringBuilder();
        report.AppendLine("[LevelLayout] BEGIN");

        Player player = Object.FindAnyObjectByType<Player>();
        if (player != null)
            report.AppendLine($"Player pos={Format(player.transform.position)}");

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        report.AppendLine($"Enemies count={enemies.Length}");
        foreach (Enemy enemy in enemies)
            report.AppendLine($"Enemy name={enemy.name} pos={Format(enemy.transform.position)}");

        foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            tilemap.CompressBounds();
            Bounds local = tilemap.localBounds;
            Bounds world = new Bounds(tilemap.transform.TransformPoint(local.center),
                Vector3.Scale(local.size, tilemap.transform.lossyScale));
            report.AppendLine($"Tilemap name={tilemap.name} cells={tilemap.cellBounds} world={Format(world)}");
            if (tilemap.name == "Ground") AppendExposedSurfaceRuns(report, tilemap);
        }

        foreach (Collider2D collider in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (!collider.enabled || collider.isTrigger) continue;
            if (collider.bounds.size.x < 1f) continue;
            report.AppendLine($"Collider name={collider.name} layer={LayerMask.LayerToName(collider.gameObject.layer)} " +
                              $"type={collider.GetType().Name} bounds={Format(collider.bounds)}");
        }

        UI_SkillTree skillTree = Object.FindFirstObjectByType<UI_SkillTree>(FindObjectsInactive.Include);
        if (skillTree != null)
        {
            RectTransform treeRect = skillTree.GetComponent<RectTransform>();
            report.AppendLine($"SkillTree pos={treeRect.anchoredPosition} size={treeRect.sizeDelta}");
            for (int i = 0; i < treeRect.childCount; i++)
            {
                RectTransform group = treeRect.GetChild(i) as RectTransform;
                if (group != null)
                    report.AppendLine($"SkillGroup name={group.name} pos={group.anchoredPosition} size={group.sizeDelta}");
            }
        }

        if (player != null && player.stats != null)
            AppendStats(report, "Player", player.stats);
        if (enemies.Length > 0 && enemies[0].stats != null)
            AppendStats(report, "Skeleton", enemies[0].stats);

        report.AppendLine("[LevelLayout] END");
        Debug.Log(report.ToString());
        EditorApplication.Exit(0);
    }

    /// <summary>从整张 Ground 边界渲染关卡总览，人工检查敌人是否仍集中在单一区域。</summary>
    public static void CaptureOverview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Tilemap ground = null;
        foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tilemap.name != "Ground") continue;
            tilemap.CompressBounds();
            ground = tilemap;
            break;
        }

        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (ground == null || camera == null)
            throw new System.InvalidOperationException("Ground Tilemap or scene Camera is missing.");

        Bounds local = ground.localBounds;
        Bounds world = new Bounds(ground.transform.TransformPoint(local.center),
            Vector3.Scale(local.size, ground.transform.lossyScale));

        Vector3 originalPosition = camera.transform.position;
        Quaternion originalRotation = camera.transform.rotation;
        bool originalOrthographic = camera.orthographic;
        float originalSize = camera.orthographicSize;
        RenderTexture originalTarget = camera.targetTexture;

        const int width = 2048;
        const int height = 1024;
        RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.transform.SetPositionAndRotation(new Vector3(world.center.x, world.center.y, -100f), Quaternion.identity);
            camera.orthographic = true;
            camera.aspect = (float)width / height;
            camera.orthographicSize = Mathf.Max(world.extents.y + 3f,
                (world.extents.x + 3f) / camera.aspect);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = Path.Combine(projectRoot, OverviewPath);
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            Debug.Log($"[LevelLayout] Captured full-map overview: {outputPath}");
        }
        finally
        {
            RenderTexture.active = null;
            camera.targetTexture = originalTarget;
            camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
            camera.orthographic = originalOrthographic;
            camera.orthographicSize = originalSize;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }

        EditorApplication.Exit(0);
    }

    /// <summary>按高度列出上方没有实体砖块的连续表面，避免仅凭截图猜测敌人落点。</summary>
    private static void AppendExposedSurfaceRuns(StringBuilder report, Tilemap tilemap)
    {
        BoundsInt bounds = tilemap.cellBounds;
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            List<Vector2Int> runs = new List<Vector2Int>();
            int runStart = int.MinValue;
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                bool isSurface = x < bounds.xMax && tilemap.HasTile(new Vector3Int(x, y, 0)) &&
                                 !tilemap.HasTile(new Vector3Int(x, y + 1, 0));
                if (isSurface && runStart == int.MinValue) runStart = x;
                if ((!isSurface || x == bounds.xMax) && runStart != int.MinValue)
                {
                    int end = x - 1;
                    if (end - runStart + 1 >= 2) runs.Add(new Vector2Int(runStart, end));
                    runStart = int.MinValue;
                }
            }

            if (runs.Count == 0) continue;
            report.Append($"Surface cellY={y} worldY={tilemap.CellToWorld(new Vector3Int(0, y + 1, 0)).y:0.##} runs=");
            for (int i = 0; i < runs.Count; i++)
            {
                if (i > 0) report.Append(", ");
                report.Append($"[{runs[i].x}..{runs[i].y}]");
            }
            report.AppendLine();
        }
    }

    private static void AppendStats(StringBuilder report, string label, Entity_Stats stats)
    {
        report.AppendLine($"Stats {label}: hp={stats.GetMaxHealth():0.##}, regen={stats.resources.healthRegen.GetValue():0.##}, " +
                          $"damage={stats.offense.damage.GetValue():0.##}, strength={stats.major.strength.GetValue():0.##}, " +
                          $"armor={stats.defense.armor.GetValue():0.##}, vitality={stats.major.vitality.GetValue():0.##}, " +
                          $"crit={stats.offense.critChance.GetValue() + stats.major.agility.GetValue() * 0.3f:0.##}%");
    }

    private static string Format(Vector3 value) => $"({value.x:0.##},{value.y:0.##},{value.z:0.##})";
    private static string Format(Bounds value) => $"center={Format(value.center)} size={Format(value.size)}";
}
#endif
