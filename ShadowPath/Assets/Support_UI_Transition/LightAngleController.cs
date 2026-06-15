using UnityEngine;

// Purpose: Controls the gameplay Directional Light angle with arrow-key input.
// Input: Left, Right, Up, and Down arrow keys.
// Output: Rotates the Directional Light within the configured X and Y angle limits.
public class LightAngleController : MonoBehaviour
{
    [Header("Light Rotation Settings")]
    public float rotateSpeed = 40f;

    [Header("Rotation Limits")]
    public float minYAngle = -60f;
    public float maxYAngle = 60f;

    public float minXAngle = -60f;
    public float maxXAngle = 60f;

    private float currentX = -15f;
    private float currentY = 10f;

    // Purpose: Sets the initial Directional Light rotation when the scene starts.
    // Input: The starting X and Y light angles stored in this script.
    // Output: Applies the initial rotation to the Directional Light Transform.
    void Start()
    {
        transform.rotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    // Purpose: Reads arrow-key input and updates the Directional Light rotation.
    // Input: Arrow-key input, rotation speed, and configured angle limits.
    // Output: Rotates the gameplay light so the player feels they are adjusting light direction.
    void Update()
    {
        float horizontalInput = GetHorizontalLightInput();
        float verticalInput = GetVerticalLightInput();

        currentY += horizontalInput * rotateSpeed * Time.deltaTime;
        currentX -= verticalInput * rotateSpeed * Time.deltaTime;

        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
        currentX = Mathf.Clamp(currentX, minXAngle, maxXAngle);

        transform.rotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    // Purpose: Reads horizontal light-control input from the arrow keys.
    // Input: Left Arrow and Right Arrow keys.
    // Output: Returns a reversed horizontal direction so the arrows represent light movement,
    //         while the projected shadow reacts in the opposite direction.
    float GetHorizontalLightInput()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            return 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            return -1f;
        }

        return 0f;
    }

    // Purpose: Reads vertical light-control input from the arrow keys.
    // Input: Up Arrow and Down Arrow keys.
    // Output: Returns a reversed vertical direction so the arrows represent light movement,
    //         while the projected shadow reacts in the opposite direction.
    float GetVerticalLightInput()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            return -1f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            return 1f;
        }

        return 0f;
    }
}