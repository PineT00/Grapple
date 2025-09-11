using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Standing,
    Walking,
    Rolling,
    OnAir,
    Swinging,
    Reeling,
    Gliding,
}

public class RagdollCharacterController : MonoBehaviour
{
    [Header("State Machine")]
    public PlayerBaseState CurrentState { get; private set; }
    public PlayerState CurrState { get; set; }

    [Header("UI")]
    public TextMeshProUGUI currStateUI;
    public TextMeshProUGUI currGlideStateUI;

    [Header("Components & Layers")]
    public LayerMask groundLayer;
    public Transform camTarget;
    public GameObject charCenterPart;
    public Transform moveFrame;

    // 상태 클래스에서 접근할 수 있도록
    [HideInInspector] public Rigidbody mainRb;
    [HideInInspector] public GrappleController grappleController;
    [HideInInspector] public RagdollAnimator ragdollAnimator;

    [Header("Input")]
    [HideInInspector] public Vector2 moveInput;

    [Header("Ground Settings")]
    public float moveForce = 30f;
    public float maxRunSpeed = 5f;
    public float turnSpeed = 5f;
    public float groundCheckDistance = 0.7f;
    public MMF_Player jumpFeedback;

    [Header("Air Settings")]
    public float jumpForce = 7f;
    public float airSpeed = 10f;
    public float maxAirSpeed = 15f;
    public float airTurnSpeed = 3f;
    public float airBrakeForce = 1.3f;

    [Header("Swing Settings")]
    public float swingMoveForce = 10f;
    public float swingTurnSpeed = 300f;

    [Header("Glide Settings")]
    public float glideSpeed = 15f;
    public float maxGlideSpeed = 50f;
    public float glideTurnSpeed = 300f;
    public float glideDashTime = 1f;
    public float dashSpeed = 15f;
    public float targetGlideGravity = -1.5f;
    public float verticalCorrectionForce = 20f;

    [Header("Dive Settings")]
    public float maxDiveSpeed = 70f;
    public float diveControlForce = 10f;
    public float diveSteeringFactor = 1.5f;
    public float diveDamper = 10f;
    public float diveToGlideSpeedConversion = 0.8f;
    public float maxDiveSpeedBonus = 25f;
    public float glideTransitionDuration = 0.6f;
    public float glideBoostDecayRate = 2f;
    private Rigidbody[] allRigidbodies;

    public float CurrentGlideBoost { get; set; }
    public Vector3 DashDir { get; set; }

    Vector3 inputDirection;
    public Vector3 worldDirection;
    Vector3 horizontalVelocity;
    Vector3 targetVelocity;

    void Awake()
    {
        mainRb = charCenterPart.GetComponent<Rigidbody>();
        grappleController = GetComponent<GrappleController>();
        ragdollAnimator = GetComponent<RagdollAnimator>();
        allRigidbodies = GetComponentsInChildren<Rigidbody>();

        Cursor.lockState = CursorLockMode.Confined;

        // 초기 상태
        SwitchState(new StandingState(this));
    }

    void FixedUpdate()
    {
        CurrentState?.FixedUpdateState();
    }

    // 상태를 전환하는 유일한 통로입니다.
    public void SwitchState(PlayerBaseState newState)
    {
        CurrentState?.ExitState(); // 이전 상태의 Exit 로직 호출
        CurrentState = newState;
        CurrentState.EnterState(); // 새 상태의 Enter 로직 호출

        // UI 업데이트 (상태 클래스의 이름으로 표시)
        currStateUI.text = newState.GetType().Name;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        CurrentState?.OnJump(context);
    }

    public void OnGlide(InputAction.CallbackContext context)
    {
        CurrentState?.OnGlide(context);
    }
    public void OnGrapple(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0 && !grappleController.IsGrappleable)
            return;

        CurrentState?.OnGrapple(context);
    }

    public void UpdateMoveInfo()
    {
        inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        worldDirection = camTarget.TransformDirection(inputDirection);
        worldDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
        horizontalVelocity = mainRb.linearVelocity;
        horizontalVelocity.y = 0f;
        targetVelocity = moveFrame.forward;
    }

    public void HandleGroundMovement()
    {
        UpdateMoveInfo();

        Vector3 velocityChange = Vector3.zero;

        if (worldDirection.sqrMagnitude > 0.01f)
        {
            targetVelocity *= maxRunSpeed;
            velocityChange = targetVelocity - horizontalVelocity;
            ragdollAnimator.RotateDirection(worldDirection, turnSpeed);
        }
        velocityChange.y = 0f;
        mainRb.AddForce(velocityChange * moveForce, ForceMode.Acceleration);
    }

    public void HandleAirMove()
    {
        UpdateMoveInfo();

        Vector3 velocityChange;

        if (worldDirection.sqrMagnitude > 0.01f)
        {
            targetVelocity *= maxAirSpeed;
            velocityChange = targetVelocity - horizontalVelocity;
            if (horizontalVelocity.sqrMagnitude < targetVelocity.sqrMagnitude)
            {
                velocityChange *= airSpeed;
            }
            else
            {
                Debug.Log("감속");
            }
            ragdollAnimator.SmoothRotate(worldDirection, airTurnSpeed);
        }
        else
        {
            velocityChange = -horizontalVelocity * airBrakeForce;
        }
        mainRb.AddForce(velocityChange, ForceMode.Acceleration);
    }

    public void HandleAirRolling()
    {
        UpdateMoveInfo();

        Vector3 velocityChange;

        if (worldDirection.sqrMagnitude > 0.01f)
        {
            targetVelocity *= maxAirSpeed;
            velocityChange = (targetVelocity - horizontalVelocity) * airSpeed;
            ragdollAnimator.SmoothRotateAndSpin(worldDirection, airTurnSpeed);

        }
        else
        {
            velocityChange = -horizontalVelocity * airBrakeForce;
            ragdollAnimator.SmoothRotateAndSpin(horizontalVelocity.normalized, airTurnSpeed);
        }
        mainRb.AddForce(velocityChange, ForceMode.Acceleration);
    }

    public void HandleSwingMovement()
    {
        UpdateMoveInfo();

        mainRb.AddForce(worldDirection.normalized * swingMoveForce, ForceMode.Acceleration);
        if (mainRb.linearVelocity.magnitude > maxAirSpeed)
        {
            Vector3 limitedVelocity = mainRb.linearVelocity.normalized * maxAirSpeed;
            mainRb.linearVelocity = limitedVelocity;
        }
        ragdollAnimator.SmoothRotate(mainRb.linearVelocity.normalized, swingTurnSpeed);
    }

    public void JumpControl(float force)
    {
        ReduceMomentum(0.1f);
        mainRb.AddForce(Vector3.up * force, ForceMode.Impulse);
    }

    public void ReduceMomentum(float amount) //예측불가능하게 움직일 수 있으니 꼭 필요한 상황외엔 사용X
    {
        if (allRigidbodies == null) return;

        Vector3 reducedVelocity = Vector3.zero;

        foreach (var rb in allRigidbodies)
        {
            reducedVelocity = rb.linearVelocity;
            reducedVelocity.y *= amount;
            rb.linearVelocity = reducedVelocity;
        }
    }

    public bool IsGrounded()
    {
        Vector3 origin = moveFrame.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }


    //글라이딩
    public void HandleDashingMovement(Vector3 dashDir)
    {
        ApplyGlidingForce(dashDir);
        AntiGravity(targetGlideGravity);
    }

    public void HandleStandardGlidingMovement(Vector3 direction)
    {
        Vector3 currDir = moveFrame.forward;
        Vector3 targetDir = direction;
        targetDir.y = 0f;
        targetDir.Normalize();

        Vector3 glideDirection = Vector3.Lerp(currDir, targetDir, glideTurnSpeed * Time.fixedDeltaTime);
        ApplyGlidingForce(glideDirection);
        AntiGravity(targetGlideGravity);
    }

    public void HandleDivingMovement()
    {
        Vector3 currentDirection = mainRb.linearVelocity.sqrMagnitude > 0.1f ? mainRb.linearVelocity.normalized : Vector3.down;
        float dot = Vector3.Dot(currentDirection, Vector3.down);
        float dampingFactor = Mathf.Clamp01(1.0f - dot) * diveDamper;

        Vector3 targetDirection = Vector3.Slerp(currentDirection, Vector3.down, diveSteeringFactor * Time.fixedDeltaTime);
        Vector3 targetVel = targetDirection * maxDiveSpeed;
        Vector3 velocityDiff = targetVel - mainRb.linearVelocity;

        mainRb.AddForce(velocityDiff * diveControlForce * dampingFactor, ForceMode.Acceleration);

        if (mainRb.linearVelocity.sqrMagnitude > 0.1f)
        {
            ragdollAnimator.RotateForGliding(mainRb.linearVelocity.normalized);
        }
    }

    public void HandleTransitionMovement(Vector3 transitionDirection)
    {
        ApplyGlidingForce(transitionDirection);
        AntiGravity(targetGlideGravity);
    }

    private void ApplyGlidingForce(Vector3 direction)
    {
        ragdollAnimator.RotateForGliding(direction);

        float currentMaxSpeed = maxGlideSpeed + CurrentGlideBoost;

        Vector3 targetVelocity = direction * currentMaxSpeed;

        Vector3 currentHorizontalVelocity = new Vector3(mainRb.linearVelocity.x, 0, mainRb.linearVelocity.z);
        Vector3 velocityChange = targetVelocity - currentHorizontalVelocity;

        Vector3 finalForce = velocityChange * glideSpeed;
        mainRb.AddForce(finalForce, ForceMode.Acceleration);
    }

    private void AntiGravity(float targetAntiGravity)
    {
        // 반중력 값 보정(안정적)
        float currentVerticalSpeed = mainRb.linearVelocity.y;
        float speedDifference = targetAntiGravity - currentVerticalSpeed;
        Vector3 correctionForce = Vector3.up * speedDifference * verticalCorrectionForce;
        mainRb.AddForce(correctionForce, ForceMode.Acceleration);
    }
}