using UnityEngine;

public class CameraLightReaction : MonoBehaviour
{
    [Header("Reference")]
    public Transform lightTransform;

    [Header("Camera Offset Settings")]
    public float horizontalOffsetAmount = 0.4f;
    public float verticalOffsetAmount = 0.25f;
    public float smoothSpeed = 5f;

    [Header("Light Angle Range")]
    public float minYAngle = -45f;
    public float maxYAngle = -15f;

    public float minXAngle = -40f;
    public float maxXAngle = -10f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void LateUpdate()
    {
        if (lightTransform == null)
        {
            return;
        }

        float lightX = NormalizeAngle(lightTransform.eulerAngles.x);
        float lightY = NormalizeAngle(lightTransform.eulerAngles.y);

        float yPercent = Mathf.InverseLerp(minYAngle, maxYAngle, lightY);
        float xPercent = Mathf.InverseLerp(minXAngle, maxXAngle, lightX);

        float cameraXOffset = Mathf.Lerp(-horizontalOffsetAmount, horizontalOffsetAmount, yPercent);
        float cameraYOffset = Mathf.Lerp(-verticalOffsetAmount, verticalOffsetAmount, xPercent);

        Vector3 targetPosition = startPosition + new Vector3(cameraXOffset, cameraYOffset, 0f);

        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}