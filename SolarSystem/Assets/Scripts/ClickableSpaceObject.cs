using System.Collections;
using UnityEngine;

public class ClickableSpaceObject : MonoBehaviour
{
    [Header("Child Friendly Fact")]
    [TextArea(2, 4)]
    public string factText;

    [Header("Camera Focus")]
    public float cameraDistance = 4f;
    public float cameraHeight = 1.5f;

    [Header("Visual Response")]
    public float pulseScale = 1.25f;
    public float pulseTime = 0.15f;

    [Header("Audio Response")]
    public AudioClip clickSound;
    public float soundVolume = 0.5f;

    private Vector3 originalScale;
    private bool isPulsing = false;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void OnMouseDown()
    {
        if (SolarCameraController.Instance != null)
        {
            SolarCameraController.Instance.FocusOnObject(
                transform,
                factText,
                cameraDistance,
                cameraHeight
            );
        }

        if (clickSound != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(clickSound, Camera.main.transform.position, soundVolume);
        }

        if (!isPulsing)
        {
            StartCoroutine(Pulse());
        }
    }

    IEnumerator Pulse()
    {
        isPulsing = true;

        transform.localScale = originalScale * pulseScale;
        yield return new WaitForSeconds(pulseTime);

        transform.localScale = originalScale;
        isPulsing = false;
    }
}