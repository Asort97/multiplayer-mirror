using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameAudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "Audio.MusicVolume";
    private const string SfxVolumeKey = "Audio.SfxVolume";

    public static GameAudioManager Instance { get; private set; }

    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource loopSource;

    public float MusicVolume { get; private set; } = 0.7f;
    public float SfxVolume { get; private set; } = 0.8f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static GameAudioManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject("GameAudioManager");
        Instance = go.AddComponent<GameAudioManager>();
        DontDestroyOnLoad(go);
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

        musicSource = CreateSource("MusicSource", true, false);
        sfxSource = CreateSource("SfxSource", false, false);
        loopSource = CreateSource("LoopSource", true, true);

        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
        ApplyVolumes();
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private AudioSource CreateSource(string name, bool persistent, bool loop)
    {
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        if (persistent)
            source.priority = 32;
        return source;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (newScene.name == "Lobby")
            PlayMusic("menu_music");
        else if (musicSource != null && musicSource.isPlaying && musicSource.clip != null && musicSource.clip.name == "menu_music")
            musicSource.Stop();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = MusicVolume;
        if (sfxSource != null)
            sfxSource.volume = SfxVolume;
        if (loopSource != null)
            loopSource.volume = SfxVolume;
    }

    public void PlayMusic(string clipName)
    {
        var clip = LoadClip(clipName);
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = MusicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlaySfx(string clipName, float volumeScale = 1f)
    {
        var clip = LoadClip(clipName);
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * SfxVolume);
    }

    public void StartLoop(string clipName)
    {
        var clip = LoadClip(clipName);
        if (clip == null || loopSource == null)
            return;

        if (loopSource.clip == clip && loopSource.isPlaying)
            return;

        loopSource.clip = clip;
        loopSource.volume = SfxVolume;
        loopSource.loop = true;
        loopSource.Play();
    }

    public void StopLoop()
    {
        if (loopSource != null)
            loopSource.Stop();
    }

    public static void PlayButtonClick()
    {
        var instance = EnsureInstance();
        instance.PlaySfx("button");
    }

    public static void PlayNamed(string clipName, float volumeScale = 1f)
    {
        var instance = EnsureInstance();
        instance.PlaySfx(clipName, volumeScale);
    }

    public static void StartNamedLoop(string clipName)
    {
        var instance = EnsureInstance();
        instance.StartLoop(clipName);
    }

    public static void StopCurrentLoop()
    {
        if (Instance != null)
            Instance.StopLoop();
    }

    private AudioClip LoadClip(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
            return null;

        if (clipCache.TryGetValue(clipName, out var cachedClip) && cachedClip != null)
            return cachedClip;

        var clip = Resources.Load<AudioClip>("Sounds/" + clipName);
        if (clip != null)
            clipCache[clipName] = clip;
        return clip;
    }
}
