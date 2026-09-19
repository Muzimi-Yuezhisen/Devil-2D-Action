using System;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// 为运行时生成和场景中已有的 TMP 文本提供中文字体。
/// 优先从项目内置字体动态生成字形图集，避免中文显示为方框；系统字体仅作为后备。
/// </summary>
public static class ChineseFontProvider
{
    private static readonly string[] PreferredFamilies =
    {
        "Noto Sans SC",
        "Microsoft YaHei UI",
        "Microsoft YaHei",
        "DengXian",
        "SimHei"
    };

    private static TMP_FontAsset fontAsset;

    public static TMP_FontAsset FontAsset
    {
        get
        {
            EnsureInitialized();
            return fontAsset;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad() => EnsureInitialized();

    public static void Configure(TMP_Text text)
    {
        if (text == null) return;
        TMP_FontAsset chineseFont = FontAsset;
        if (chineseFont != null) text.font = chineseFont;
    }

    public static void ApplyTo(Transform root)
    {
        if (root == null) return;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            Configure(text);
    }

    private static void EnsureInitialized()
    {
        if (fontAsset != null) return;

        Font bundledFont = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        if (bundledFont != null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(bundledFont);
            FinalizeFontAsset("Noto Sans SC（项目内置）");
            return;
        }

        string[] installedFamilies = Font.GetOSInstalledFontNames();
        string family = PreferredFamilies.FirstOrDefault(preferred =>
            installedFamilies.Any(installed => string.Equals(installed, preferred, StringComparison.OrdinalIgnoreCase)));

        if (string.IsNullOrEmpty(family))
        {
            Debug.LogWarning("[ChineseFont] 未找到支持中文的系统字体，将使用 TMP 默认字体。");
            return;
        }

        fontAsset = TMP_FontAsset.CreateFontAsset(family, "Regular", 72);
        if (fontAsset == null)
        {
            Font dynamicFont = Font.CreateDynamicFontFromOSFont(family, 72);
            if (dynamicFont != null) fontAsset = TMP_FontAsset.CreateFontAsset(dynamicFont);
        }

        if (fontAsset == null)
        {
            Debug.LogWarning($"[ChineseFont] 无法从系统字体 {family} 创建 TMP 字体资源。");
            return;
        }

        FinalizeFontAsset(family);
    }

    private static void FinalizeFontAsset(string sourceName)
    {
        if (fontAsset == null)
        {
            Debug.LogWarning($"[ChineseFont] 无法从 {sourceName} 创建 TMP 字体资源。");
            return;
        }

        fontAsset.name = $"RuntimeChineseFont_{sourceName}";
        fontAsset.hideFlags = HideFlags.DontSave;
        PrewarmCommonCharacters();
        TMP_Settings.defaultFontAsset = fontAsset;
    }

    private static void PrewarmCommonCharacters()
    {
        TextAsset characterSet = Resources.Load<TextAsset>("Fonts/ChineseCommon3500");
        if (characterSet == null) return;

        string characters = new string(characterSet.text.Where(character => !char.IsWhiteSpace(character)).ToArray());
        if (characters.Length == 0) return;

        if (!fontAsset.TryAddCharacters(characters, out string missingCharacters) &&
            !string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogWarning($"[ChineseFont] 常用字集中有 {missingCharacters.Length} 个字形无法加入图集。");
        }
    }
}
