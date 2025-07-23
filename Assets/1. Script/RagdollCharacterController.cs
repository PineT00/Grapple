using UnityEngine;
using UnityEngine.InputSystem;

public class RagdollCharacterController : MonoBehaviour
{
    [Header("Essencial")]
    public LayerMask groundLayer;
    public Transform camTarget;
    public Rigidbody mainRb;
    public Transform bodyTrans;

    [Header("Settings")]
    public float moveForce = 30f;
    public float maxSpeed = 5f;
    public float swingMoveForce = 10f;

    public float jumpForce = 7f;
    public float turnSpeed = 5f;
    public float groundCheckDistance = 0.2f;

    private Vector2 moveInput;
    private PlayerState currState;

    void Awake()
    {
        if (mainRb == null)
        {
            mainRb = GetComponent<Rigidbody>();
        }
        currState = PlayerState.Walking;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void FixedUpdate()
    {
        MaintainBalance();
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
                Vector3 vel = mainRb.linearVelocity;
                vel.y = 0;
                mainRb.linearVelocity = vel;
                mainRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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
                    Vector3 horizontalVelocity = mainRb.linearVelocity;
                    horizontalVelocity.y = 0f;

                    Vector3 velocityChange = targetVelocity - horizontalVelocity;
                    velocityChange.y = 0f;

                    currForce = moveForce;
                    mainRb.AddForce(velocityChange * currForce, ForceMode.Acceleration);
                    break;
                }
            case PlayerState.Swinging:
                {
                    // 단순히 방향으로 힘을 더해주는 방식
                    currForce = swingMoveForce;
                    mainRb.AddForce(worldDirection.normalized * currForce, ForceMode.Force);
                    break;
                }
        }
    }

    private void directionCheck()
    {
        float yCurrent = bodyTrans.rotation.eulerAngles.y;
        float yTarget = camTarget.rotation.eulerAngles.y;

        float diff = Mathf.DeltaAngle(yCurrent, yTarget);

        if (Mathf.Abs(diff) > 0.1f)
        {
            float dynamicTurnSpeed = Mathf.Abs(diff) * turnSpeed;
            Quaternion targetRotation = Quaternion.Euler(0f, yTarget, 0f);

            mainRb.MoveRotation(Quaternion.RotateTowards(
                bodyTrans.rotation,
                targetRotation,
                dynamicTurnSpeed * Time.fixedDeltaTime
            ));
        }
    }

    private void MaintainBalance()
    {
        if (currState != PlayerState.Walking) return;

        Vector3 origin = mainRb.worldCenterOfMass;
        Ray ray = new Ray(origin, Vector3.down);

        float desiredHeight = 1.0f; // 원하는 지면과의 거리
        float springStrength = 100f;
        float damping = 10f;

        if (Physics.Raycast(ray, out RaycastHit hit, desiredHeight, groundLayer))
        {
            float distance = hit.distance;
            float displacement = desiredHeight - distance;

            Vector3 velocity = mainRb.linearVelocity;
            float verticalVelocity = Vector3.Dot(Vector3.up, velocity);

            float springForce = displacement * springStrength - verticalVelocity * damping;

            mainRb.AddForce(Vector3.up * springForce, ForceMode.Force);
        }
    }

    bool IsGrounded()
    {
        Vector3 origin = bodyTrans.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }
}
