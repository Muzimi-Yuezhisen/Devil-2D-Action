using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>独立主菜单：开始游戏、音频设置与退出。</summary>
public sealed class MainMenuController : MonoBehaviour
{
    public const string SceneName = "MainMenu";
    public const string GameplaySceneName = "SampleScene";

    public GameObject MainPanel { get; private set; }
    public GameObject SettingsPanel { get; private set; }
    public Button StartButton { get; private set; }
    public Button SettingsButton { get; private set; }
    public Button ExitButton { get; private set; }
    public Camera MenuCamera { get; private set; }

    private TextMeshProUGUI muteButtonLabel;
    private Image fadeImage;
    private bool transitionStarted;

    private void Awake()
    {
        Time.timeScale = 1;
        EnsureRenderCamera();
        BuildInterface();
        EnsureEventSystem();
    }

    private void Start()
    {
        AudioManager.PlayMusic(AudioCue.MainMenuMusic, 0.6f);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
        if (SettingsPanel != null && SettingsPanel.activeSelf) CloseSettings();
    }

    public void StartGame()
    {
        if (transitionStarted) return;
        transitionStarted = true;
        AudioManager.PlaySfx(AudioCue.UiClick);
        StartCoroutine(LoadGameplayCo());
    }

    public void OpenSettings()
    {
        AudioManager.PlaySfx(AudioCue.MenuOpen);
        MainPanel.SetActive(false);
        SettingsPanel.SetActive(true);
        RefreshMuteLabel();
    }

    public void CloseSettings()
    {
        AudioManager.PlaySfx(AudioCue.MenuClose);
        SettingsPanel.SetActive(false);
        MainPanel.SetActive(true);
    }

    public void QuitGame()
    {
        AudioManager.PlaySfx(AudioCue.UiClick);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadGameplayCo()
    {
        fadeImage.raycastTarget = true;
        float elapsed = 0;
        const float duration = 0.38f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeImage.color = new Color(0.005f, 0.01f, 0.02f, t);
            yield return null;
        }

        SceneManager.LoadScene(GameplaySceneName);
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("MainMenu_UI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject background = CreatePanel(canvasObject.transform, "Background",
            new Color(0.027f, 0.009f, 0.018f, 1));
        Stretch(background.GetComponent<RectTransform>(), 0, 0, 0, 0);

        BuildBackgroundDecoration(background.transform);
        MainPanel = BuildMainPanel(canvasObject.transform);
        SettingsPanel = BuildSettingsPanel(canvasObject.transform);
        SettingsPanel.SetActive(false);

        GameObject fade = CreatePanel(canvasObject.transform, "Fade",
            new Color(0.012f, 0.003f, 0.008f, 0));
        Stretch(fade.GetComponent<RectTransform>(), 0, 0, 0, 0);
        fadeImage = fade.GetComponent<Image>();
        fadeImage.raycastTarget = false;
    }

    private void BuildBackgroundDecoration(Transform parent)
    {
        // 原创的暗红月夜与城堡剪影，借鉴哥特动作游戏的气氛，但不使用第三方画面。
        Color[] skyBands =
        {
            new Color(0.055f, 0.012f, 0.035f, 1),
            new Color(0.105f, 0.018f, 0.045f, 0.92f),
            new Color(0.16f, 0.025f, 0.055f, 0.75f),
            new Color(0.22f, 0.035f, 0.055f, 0.48f)
        };
        for (int i = 0; i < skyBands.Length; i++)
        {
            GameObject band = CreatePanel(parent, $"CrimsonSky_{i}", skyBands[i]);
            SetRect(band.GetComponent<RectTransform>(), new Vector2(0, 0.25f + i * 0.15f),
                new Vector2(1, 0.41f + i * 0.15f), Vector2.zero, Vector2.zero);
        }

        GameObject moonGlow = CreatePanel(parent, "BloodMoonGlow", new Color(0.62f, 0.04f, 0.12f, 0.14f));
        Image moonGlowImage = moonGlow.GetComponent<Image>();
        moonGlowImage.sprite = CreateMoonSprite();
        moonGlowImage.preserveAspect = true;
        SetRect(moonGlow.GetComponent<RectTransform>(), new Vector2(0.72f, 0.63f), new Vector2(0.72f, 0.63f),
            new Vector2(820, 820), Vector2.zero);

        GameObject moon = CreatePanel(parent, "BloodMoon", Color.white);
        Image moonImage = moon.GetComponent<Image>();
        moonImage.sprite = CreateMoonSprite();
        moonImage.preserveAspect = true;
        moonImage.color = new Color(0.95f, 0.18f, 0.24f, 0.88f);
        SetRect(moon.GetComponent<RectTransform>(), new Vector2(0.73f, 0.66f), new Vector2(0.73f, 0.66f),
            new Vector2(570, 570), Vector2.zero);

        BuildDistantCliffs(parent);
        BuildCastleSilhouette(parent);

        for (int i = 0; i < 9; i++)
        {
            float x = 190 + ((i * 173) % 720);
            float y = 180 + ((i * 97) % 360);
            BuildBat(parent, $"Bat_{i:00}", new Vector2(x, y), 0.6f + (i % 3) * 0.18f);
        }

        GameObject fog = CreatePanel(parent, "LowFog", new Color(0.48f, 0.07f, 0.1f, 0.15f));
        SetRect(fog.GetComponent<RectTransform>(), new Vector2(0, 0.12f), new Vector2(1, 0.3f),
            new Vector2(0, 0), new Vector2(0, 0));

        GameObject lowerShade = CreatePanel(parent, "ForegroundShade", new Color(0.015f, 0.006f, 0.012f, 0.88f));
        SetRect(lowerShade.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 0.14f),
            Vector2.zero, Vector2.zero);
    }

    private GameObject BuildMainPanel(Transform parent)
    {
        GameObject panel = new GameObject("MainPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>(), 0, 0, 0, 0);

        GameObject titleRule = CreatePanel(panel.transform, "TitleRule", new Color(0.78f, 0.53f, 0.24f, 1));
        SetRect(titleRule.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(72, 3), new Vector2(-582, 186));

        TextMeshProUGUI eyebrow = CreateText(panel.transform, "Eyebrow", 17, TextAlignmentOptions.Left);
        eyebrow.text = "暗黑幻想动作试玩";
        eyebrow.characterSpacing = 6;
        eyebrow.color = new Color(0.75f, 0.62f, 0.56f);
        SetRect(eyebrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(650, 34), new Vector2(-185, 188));

        TextMeshProUGUI title = CreateText(panel.transform, "Title", 108, TextAlignmentOptions.Left);
        title.text = "恶魔";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 3;
        title.color = new Color(0.96f, 0.9f, 0.83f);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(680, 132), new Vector2(-185, 98));

        TextMeshProUGUI subtitle = CreateText(panel.transform, "Subtitle", 24, TextAlignmentOptions.Left);
        subtitle.text = "血月之下";
        subtitle.characterSpacing = 5;
        subtitle.color = new Color(0.79f, 0.22f, 0.24f);
        SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(660, 42), new Vector2(-185, 22));

        GameObject menuCard = CreatePanel(panel.transform, "MenuCard", new Color(0.055f, 0.018f, 0.028f, 0.93f));
        SetRect(menuCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(500, 438), new Vector2(405, -18));

        GameObject cardAccent = CreatePanel(menuCard.transform, "Accent", new Color(0.64f, 0.11f, 0.14f, 0.95f));
        SetRect(cardAccent.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1), new Vector2(4, 0), Vector2.zero,
            new Vector2(0, 0.5f));

        TextMeshProUGUI menuLabel = CreateText(menuCard.transform, "MenuLabel", 15, TextAlignmentOptions.Left);
        menuLabel.text = "进入古堡";
        menuLabel.characterSpacing = 6;
        menuLabel.color = new Color(0.73f, 0.61f, 0.55f);
        SetRect(menuLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(320, 28), new Vector2(44, -38), new Vector2(0, 1));

        StartButton = CreateMenuButton(menuCard.transform, "StartGame", "一", "开始游戏", new Vector2(0, 72), StartGame);
        SettingsButton = CreateMenuButton(menuCard.transform, "Settings", "二", "设置", new Vector2(0, -20), OpenSettings);
        ExitButton = CreateMenuButton(menuCard.transform, "Exit", "三", "退出游戏", new Vector2(0, -112), QuitGame);

        TextMeshProUGUI footer = CreateText(panel.transform, "Footer", 15, TextAlignmentOptions.Left);
        footer.text = "UNITY 2D  ·  类银河恶魔城战斗演示";
        footer.color = new Color(0.46f, 0.34f, 0.34f);
        SetRect(footer.rectTransform, Vector2.zero, Vector2.zero,
            new Vector2(720, 34), new Vector2(48, 30), Vector2.zero);

        return panel;
    }

    private GameObject BuildSettingsPanel(Transform parent)
    {
        GameObject overlay = CreatePanel(parent, "SettingsPanel", new Color(0.02f, 0.004f, 0.012f, 0.9f));
        Stretch(overlay.GetComponent<RectTransform>(), 0, 0, 0, 0);

        GameObject card = CreatePanel(overlay.transform, "SettingsCard", new Color(0.07f, 0.02f, 0.03f, 0.98f));
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(700, 610), Vector2.zero);

        GameObject accent = CreatePanel(card.transform, "Accent", new Color(0.7f, 0.14f, 0.16f, 1));
        SetRect(accent.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 4), Vector2.zero, new Vector2(0.5f, 1));

        TextMeshProUGUI title = CreateText(card.transform, "Title", 42, TextAlignmentOptions.Left);
        title.text = "设置";
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.95f, 0.89f, 0.81f);
        SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(420, 64), new Vector2(52, -45), new Vector2(0, 1));

        TextMeshProUGUI hint = CreateText(card.transform, "Hint", 15, TextAlignmentOptions.Right);
        hint.text = "ESC  返回";
        hint.characterSpacing = 3;
        hint.color = new Color(0.72f, 0.58f, 0.53f);
        SetRect(hint.rectTransform, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(180, 42), new Vector2(-48, -52), new Vector2(1, 1));

        CreateVolumeSlider(card.transform, "Master", "主音量", AudioManager.MasterVolume, 110, AudioManager.SetMasterVolume);
        CreateVolumeSlider(card.transform, "Music", "音乐", AudioManager.MusicVolume, 15, AudioManager.SetMusicVolume);
        CreateVolumeSlider(card.transform, "Sfx", "音效", AudioManager.SfxVolume, -80, AudioManager.SetSfxVolume);

        Button muteButton = CreateMenuButton(card.transform, "Mute", "", "静音", new Vector2(-145, -205), ToggleMute, 270);
        muteButtonLabel = muteButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        RefreshMuteLabel();
        CreateMenuButton(card.transform, "Back", "", "返回", new Vector2(145, -205), CloseSettings, 270);

        return overlay;
    }

    private void ToggleMute()
    {
        AudioManager.SetMuted(!AudioManager.IsMuted);
        AudioManager.PlaySfx(AudioCue.UiClick);
        RefreshMuteLabel();
    }

    private void RefreshMuteLabel()
    {
        if (muteButtonLabel != null)
            muteButtonLabel.text = AudioManager.IsMuted ? "取消静音" : "静音";
    }

    private static Button CreateMenuButton(Transform parent, string name, string index, string label,
        Vector2 position, UnityEngine.Events.UnityAction onClick, float width = 420)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(width, 70), position);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.14f, 0.035f, 0.052f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.86f, 0.36f, 0.32f);
        colors.pressedColor = new Color(0.76f, 0.56f, 0.28f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        if (!string.IsNullOrEmpty(index))
        {
            TextMeshProUGUI indexText = CreateText(buttonObject.transform, "Index", 14, TextAlignmentOptions.Left);
            indexText.text = index;
            indexText.color = new Color(0.8f, 0.57f, 0.3f);
            SetRect(indexText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(38, 36), new Vector2(20, 0), new Vector2(0, 0.5f));
        }

        TextMeshProUGUI labelText = CreateText(buttonObject.transform, "Label", 21, TextAlignmentOptions.Left);
        labelText.text = label;
        labelText.fontStyle = FontStyles.Bold;
        labelText.characterSpacing = 2;
        Stretch(labelText.rectTransform, string.IsNullOrEmpty(index) ? 24 : 68, 0, 18, 0);
        return button;
    }

    private static void CreateVolumeSlider(Transform parent, string name, string label, float value, float y,
        UnityEngine.Events.UnityAction<float> onChanged)
    {
        TextMeshProUGUI labelText = CreateText(parent, name + "_Label", 17, TextAlignmentOptions.Left);
        labelText.text = label;
        labelText.characterSpacing = 3;
        labelText.color = new Color(0.8f, 0.7f, 0.64f);
        SetRect(labelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(150, 34), new Vector2(-220, y));

        GameObject sliderObject = new GameObject(name + "_Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        SetRect(sliderObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(350, 30), new Vector2(35, y));

        GameObject track = CreatePanel(sliderObject.transform, "Track", new Color(0.16f, 0.045f, 0.06f, 1));
        Stretch(track.GetComponent<RectTransform>(), 0, 8, 0, 8);

        GameObject fill = CreatePanel(sliderObject.transform, "Fill", new Color(0.7f, 0.13f, 0.16f, 1));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect, 3, 10, 3, 10);

        GameObject handle = CreatePanel(sliderObject.transform, "Handle", new Color(0.84f, 0.62f, 0.34f, 1));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        SetRect(handleRect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 36), Vector2.zero);

        TextMeshProUGUI valueText = CreateText(parent, name + "_Value", 17, TextAlignmentOptions.Right);
        SetRect(valueText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(72, 34), new Vector2(265, y));

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.value = Mathf.Clamp01(value);
        valueText.text = Mathf.RoundToInt(slider.value * 100) + "%";
        slider.onValueChanged.AddListener(newValue =>
        {
            valueText.text = Mathf.RoundToInt(newValue * 100) + "%";
            onChanged?.Invoke(newValue);
        });
    }

    private void EnsureRenderCamera()
    {
        MenuCamera = Camera.main;
        if (MenuCamera == null)
        {
            GameObject cameraObject = new GameObject("MainMenu_Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0, 0, -10);
            MenuCamera = cameraObject.GetComponent<Camera>();
            MenuCamera.orthographic = true;
            MenuCamera.orthographicSize = 5;
            MenuCamera.clearFlags = CameraClearFlags.SolidColor;
            MenuCamera.backgroundColor = new Color(0.025f, 0.007f, 0.015f);
            MenuCamera.cullingMask = 0;
        }

        if (FindAnyObjectByType<AudioListener>() == null)
            MenuCamera.gameObject.AddComponent<AudioListener>();
    }

    private static Sprite CreateMoonSprite()
    {
        const int size = 192;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime_BloodMoon",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2 - 1;
                float ny = (y + 0.5f) / size * 2 - 1;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = 1 - Mathf.SmoothStep(0.93f, 1f, distance);
                float mottling = Mathf.PerlinNoise(x * 0.055f, y * 0.055f) * 0.16f;
                float value = 0.84f + mottling;
                pixels[y * size + x] = new Color(value, value * 0.88f, value * 0.84f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }

    private static void BuildDistantCliffs(Transform parent)
    {
        Color far = new Color(0.075f, 0.018f, 0.037f, 0.98f);
        for (int i = 0; i < 7; i++)
        {
            GameObject cliff = CreatePanel(parent, $"DistantCliff_{i:00}", far);
            float width = 430 + i * 55;
            float height = 180 + (i % 3) * 90;
            float x = -900 + i * 330;
            SetRect(cliff.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(width, height), new Vector2(x, 90), new Vector2(0.5f, 0));
            cliff.transform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -13 : 11);
        }
    }

    private static void BuildCastleSilhouette(Transform parent)
    {
        Color stone = new Color(0.022f, 0.009f, 0.018f, 1);
        GameObject keep = CreatePanel(parent, "CastleKeep", stone);
        SetRect(keep.GetComponent<RectTransform>(), new Vector2(0.72f, 0), new Vector2(0.72f, 0),
            new Vector2(410, 310), new Vector2(0, 100), new Vector2(0.5f, 0));

        float[] offsets = { -245, -135, 115, 235 };
        float[] heights = { 430, 520, 480, 390 };
        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject tower = CreatePanel(parent, $"CastleTower_{i:00}", stone);
            SetRect(tower.GetComponent<RectTransform>(), new Vector2(0.72f, 0), new Vector2(0.72f, 0),
                new Vector2(100, heights[i]), new Vector2(offsets[i], 98), new Vector2(0.5f, 0));

            GameObject spire = CreatePanel(parent, $"CastleSpire_{i:00}", stone);
            SetRect(spire.GetComponent<RectTransform>(), new Vector2(0.72f, 0), new Vector2(0.72f, 0),
                new Vector2(105, 155), new Vector2(offsets[i], 98 + heights[i]), new Vector2(0.5f, 0));
            spire.transform.localRotation = Quaternion.Euler(0, 0, 45);

            for (int windowIndex = 0; windowIndex < 2; windowIndex++)
            {
                GameObject window = CreatePanel(parent, $"CastleWindow_{i:00}_{windowIndex}",
                    new Color(0.78f, 0.16f, 0.13f, 0.75f));
                SetRect(window.GetComponent<RectTransform>(), new Vector2(0.72f, 0), new Vector2(0.72f, 0),
                    new Vector2(13, 35), new Vector2(offsets[i], 185 + windowIndex * 92), new Vector2(0.5f, 0));
            }
        }
    }

    private static void BuildBat(Transform parent, string name, Vector2 position, float scale)
    {
        Color batColor = new Color(0.025f, 0.008f, 0.018f, 0.82f);
        GameObject leftWing = CreatePanel(parent, name + "_L", batColor);
        SetRect(leftWing.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(30 * scale, 6 * scale), position + Vector2.left * 7 * scale);
        leftWing.transform.localRotation = Quaternion.Euler(0, 0, 22);

        GameObject rightWing = CreatePanel(parent, name + "_R", batColor);
        SetRect(rightWing.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(30 * scale, 6 * scale), position + Vector2.right * 7 * scale);
        rightWing.transform.localRotation = Quaternion.Euler(0, 0, -22);
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return panel;
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

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size,
        Vector2 position, Vector2? pivot = null)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
