using UnityEngine;

public class OrbitMotion : MonoBehaviour
{
    public float orbitSpeed = 20f;

    void Update()
    {
        transform.Rotate(Vector3.up, orbitSpeed * Time.deltaTime, Space.Self);
    }
}