using UnityEngine;
using TMPro;

public class SolarCameraController : MonoBehaviour
{
    public static SolarCameraController Instance;

    [Header("UI")]
    public GameObject infoPanel;
    public TMP_Text factText;

    [Header("Camera Movement")]
    public float moveSpeed = 3f;
    public float rotateSpeed = 5f;

    private Transform currentTarget;
    private Vector3 mainPosition;
    private Quaternion mainRotation;

    private float focusDistance = 4f;
    private float focusHeight = 1.5f;
    private bool isReturning = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        mainPosition = transform.position;
        mainRotation = transform.rotation;

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (isReturning)
        {
            transform.position = Vector3.Lerp(transform.position, mainPosition, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, mainRotation, rotateSpeed * Time.deltaTime);
            return;
        }

        if (currentTarget == null)
        {
            return;
        }

        Vector3 desiredPosition = currentTarget.position + new Vector3(0, focusHeight, -focusDistance);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, moveSpeed * Time.deltaTime);

        Vector3 lookDirection = currentTarget.position - transform.position;
        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotateSpeed * Time.deltaTime);
    }

    public void FocusOnObject(Transform target, string fact, float distance, float height)
    {
        currentTarget = target;
        focusDistance = distance;
        focusHeight = height;
        isReturning = false;

        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
        }

        if (factText != null)
        {
            factText.text = fact;
        }
    }

    public void ReturnToMainView()
    {
        currentTarget = null;
        isReturning = true;

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }
}