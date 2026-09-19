using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主 UI：统一管理技能树的暂停、显示层级、动效与操作提示。
/// </summary>
public class UI : MonoBehaviour
{
    private const float TransitionDuration = 0.18f;

    public UI_SkillToolTip skillToolTip;
    public UI_SkillTree skillTree;

    private Canvas rootCanvas;
    private CanvasGroup overlayCanvasGroup;
    private RectTransform skillTreeRect;
    private GameObject skillTreeOverlay;
    private GameObject closedHint;
    private TextMeshProUGUI closedHintText;
    private TextMeshProUGUI skillPointText;
    private Coroutine transitionRoutine;
    private Coroutine hintRoutine;
    private Vector3 openedTreeScale = Vector3.one;
    private bool ownsGameplayPause;
    private float timeScaleBeforeOpen = 1;

    /// <summary>技能树当前是否打开。</summary>
    public bool IsSkillTreeOpen { get; private set; }
    /// <summary>用于 UI 回归测试读取真正控制整层可见性的 CanvasGroup。</summary>
    public CanvasGroup SkillTreeCanvasGroup => overlayCanvasGroup;
    /// <summary>运行时生成的全屏技能树层。</summary>
    public GameObject SkillTreeOverlay => skillTreeOverlay;

    private void Awake()
    {
        skillToolTip = GetComponentInChildren<UI_SkillToolTip>(true);
        skillTree = GetComponentInChildren<UI_SkillTree>(true);
        rootCanvas = GetComponentInParent<Canvas>();

        if (skillTree == null || rootCanvas == null) return;

        // 节点依赖 Awake/Start 执行默认解锁，所以让业务对象保持 active，
        // 仅通过上层 CanvasGroup 关闭视觉与射线。
        skillTree.gameObject.SetActive(true);
        skillTreeRect = skillTree.GetComponent<RectTransform>();
        BuildSkillTreeChrome();
        ChineseFontProvider.ApplyTo(rootCanvas.transform);
        skillTree.SkillPointsChanged += HandleSkillPointsChanged;
        HandleSkillPointsChanged(skillTree.CurrentSkillPoints, 0, "INITIAL");
        ApplyVisibility(false, true);
    }

    private void OnDestroy()
    {
        if (skillTree != null) skillTree.SkillPointsChanged -= HandleSkillPointsChanged;
        RestoreGameplayTime();
    }

    public void ToggleSkillTreeUI()
    {
        SetSkillTreeVisible(!IsSkillTreeOpen);
    }

    public void OpenSkillTree()
    {
        SetSkillTreeVisible(true);
    }

    public void CloseSkillTree()
    {
        SetSkillTreeVisible(false);
    }

    /// <summary>关闭已打开的技能树，返回值表示 Esc 是否已被消费。</summary>
    public bool TryCloseSkillTree()
    {
        if (!IsSkillTreeOpen) return false;
        CloseSkillTree();
        return true;
    }

    private void SetSkillTreeVisible(bool visible)
    {
        if (skillTree == null || overlayCanvasGroup == null || IsSkillTreeOpen == visible) return;

        // 暂停菜单、胜负结算已经占用暂停状态时，不允许再叠加技能树。
        if (visible && Time.timeScale <= 0) return;

        IsSkillTreeOpen = visible;
        if (visible) PauseGameplay();
        else RestoreGameplayTime();

        AudioManager.PlaySfx(visible ? AudioCue.MenuOpen : AudioCue.MenuClose);
        if (skillToolTip != null) skillToolTip.ShowToolTip(false, null);

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(AnimateVisibility(visible));

        if (visible)
        {
            if (hintRoutine != null) StopCoroutine(hintRoutine);
            if (closedHint != null) closedHint.SetActive(false);
        }
        else
        {
            ShowClosedHintFeedback();
        }
    }

    private void PauseGameplay()
    {
        if (ownsGameplayPause) return;
        timeScaleBeforeOpen = Time.timeScale;
        ownsGameplayPause = timeScaleBeforeOpen > 0;
        if (ownsGameplayPause) Time.timeScale = 0;
    }

    private void RestoreGameplayTime()
    {
        if (!ownsGameplayPause) return;
        Time.timeScale = timeScaleBeforeOpen;
        ownsGameplayPause = false;
    }

    private IEnumerator AnimateVisibility(bool visible)
    {
        float startAlpha = overlayCanvasGroup.alpha;
        Vector3 startScale = skillTreeRect.localScale;
        Vector3 targetScale = visible ? openedTreeScale : openedTreeScale * 0.97f;

        overlayCanvasGroup.interactable = visible;
        overlayCanvasGroup.blocksRaycasts = visible;

        float elapsed = 0;
        while (elapsed < TransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / TransitionDuration);
            t = 1 - Mathf.Pow(1 - t, 3);
            overlayCanvasGroup.alpha = Mathf.Lerp(startAlpha, visible ? 1 : 0, t);
            skillTreeRect.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        ApplyVisibility(visible, false);
        transitionRoutine = null;
    }

    private void ApplyVisibility(bool visible, bool initialize)
    {
        IsSkillTreeOpen = visible;
        overlayCanvasGroup.alpha = visible ? 1 : 0;
        overlayCanvasGroup.interactable = visible;
        overlayCanvasGroup.blocksRaycasts = visible;
        skillTreeRect.localScale = visible ? openedTreeScale : openedTreeScale * 0.97f;

        if (closedHint == null) return;
        closedHint.SetActive(!visible);
        if (initialize && closedHintText != null)
            RefreshClosedHint();
    }

    private void ShowClosedHintFeedback()
    {
        if (closedHint == null || closedHintText == null) return;
        closedHint.SetActive(true);
        closedHintText.text = $"技能树已关闭   ·   剩余技能点 {skillTree.CurrentSkillPoints}";
        if (hintRoutine != null) StopCoroutine(hintRoutine);
        hintRoutine = StartCoroutine(RestoreClosedHint());
    }

    private IEnumerator RestoreClosedHint()
    {
        yield return new WaitForSecondsRealtime(1.35f);
        if (!IsSkillTreeOpen && closedHintText != null) RefreshClosedHint();
        hintRoutine = null;
    }

    private void HandleSkillPointsChanged(int current, int _, string __)
    {
        if (skillPointText != null) skillPointText.text = $"可用技能点  {current}";
        if (!IsSkillTreeOpen) RefreshClosedHint();
    }

    private void RefreshClosedHint()
    {
        if (closedHintText != null && skillTree != null)
            closedHintText.text = $"技能点 {skillTree.CurrentSkillPoints}     [L] 打开技能树";
    }

    /// <summary>创建完整的技能树视觉层，不把运行时装饰写进庞大的场景 YAML。</summary>
    private void BuildSkillTreeChrome()
    {
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = 110;

        Transform canvasTransform = rootCanvas.transform;
        skillTreeOverlay = new GameObject("SkillTree_Overlay", typeof(RectTransform), typeof(CanvasGroup));
        skillTreeOverlay.transform.SetParent(canvasTransform, false);
        Stretch(skillTreeOverlay.GetComponent<RectTransform>(), 0, 0, 0, 0);
        skillTreeOverlay.transform.SetAsFirstSibling();
        overlayCanvasGroup = skillTreeOverlay.GetComponent<CanvasGroup>();

        GameObject backdrop = CreatePanel(skillTreeOverlay.transform, "Backdrop",
            new Color(0.005f, 0.012f, 0.025f, 0.92f));
        Stretch(backdrop.GetComponent<RectTransform>(), 0, 0, 0, 0);

        GameObject frame = CreatePanel(skillTreeOverlay.transform, "SkillTree_Frame",
            new Color(0.46f, 0.56f, 0.64f, 0.82f));
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.025f, 0.04f);
        frameRect.anchorMax = new Vector2(0.975f, 0.94f);
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        GameObject frameInner = CreatePanel(frame.transform, "Inner",
            new Color(0.012f, 0.02f, 0.034f, 1f));
        Stretch(frameInner.GetComponent<RectTransform>(), 3, 3, 3, 3);

        Texture backgroundTexture = Resources.Load<Texture>("UI/SkillTree/skill_tree_cathedral");
        if (backgroundTexture != null)
        {
            GameObject art = CreateRawImage(frameInner.transform, "Cathedral_Background", backgroundTexture,
                new Color(0.72f, 0.79f, 0.86f, 0.82f));
            Stretch(art.GetComponent<RectTransform>(), 0, 0, 0, 0);
        }

        GameObject readabilityVeil = CreatePanel(frameInner.transform, "Readability_Veil",
            new Color(0.004f, 0.01f, 0.02f, 0.32f));
        Stretch(readabilityVeil.GetComponent<RectTransform>(), 0, 0, 0, 0);

        GameObject topGlow = CreatePanel(frameInner.transform, "TopGlow",
            new Color(0.82f, 0.62f, 0.3f, 0.52f));
        SetRect(topGlow.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 2), Vector2.zero, new Vector2(0.5f, 1));

        // 四个既有分支的水平中心经过实测，不改节点连线，只把整树放进视觉框并给底栏让位。
        skillTree.transform.SetParent(frameInner.transform, false);
        skillTreeRect.anchorMin = skillTreeRect.anchorMax = new Vector2(0.5f, 0.5f);
        skillTreeRect.anchoredPosition = new Vector2(373f, 38f);
        openedTreeScale = Vector3.one * 0.94f;
        skillTreeRect.localScale = openedTreeScale;

        GameObject header = CreatePanel(frameInner.transform, "SkillTree_Header",
            new Color(0.01f, 0.018f, 0.03f, 0.92f));
        SetRect(header.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 82), new Vector2(0, -10), new Vector2(0.5f, 1));

        GameObject headerAccent = CreatePanel(header.transform, "Accent",
            new Color(0.95f, 0.65f, 0.18f, 1));
        SetRect(headerAccent.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(5, 0), Vector2.zero, new Vector2(0, 0.5f));

        TextMeshProUGUI title = CreateText(header.transform, "Title", 28, TextAlignmentOptions.Left);
        title.text = "技能树";
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.98f, 0.76f, 0.3f);
        SetRect(title.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(240, 46), new Vector2(28, 8), new Vector2(0, 0.5f));

        TextMeshProUGUI subtitle = CreateText(header.transform, "Subtitle", 14, TextAlignmentOptions.Left);
        subtitle.text = "选择成长路线 · 查看节点联系 · 分配技能点";
        subtitle.color = new Color(0.54f, 0.68f, 0.82f);
        SetRect(subtitle.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(360, 28), new Vector2(29, -21), new Vector2(0, 0.5f));

        TextMeshProUGUI closePrompt = CreateText(header.transform, "ClosePrompt", 16, TextAlignmentOptions.Right);
        closePrompt.text = "[L / ESC]  关闭";
        closePrompt.color = new Color(0.74f, 0.84f, 0.95f);
        SetRect(closePrompt.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(220, 42), new Vector2(-70, 0), new Vector2(1, 0.5f));

        GameObject pointBadge = CreatePanel(header.transform, "SkillPointBadge",
            new Color(0.13f, 0.09f, 0.04f, 1f));
        SetRect(pointBadge.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(190, 42), new Vector2(-286, 0), new Vector2(1, 0.5f));
        skillPointText = CreateText(pointBadge.transform, "Value", 16, TextAlignmentOptions.Center);
        skillPointText.fontStyle = FontStyles.Bold;
        skillPointText.color = new Color(1f, 0.78f, 0.3f);
        Stretch(skillPointText.rectTransform, 8, 4, 8, 4);

        GameObject closeButtonObject = CreateButtonSurface(header.transform, "CloseButton",
            new Color(0.14f, 0.21f, 0.33f, 1));
        SetRect(closeButtonObject.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(46, 46), new Vector2(-14, 0), new Vector2(1, 0.5f));
        Button closeButton = closeButtonObject.GetComponent<Button>();
        closeButton.onClick.AddListener(CloseSkillTree);

        TextMeshProUGUI closeLabel = CreateText(closeButtonObject.transform, "Label", 24, TextAlignmentOptions.Center);
        closeLabel.text = "×";
        closeLabel.fontStyle = FontStyles.Bold;
        Stretch(closeLabel.rectTransform, 0, 0, 0, 2);

        CreateFooterCard(frameInner.transform, "EarnSkillPoints", new Vector2(0.18f, 0),
            "获取技能点", "普通敌人  +1     冥界蛮兵  +2\n古老宝箱  +2", new Color(0.94f, 0.66f, 0.28f));
        CreateFooterCard(frameInner.transform, "NodeLegend", new Vector2(0.5f, 0),
            "节点状态", "金色  可解锁     青色  已学习\n暗色  前置条件未满足", new Color(0.34f, 0.76f, 0.92f));
        CreateFooterCard(frameInner.transform, "PauseLegend", new Vector2(0.82f, 0),
            "战术暂停", "规划技能时战斗暂停\n按 [L / ESC] 返回战斗", new Color(0.72f, 0.79f, 0.9f));

        if (skillToolTip != null)
        {
            skillToolTip.transform.SetParent(skillTreeOverlay.transform, false);
            skillToolTip.transform.SetAsLastSibling();
        }

        closedHint = CreatePanel(canvasTransform, "SkillTree_ClosedHint",
            new Color(0.018f, 0.03f, 0.055f, 0.93f));
        SetRect(closedHint.GetComponent<RectTransform>(), Vector2.one, Vector2.one,
            new Vector2(350, 54), new Vector2(-28, -28), Vector2.one);

        GameObject hintAccent = CreatePanel(closedHint.transform, "Accent",
            new Color(0.25f, 0.78f, 1f, 1));
        SetRect(hintAccent.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(5, 0), Vector2.zero, new Vector2(0, 0.5f));

        closedHintText = CreateText(closedHint.transform, "Label", 17, TextAlignmentOptions.Center);
        closedHintText.fontStyle = FontStyles.Bold;
        closedHintText.color = new Color(0.86f, 0.93f, 1f);
        Stretch(closedHintText.rectTransform, 14, 6, 8, 6);
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

    private static GameObject CreateRawImage(Transform parent, string name, Texture texture, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(parent, false);
        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = texture;
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    private static void CreateFooterCard(Transform parent, string name, Vector2 anchor, string titleValue,
        string bodyValue, Color accentColor)
    {
        GameObject card = CreatePanel(parent, name, new Color(0.008f, 0.015f, 0.027f, 0.9f));
        SetRect(card.GetComponent<RectTransform>(), anchor, anchor,
            new Vector2(500, 108), new Vector2(0, 22), new Vector2(0.5f, 0));

        GameObject accent = CreatePanel(card.transform, "Accent", accentColor);
        SetRect(accent.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(4, 0), Vector2.zero, new Vector2(0, 0.5f));

        TextMeshProUGUI title = CreateText(card.transform, "Title", 16, TextAlignmentOptions.Left);
        title.text = titleValue;
        title.fontStyle = FontStyles.Bold;
        title.color = accentColor;
        SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(-32, 30), new Vector2(18, -8), new Vector2(0.5f, 1));

        TextMeshProUGUI body = CreateText(card.transform, "Body", 13, TextAlignmentOptions.Left);
        body.text = bodyValue;
        body.color = new Color(0.74f, 0.82f, 0.9f);
        body.textWrappingMode = TextWrappingModes.NoWrap;
        SetRect(body.rectTransform, Vector2.zero, Vector2.one,
            new Vector2(-32, -28), new Vector2(18, -8));
    }

    private static GameObject CreateButtonSurface(Transform parent, string name, Color color)
    {
        GameObject buttonObject = CreatePanel(parent, name, color);
        Image image = buttonObject.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.5f, 0.75f, 1f);
        colors.pressedColor = new Color(0.24f, 0.48f, 0.78f);
        button.colors = colors;
        return buttonObject;
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
}
