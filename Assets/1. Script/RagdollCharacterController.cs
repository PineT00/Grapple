using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Walking,
    OnAir,
    Swinging,
    Gliding,
}

public class RagdollCharacterController : MonoBehaviour
{
    [Header("Essencial")]
    public LayerMask groundLayer;
    public Transform camTarget;
    public Rigidbody mainRb;
    public Transform bodyTrans;
    public ConfigurableJoint mainJoint;

    [Header("Settings")]
    public float moveForce = 30f;
    public float maxSpeed = 5f;
    public float swingMoveForce = 10f;

    public float jumpForce = 7f;
    public float turnSpeed = 5f;
    public float groundCheckDistance = 0.2f;

    private Vector2 moveInput;
    private PlayerState currState;
    private GrappleController grappleController;


    void Awake()
    {
        if (mainRb == null)
        {
            mainRb = GetComponent<Rigidbody>();
        }
        SetPlayerState(PlayerState.Walking);
        Cursor.lockState = CursorLockMode.Confined;
        grappleController = GetComponent<GrappleController>();
    }

    void FixedUpdate()
    {
        CheckCurrState();
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
            if (currState == PlayerState.Walking)
            {
                Vector3 vel = mainRb.linearVelocity;
                vel.y = 0;
                mainRb.linearVelocity = vel;
                mainRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            else if (currState == PlayerState.Swinging)
            {
                grappleController.StartReeling();
            }
        }
        else if (context.canceled)
        {
            if (currState == PlayerState.Swinging)
            {
                grappleController.StopReeling();
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

        switch (currState)
        {
            case PlayerState.Walking:
                {
                    Vector3 targetVelocity = worldDirection.normalized * maxSpeed;
                    Vector3 horizontalVelocity = mainRb.linearVelocity;
                    horizontalVelocity.y = 0f;

                    Vector3 velocityChange = targetVelocity - horizontalVelocity;
                    velocityChange.y = 0f;

                    mainRb.AddForce(velocityChange * moveForce, ForceMode.Acceleration);
                    break;
                }
            case PlayerState.OnAir:
                if (mainRb.linearVelocity.magnitude < maxSpeed)
                {
                    mainRb.AddForce(worldDirection.normalized * moveForce, ForceMode.Acceleration);
                }
                break;
            case PlayerState.Swinging:
                {
                    mainRb.AddForce(worldDirection.normalized * swingMoveForce, ForceMode.Acceleration);
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
            Quaternion targetRotation = Quaternion.Euler(0f, -yTarget, 0f);
            mainJoint.targetRotation = targetRotation;
        }
    }

    private void CheckCurrState()
    {
        if (currState == PlayerState.Swinging)
            return;

        if (IsGrounded())
        {
            currState = PlayerState.Walking;
        }
        else
        {
            currState = PlayerState.OnAir;
        }
    }

    private bool IsGrounded()
    {
        Vector3 origin = bodyTrans.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }

}