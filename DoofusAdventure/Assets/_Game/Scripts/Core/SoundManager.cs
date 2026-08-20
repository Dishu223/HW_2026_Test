using UnityEngine;

/// <summary>
/// Dual-Mode Dynamic Audio & Sound Engine:
/// - Priority 1: Plays custom AudioClips assigned in Inspector
/// - Priority 2 (Fallback): Procedurally synthesizes crisp 44.1kHz sound waves on the fly
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Custom Audio Clips (Optional - Drop your custom SFX here!)")]
    [SerializeField] private AudioClip customFootstepClip;
    [SerializeField] private AudioClip customLandingClip;
    [SerializeField] private AudioClip customShatterClip;
    [SerializeField] private AudioClip customRewindClip;
    [SerializeField] private AudioClip customResumeClip;
    [SerializeField] private AudioClip customMilestoneClip;
    [SerializeField] private AudioClip customGameOverClip;

    [Header("Volume Controls")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.85f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.90f;

    private AudioSource sfxSource;
    private AudioSource loopingRewindSource;

    // Procedural Fallback Clips Cache
    private AudioClip procFootstepClip;
    private AudioClip procLandingClip;
    private AudioClip procShatterClip;
    private AudioClip procRewindClip;
    private AudioClip procResumeClip;
    private AudioClip procMilestoneClip;
    private AudioClip procGameOverClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupAudioSources();
        GenerateProceduralFallbackClips();
    }

    private void SetupAudioSources()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D crisp audio

        loopingRewindSource = gameObject.AddComponent<AudioSource>();
        loopingRewindSource.playOnAwake = false;
        loopingRewindSource.loop = true;
        loopingRewindSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitLanded += HandlePulpitLanded;
        GameEvents.OnPulpitDestroyed += HandlePulpitDestroyed;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindReadyToResume += HandleRewindReadyToResume;
        GameEvents.OnRewindComplete += HandleRewindComplete;
        GameEvents.OnMilestoneReached += HandleMilestoneReached;
        GameEvents.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitLanded -= HandlePulpitLanded;
        GameEvents.OnPulpitDestroyed -= HandlePulpitDestroyed;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindReadyToResume -= HandleRewindReadyToResume;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
        GameEvents.OnMilestoneReached -= HandleMilestoneReached;
        GameEvents.OnGameOver -= HandleGameOver;
    }

    #region Public Play Methods
    public void PlayFootstep()
    {
        AudioClip clip = customFootstepClip != null ? customFootstepClip : procFootstepClip;
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = Random.Range(0.92f, 1.08f); // Natural step variation
            sfxSource.PlayOneShot(clip, 0.40f * masterVolume * sfxVolume);
        }
    }

    public void PlayLandingChime()
    {
        AudioClip clip = customLandingClip != null ? customLandingClip : procLandingClip;
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(clip, 0.75f * masterVolume * sfxVolume);
        }
    }

    public void PlayShatterCrunch()
    {
        AudioClip clip = customShatterClip != null ? customShatterClip : procShatterClip;
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = Random.Range(0.85f, 1.15f);
            sfxSource.PlayOneShot(clip, 0.85f * masterVolume * sfxVolume);
        }
    }

    public void PlayRewindWhoosh()
    {
        AudioClip clip = customRewindClip != null ? customRewindClip : procRewindClip;
        if (clip != null && loopingRewindSource != null)
        {
            loopingRewindSource.clip = clip;
            loopingRewindSource.volume = 0.80f * masterVolume * sfxVolume;
            loopingRewindSource.Play();
        }
    }

    public void StopRewindWhoosh()
    {
        if (loopingRewindSource != null && loopingRewindSource.isPlaying)
        {
            loopingRewindSource.Stop();
        }
    }

    public void PlayResumeChime()
    {
        AudioClip clip = customResumeClip != null ? customResumeClip : procResumeClip;
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(clip, 0.80f * masterVolume * sfxVolume);
        }
    }

    public void PlayMilestoneFanfare()
    {
        AudioClip clip = customMilestoneClip != null ? customMilestoneClip : procMilestoneClip;
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(clip, 0.95f * masterVolume * sfxVolume);
        }
    }

    public void PlayGameOverTone()
    {
        AudioClip clip = customGameOverClip != null ? customGameOverClip : procGameOverClip;
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(clip, 0.90f * masterVolume * sfxVolume);
        }
    }
    #endregion

    #region Event Handlers
    private void HandlePulpitLanded() => PlayLandingChime();
    private void HandlePulpitDestroyed(Vector3 pos) => PlayShatterCrunch();
    private void HandleRewindStart() => PlayRewindWhoosh();
    private void HandleRewindReadyToResume()
    {
        StopRewindWhoosh();
        PlayResumeChime();
    }
    private void HandleRewindComplete() => StopRewindWhoosh();
    private void HandleMilestoneReached(int score) => PlayMilestoneFanfare();
    private void HandleGameOver()
    {
        StopRewindWhoosh();
        PlayGameOverTone();
    }
    #endregion

    #region Procedural Audio Synthesizer (PCM Wave Synthesis)
    private void GenerateProceduralFallbackClips()
    {
        procFootstepClip = CreateSynthClip("Footstep_Proc", 0.08f, (t) => {
            float env = Mathf.Exp(-t * 45f);
            float freq = Mathf.Lerp(220f, 90f, t / 0.08f);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        });

        procLandingClip = CreateSynthClip("Landing_Proc", 0.22f, (t) => {
            float env = Mathf.Exp(-t * 12f);
            float note1 = Mathf.Sin(2f * Mathf.PI * 523.25f * t); // C5
            float note2 = Mathf.Sin(2f * Mathf.PI * 659.25f * t); // E5
            return (note1 * 0.6f + note2 * 0.4f) * env;
        });

        procShatterClip = CreateSynthClip("Shatter_Proc", 0.35f, (t) => {
            float env = Mathf.Exp(-t * 8f);
            float noise = Random.Range(-1f, 1f);
            float rumble = Mathf.Sin(2f * Mathf.PI * 75f * t);
            return (noise * 0.65f + rumble * 0.35f) * env;
        });

        procRewindClip = CreateSynthClip("Rewind_Proc", 1.2f, (t) => {
            float progress = t / 1.2f;
            float freq = Mathf.Lerp(180f, 720f, progress);
            float modulation = Mathf.Sin(2f * Mathf.PI * 35f * t) * 0.3f;
            float wave = Mathf.Sin(2f * Mathf.PI * (freq + modulation * 100f) * t);
            return wave * 0.7f;
        });

        procResumeClip = CreateSynthClip("Resume_Proc", 0.18f, (t) => {
            float env = Mathf.Exp(-t * 18f);
            float freq = Mathf.Lerp(440f, 880f, t / 0.18f);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        });

        procMilestoneClip = CreateSynthClip("Milestone_Proc", 0.55f, (t) => {
            float env = Mathf.Exp(-t * 4f);
            float freq;
            if (t < 0.15f) freq = 523.25f; // C5
            else if (t < 0.30f) freq = 659.25f; // E5
            else freq = 783.99f; // G5
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        });

        procGameOverClip = CreateSynthClip("GameOver_Proc", 0.65f, (t) => {
            float env = Mathf.Exp(-t * 3f);
            float freq = Mathf.Lerp(360f, 110f, t / 0.65f);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        });
    }

    private AudioClip CreateSynthClip(string name, float duration, System.Func<float, float> sampleFunc)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            samples[i] = Mathf.Clamp(sampleFunc(t), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion
}
