using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 世界空间异常状态提示：元素徽记、脚下光环与环绕粒子。
/// 身份精灵保持原色，避免“敌人种族色”和燃烧/冰冻/感电反馈互相覆盖。
/// </summary>
public sealed class StatusEffectPresenter : MonoBehaviour
{
    private static readonly Dictionary<ElementType, Sprite> IconSprites = new Dictionary<ElementType, Sprite>();
    private static Material sharedSpriteMaterial;

    private SpriteRenderer badge;
    private LineRenderer ring;
    private Vector3 badgeBaseScale;
    private Color effectColor;

    public static StatusEffectPresenter Create(Transform owner, SpriteRenderer body, ElementType element, Color color)
    {
        GameObject root = new GameObject($"StatusEffect_{element}");
        root.transform.SetParent(owner, false);
        StatusEffectPresenter presenter = root.AddComponent<StatusEffectPresenter>();
        presenter.Build(body, element, color);
        return presenter;
    }

    private void Build(SpriteRenderer body, ElementType element, Color color)
    {
        effectColor = color;
        CapsuleCollider2D capsule = GetComponentInParent<CapsuleCollider2D>();
        float badgeHeight = capsule == null ? 1.35f : capsule.offset.y + capsule.size.y * 0.5f + 0.38f;
        transform.localPosition = new Vector3(0, badgeHeight, -0.2f);

        GameObject badgeObject = new GameObject("ElementBadge", typeof(SpriteRenderer));
        badgeObject.transform.SetParent(transform, false);
        badge = badgeObject.GetComponent<SpriteRenderer>();
        badge.sprite = GetOrCreateIcon(element);
        badge.color = Color.white;
        badge.sortingLayerID = body.sortingLayerID;
        badge.sortingOrder = body.sortingOrder + 24;
        badgeBaseScale = Vector3.one * 0.46f;
        badgeObject.transform.localScale = badgeBaseScale;

        GameObject ringObject = new GameObject("GroundAura", typeof(LineRenderer));
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = new Vector3(0, -badgeHeight + 0.08f, 0);
        ring = ringObject.GetComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 32;
        ring.widthMultiplier = 0.035f;
        ring.material = GetSpriteMaterial();
        ring.sortingLayerID = body.sortingLayerID;
        ring.sortingOrder = body.sortingOrder + 2;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i / (float)ring.positionCount * Mathf.PI * 2;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.72f, Mathf.Sin(angle) * 0.16f, 0));
        }

        CreateMotes(body, color, badgeHeight);
        ApplyRingAlpha(0.6f);
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.08f;
        if (badge != null) badge.transform.localScale = badgeBaseScale * pulse;
        ApplyRingAlpha(0.42f + Mathf.Sin(Time.time * 5f) * 0.12f);
    }

    private void ApplyRingAlpha(float alpha)
    {
        if (ring == null) return;
        Color faded = effectColor;
        faded.a = alpha;
        ring.startColor = faded;
        ring.endColor = faded;
    }

    private void CreateMotes(SpriteRenderer body, Color color, float badgeHeight)
    {
        GameObject particleObject = new GameObject("ElementMotes", typeof(ParticleSystem));
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.localPosition = new Vector3(0, -badgeHeight + 0.75f, 0);
        ParticleSystem particles = particleObject.GetComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 1;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.28f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
        main.startColor = new ParticleSystem.MinMaxGradient(color * 0.75f, color);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 28;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 11;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.68f;
        shape.radiusThickness = 1;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetSpriteMaterial();
        renderer.sortingLayerID = body.sortingLayerID;
        renderer.sortingOrder = body.sortingOrder + 3;
        particles.Play();
    }

    private static Material GetSpriteMaterial()
    {
        if (sharedSpriteMaterial == null)
            sharedSpriteMaterial = new Material(Shader.Find("Sprites/Default"));
        return sharedSpriteMaterial;
    }

    private static Sprite GetOrCreateIcon(ElementType element)
    {
        if (IconSprites.TryGetValue(element, out Sprite existing)) return existing;

        const int size = 24;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = $"StatusIcon_{element}",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32[] pixels = new Color32[size * size];
        Color32 ink = element switch
        {
            ElementType.Ice => new Color32(113, 220, 255, 255),
            ElementType.Fire => new Color32(255, 104, 55, 255),
            ElementType.Lightning => new Color32(255, 224, 64, 255),
            _ => new Color32(235, 235, 235, 255)
        };
        Color32 plate = new Color32(5, 11, 20, 220);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - 11);
                int dy = Mathf.Abs(y - 11);
                if (dx + dy <= 10) pixels[y * size + x] = plate;
                if (dx + dy >= 9 && dx + dy <= 10) pixels[y * size + x] = ink;
            }
        }

        PaintElementGlyph(pixels, size, element, ink);
        texture.SetPixels32(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = texture.name;
        IconSprites[element] = sprite;
        return sprite;
    }

    private static void PaintElementGlyph(Color32[] pixels, int size, ElementType element, Color32 ink)
    {
        void Set(int x, int y)
        {
            if (x >= 0 && x < size && y >= 0 && y < size) pixels[y * size + x] = ink;
        }

        if (element == ElementType.Lightning)
        {
            int[,] points = { { 13, 18 }, { 9, 12 }, { 12, 12 }, { 9, 5 }, { 16, 14 }, { 13, 14 } };
            for (int i = 0; i < points.GetLength(0); i++)
                for (int ox = -1; ox <= 1; ox++)
                    Set(points[i, 0] + ox, points[i, 1]);
            return;
        }

        if (element == ElementType.Fire)
        {
            for (int y = 6; y <= 17; y++)
            {
                int halfWidth = y < 10 ? 2 : Mathf.Max(1, (18 - y) / 2);
                int center = y < 12 ? 11 : 12;
                for (int x = center - halfWidth; x <= center + halfWidth; x++) Set(x, y);
            }
            Set(9, 10);
            Set(14, 9);
            return;
        }

        // Ice：六向雪花，轮廓比整身染色更容易识别也不破坏怪物固有配色。
        for (int i = 6; i <= 17; i++)
        {
            Set(11, i);
            Set(i, 11);
        }
        for (int i = 0; i <= 6; i++)
        {
            Set(8 + i, 8 + i);
            Set(14 - i, 8 + i);
        }
    }
}
