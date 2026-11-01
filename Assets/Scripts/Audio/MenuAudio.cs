using UnityEngine;
using UnityEngine.SceneManagement;

// Lives beyond a closing panel so its final click is never cut off.
public sealed class MenuAudio : MonoBehaviour
{
    private static MenuAudio instance;
    private AudioSource music, effects;
    private AudioClip hover, click;
    private float nextHover;
    private bool menuVisible;

    public static MenuAudio Ensure()
    {
        if (instance != null) return instance;
        var root = new GameObject("Menu Audio");
        instance = root.AddComponent<MenuAudio>();
        DontDestroyOnLoad(root);
        return instance;
    }

    private void Awake()
    {
        music = Source(); effects = Source();
        music.clip = Resources.Load<AudioClip>("Audio/Courier/Loops/night_waltz");
        music.loop = true;
        hover = Resources.Load<AudioClip>("Audio/Courier/hover");
        click = Resources.Load<AudioClip>("Audio/Courier/click");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private AudioSource Source()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false; source.spatialBlend = 0f;
        return source;
    }

    public void ShowMenu()
    {
        menuVisible = true;
        if (music.clip != null && !music.isPlaying)
        { music.volume = 0f; music.Play(); }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        menuVisible = scene.name == "MainMenu";
        if (!menuVisible) music.Stop();
    }

    private void Update()
    {
        BindMixer(music, "BGM"); BindMixer(effects, "Sfx");
        music.volume = Mathf.MoveTowards(music.volume,
            menuVisible ? .22f * Gain(music, true) : 0f, Time.unscaledDeltaTime * .2f);
        effects.volume = Gain(effects, false);
    }

    private static void BindMixer(AudioSource source, string group)
    {
        var manager = AudioManager.Instance;
        if (source.outputAudioMixerGroup != null || manager == null || manager.audioMixer == null) return;
        var groups = manager.audioMixer.FindMatchingGroups(group);
        if (groups.Length > 0) source.outputAudioMixerGroup = groups[0];
    }

    private static float Gain(AudioSource source, bool bgm)
    {
        if (source.outputAudioMixerGroup != null) return 1f;
        var manager = AudioManager.Instance;
        return manager != null ? manager.mainVolume * (bgm ? manager.bgmVolumeFactor : manager.sfxVolumeFactor) :
            PlayerPrefs.GetFloat("MainVolume", 1f) * PlayerPrefs.GetFloat(bgm ? "BgmVolumeFactor" : "SfxVolumeFactor", .8f);
    }

    public static void PlayInterface(string cue)
    {
        var audio = Ensure();
        bool isHover = cue == "hover";
        if (isHover && Time.unscaledTime < audio.nextHover) return;
        if (isHover) audio.nextHover = Time.unscaledTime + .08f;
        BindMixer(audio.effects, "Sfx");
        audio.effects.volume = Gain(audio.effects, false);
        var clip = isHover ? audio.hover : audio.click;
        if (clip != null) audio.effects.PlayOneShot(clip, isHover ? .12f : .35f);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }
}
