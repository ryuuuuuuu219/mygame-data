using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GeneratedAudioCue
{
    UiMove,
    UiSubmit,
    UiCancel,
    WeaponChange,
    GunFire,
    EnemyGunFire,
    MissileLaunch,
    EnemyMissileLaunch,
    BombDrop,
    BombExplosion,
    LockOn,
    LockLost,
    MissileWarning,
    LockWarning,
    Hit,
    Destroyed,
    WaveStart,
    StageClear,
    Empty
}

public enum GeneratedBgmState
{
    Title,
    Menu,
    Briefing,
    Setup,
    Mission,
    Danger,
    Clear,
    Result
}

public class GeneratedAudioManager : MonoBehaviour
{
    public static GeneratedAudioManager Instance { get; private set; }

    const int SampleRate = 44100;

    readonly Dictionary<GeneratedAudioCue, AudioClip> clips = new();
    readonly Dictionary<GeneratedBgmState, AudioClip> bgmClips = new();
    readonly List<AudioSource> oneShotSources = new();

    AudioSource bgmSource;
    AudioSource warningSource;
    AudioSource engineSource;
    GeneratedBgmState currentBgmState;
    bool hasBgmState;
    float nextMissileWarningTime;
    float nextLockWarningTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        var go = new GameObject("GeneratedAudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<GeneratedAudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = CreateSource("GeneratedBGM", true, 0.2f);
        warningSource = CreateSource("GeneratedWarningLoop", true, 0.0f);
        engineSource = CreateSource("GeneratedEngineLoop", true, 0.0f);

        BuildClips();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        UpdateMissionAudio();
    }

    public static void Play(GeneratedAudioCue cue, Vector3? position = null, float volume = 1f)
    {
        if (Instance == null) return;
        Instance.PlayInternal(cue, position, volume);
    }

    public static void SetBgm(GeneratedBgmState state)
    {
        if (Instance == null) return;
        Instance.SetBgmInternal(state);
    }

    public static void SetWarning(bool missileThreat, bool lockThreat)
    {
        SetWarning(missileThreat, lockThreat, 0.8f, 1.2f);
    }

    public static void SetWarning(bool missileThreat, bool lockThreat, float missileInterval, float lockInterval)
    {
        if (Instance == null) return;
        Instance.SetWarningInternal(missileThreat, lockThreat, missileInterval, lockInterval);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetBgmInternal(GetBgmForScene(scene.name));
    }

    void PlayInternal(GeneratedAudioCue cue, Vector3? position, float volume)
    {
        if (!clips.TryGetValue(cue, out AudioClip clip) || clip == null) return;

        AudioSource source = GetOneShotSource();
        source.transform.position = position ?? Vector3.zero;
        source.spatialBlend = position.HasValue ? 0.65f : 0f;
        source.pitch = Random.Range(0.96f, 1.04f);
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    void SetBgmInternal(GeneratedBgmState state)
    {
        if (hasBgmState && currentBgmState == state && bgmSource.isPlaying) return;
        if (!bgmClips.TryGetValue(state, out AudioClip clip) || clip == null) return;

        currentBgmState = state;
        hasBgmState = true;
        bgmSource.clip = clip;
        bgmSource.volume = state == GeneratedBgmState.Danger ? 0.28f : 0.2f;
        bgmSource.pitch = 1f;
        bgmSource.Play();
    }

    void SetWarningInternal(bool missileThreat, bool lockThreat, float missileInterval, float lockInterval)
    {
        missileInterval = Mathf.Max(0.05f, missileInterval);
        lockInterval = Mathf.Max(0.05f, lockInterval);

        if (missileThreat)
        {
            if (warningSource.isPlaying) warningSource.Stop();

            if (Time.time >= nextMissileWarningTime)
            {
                PlayInternal(GeneratedAudioCue.MissileWarning, null, 0.8f);
                nextMissileWarningTime = Time.time + missileInterval;
            }
            SetBgmInternal(GeneratedBgmState.Danger);
            return;
        }

        if (lockThreat)
        {
            if (warningSource.isPlaying) warningSource.Stop();

            if (Time.time >= nextLockWarningTime)
            {
                PlayInternal(GeneratedAudioCue.LockWarning, null, 0.7f);
                nextLockWarningTime = Time.time + lockInterval;
            }
            return;
        }

        warningSource.Stop();
    }

    void UpdateMissionAudio()
    {
        var player = FindPlayerAircraft();
        if (player == null)
        {
            engineSource.volume = Mathf.Lerp(engineSource.volume, 0f, Time.deltaTime * 2f);
            return;
        }

        if (engineSource.clip == null)
            engineSource.clip = MakeNoiseClip("Generated_EngineWind", 1.2f, 0.12f, 0.35f);
        if (!engineSource.isPlaying)
            engineSource.Play();

        float speedRate = player.maxSpeed > 1f
            ? Mathf.Clamp01(player.Velocity.magnitude / player.maxSpeed)
            : 0f;

        engineSource.volume = Mathf.Lerp(engineSource.volume, Mathf.Lerp(0.04f, 0.22f, speedRate), Time.deltaTime * 2f);
        engineSource.pitch = Mathf.Lerp(0.75f, 1.55f, Mathf.Clamp01(player.throttle / 2f));
    }

    AircraftController FindPlayerAircraft()
    {
        var statuses = FindObjectsByType<AugumentStatus>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var status in statuses)
        {
            if (status != null && status.isPlayer)
                return status.GetComponent<AircraftController>();
        }

        return null;
    }

    AudioSource CreateSource(string sourceName, bool loop, float volume)
    {
        var child = new GameObject(sourceName);
        child.transform.SetParent(transform);
        var source = child.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = volume;
        return source;
    }

    AudioSource GetOneShotSource()
    {
        foreach (AudioSource source in oneShotSources)
        {
            if (!source.isPlaying)
                return source;
        }

        AudioSource created = CreateSource("GeneratedSE_" + oneShotSources.Count, false, 1f);
        oneShotSources.Add(created);
        return created;
    }

    GeneratedBgmState GetBgmForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "Title": return GeneratedBgmState.Title;
            case "Menu": return GeneratedBgmState.Menu;
            case "Briefing": return GeneratedBgmState.Briefing;
            case "SetUp": return GeneratedBgmState.Setup;
            case "Result": return GeneratedBgmState.Result;
            default: return sceneName.StartsWith("M") || sceneName == "MVP" ? GeneratedBgmState.Mission : GeneratedBgmState.Menu;
        }
    }

    void BuildClips()
    {
        clips[GeneratedAudioCue.UiMove] = MakeTone("SE_UiMove", 660f, 0.055f, 0.15f, Wave.Square);
        clips[GeneratedAudioCue.UiSubmit] = MakeTone("SE_UiSubmit", 880f, 0.09f, 0.22f, Wave.Sine, 1320f);
        clips[GeneratedAudioCue.UiCancel] = MakeTone("SE_UiCancel", 360f, 0.1f, 0.2f, Wave.Triangle, 220f);
        clips[GeneratedAudioCue.WeaponChange] = MakeTone("SE_WeaponChange", 520f, 0.09f, 0.2f, Wave.Saw, 780f);
        clips[GeneratedAudioCue.GunFire] = MakeNoiseClip("SE_GunFire", 0.06f, 0.65f, 0.9f);
        clips[GeneratedAudioCue.EnemyGunFire] = MakeNoiseClip("SE_EnemyGunFire", 0.05f, 0.45f, 0.85f);
        clips[GeneratedAudioCue.MissileLaunch] = MakeSweep("SE_MissileLaunch", 150f, 520f, 0.32f, 0.55f);
        clips[GeneratedAudioCue.EnemyMissileLaunch] = MakeSweep("SE_EnemyMissileLaunch", 120f, 420f, 0.3f, 0.45f);
        clips[GeneratedAudioCue.BombDrop] = MakeSweep("SE_BombDrop", 280f, 90f, 0.25f, 0.28f);
        clips[GeneratedAudioCue.BombExplosion] = MakeNoiseClip("SE_BombExplosion", 0.45f, 0.9f, 0.55f);
        clips[GeneratedAudioCue.LockOn] = MakeTone("SE_LockOn", 1180f, 0.12f, 0.22f, Wave.Square);
        clips[GeneratedAudioCue.LockLost] = MakeTone("SE_LockLost", 440f, 0.1f, 0.18f, Wave.Triangle, 300f);
        clips[GeneratedAudioCue.MissileWarning] = MakeToneChord(
            "SE_MissileWarning",
            new[] { 880f, 904f, 1760f, 1808f, 2640f },
            new[] { Wave.Saw, Wave.Saw, Wave.Square, Wave.Square, Wave.Triangle },
            new[] { 1.0f, 0.7f, 0.3f, 0.25f, 0.1f },
            0.16f,
            0.28f);
        clips[GeneratedAudioCue.LockWarning] = MakeTone("SE_LockWarning", 740f, 0.18f, 0.16f, Wave.Sine);
        clips[GeneratedAudioCue.Hit] = MakeNoiseClip("SE_Hit", 0.12f, 0.5f, 0.8f);
        clips[GeneratedAudioCue.Destroyed] = MakeNoiseClip("SE_Destroyed", 0.38f, 0.75f, 0.6f);
        clips[GeneratedAudioCue.WaveStart] = MakeTone("SE_WaveStart", 520f, 0.22f, 0.24f, Wave.Saw, 760f);
        clips[GeneratedAudioCue.StageClear] = MakeTone("SE_StageClear", 660f, 0.45f, 0.26f, Wave.Sine, 990f);
        clips[GeneratedAudioCue.Empty] = MakeTone("SE_Empty", 180f, 0.08f, 0.18f, Wave.Square);

        bgmClips[GeneratedBgmState.Title] = MakeBgm("BGM_Title", 55f, 0.55f, 0.13f);
        bgmClips[GeneratedBgmState.Menu] = MakeBgm("BGM_Menu", 62f, 0.48f, 0.12f);
        bgmClips[GeneratedBgmState.Briefing] = MakeBgm("BGM_Briefing", 49f, 0.36f, 0.11f);
        bgmClips[GeneratedBgmState.Setup] = MakeBgm("BGM_Setup", 68f, 0.52f, 0.12f);
        bgmClips[GeneratedBgmState.Mission] = MakeBgm("BGM_Mission", 74f, 0.62f, 0.13f);
        bgmClips[GeneratedBgmState.Danger] = MakeBgm("BGM_Danger", 82f, 0.72f, 0.16f);
        bgmClips[GeneratedBgmState.Clear] = MakeBgm("BGM_Clear", 66f, 0.68f, 0.14f);
        bgmClips[GeneratedBgmState.Result] = MakeBgm("BGM_Result", 58f, 0.46f, 0.12f);
    }

    enum Wave { Sine, Square, Triangle, Saw }

    AudioClip MakeTone(string clipName, float frequency, float seconds, float volume, Wave wave, float endFrequency = 0f)
    {
        int samples = Mathf.CeilToInt(SampleRate * seconds);
        float[] data = new float[samples];
        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float freq = endFrequency > 0f ? Mathf.Lerp(frequency, endFrequency, t) : frequency;
            phase += freq / SampleRate;
            float value = WaveValue(phase, wave);
            data[i] = value * volume * Envelope(t);
        }

        AudioClip clip = AudioClip.Create(clipName, samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip MakeToneChord(string clipName, float[] frequencies, Wave[] waves, float[] componentVolumes, float seconds, float volume)
    {
        if (frequencies == null || frequencies.Length == 0)
            frequencies = new[] { 440f };

        int samples = Mathf.CeilToInt(SampleRate * seconds);
        float[] data = new float[samples];
        float[] phases = new float[frequencies.Length];
        float volumeSum = 0f;
        for (int f = 0; f < frequencies.Length; f++)
            volumeSum += GetComponentVolume(componentVolumes, f);
        volumeSum = Mathf.Max(0.001f, volumeSum);

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float value = 0f;
            for (int f = 0; f < frequencies.Length; f++)
            {
                phases[f] += frequencies[f] / SampleRate;
                value += WaveValue(phases[f], GetComponentWave(waves, f)) * GetComponentVolume(componentVolumes, f);
            }
            value /= volumeSum;
            data[i] = value * volume * Envelope(t);
        }

        AudioClip clip = AudioClip.Create(clipName, samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    Wave GetComponentWave(Wave[] waves, int index)
    {
        if (waves == null || waves.Length == 0) return Wave.Sine;
        if (index < waves.Length) return waves[index];
        return waves[waves.Length - 1];
    }

    float GetComponentVolume(float[] volumes, int index)
    {
        if (volumes == null || volumes.Length == 0) return 1f;
        if (index < volumes.Length) return Mathf.Max(0f, volumes[index]);
        return Mathf.Max(0f, volumes[volumes.Length - 1]);
    }

    AudioClip MakeSweep(string clipName, float startFrequency, float endFrequency, float seconds, float volume)
    {
        return MakeTone(clipName, startFrequency, seconds, volume, Wave.Saw, endFrequency);
    }

    AudioClip MakeNoiseClip(string clipName, float seconds, float volume, float brightness)
    {
        int samples = Mathf.CeilToInt(SampleRate * seconds);
        float[] data = new float[samples];
        float previous = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float white = Random.Range(-1f, 1f);
            previous = Mathf.Lerp(previous, white, brightness);
            data[i] = previous * volume * Envelope(t);
        }

        AudioClip clip = AudioClip.Create(clipName, samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip MakeBgm(string clipName, float rootFrequency, float pulseRate, float volume)
    {
        float seconds = 8f;
        int samples = Mathf.CeilToInt(SampleRate * seconds);
        float[] data = new float[samples];
        float[] intervals = { 1f, 1.5f, 1.25f, 1.75f };

        for (int i = 0; i < samples; i++)
        {
            float time = i / (float)SampleRate;
            int step = Mathf.FloorToInt(time * pulseRate * 4f) % intervals.Length;
            float root = rootFrequency * intervals[step];
            float pad = Mathf.Sin(2f * Mathf.PI * root * time) * 0.45f;
            pad += Mathf.Sin(2f * Mathf.PI * root * 2f * time) * 0.2f;
            float pulse = Mathf.Sin(2f * Mathf.PI * pulseRate * time) > 0.15f ? 1f : 0.55f;
            data[i] = pad * pulse * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    float WaveValue(float phase, Wave wave)
    {
        phase -= Mathf.Floor(phase);
        switch (wave)
        {
            case Wave.Square: return phase < 0.5f ? 1f : -1f;
            case Wave.Triangle: return 1f - 4f * Mathf.Abs(phase - 0.5f);
            case Wave.Saw: return phase * 2f - 1f;
            default: return Mathf.Sin(phase * Mathf.PI * 2f);
        }
    }

    float Envelope(float t)
    {
        float attack = Mathf.Clamp01(t / 0.08f);
        float release = Mathf.Clamp01((1f - t) / 0.35f);
        return attack * release;
    }
}
