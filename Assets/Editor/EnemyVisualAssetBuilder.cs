#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 把 Open Duelyst 的 CC0 图集/帧描述转成 Unity Sprite、AnimationClip 与覆盖控制器。
/// 生成的控制器复用原骷髅状态机，因此 AI、受击、弹反和伤害判定仍走同一套逻辑。
/// </summary>
public static class EnemyVisualAssetBuilder
{
    private const string SourceRoot = "Assets/ThirdParty/OpenDuelyst";
    private const string OutputRoot = "Assets/Resources/EnemyVisuals";
    private const string BaseControllerPath = "Assets/Animations/AnimatorControllers/Enemy_Skeleton.controller";

    private readonly struct AtlasRegion
    {
        public readonly int x;
        public readonly int yFromTop;
        public readonly int width;
        public readonly int height;

        public AtlasRegion(int x, int yFromTop, int width, int height)
        {
            this.x = x;
            this.yFromTop = yFromTop;
            this.width = width;
            this.height = height;
        }

        public string SpriteName => $"Frame_{x}_{yFromTop}_{width}_{height}";
    }

    private sealed class ParsedAtlas
    {
        public readonly Dictionary<string, AtlasRegion> regions = new Dictionary<string, AtlasRegion>();
        public readonly Dictionary<string, List<string>> animations = new Dictionary<string, List<string>>();
        public readonly Dictionary<string, float> speeds = new Dictionary<string, float>();
    }

    [MenuItem("Tools/Devil/Rebuild Open Enemy Visuals")]
    public static void Build()
    {
        Directory.CreateDirectory(OutputRoot);
        BuildProfile("NightSorrow", "f4_nightsorrow", 32, new Vector2(0.5f, 0.38f));
        BuildProfile("UnderworldBrute", "f4_underworldbrute", 32, new Vector2(0.5f, 0.34f));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnemyVisualAssetBuilder] Built NightSorrow and UnderworldBrute CC0 animation overrides.");
    }

    private static void BuildProfile(string profileName, string sourceName, float pixelsPerUnit, Vector2 pivot)
    {
        string texturePath = $"{SourceRoot}/{sourceName}.png";
        string dataPath = $"{SourceRoot}/{sourceName}.tres.txt";
        TextAsset dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
        if (dataAsset == null) throw new InvalidOperationException($"Missing frame data: {dataPath}");

        ParsedAtlas parsed = Parse(dataAsset.text);
        ImportSprites(texturePath, parsed.regions.Values, pixelsPerUnit, pivot);
        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>().ToDictionary(sprite => sprite.name, sprite => sprite);

        RuntimeAnimatorController baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);
        if (baseController == null) throw new InvalidOperationException($"Missing base enemy controller: {BaseControllerPath}");

        Dictionary<string, AnimationClip> replacements = new Dictionary<string, AnimationClip>
        {
            ["Skeleton_idle"] = BuildClip(profileName, "Idle", parsed, sprites, "idle", true, null),
            ["Skeleton_move"] = BuildClip(profileName, "Move", parsed, sprites, "run", true, null),
            ["Skeleton_attack"] = BuildClip(profileName, "Attack", parsed, sprites, "attack", false,
                AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Skeleton/Skeleton_attack.anim")),
            ["Skeleton_stunned"] = BuildClip(profileName, "Stunned", parsed, sprites, "hit", true, null)
        };

        string overridePath = $"{OutputRoot}/{profileName}.overrideController";
        AnimatorOverrideController controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
        if (controller == null)
        {
            controller = new AnimatorOverrideController(baseController) { name = profileName };
            AssetDatabase.CreateAsset(controller, overridePath);
        }
        else
        {
            controller.runtimeAnimatorController = baseController;
        }

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        controller.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip original = overrides[i].Key;
            if (original != null && replacements.TryGetValue(original.name, out AnimationClip replacement))
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
        }
        controller.ApplyOverrides(overrides);
        EditorUtility.SetDirty(controller);
    }

    private static AnimationClip BuildClip(string profileName, string logicalName, ParsedAtlas parsed,
        Dictionary<string, Sprite> sprites, string sourceAnimation, bool loop, AnimationClip eventTemplate)
    {
        if (!parsed.animations.TryGetValue(sourceAnimation, out List<string> regionIds) || regionIds.Count == 0)
            throw new InvalidOperationException($"Animation '{sourceAnimation}' missing for {profileName}.");

        string directory = $"{OutputRoot}/{profileName}";
        Directory.CreateDirectory(directory);
        string clipPath = $"{directory}/{profileName}_{logicalName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = $"{profileName}_{logicalName}" };
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        float framesPerSecond = parsed.speeds.TryGetValue(sourceAnimation, out float sourceSpeed)
            ? Mathf.Clamp(sourceSpeed, 7f, 12f)
            : 9f;
        clip.frameRate = framesPerSecond;

        List<ObjectReferenceKeyframe> keyframes = new List<ObjectReferenceKeyframe>(regionIds.Count);
        for (int i = 0; i < regionIds.Count; i++)
        {
            AtlasRegion region = parsed.regions[regionIds[i]];
            if (!sprites.TryGetValue(region.SpriteName, out Sprite sprite))
                throw new InvalidOperationException($"Imported sprite missing: {region.SpriteName}");
            keyframes.Add(new ObjectReferenceKeyframe { time = i / framesPerSecond, value = sprite });
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());
        SetLoopTime(clip, loop);

        if (eventTemplate != null)
        {
            AnimationEvent[] sourceEvents = AnimationUtility.GetAnimationEvents(eventTemplate);
            float sourceLength = Mathf.Max(0.01f, eventTemplate.length);
            float targetLength = Mathf.Max(0.01f, (regionIds.Count - 1) / framesPerSecond);
            foreach (AnimationEvent animationEvent in sourceEvents)
                animationEvent.time = Mathf.Clamp(animationEvent.time / sourceLength * targetLength, 0, targetLength);
            AnimationUtility.SetAnimationEvents(clip, sourceEvents);
        }
        else
        {
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
        }

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void ImportSprites(string texturePath, IEnumerable<AtlasRegion> sourceRegions,
        float pixelsPerUnit, Vector2 pivot)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"Texture importer missing: {texturePath}");

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null) throw new InvalidOperationException($"Texture missing: {texturePath}");

        List<SpriteMetaData> metadata = new List<SpriteMetaData>();
        foreach (AtlasRegion region in sourceRegions.GroupBy(r => r.SpriteName).Select(group => group.First()))
        {
            metadata.Add(new SpriteMetaData
            {
                name = region.SpriteName,
                rect = new Rect(region.x, texture.height - region.yFromTop - region.height, region.width, region.height),
                alignment = (int)SpriteAlignment.Custom,
                pivot = pivot
            });
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
#pragma warning disable 0618
        importer.spritesheet = metadata.ToArray();
#pragma warning restore 0618
        importer.SaveAndReimport();
    }

    private static ParsedAtlas Parse(string source)
    {
        ParsedAtlas parsed = new ParsedAtlas();
        Regex regionRegex = new Regex(
            @"\[sub_resource type=""AtlasTexture"" id=""([^""]+)""\]\s*atlas\s*=.*?\s*region\s*=\s*Rect2\((\d+),\s*(\d+),\s*(\d+),\s*(\d+)\)",
            RegexOptions.Singleline);
        foreach (Match match in regionRegex.Matches(source))
        {
            parsed.regions[match.Groups[1].Value] = new AtlasRegion(
                int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value),
                int.Parse(match.Groups[4].Value), int.Parse(match.Groups[5].Value));
        }

        Regex animationRegex = new Regex(
            @"""frames""\s*:\s*\[(.*?)\],\s*""loop""\s*:\s*\d+,\s*""name""\s*:\s*&""([^""]+)"",\s*""speed""\s*:\s*([\d.]+)",
            RegexOptions.Singleline);
        Regex referenceRegex = new Regex(@"SubResource\(""([^""]+)""\)");
        foreach (Match animationMatch in animationRegex.Matches(source))
        {
            string name = animationMatch.Groups[2].Value;
            parsed.animations[name] = referenceRegex.Matches(animationMatch.Groups[1].Value)
                .Cast<Match>().Select(match => match.Groups[1].Value).ToList();
            parsed.speeds[name] = float.Parse(animationMatch.Groups[3].Value,
                System.Globalization.CultureInfo.InvariantCulture);
        }

        return parsed;
    }

    private static void SetLoopTime(AnimationClip clip, bool loop)
    {
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        SerializedProperty loopTime = settings?.FindPropertyRelative("m_LoopTime");
        if (loopTime != null) loopTime.boolValue = loop;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
