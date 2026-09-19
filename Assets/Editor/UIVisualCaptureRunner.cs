#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>生成主菜单和技能树截图，供人工检查间距、遮挡与视觉层级。</summary>
[InitializeOnLoad]
public static class UIVisualCaptureRunner
{
    private const string RunningKey = "Devil.UIVisualCapture.Running";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    private enum Phase
    {
        Menu,
        MenuCaptured,
        Settings,
        SettingsCaptured,
        LoadingGameplay,
        Gameplay,
        GameplayCaptured,
        EnemyShowcase,
        EnemyShowcaseCaptured,
        SkillTree,
        SkillTreeCaptured,
        Finished
    }

    private static Phase phase;
    private static double phaseStart;
    private static string logDirectory;

    static UIVisualCaptureRunner()
    {
        if (SessionState.GetBool(RunningKey, false)) Attach();
    }

    [MenuItem("Tools/Devil/Capture UI Review Screenshots")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        logDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
        Directory.CreateDirectory(logDirectory);
        SessionState.SetBool(RunningKey, true);
        Attach();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void Attach()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            logDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
            phase = Phase.Menu;
            phaseStart = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
        else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RunningKey, false))
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            SessionState.EraseBool(RunningKey);
            Debug.Log("[UIVisual] CAPTURED");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }

    private static void Tick()
    {
        double elapsed = EditorApplication.timeSinceStartup - phaseStart;

        if (phase == Phase.Menu && elapsed >= 2)
        {
            CaptureFrame("ui-main-menu.png");
            Next(Phase.MenuCaptured);
        }
        else if (phase == Phase.MenuCaptured && elapsed >= 1)
        {
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu == null) Finish();
            else
            {
                menu.OpenSettings();
                Next(Phase.Settings);
            }
        }
        else if (phase == Phase.Settings && elapsed >= 0.5f)
        {
            CaptureFrame("ui-settings.png");
            Next(Phase.SettingsCaptured);
        }
        else if (phase == Phase.SettingsCaptured && elapsed >= 1)
        {
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu == null) Finish();
            else
            {
                menu.CloseSettings();
                menu.StartGame();
                Next(Phase.LoadingGameplay);
            }
        }
        else if (phase == Phase.LoadingGameplay &&
                 SceneManager.GetActiveScene().name == MainMenuController.GameplaySceneName)
        {
            Next(Phase.Gameplay);
        }
        else if (phase == Phase.LoadingGameplay && elapsed > 8)
        {
            Finish();
        }
        else if (phase == Phase.Gameplay && elapsed >= 2)
        {
            CaptureFrame("ui-gameplay-layout.png");
            Next(Phase.GameplayCaptured);
        }
        else if (phase == Phase.GameplayCaptured && elapsed >= 1)
        {
            PrepareEnemyShowcase();
            Next(Phase.EnemyShowcase);
        }
        else if (phase == Phase.EnemyShowcase && elapsed >= 0.8f)
        {
            CaptureFrame("ui-enemy-status-showcase.png");
            Next(Phase.EnemyShowcaseCaptured);
        }
        else if (phase == Phase.EnemyShowcaseCaptured && elapsed >= 0.4f)
        {
            UI gameUI = Object.FindAnyObjectByType<UI>();
            if (gameUI == null) Finish();
            else
            {
                foreach (Entity_VFX vfx in Object.FindObjectsByType<Entity_VFX>(FindObjectsSortMode.None))
                    vfx.StopAllVfx();
                gameUI.OpenSkillTree();
                Next(Phase.SkillTree);
            }
        }
        else if (phase == Phase.SkillTree && elapsed >= 0.5f)
        {
            CaptureFrame("ui-skill-tree.png");
            Next(Phase.SkillTreeCaptured);
        }
        else if (phase == Phase.SkillTreeCaptured && elapsed >= 1)
        {
            Finish();
        }
    }

    private static void Next(Phase next)
    {
        phase = next;
        phaseStart = EditorApplication.timeSinceStartup;
    }

    /// <summary>把相机移到大厅中段，并给三种敌人施加不同状态，专门检查体型与状态辨识度。</summary>
    private static void PrepareEnemyShowcase()
    {
        Player player = Object.FindAnyObjectByType<Player>();
        if (player != null)
        {
            player.transform.position = new Vector3(18f, -22f, player.transform.position.z);
            if (player.rb != null)
                player.rb.linearVelocity = Vector2.zero;

            // 保留玩家作为相机目标，但隐藏其视觉，避免敌人展示互相遮挡。
            foreach (SpriteRenderer renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.enabled = false;

            foreach (Canvas canvas in player.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                    canvas.gameObject.SetActive(false);
            }
        }

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy night = null;
        Enemy brute = null;
        Enemy soldier = null;
        foreach (Enemy enemy in enemies)
        {
            if (enemy.Archetype == EnemyArchetype.NightSorrow && night == null) night = enemy;
            else if (enemy.Archetype == EnemyArchetype.UnderworldBrute && brute == null) brute = enemy;
            else if (enemy.Archetype == EnemyArchetype.BoneSoldier && soldier == null) soldier = enemy;
        }

        Enemy[] showcase = { soldier, night, brute };
        foreach (Enemy enemy in enemies)
        {
            bool isShowcaseEnemy = enemy == showcase[0] || enemy == showcase[1] || enemy == showcase[2];
            if (!isShowcaseEnemy)
            {
                enemy.gameObject.SetActive(false);
                continue;
            }

            // 截图阶段关闭 AI，动画与状态 VFX 仍正常播放。
            enemy.enabled = false;
            if (enemy.rb != null)
                enemy.rb.linearVelocity = Vector2.zero;
        }

        PlaceShowcaseEnemy(soldier, 12f);
        PlaceShowcaseEnemy(night, 18f);
        PlaceShowcaseEnemy(brute, 24f);

        night?.GetComponent<Entity_VFX>()?.PlayOnStatusVfx(5, ElementType.Fire);
        brute?.GetComponent<Entity_VFX>()?.PlayOnStatusVfx(5, ElementType.Ice);
        soldier?.GetComponent<Entity_VFX>()?.PlayOnStatusVfx(5, ElementType.Lightning);
    }

    private static void PlaceShowcaseEnemy(Enemy enemy, float x)
    {
        if (enemy == null)
            return;

        enemy.transform.position = new Vector3(x, -22f, enemy.transform.position.z);
        if (enemy.rb != null)
            enemy.rb.linearVelocity = Vector2.zero;
    }

    /// <summary>在 BatchMode 中把 Overlay Canvas 临时交给主相机，得到可重复的 1920x1080 截图。</summary>
    private static void CaptureFrame(string fileName)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError($"[UIVisual] Cannot capture {fileName}: no MainCamera.");
            return;
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        RenderMode[] originalModes = new RenderMode[canvases.Length];
        Camera[] originalCameras = new Camera[canvases.Length];
        float[] originalPlaneDistances = new float[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            originalModes[i] = canvases[i].renderMode;
            originalCameras[i] = canvases[i].worldCamera;
            originalPlaneDistances[i] = canvases[i].planeDistance;
            if (canvases[i].renderMode != RenderMode.ScreenSpaceOverlay) continue;
            canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = camera;
            canvases[i].planeDistance = 1f;
        }

        Canvas.ForceUpdateCanvases();
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        int previousCullingMask = camera.cullingMask;
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            camera.cullingMask = -1;
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(logDirectory, fileName), image.EncodeToPNG());
            Debug.Log($"[UIVisual] Captured {fileName}");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.cullingMask = previousCullingMask;
            RenderTexture.active = previousActive;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = originalModes[i];
                canvases[i].worldCamera = originalCameras[i];
                canvases[i].planeDistance = originalPlaneDistances[i];
            }
            Object.DestroyImmediate(image);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void Finish()
    {
        if (phase == Phase.Finished) return;
        phase = Phase.Finished;
        Time.timeScale = 1;
        EditorApplication.ExitPlaymode();
    }
}
#endif
