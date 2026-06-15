using UnityEngine;

/// <summary>
/// Purpose: Makes a light flash once when the tutorial lets the player start moving.
/// Input: The light, flash brightness, flash timing, and optional sound.
/// Output: The light briefly gets brighter, then returns to normal.
/// </summary>
public class LightUnlockPulse : MonoBehaviour
{
    [Header("Reference")]
    // The light that will flash.
    public Light targetLight;

    [Header("Opening State")]
    // If true, the light starts dark and only turns on when PlayPulse is called.
    public bool startDimmedUntilPulse = true;
    // Brightness before the pulse starts, usually 0 for dark.
    public float dimmedIntensity = 0f;

    [Header("Pulse Settings")]
    // Normal brightness after the pulse finishes.
    public float normalIntensity = 1.4f;
    // Brightest point during the pulse.
    public float pulseIntensity = 2.1f;
    // How long the light takes to brighten.
    public float fadeInTime = 0.12f;
    // How long the light stays at its brightest point.
    public float holdTime = 0.08f;
    // How long the light takes to return to normal.
    public float fadeOutTime = 0.35f;

    [Header("Pulse Audio")]
    // AudioSource used to play the pulse sound.
    public AudioSource pulseAudioSource;
    // Sound played when the pulse begins.
    public AudioClip pulseSound;
    // Volume of the pulse sound.
    [Range(0f, 1f)] public float pulseSoundVolume = 0.7f;

    private float timer;
    private bool isPlaying;
    private float pulseStartIntensity;

    // Purpose: Sets the light brightness when the level starts.
    // Input: Inspector settings for the light and starting brightness.
    // Output: The light starts either dark or at normal brightness.
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

    // Purpose: Runs the flash while it is active.
    // Input: Time passing every frame.
    // Output: Changes the light brightness step by step.
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

    // Purpose: Starts the flash from the beginning.
    // Input: A call from another script, usually when the tutorial unlocks movement.
    // Output: The light flashes and the sound plays.
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
    /// Purpose: Plays the flash sound once.
    /// Input: The sound clip and AudioSource set in the Inspector.
    /// Output: The sound plays at the start of the flash.
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
