using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class CharacterContoller : MonoBehaviour
{
    [Header("Settings")]
    public float moveForce = 30f;
    public float maxSpeed = 5f;
    public float jumpForce = 7f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;

    [Header("References")]
    public Transform orientation; // 카메라의 y축 기준 정렬을 위해 사용

    private Rigidbody rb;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (IsGrounded())
            {
                Vector3 vel = rb.linearVelocity;
                vel.y = 0;
                rb.linearVelocity = vel;
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
    }

    void HandleMovement()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (orientation != null)
        {
            inputDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;
            inputDirection.y = 0;
        }

        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(inputDirection.normalized * moveForce, ForceMode.Force);
        }

        if (inputDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
        }
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }
}
