using UnityEngine;

/// <summary>
/// Comprehensive Audio & Music Engine for Doofus Adventure:
/// - Left & Right footstep audio slots (for alternating Boop sounds!)
/// - Start Screen / Lobby Background Music
/// - In-Game Gameplay Background Music (stops on gameover, slow-mo warped during rewind)
/// - Victory Fanfare for 50-pulpit completion
/// - Complete SFX suite with procedural fallbacks
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Music Tracks (BGM)")]
    [Tooltip("Looping music playing on Start Screen and Lobby")]
    [SerializeField] private AudioClip startScreenBGMClip;

    [Tooltip("Looping high-energy music playing during active gameplay")]
    [SerializeField] private AudioClip inGameBGMClip;

    [Header("Footsteps (Left / Right Alternating Boops!)")]
    [SerializeField] private AudioClip customFootstepLeftClip;
    [SerializeField] private AudioClip customFootstepRightClip;

    [Header("Gameplay SFX")]
    [SerializeField] private AudioClip customLandingClip;
    [SerializeField] private AudioClip customShatterClip;
    [SerializeField] private AudioClip customRewindClip;
    [SerializeField] private AudioClip customResumeClip;
    [SerializeField] private AudioClip customMilestoneClip;
    [SerializeField] private AudioClip customVictoryClip;
    [SerializeField] private AudioClip customGameOverClip;

    [Header("Volume Controls")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.85f;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.90f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioSource loopingRewindSource;

    // Procedural Fallbacks
    private AudioClip procFootstepLeftClip;
    private AudioClip procFootstepRightClip;
    private AudioClip procLandingClip;
    private AudioClip procShatterClip;
    private AudioClip procRewindClip;
    private AudioClip procResumeClip;
    private AudioClip procMilestoneClip;
    private AudioClip procVictoryClip;
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

    private void Start()
    {
        // Play start screen music on boot
        PlayStartScreenBGM();
    }

    private void SetupAudioSources()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        loopingRewindSource = gameObject.AddComponent<AudioSource>();
        loopingRewindSource.playOnAwake = false;
        loopingRewindSource.loop = true;
        loopingRewindSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameRestart += HandleGameStart;
        GameEvents.OnReturnToLobby += HandleReturnToLobby;
        GameEvents.OnGameOver += HandleGameOver;

        GameEvents.OnPulpitLanded += HandlePulpitLanded;
        GameEvents.OnPulpitDestroyed += HandlePulpitDestroyed;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnRewindReadyToResume += HandleRewindReadyToResume;
        GameEvents.OnRewindComplete += HandleRewindComplete;
        GameEvents.OnMilestoneReached += HandleMilestoneReached;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameRestart -= HandleGameStart;
        GameEvents.OnReturnToLobby -= HandleReturnToLobby;
        GameEvents.OnGameOver -= HandleGameOver;

        GameEvents.OnPulpitLanded -= HandlePulpitLanded;
        GameEvents.OnPulpitDestroyed -= HandlePulpitDestroyed;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnRewindReadyToResume -= HandleRewindReadyToResume;
        GameEvents.OnRewindComplete -= HandleRewindComplete;
        GameEvents.OnMilestoneReached -= HandleMilestoneReached;
    }

    #region Music (BGM) Controls
    public void PlayStartScreenBGM()
    {
        if (startScreenBGMClip != null && bgmSource != null)
        {
            if (bgmSource.clip == startScreenBGMClip && bgmSource.isPlaying) return;
            bgmSource.clip = startScreenBGMClip;
            bgmSource.volume = bgmVolume * masterVolume;
            bgmSource.pitch = 1.0f;
            bgmSource.Play();
        }
        else if (bgmSource != null && bgmSource.clip != startScreenBGMClip)
        {
            bgmSource.Stop();
        }
    }

    public void PlayInGameBGM()
    {
        if (inGameBGMClip != null && bgmSource != null)
        {
            bgmSource.clip = inGameBGMClip;
            bgmSource.volume = bgmVolume * masterVolume;
            bgmSource.pitch = 1.0f;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }
    #endregion

    #region Footstep SFX (Alternating Left / Right)
    public void PlayFootstep(bool isLeft)
    {
        AudioClip clip;
        if (isLeft)
        {
            clip = customFootstepLeftClip != null ? customFootstepLeftClip : procFootstepLeftClip;
        }
        else
        {
            clip = customFootstepRightClip != null ? customFootstepRightClip : procFootstepRightClip;
        }

        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = Random.Range(0.95f, 1.05f);
            sfxSource.PlayOneShot(clip, 0.45f * masterVolume * sfxVolume);
        }
    }
    #endregion

    #region Gameplay SFX
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

    public void PlayVictoryFanfare()
    {
        AudioClip clip = customVictoryClip != null ? customVictoryClip : procVictoryClip;
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(clip, 1.0f * masterVolume * sfxVolume);
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
    private void HandleGameStart()
    {
        PlayInGameBGM();
    }

    private void HandleReturnToLobby()
    {
        PlayStartScreenBGM();
    }

    private void HandleGameOver()
    {
        StopBGM();
        StopRewindWhoosh();
        PlayGameOverTone();
    }

    private void HandlePulpitLanded() => PlayLandingChime();
    private void HandlePulpitDestroyed(Vector3 pos) => PlayShatterCrunch();

    private void HandleRewindStart()
    {
        PlayRewindWhoosh();
        if (bgmSource != null) bgmSource.pitch = 0.5f; // Slow-mo tape pitch
    }

    private void HandleRewindReadyToResume()
    {
        StopRewindWhoosh();
        PlayResumeChime();
    }

    private void HandleRewindComplete()
    {
        StopRewindWhoosh();
        if (bgmSource != null) bgmSource.pitch = 1.0f; // Restore normal pitch
    }

    private void HandleMilestoneReached(int score)
    {
        if (score >= 50)
        {
            PlayVictoryFanfare();
        }
        else
        {
            PlayMilestoneFanfare();
        }
    }
    #endregion

    #region Procedural Audio Synthesizer Fallbacks
    private void GenerateProceduralFallbackClips()
    {
        // Boop 1 (Left Foot: 260Hz -> 140Hz)
        procFootstepLeftClip = CreateSynthClip("Footstep_Left_Proc", 0.07f, (t) => {
            float env = Mathf.Exp(-t * 50f);
            float freq = Mathf.Lerp(260f, 140f, t / 0.07f);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        });

        // Boop 2 (Right Foot: 310Hz -> 180Hz - slightly higher pitch boop!)
        procFootstepRightClip = CreateSynthClip("Footstep_Right_Proc", 0.07f, (t) => {
            float env = Mathf.Exp(-t * 50f);
            float freq = Mathf.Lerp(310f, 180f, t / 0.07f);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        });

        procLandingClip = CreateSynthClip("Landing_Proc", 0.22f, (t) => {
            float env = Mathf.Exp(-t * 12f);
            float note1 = Mathf.Sin(2f * Mathf.PI * 523.25f * t);
            float note2 = Mathf.Sin(2f * Mathf.PI * 659.25f * t);
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
            return Mathf.Sin(2f * Mathf.PI * (freq + modulation * 100f) * t) * 0.7f;
        });

        procResumeClip = CreateSynthClip("Resume_Proc", 0.18f, (t) => {
            float env = Mathf.Exp(-t * 18f);
            float freq = Mathf.Lerp(440f, 880f, t / 0.18f);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        });

        procMilestoneClip = CreateSynthClip("Milestone_Proc", 0.55f, (t) => {
            float env = Mathf.Exp(-t * 4f);
            float freq = (t < 0.15f) ? 523.25f : (t < 0.30f) ? 659.25f : 783.99f;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        });

        // 50-Pulpit Victory Grand Chord
        procVictoryClip = CreateSynthClip("Victory_Proc", 1.2f, (t) => {
            float env = Mathf.Exp(-t * 2f);
            float n1 = Mathf.Sin(2f * Mathf.PI * 523.25f * t); // C5
            float n2 = Mathf.Sin(2f * Mathf.PI * 659.25f * t); // E5
            float n3 = Mathf.Sin(2f * Mathf.PI * 783.99f * t); // G5
            float n4 = Mathf.Sin(2f * Mathf.PI * 1046.50f * t); // C6
            return (n1 * 0.3f + n2 * 0.25f + n3 * 0.25f + n4 * 0.2f) * env;
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
