using UnityEngine;

public class Hovering : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverHeight = 2.0f;
    public float hoverForce = 50.0f;
    public float damping = 5.0f;
    public float raycastLength = 3.0f;
    public float gravity = 20f;
    public LayerMask groundLayer;
    public Rigidbody rb;

    void FixedUpdate()
    {
        Ray ray = new Ray(transform.position, -transform.up);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastLength, groundLayer))
        {
            float distance = hit.distance;
            float sub = hoverHeight - distance;

            float verticalVelocity = Vector3.Dot(rb.linearVelocity, transform.up);
            float force = (sub * hoverForce) - (verticalVelocity * damping);

            if (force < 0)
            {
                force *= gravity;
            }

            rb.AddForce(transform.up * force, ForceMode.Force);
        }
    }
}
