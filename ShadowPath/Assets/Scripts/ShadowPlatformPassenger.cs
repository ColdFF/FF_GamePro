using UnityEngine;

[DefaultExecutionOrder(100)]
public class ShadowPlatformPassenger : MonoBehaviour
{
    private Transform currentPassenger;
    private Rigidbody currentPassengerRigidbody;
    private Vector3 previousPlatformPosition;

    // Purpose: Stores the starting position of the shadow platform.
    // Input: Current platform transform position.
    // Output: Prepares movement delta tracking before gameplay starts.
    void Start()
    {
        previousPlatformPosition = transform.position;
    }

    // Purpose: Moves the player by the same position delta as the shadow platform.
    // Input: Platform movement from the previous frame to the current frame.
    // Output: Carries the player without inheriting platform scale changes.
    void LateUpdate()
    {
        Vector3 platformDelta = transform.position - previousPlatformPosition;

        if (currentPassenger != null && platformDelta.sqrMagnitude > 0f)
        {
            MovePassenger(platformDelta);
        }

        previousPlatformPosition = transform.position;
    }

    // Purpose: Starts carrying the player when they land on the platform.
    // Input: Collision contact with the player.
    // Output: Stores the player as the current passenger when standing on top.
    void OnCollisionEnter(Collision collision)
    {
        TrySetPassenger(collision);
    }

    // Purpose: Keeps the player registered while they remain on the platform.
    // Input: Ongoing collision contact with the player.
    // Output: Refreshes passenger tracking while top contact exists.
    void OnCollisionStay(Collision collision)
    {
        TrySetPassenger(collision);
    }

    // Purpose: Stops carrying the player when they leave the platform.
    // Input: Collision exit from the player.
    // Output: Clears the current passenger reference.
    void OnCollisionExit(Collision collision)
    {
        Transform exitingPlayer = GetPlayerTransform(collision);

        if (exitingPlayer == null)
        {
            return;
        }

        if (exitingPlayer == currentPassenger)
        {
            ClearPassenger();
        }
    }

    // Purpose: Registers the player only when they are standing on the top side of the platform.
    // Input: Collision data from the player and platform contact points.
    // Output: Stores the player Transform and Rigidbody for movement carrying.
    void TrySetPassenger(Collision collision)
    {
        Transform playerTransform = GetPlayerTransform(collision);

        if (playerTransform == null)
        {
            return;
        }

        if (!IsStandingOnTop(collision))
        {
            return;
        }

        currentPassenger = playerTransform;
        currentPassengerRigidbody = collision.rigidbody;
    }

    // Purpose: Moves the carried player without parenting them to the platform.
    // Input: World-space movement delta of the platform.
    // Output: Updates player position while preserving player scale.
    void MovePassenger(Vector3 platformDelta)
    {
        if (currentPassengerRigidbody != null)
        {
            currentPassengerRigidbody.position += platformDelta;
        }
        else
        {
            currentPassenger.position += platformDelta;
        }
    }

    // Purpose: Finds the Player Transform from a collision.
    // Input: Collision data from a collider or Rigidbody tagged as Player.
    // Output: Returns the Player Transform or null.
    Transform GetPlayerTransform(Collision collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            return collision.transform;
        }

        if (collision.rigidbody != null && collision.rigidbody.CompareTag("Player"))
        {
            return collision.rigidbody.transform;
        }

        return null;
    }

    // Purpose: Checks whether the player contact is on the platform top surface.
    // Input: Collision contact normals.
    // Output: Returns true when the player is standing on top of the platform.
    bool IsStandingOnTop(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);

            if (contact.normal.y < -0.5f)
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Removes the current passenger references.
    // Input: None.
    // Output: Stops carrying the player.
    void ClearPassenger()
    {
        currentPassenger = null;
        currentPassengerRigidbody = null;
    }
}