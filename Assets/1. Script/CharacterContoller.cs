using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Walking,
    Swinging,
    gliding,
}


public class CharacterContoller : MonoBehaviour
{
    [Header("Settings")]
    public float moveForce = 30f;
    public float maxSpeed = 5f;
    public float swingMoveForce = 10f;

    public float jumpForce = 7f;
    public float turnSpeed = 5f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;
    public Transform camTarget;
    public Rigidbody rb;
    private Vector2 moveInput;
    private PlayerState currState;

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        currState = PlayerState.Walking;
        Cursor.lockState = CursorLockMode.Confined;
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
    public void SetPlayerState(PlayerState state)
    {
        currState = state;
    }

    void HandleMovement()
    {
        if (moveInput.sqrMagnitude < 0.1f)
            return;

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 worldDirection = camTarget.TransformDirection(inputDirection);
        worldDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);

        float currForce = 0f;

        switch (currState)
        {
            case PlayerState.Walking:
            {
                Vector3 targetVelocity = worldDirection.normalized * maxSpeed;
                Vector3 horizontalVelocity = rb.linearVelocity;
                horizontalVelocity.y = 0f;

                Vector3 velocityChange = targetVelocity - horizontalVelocity;
                velocityChange.y = 0f;

                currForce = moveForce;
                rb.AddForce(velocityChange * currForce, ForceMode.Acceleration);
                break;
            }
            case PlayerState.Swinging:
            {
                // 단순히 방향으로 힘을 더해주는 방식
                currForce = swingMoveForce;
                rb.AddForce(worldDirection.normalized * currForce, ForceMode.Force);
                break;
            }
        }
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
