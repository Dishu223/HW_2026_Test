using UnityEngine;

// Central audio manager. Controls music loops and one-shot sound effects.
// Includes escalating musical pitch progressions per platform step!
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips - SFX")]
    [SerializeField] private AudioClip stepClip;
    [SerializeField] private AudioClip pulpitLandClip;
    [SerializeField] private AudioClip pulpitWarningClip;
    [SerializeField] private AudioClip pulpitShatterClip;
    [SerializeField] private AudioClip milestoneClip;
    [SerializeField] private AudioClip fallClip;
    [SerializeField] private AudioClip rewindClip;
    [SerializeField] private AudioClip uiClickClip;

    [Header("Pitch Scaling Settings")]
    [SerializeField] private float basePitch = 1.0f;
    [SerializeField] private float pitchStep = 0.02f;
    [SerializeField] private float maxPitch = 1.8f;

    private int consecutiveSteps = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPulpitLanded += HandlePulpitLanded;
        GameEvents.OnPulpitDestroyed += HandlePulpitDestroyed;
        GameEvents.OnMilestoneReached += HandleMilestone;
        GameEvents.OnDoofusFell += HandleDoofusFell;
        GameEvents.OnRewindStart += HandleRewindStart;
        GameEvents.OnGameStart += ResetPitch;
        GameEvents.OnGameRestart += ResetPitch;
    }

    private void OnDisable()
    {
        GameEvents.OnPulpitLanded -= HandlePulpitLanded;
        GameEvents.OnPulpitDestroyed -= HandlePulpitDestroyed;
        GameEvents.OnMilestoneReached -= HandleMilestone;
        GameEvents.OnDoofusFell -= HandleDoofusFell;
        GameEvents.OnRewindStart -= HandleRewindStart;
        GameEvents.OnGameStart -= ResetPitch;
        GameEvents.OnGameRestart -= ResetPitch;
    }

    private void HandlePulpitLanded()
    {
        consecutiveSteps++;
        // Dynamically scale pitch so each consecutive platform sounds like an ascending musical melody!
        float targetPitch = Mathf.Min(basePitch + (consecutiveSteps * pitchStep), maxPitch);
        PlaySFX(pulpitLandClip, 1f, targetPitch);
    }

    private void HandlePulpitDestroyed(Vector3 position)
    {
        PlaySFX(pulpitShatterClip, 0.9f, Random.Range(0.9f, 1.1f));
    }

    private void HandleMilestone(int milestone)
    {
        PlaySFX(milestoneClip, 1f, 1f);
    }

    private void HandleDoofusFell()
    {
        PlaySFX(fallClip, 0.8f, 1f);
    }

    private void HandleRewindStart()
    {
        PlaySFX(rewindClip, 1f, 1f);
    }

    private void ResetPitch()
    {
        consecutiveSteps = 0;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayButtonClick()
    {
        PlaySFX(uiClickClip, 0.8f, 1.2f);
    }
}
