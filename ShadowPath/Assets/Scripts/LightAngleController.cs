using UnityEngine;

public class LightAngleController : MonoBehaviour
{
    [Header("Light Rotation Settings")]
    public float rotateSpeed = 40f;

    [Header("Rotation Limits")]
    public float minYAngle = -60f;
    public float maxYAngle = 60f;

    public float minXAngle = -60f;
    public float maxXAngle = 60f;

    private float currentX = -25f;
    private float currentY = 20f;

    void Start()
    {
        transform.rotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    void Update()
    {
        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            horizontalInput = -1f;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            horizontalInput = 1f;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            verticalInput = 1f;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            verticalInput = -1f;
        }

        currentY += horizontalInput * rotateSpeed * Time.deltaTime;
        currentX -= verticalInput * rotateSpeed * Time.deltaTime;

        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
        currentX = Mathf.Clamp(currentX, minXAngle, maxXAngle);

        transform.rotation = Quaternion.Euler(currentX, currentY, 0f);
    }
}