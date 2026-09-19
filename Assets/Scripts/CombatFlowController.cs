using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 单场景战斗闭环：显示目标、统计敌人，并提供暂停、胜负结算与重开。
/// 无需场景手工挂载；包含 Player 的场景载入后会自动创建。
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class CombatFlowController : MonoBehaviour
{
    private enum CombatState
    {
        Running,
        Paused,
        Victory,
        Defeat
    }

    private readonly List<Entity_Health> trackedEnemies = new List<Entity_Health>();

    private Player player;
    private UI gameUI;
    private UI_SkillTree skillTree;
    private CombatState state = CombatState.Running;
    private int totalEnemies;
    private int defeatedEnemies;

    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI rewardText;
    private GameObject rewardBackground;
    private Coroutine rewardRoutine;
    private GameObject modalPanel;
    private TextMeshProUGUI modalTitle;
    private TextMeshProUGUI modalMessage;
    private GameObject resumeButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoadedForBootstrap;
        SceneManager.sceneLoaded += HandleSceneLoadedForBootstrap;
        EnsureControllerForActiveScene();
    }

    private static void HandleSceneLoadedForBootstrap(Scene _, LoadSceneMode __)
    {
        EnsureControllerForActiveScene();
    }

    private static void EnsureControllerForActiveScene()
    {
        if (FindAnyObjectByType<Player>() == null || FindAnyObjectByType<CombatFlowController>() != null) return;

        GameObject controller = new GameObject(nameof(CombatFlowController));
        controller.AddComponent<CombatFlowController>();
    }

    private void Start()
    {
        Time.timeScale = 1;
        player = FindAnyObjectByType<Player>();
        gameUI = FindAnyObjectByType<UI>();

        if (player == null)
        {
            enabled = false;
            return;
        }

        if (player.health != null) player.health.OnDied += HandlePlayerDied;

        Enemy_Health[] enemies = FindObjectsByType<Enemy_Health>(FindObjectsSortMode.None);
        foreach (Enemy_Health enemyHealth in enemies)
        {
            if (enemyHealth == null || enemyHealth.isDead) continue;
            trackedEnemies.Add(enemyHealth);
            enemyHealth.OnDied += HandleEnemyDied;
        }

        totalEnemies = trackedEnemies.Count;

        AudioManager.PlayMusic(AudioCue.GameplayMusic);
        BuildInterface();
        skillTree = gameUI != null ? gameUI.skillTree : FindAnyObjectByType<UI_SkillTree>();
        if (skillTree != null) skillTree.SkillPointsChanged += HandleSkillPointsChanged;
        Object_Buff.OnBuffPickedUp += HandleBuffPickedUp;
        UpdateObjective();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            // Esc 优先关闭技能树，避免一次按键同时关技能树又打开暂停菜单。
            if (state == CombatState.Running && gameUI != null && gameUI.TryCloseSkillTree()) return;
            if (state == CombatState.Running) PauseCombat();
            else if (state == CombatState.Paused) ResumeCombat();
        }

        if (keyboard.mKey.wasPressedThisFrame)
            AudioManager.SetMuted(!AudioManager.IsMuted);

        if ((state == CombatState.Victory || state == CombatState.Defeat) &&
            (keyboard.rKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            RestartCombat();
        }
    }

    private void HandlePlayerDied(Entity_Health _)
    {
        if (state != CombatState.Running) return;
        FinishCombat(CombatState.Defeat, "战斗失败", "你倒在了战斗中。观察敌人的节奏，再试一次。");
    }

    private void HandleEnemyDied(Entity_Health enemyHealth)
    {
        if (state != CombatState.Running || trackedEnemies.Remove(enemyHealth) == false) return;

        enemyHealth.OnDied -= HandleEnemyDied;
        defeatedEnemies++;
        Enemy defeatedEnemy = enemyHealth.GetComponent<Enemy>();
        if (skillTree != null && defeatedEnemy != null)
            skillTree.AddSkillPoints(defeatedEnemy.SkillPointReward, defeatedEnemy.DisplayName);
        UpdateObjective();

        if (trackedEnemies.Count == 0)
            FinishCombat(CombatState.Victory, "战斗胜利", "所有敌人均已击败，战斗试炼完成。");
    }

    private void FinishCombat(CombatState result, string title, string message)
    {
        state = result;
        Time.timeScale = 0;
        AudioManager.PlaySfx(result == CombatState.Victory ? AudioCue.Victory : AudioCue.Defeat);

        if (objectiveText != null)
            objectiveText.text = result == CombatState.Victory ? "区域已肃清" : "玩家已阵亡";

        ShowModal(title, message, false);
    }

    public void PauseCombat()
    {
        if (state != CombatState.Running) return;
        gameUI?.TryCloseSkillTree();
        state = CombatState.Paused;
        Time.timeScale = 0;
        AudioManager.PlaySfx(AudioCue.MenuOpen);
        ShowModal("游戏暂停", "调整状态后，继续投入战斗。", true);
    }

    public void ResumeCombat()
    {
        if (state != CombatState.Paused) return;
        state = CombatState.Running;
        Time.timeScale = 1;
        AudioManager.PlaySfx(AudioCue.MenuClose);
        if (modalPanel != null) modalPanel.SetActive(false);
    }

    public void RestartCombat()
    {
        Time.timeScale = 1;
        AudioManager.PlaySfx(AudioCue.UiClick);
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0) SceneManager.LoadScene(activeScene.buildIndex);
        else SceneManager.LoadScene(activeScene.name);
    }

    private void UpdateObjective()
    {
        if (objectiveText == null) return;

        objectiveText.text = totalEnemies > 0
            ? $"击败全部敌人   {defeatedEnemies} / {totalEnemies}"
            : "战斗训练场   当前没有敌人";
    }

    private void HandleSkillPointsChanged(int current, int delta, string source)
    {
        if (delta == 0 || rewardText == null) return;

        if (delta > 0)
            ShowReward($"技能点 +{delta}   ·   {source}", new Color(1f, 0.78f, 0.3f));
        else
            ShowReward($"已解锁   ·   {source}   （剩余 {current} 点）", new Color(0.46f, 0.9f, 1f));
    }

    private void HandleBuffPickedUp(string buffName, float duration)
    {
        ShowReward($"获得增益   ·   {buffName}   持续 {duration:0} 秒",
            new Color(0.54f, 1f, 0.66f));
    }

    private void ShowReward(string message, Color color)
    {
        if (rewardText == null) return;
        if (rewardRoutine != null) StopCoroutine(rewardRoutine);
        rewardRoutine = StartCoroutine(ShowRewardCo(message, color));
    }

    private System.Collections.IEnumerator ShowRewardCo(string message, Color color)
    {
        rewardText.text = message;
        rewardText.color = color;
        rewardText.gameObject.SetActive(true);
        if (rewardBackground != null) rewardBackground.SetActive(true);
        yield return new WaitForSecondsRealtime(2.25f);
        rewardText.gameObject.SetActive(false);
        if (rewardBackground != null) rewardBackground.SetActive(false);
        rewardRoutine = null;
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("CombatFlow_UI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        objectiveText = CreateText(canvasObject.transform, "Objective", 26, TextAlignmentOptions.Center);
        SetRect(objectiveText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(680, 64), new Vector2(0, -42));
        AddBackground(objectiveText.gameObject, new Color(0.035f, 0.045f, 0.07f, 0.88f));

        rewardText = CreateText(canvasObject.transform, "RewardToast", 20, TextAlignmentOptions.Center);
        rewardText.fontStyle = FontStyles.Bold;
        SetRect(rewardText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(620, 46), new Vector2(0, -98));
        rewardBackground = AddBackground(rewardText.gameObject, new Color(0.035f, 0.045f, 0.07f, 0.84f));
        rewardText.gameObject.SetActive(false);
        rewardBackground.SetActive(false);

        TextMeshProUGUI controls = CreateText(canvasObject.transform, "Controls", 18, TextAlignmentOptions.Left);
        controls.text = "A/D 移动   空格 跳跃   鼠标左键 攻击   鼠标右键 瞄准\nShift 冲刺   Q 弹反   F 时光碎片   L 技能树   Esc 关闭/暂停   M 静音";
        controls.color = new Color(0.82f, 0.86f, 0.95f, 0.82f);
        SetRect(controls.rectTransform, Vector2.zero, Vector2.zero, new Vector2(760, 70), new Vector2(28, 28), Vector2.zero);
        AddBackground(controls.gameObject, new Color(0.018f, 0.025f, 0.045f, 0.82f));

        modalPanel = new GameObject("CombatResult", typeof(RectTransform), typeof(Image));
        modalPanel.transform.SetParent(canvasObject.transform, false);
        SetRect(modalPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(650, 560), Vector2.zero);
        modalPanel.GetComponent<Image>().color = new Color(0.025f, 0.03f, 0.055f, 0.96f);

        modalTitle = CreateText(modalPanel.transform, "Title", 56, TextAlignmentOptions.Center);
        modalTitle.fontStyle = FontStyles.Bold;
        modalTitle.color = new Color(0.95f, 0.72f, 0.25f);
        SetRect(modalTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(560, 80), new Vector2(0, 205));

        modalMessage = CreateText(modalPanel.transform, "Message", 24, TextAlignmentOptions.Center);
        SetRect(modalMessage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(540, 72), new Vector2(0, 130));

        CreateVolumeSlider(modalPanel.transform, "Master", "主音量", AudioManager.MasterVolume, 60,
            AudioManager.SetMasterVolume);
        CreateVolumeSlider(modalPanel.transform, "Music", "音乐", AudioManager.MusicVolume, 10,
            AudioManager.SetMusicVolume);
        CreateVolumeSlider(modalPanel.transform, "Sfx", "音效", AudioManager.SfxVolume, -40,
            AudioManager.SetSfxVolume);

        resumeButton = CreateButton(modalPanel.transform, "Resume", "继续游戏", new Vector2(-145, -145), ResumeCombat);
        CreateButton(modalPanel.transform, "Restart", "重新开始", new Vector2(145, -145), RestartCombat);

        TextMeshProUGUI shortcut = CreateText(modalPanel.transform, "Shortcut", 16, TextAlignmentOptions.Center);
        shortcut.text = "Esc：继续游戏   |   战斗结束后按 R / 回车重新开始";
        shortcut.color = new Color(0.7f, 0.74f, 0.82f);
        SetRect(shortcut.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(520, 30), new Vector2(0, -235));

        modalPanel.SetActive(false);
        EnsureEventSystem();
    }

    private void ShowModal(string title, string message, bool canResume)
    {
        if (modalPanel == null) return;
        modalTitle.text = title;
        modalMessage.text = message;
        resumeButton.SetActive(canResume);
        modalPanel.SetActive(true);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, float size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        ChineseFontProvider.Configure(text);
        return text;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Vector2 position,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(230, 62), position);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.23f, 0.36f, 1);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.4f, 0.62f, 1);
        colors.pressedColor = new Color(0.12f, 0.16f, 0.26f, 1);
        button.colors = colors;

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", 22, TextAlignmentOptions.Center);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return buttonObject;
    }

    private static void CreateVolumeSlider(Transform parent, string name, string label, float initialValue,
        float y, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        TextMeshProUGUI labelText = CreateText(parent, $"{name}_Label", 18, TextAlignmentOptions.Left);
        labelText.text = label;
        SetRect(labelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(105, 34), new Vector2(-220, y));

        GameObject sliderObject = new GameObject($"{name}_Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        SetRect(sliderRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(330, 28), new Vector2(20, y));

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderObject.transform, false);
        SetRect(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        background.GetComponent<Image>().color = new Color(0.1f, 0.13f, 0.2f, 1);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(sliderObject.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4, 4);
        fillRect.offsetMax = new Vector2(-4, -4);
        fill.GetComponent<Image>().color = new Color(0.28f, 0.68f, 0.95f, 1);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(sliderObject.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        SetRect(handleRect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 34), Vector2.zero);
        handle.GetComponent<Image>().color = Color.white;

        TextMeshProUGUI valueText = CreateText(parent, $"{name}_Value", 17, TextAlignmentOptions.Center);
        SetRect(valueText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(65, 34), new Vector2(235, y));

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.wholeNumbers = false;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.value = Mathf.Clamp01(initialValue);
        valueText.text = $"{Mathf.RoundToInt(slider.value * 100)}%";
        slider.onValueChanged.AddListener(value =>
        {
            valueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            onValueChanged?.Invoke(value);
        });
    }

    private static GameObject AddBackground(GameObject textObject, Color color)
    {
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        GameObject backgroundObject = new GameObject($"{textObject.name}_Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(textObject.transform.parent, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = textRect.anchorMin;
        backgroundRect.anchorMax = textRect.anchorMax;
        backgroundRect.pivot = textRect.pivot;
        backgroundRect.sizeDelta = textRect.sizeDelta;
        backgroundRect.anchoredPosition = textRect.anchoredPosition;
        backgroundObject.transform.SetSiblingIndex(textObject.transform.GetSiblingIndex());

        Image image = backgroundObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return backgroundObject;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size,
        Vector2 position, Vector2? pivot = null)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private void OnDestroy()
    {
        Time.timeScale = 1;
        if (skillTree != null) skillTree.SkillPointsChanged -= HandleSkillPointsChanged;
        Object_Buff.OnBuffPickedUp -= HandleBuffPickedUp;
        if (player != null && player.health != null) player.health.OnDied -= HandlePlayerDied;
        foreach (Entity_Health enemyHealth in trackedEnemies)
        {
            if (enemyHealth != null) enemyHealth.OnDied -= HandleEnemyDied;
        }
    }
}
