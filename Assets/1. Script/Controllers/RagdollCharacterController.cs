using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Standing,
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
    public GameObject charCenterPart;
    public Transform moveFrame; //No x,z turn

    [Header("Ground Settings")]
    public float moveForce = 30f;
    public float maxRunSpeed = 5f;
    public float turnSpeed = 5f;
    public float groundCheckDistance = 0.2f;

    [Header("Air Settings")]
    public float maxAirSpeed = 30f;
    public float airControlForce = 15f;
    public float airBrakeForce = 2f;
    public float swingMoveForce = 10f;
    public float jumpForce = 7f;
    public float airTurnSpeed = 3f;

    [Header("Glide Settings")]
    public float glideSpeed = 15f;
    public float glideTurnSpeed = 300f;
    public float reducedGravity = 0.3f;
    public float glidDrag = 3f;
    public float normalDrag = 0f;
    public float glideDashTime = 1f;
    public float dashSpeedMultiplier = 2f;
    private float dashTimer = 0f;
    private Vector3 dashDir = Vector3.zero;

    public PlayerState CurrState { get; private set; }
    private Rigidbody mainRb;
    private ConfigurableJoint mainJoint;
    private Vector2 moveInput;
    private GrappleController grappleController;
    private RagdollAnimator ragdollAnimator;
    private Rigidbody[] allRigidbodies;

    void Awake()
    {
        mainRb = charCenterPart.GetComponent<Rigidbody>();
        mainJoint = charCenterPart.GetComponent<ConfigurableJoint>();
        grappleController = GetComponent<GrappleController>();
        ragdollAnimator = GetComponent<RagdollAnimator>();

        SetPlayerState(PlayerState.Walking);

        Cursor.lockState = CursorLockMode.Confined; //Mouse Screen Lock

        allRigidbodies = GetComponentsInChildren<Rigidbody>();
    }

    void FixedUpdate()
    {
        CheckCurrState();
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
            if (CurrState == PlayerState.Walking || CurrState == PlayerState.Standing)
            {
                Vector3 vel = mainRb.linearVelocity;
                vel.y = 0;
                mainRb.linearVelocity = vel;
                mainRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                Vector3 effectPos = moveFrame.position;
                effectPos.y -= 1f;
                ParticleManager.Instance.Play("SmokeEffect", effectPos, moveFrame.rotation);
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

            dashTimer = glideDashTime;
            dashDir = Vector3.zero;
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
            case PlayerState.Standing:
                mainRb.linearDamping = normalDrag;
                ragdollAnimator.SetAnimation(PlayerState.Standing);
                break;
            case PlayerState.Swinging:
                mainRb.linearDamping = normalDrag;
                ragdollAnimator.SetHookTarget(grappleController.GetGrapplePoint());
                ragdollAnimator.SetAnimation(PlayerState.Swinging);
                break;
            case PlayerState.OnAir:
                mainRb.linearDamping = normalDrag;
                ragdollAnimator.SetAnimation(PlayerState.Standing);
                break;
            case PlayerState.Walking:
                mainRb.linearDamping = normalDrag;
                ragdollAnimator.SetAnimation(PlayerState.Walking);
                break;
            case PlayerState.Reeling:
                mainRb.linearDamping = normalDrag;
                ragdollAnimator.SetAnimation(PlayerState.Reeling);
                break;
            case PlayerState.Gliding:
                mainRb.linearDamping = glidDrag;
                Vector3 currVel = mainRb.linearVelocity;
                currVel.y = 0;
                mainRb.linearVelocity = currVel;
                ragdollAnimator.SetAnimation(PlayerState.Gliding);
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

        Vector3 horizontalVelocity = mainRb.linearVelocity;
        horizontalVelocity.y = 0f;
        Vector3 velocityChange;
        Vector3 targetVelocity = moveFrame.forward;

        switch (CurrState)
        {
            case PlayerState.Standing:
            case PlayerState.Walking:
                {
                    if (worldDirection.sqrMagnitude > 0.01f)
                    {
                        targetVelocity *= maxRunSpeed;
                        velocityChange = targetVelocity - horizontalVelocity;
                        ragdollAnimator.RotateDirection(worldDirection, turnSpeed);
                    }
                    else
                    {
                        velocityChange = -horizontalVelocity * 0.2f;
                    }
                    velocityChange.y = 0f;
                    mainRb.AddForce(velocityChange * moveForce, ForceMode.Acceleration);
                    break;
                }
            case PlayerState.OnAir:
                {
                    if (worldDirection.sqrMagnitude > 0.01f)
                    {
                        targetVelocity *= maxAirSpeed;
                        velocityChange = targetVelocity - horizontalVelocity;
                        //ragdollAnimator.RotateDirection(worldDirection, airTurnSpeed);
                        ragdollAnimator.SmoothRotate(worldDirection, 5f);
                        mainRb.AddForce(velocityChange * airControlForce, ForceMode.Acceleration);
                    }
                    else
                    {
                        velocityChange = -horizontalVelocity * airBrakeForce;
                        mainRb.AddForce(velocityChange, ForceMode.Acceleration);
                    }
                    break;
                }
            case PlayerState.Swinging:
                {
                    if (horizontalVelocity.magnitude > maxAirSpeed)
                        return;

                    mainRb.AddForce(worldDirection.normalized * swingMoveForce, ForceMode.Acceleration);
                    break;
                }
            case PlayerState.Gliding:
                {
                    if (worldDirection.sqrMagnitude > 0.01f)
                    {
                        Vector3 finalForce = Vector3.zero;
                        Vector3 antiGravity = Vector3.zero;
                        if (dashTimer > 0f)
                        {
                            if (dashDir == Vector3.zero)
                            {
                                dashDir = worldDirection;
                                StopAllMotion();
                            }
                            ragdollAnimator.RotateForGliding(dashDir, glideTurnSpeed * dashSpeedMultiplier * 10f);
                            float tempSpeedLimit = maxAirSpeed * dashSpeedMultiplier;
                            targetVelocity *= tempSpeedLimit;
                            velocityChange = targetVelocity - horizontalVelocity;
                            finalForce = velocityChange * glideSpeed * dashSpeedMultiplier;
                            antiGravity = Physics.gravity * -reducedGravity * 1.5f;
                            dashTimer -= Time.deltaTime;
                        }
                        else
                        {
                            ragdollAnimator.RotateForGliding(worldDirection, glideTurnSpeed);
                            targetVelocity *= maxAirSpeed;
                            velocityChange = targetVelocity - horizontalVelocity;
                            finalForce = velocityChange * glideSpeed;
                            antiGravity = Physics.gravity * -reducedGravity;
                        }
                        mainRb.AddForce(finalForce, ForceMode.Acceleration);
                        mainRb.AddForce(antiGravity, ForceMode.Acceleration);

                    }
                    break;
                }
            case PlayerState.Reeling:
                break;
        }
    }



    private void CheckCurrState()
    {
        if (CurrState == PlayerState.Swinging || CurrState == PlayerState.Reeling)
            return;

        if (IsGrounded())
        {
            if (mainRb.linearVelocity.sqrMagnitude < 0.08f)
            {
                SetPlayerState(PlayerState.Standing);
            }
            else
            {
                SetPlayerState(PlayerState.Walking);
            }
        }
        else
        {
            if (CurrState == PlayerState.Gliding)
                return;

            SetPlayerState(PlayerState.OnAir);
        }
    }
    public void StopAllMotion()
    {
        if (allRigidbodies == null) return;

        foreach (var rb in allRigidbodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private bool IsGrounded()
    {
        Vector3 origin = mainRb.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }

}