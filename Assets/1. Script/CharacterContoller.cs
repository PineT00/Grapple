using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class CharacterContoller : MonoBehaviour
{
    [Header("Settings")]
    public float moveForce = 30f;
    public float maxSpeed = 5f;
    public float jumpForce = 7f;
    public float turnSpeed = 5f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;
    public Transform camTarget;
    private Rigidbody rb;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        HandleMovement();
        directionCheck();
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

        Vector3 worldDirection = camTarget.TransformDirection(inputDirection);
        worldDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);

        Vector3 targetVelocity = worldDirection.normalized * maxSpeed;

        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        Vector3 velocityChange = targetVelocity - horizontalVelocity;
        velocityChange.y = 0f;

        rb.AddForce(velocityChange * moveForce, ForceMode.Acceleration);
    }

    private void directionCheck()
    {
        float yCurrent = transform.rotation.eulerAngles.y;
        float yTarget = camTarget.rotation.eulerAngles.y;

        float diff = Mathf.DeltaAngle(yCurrent, yTarget);

        if (Mathf.Abs(diff) > 0.1f)
        {
            float dynamicTurnSpeed = Mathf.Abs(diff) * turnSpeed;
            Quaternion targetRotation = Quaternion.Euler(0f, yTarget, 0f);

            rb.MoveRotation(Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                dynamicTurnSpeed * Time.fixedDeltaTime
            ));
        }
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }
}
