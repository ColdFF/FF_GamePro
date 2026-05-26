using UnityEngine;

/// <summary>
/// Purpose: Plays a short light pulse when tutorial input becomes available.
/// Input: A Light component and pulse timing/intensity settings.
/// Output: Briefly increases the light intensity, then smoothly returns it to normal.
/// </summary>
public class LightUnlockPulse : MonoBehaviour
{
    [Header("Reference")]
    public Light targetLight;

    [Header("Opening State")]
    public bool startDimmedUntilPulse = true;
    public float dimmedIntensity = 0f;

    [Header("Pulse Settings")]
    public float normalIntensity = 1.4f;
    public float pulseIntensity = 2.1f;
    public float fadeInTime = 0.12f;
    public float holdTime = 0.08f;
    public float fadeOutTime = 0.35f;

    [Header("Pulse Audio")]
    public AudioSource pulseAudioSource;
    public AudioClip pulseSound;
    [Range(0f, 1f)] public float pulseSoundVolume = 0.7f;

    private float timer;
    private bool isPlaying;
    private float pulseStartIntensity;

    // Purpose: Initializes the target light intensity.
    // Input: Target light reference and normal intensity.
    // Output: Sets the light to a dimmed opening state or its normal intensity at level start.
    void Start()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        if (targetLight != null)
        {
            targetLight.intensity = startDimmedUntilPulse
                ? dimmedIntensity
                : normalIntensity;
            pulseStartIntensity = targetLight.intensity;
        }
    }

    // Purpose: Updates the pulse animation while it is active.
    // Input: Time since pulse start.
    // Output: Adjusts the target light intensity over the pulse timeline.
    void Update()
    {
        if (!isPlaying || targetLight == null)
        {
            return;
        }

        timer += Time.deltaTime;

        float totalTime = fadeInTime + holdTime + fadeOutTime;

        if (timer <= fadeInTime)
        {
            float t = timer / fadeInTime;
            targetLight.intensity = Mathf.Lerp(pulseStartIntensity, pulseIntensity, t);
            return;
        }

        if (timer <= fadeInTime + holdTime)
        {
            targetLight.intensity = pulseIntensity;
            return;
        }

        if (timer <= totalTime)
        {
            float t = (timer - fadeInTime - holdTime) / fadeOutTime;
            targetLight.intensity = Mathf.Lerp(pulseIntensity, normalIntensity, t);
            return;
        }

        targetLight.intensity = normalIntensity;
        isPlaying = false;
    }

    // Purpose: Starts the unlock pulse effect from the beginning.
    // Input: External call from the tutorial opening flow.
    // Output: Begins the light intensity pulse.
    public void PlayPulse()
    {
        if (targetLight == null)
        {
            return;
        }

        timer = 0f;
        pulseStartIntensity = targetLight.intensity;
        isPlaying = true;
        PlayPulseSound();
    }

    /// <summary>
    /// Purpose: Plays a short sound when the unlock pulse begins.
    /// Input: Audio clip and AudioSource assigned in the Inspector.
    /// Output: Light pulse sound plays once at the start of the pulse.
    /// </summary>
    void PlayPulseSound()
    {
        if (pulseSound == null)
        {
            return;
        }

        if (pulseAudioSource == null)
        {
            pulseAudioSource = GetComponent<AudioSource>();
        }

        if (pulseAudioSource == null)
        {
            return;
        }

        pulseAudioSource.PlayOneShot(pulseSound, pulseSoundVolume);
    }
}
