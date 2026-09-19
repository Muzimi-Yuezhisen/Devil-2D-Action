using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局音频入口：管理 BGM 交叉淡入淡出、SFX 声源池、随机变体与持久化音量。
/// 在首个场景载入前自动创建，业务代码只依赖 AudioCue，不直接持有 AudioClip。
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class AudioManager : MonoBehaviour
{
    private const string MasterVolumeKey = "audio.master";
    private const string MusicVolumeKey = "audio.music";
    private const string SfxVolumeKey = "audio.sfx";
    private const string MutedKey = "audio.muted";
    private const string MixVersionKey = "audio.mixVersion";
    private const int CurrentMixVersion = 3;
    private const int SfxPoolSize = 12;

    private sealed class CueConfig
    {
        public readonly AudioClip[] clips;
        public readonly float volume;
        public readonly float pitchVariation;
        public readonly bool spatial;

        public CueConfig(float volume, float pitchVariation, bool spatial, params string[] resourcePaths)
        {
            this.volume = volume;
            this.pitchVariation = pitchVariation;
            this.spatial = spatial;

            List<AudioClip> loadedClips = new List<AudioClip>();
            foreach (string resourcePath in resourcePaths)
            {
                AudioClip clip = Resources.Load<AudioClip>(resourcePath);
                if (clip != null) loadedClips.Add(clip);
                else Debug.LogWarning($"Audio clip not found at Resources/{resourcePath}");
            }

            clips = loadedClips.ToArray();
        }

        public AudioClip GetRandomClip(AudioClip previous = null)
        {
            if (clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];

            AudioClip selected = clips[Random.Range(0, clips.Length)];
            if (selected == previous)
                selected = clips[(System.Array.IndexOf(clips, selected) + 1) % clips.Length];
            return selected;
        }
    }

    public static AudioManager Instance { get; private set; }
    public static float MasterVolume => EnsureInstance().masterVolume;
    public static float MusicVolume => EnsureInstance().musicVolume;
    public static float SfxVolume => EnsureInstance().sfxVolume;
    public static bool IsMuted => EnsureInstance().isMuted;
    public static bool IsMusicPlaying => EnsureInstance().activeMusicSource != null &&
                                         EnsureInstance().activeMusicSource.isPlaying;
    public static AudioCue? CurrentMusicCue => EnsureInstance().currentMusicCue;
    public static string CurrentMusicClipName => EnsureInstance().activeMusicSource != null &&
                                                 EnsureInstance().activeMusicSource.clip != null
        ? EnsureInstance().activeMusicSource.clip.name
        : string.Empty;
    public static float CurrentMusicOutputVolume => EnsureInstance().activeMusicSource != null
        ? EnsureInstance().activeMusicSource.volume
        : 0;

    private readonly Dictionary<AudioCue, CueConfig> cues = new Dictionary<AudioCue, CueConfig>();
    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private readonly Dictionary<AudioSource, float> sfxBaseVolumes = new Dictionary<AudioSource, float>();

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private AudioCue? currentMusicCue;
    private AudioClip lastMusicClip;
    private Coroutine musicFadeRoutine;
    private int nextSfxSource;
    private Transform listenerTransform;

    private float masterVolume = 1f;
    private float musicVolume = 0.82f;
    private float sfxVolume = 0.9f;
    private bool isMuted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureInstance();

    private static AudioManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        Instance = FindAnyObjectByType<AudioManager>();
        if (Instance == null)
            Instance = new GameObject(nameof(AudioManager)).AddComponent<AudioManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadPreferences();
        BuildCueLibrary();
        BuildSources();
        ApplyMasterVolume();
    }

    private void OnEnable()
    {
        if (Instance == this) SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        TryStartSceneMusic();
    }

    private void OnDisable()
    {
        if (Instance == this) SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save();
    }

    private void OnApplicationQuit() => PlayerPrefs.Save();

    private void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        listenerTransform = null;
        TryStartSceneMusic();
    }

    private void TryStartSceneMusic()
    {
        if (SceneManager.GetActiveScene().name == MainMenuController.SceneName)
            PlayMusicInternal(AudioCue.MainMenuMusic, 0.6f);
        else if (FindAnyObjectByType<Player>() != null)
            PlayMusicInternal(AudioCue.GameplayMusic, 0.75f);
    }

    private void LoadPreferences()
    {
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.82f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.9f));
        isMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;

        // 旧混音默认值偏小。只迁移未静音用户一次，之后完全尊重设置界面的选择。
        if (PlayerPrefs.GetInt(MixVersionKey, 0) < CurrentMixVersion)
        {
            if (!isMuted)
            {
                musicVolume = Mathf.Max(musicVolume, 0.82f);
                sfxVolume = Mathf.Max(sfxVolume, 1f);
                PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
                PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            }
            PlayerPrefs.SetInt(MixVersionKey, CurrentMixVersion);
            PlayerPrefs.Save();
        }
    }

    private void BuildCueLibrary()
    {
        Register(AudioCue.MainMenuMusic, 0.68f, 0f, false,
            "Audio/BGM/menu_gothic_piano");
        Register(AudioCue.GameplayMusic, 0.72f, 0f, false,
            "Audio/BGM/gameplay_gothic_loop");
        Register(AudioCue.AttackSwing, 1f, 0.055f, true,
            "Audio/SFX/dark_sword_swing_01", "Audio/SFX/dark_sword_swing_02");
        Register(AudioCue.AttackHit, 1f, 0.06f, true,
            "Audio/SFX/dark_hit_01", "Audio/SFX/dark_hit_02");
        Register(AudioCue.Hurt, 0.82f, 0.05f, true,
            "Audio/SFX/dark_hurt");
        Register(AudioCue.Death, 0.9f, 0.035f, true,
            "Audio/SFX/dark_death");
        Register(AudioCue.Dash, 0.74f, 0.08f, true,
            "Audio/SFX/dark_sword_swing_02");
        Register(AudioCue.ShardExplosion, 0.92f, 0.05f, true,
            "Audio/SFX/dark_shard_impact");
        Register(AudioCue.SwordThrow, 0.86f, 0.045f, true,
            "Audio/SFX/dark_sword_swing_01");
        Register(AudioCue.UiClick, 0.68f, 0.025f, false,
            "Audio/SFX/gothic_ui_click");
        Register(AudioCue.MenuOpen, 0.68f, 0.02f, false,
            "Audio/SFX/gothic_menu_open");
        Register(AudioCue.MenuClose, 0.64f, 0.02f, false,
            "Audio/SFX/gothic_menu_close");
        Register(AudioCue.Victory, 0.84f, 0f, false,
            "Audio/SFX/gothic_victory");
        Register(AudioCue.Defeat, 0.82f, 0f, false,
            "Audio/SFX/gothic_defeat");
    }

    private void Register(AudioCue cue, float volume, float pitchVariation, bool spatial,
        params string[] resourcePaths)
    {
        cues[cue] = new CueConfig(volume, pitchVariation, spatial, resourcePaths);
    }

    private void BuildSources()
    {
        musicSourceA = CreateSource("Music_A", false);
        musicSourceB = CreateSource("Music_B", false);
        musicSourceA.loop = true;
        musicSourceB.loop = true;

        for (int i = 0; i < SfxPoolSize; i++)
            sfxSources.Add(CreateSource($"SFX_{i:00}", false));
    }

    private AudioSource CreateSource(string sourceName, bool playOnAwake)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = playOnAwake;
        source.dopplerLevel = 0;
        return source;
    }

    public static bool PlayMusic(AudioCue cue, float fadeDuration = 0.75f)
        => EnsureInstance().PlayMusicInternal(cue, fadeDuration);

    public static void StopMusic(float fadeDuration = 0.5f)
        => EnsureInstance().StopMusicInternal(fadeDuration);

    public static bool PlaySfx(AudioCue cue)
        => EnsureInstance().PlaySfxInternal(cue, Vector3.zero, false);

    public static bool PlaySfxAt(AudioCue cue, Vector3 worldPosition)
        => EnsureInstance().PlaySfxInternal(cue, worldPosition, true);

    public static bool IsCueAvailable(AudioCue cue)
    {
        AudioManager manager = EnsureInstance();
        return manager.cues.TryGetValue(cue, out CueConfig config) && config.clips.Length > 0;
    }

    private bool PlayMusicInternal(AudioCue cue, float fadeDuration)
    {
        if (!cues.TryGetValue(cue, out CueConfig config) || config.clips.Length == 0) return false;
        if (currentMusicCue == cue && activeMusicSource != null && activeMusicSource.isPlaying) return true;
        if (currentMusicCue == cue && musicFadeRoutine != null) return true;

        AudioClip nextClip = config.GetRandomClip(lastMusicClip);
        if (nextClip == null) return false;

        if (musicFadeRoutine != null) StopCoroutine(musicFadeRoutine);
        currentMusicCue = cue;
        lastMusicClip = nextClip;
        musicFadeRoutine = StartCoroutine(CrossFadeMusic(config, nextClip, Mathf.Max(0, fadeDuration)));
        return true;
    }

    private IEnumerator CrossFadeMusic(CueConfig config, AudioClip nextClip, float duration)
    {
        AudioSource previous = activeMusicSource;
        AudioSource next = previous == musicSourceA ? musicSourceB : musicSourceA;
        float previousStartVolume = previous != null ? previous.volume : 0;
        float targetVolume = config.volume * musicVolume;

        next.clip = nextClip;
        next.volume = duration > 0 ? 0 : targetVolume;
        next.Play();

        if (duration > 0)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                next.volume = Mathf.Lerp(0, targetVolume, t);
                if (previous != null) previous.volume = Mathf.Lerp(previousStartVolume, 0, t);
                yield return null;
            }
        }

        if (previous != null)
        {
            previous.Stop();
            previous.clip = null;
            previous.volume = 0;
        }

        next.volume = targetVolume;
        activeMusicSource = next;
        musicFadeRoutine = null;
    }

    private void StopMusicInternal(float fadeDuration)
    {
        if (musicFadeRoutine != null) StopCoroutine(musicFadeRoutine);
        musicFadeRoutine = StartCoroutine(FadeOutMusic(Mathf.Max(0, fadeDuration)));
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        AudioSource source = activeMusicSource;
        if (source == null)
        {
            musicFadeRoutine = null;
            yield break;
        }

        float startVolume = source.volume;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0, duration > 0 ? elapsed / duration : 1);
            yield return null;
        }

        source.Stop();
        source.clip = null;
        activeMusicSource = null;
        currentMusicCue = null;
        musicFadeRoutine = null;
    }

    private bool PlaySfxInternal(AudioCue cue, Vector3 worldPosition, bool allowSpatial)
    {
        if (!cues.TryGetValue(cue, out CueConfig config) || config.clips.Length == 0) return false;

        AudioSource source = GetAvailableSfxSource();
        AudioClip clip = config.GetRandomClip(source.clip);
        if (clip == null) return false;

        bool spatial = allowSpatial && config.spatial;
        source.transform.position = spatial ? GetPlanarAudioPosition(worldPosition) : transform.position;
        // 2D 横版保留轻微方位感即可；全 3D 衰减会让镜头边缘的攻击过小。
        source.spatialBlend = spatial ? 0.2f : 0;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 7;
        source.maxDistance = 28;
        source.pitch = Random.Range(1f - config.pitchVariation, 1f + config.pitchVariation);
        source.volume = config.volume * sfxVolume;
        source.clip = clip;
        sfxBaseVolumes[source] = config.volume;
        source.Play();
        return true;
    }

    private Vector3 GetPlanarAudioPosition(Vector3 worldPosition)
    {
        if (listenerTransform == null)
        {
            AudioListener listener = FindAnyObjectByType<AudioListener>();
            if (listener != null) listenerTransform = listener.transform;
        }

        if (listenerTransform != null) worldPosition.z = listenerTransform.position.z;
        return worldPosition;
    }

    private AudioSource GetAvailableSfxSource()
    {
        foreach (AudioSource source in sfxSources)
        {
            if (!source.isPlaying) return source;
        }

        AudioSource reused = sfxSources[nextSfxSource];
        nextSfxSource = (nextSfxSource + 1) % sfxSources.Count;
        return reused;
    }

    public static void SetMasterVolume(float value)
    {
        AudioManager manager = EnsureInstance();
        manager.masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, manager.masterVolume);
        manager.ApplyMasterVolume();
    }

    public static void SetMusicVolume(float value)
    {
        AudioManager manager = EnsureInstance();
        manager.musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, manager.musicVolume);
        manager.ApplyMusicVolume();
    }

    public static void SetSfxVolume(float value)
    {
        AudioManager manager = EnsureInstance();
        manager.sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, manager.sfxVolume);
        manager.ApplySfxVolume();
    }

    public static void SetMuted(bool muted)
    {
        AudioManager manager = EnsureInstance();
        manager.isMuted = muted;
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        manager.ApplyMasterVolume();
    }

    private void ApplyMasterVolume()
    {
        AudioListener.volume = isMuted ? 0 : masterVolume;
    }

    private void ApplyMusicVolume()
    {
        if (activeMusicSource == null || !currentMusicCue.HasValue) return;
        if (cues.TryGetValue(currentMusicCue.Value, out CueConfig config))
            activeMusicSource.volume = config.volume * musicVolume;
    }

    private void ApplySfxVolume()
    {
        foreach (AudioSource source in sfxSources)
        {
            if (sfxBaseVolumes.TryGetValue(source, out float baseVolume))
                source.volume = baseVolume * sfxVolume;
        }
    }
}
