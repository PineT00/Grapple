using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Walking,
    OnAir,
    Swinging,
    Reeling,
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
    public PlayerState CurrState { get; private set; }
    public GrappleController grappleController;
    public RagdollAnimator ragdollAnimator;

    void Awake()
    {
        if (mainRb == null)
        {
            mainRb = GetComponent<Rigidbody>();
        }
        SetPlayerState(PlayerState.Walking);
        Cursor.lockState = CursorLockMode.Confined;
        grappleController = GetComponent<GrappleController>();


        if (ragdollAnimator == null)
        {
            ragdollAnimator = GetComponent<RagdollAnimator>();
        }
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
            if (CurrState == PlayerState.Walking)
            {
                Vector3 vel = mainRb.linearVelocity;
                vel.y = 0;
                mainRb.linearVelocity = vel;
                mainRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            else if (CurrState == PlayerState.Swinging)
            {
                grappleController.StartReeling();
            }
        }
        else if (context.canceled)
        {
            if (CurrState == PlayerState.Reeling)
            {
                grappleController.StopReeling();
            }
        }
    }
    public void OnGlide(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (CurrState != PlayerState.OnAir)
                return;

            SetPlayerState(PlayerState.Gliding);
        }
        else if (context.canceled)
        {
            SetPlayerState(PlayerState.OnAir);
        }
    }
    
    public void SetPlayerState(PlayerState state)
    {
        CurrState = state;
        switch (state)
        {
            case PlayerState.Swinging:
                ragdollAnimator.SetAnimation(RagdollAnimState.Sway);
                break;
            case PlayerState.OnAir:
                ragdollAnimator.SetAnimation(RagdollAnimState.Stand);
                break;
            case PlayerState.Walking:
                ragdollAnimator.SetAnimation(RagdollAnimState.Walk);
                break;
            case PlayerState.Gliding:
                break;
        }
    }

    void HandleMovement()
    {
        if (moveInput.sqrMagnitude < 0.01f && mainRb.linearVelocity.sqrMagnitude < 0.01f)
            return;

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 worldDirection = camTarget.TransformDirection(inputDirection);
        worldDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);

        switch (CurrState)
        {
            case PlayerState.Walking:
                {
                    Vector3 targetVelocity = worldDirection.normalized * maxSpeed;
                    Vector3 horizontalVelocity = mainRb.linearVelocity;
                    horizontalVelocity.y = 0f;
                    Vector3 velocityChange;
                    if (worldDirection.sqrMagnitude > 0.01f)
                    {
                        // 입력이 있을 때: 가속
                        velocityChange = targetVelocity - horizontalVelocity;
                    }
                    else
                    {
                        // 입력이 없을 때: 감속
                        velocityChange = -horizontalVelocity * 0.5f;
                    }
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
            case PlayerState.Gliding:
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
        if (CurrState == PlayerState.Swinging || CurrState == PlayerState.Reeling)
            return;

        if (IsGrounded())
        {
            SetPlayerState(PlayerState.Walking);
        }
        else
        {
            if (CurrState == PlayerState.Gliding)
                return;

            SetPlayerState(PlayerState.OnAir);
        }
    }

    private bool IsGrounded()
    {
        Vector3 origin = bodyTrans.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }

}