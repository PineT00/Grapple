using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerAnimState
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
    public PlayerAnimState CurrState { get; set; }

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
    [HideInInspector] public ConfigurableJoint mainJoint;
    [HideInInspector] public GrappleController grappleController;
    [HideInInspector] public RagdollAnimator ragdollAnimator;

    [Header("Input")]
    [HideInInspector] public Vector2 moveInput;

    [Header("Ground Settings")]
    public float groundSpeed = 30f;
    public float maxGroundSpeed = 5f;
    public float turnSpeed = 5f;
    public float groundCheckDistance = 0.7f;
    public MMF_Player jumpFeedback;

    [Header("Air Settings")]
    public float jumpForce = 7f;
    public float airSpeed = 10f;
    public float maxAirSpeed = 15f;
    public float airTurnSpeed = 3f;
    public float airBrakeForce = 1.3f;
    public float additionalGravity = 10f;
    public float spinSpeed = 200f;

    [Header("Swing Settings")]
    public float swingMoveForce = 10f;
    public float swingTurnSpeed = 300f;
    public float swingEndDashSpeed = 15f;

    [Header("Glide Settings")]
    public float glideAccel = 15f;
    public float maxGlideSpeed = 50f;
    public float glideTurnSpeed = 300f;
    public float glideDashTime = 1f;
    public float dashSpeed = 15f;
    public float targetGlideGravity = -1.5f;
    public float verticalCorrectionForce = 20f;

    [Header("Dive Settings")]
    public float maxDiveSpeed = 70f;
    public float diveForce = 10f;
    public float diveSteeringFactor = 1.5f;
    public float diveDamper = 10f;
    public float diveToGlideSpeedConversion = 0.8f;
    public float glideTransitionDuration = 0.6f;
    public float maxDiveSpeedBoost = 25f;
    public float glideBoostDecayRate = 2f;
    private Rigidbody[] allRigidbodies;

    public float CurrentGlideBoost { get; set; }
    public Vector3 DashDir { get; set; }

    Vector3 inputDirection;
    Vector3 horizontalVelocity;
    Vector3 targetVelocity;
    [HideInInspector] public Vector3 worldDirection;

    void Awake()
    {
        mainRb = charCenterPart.GetComponent<Rigidbody>();
        mainJoint = charCenterPart.GetComponent<ConfigurableJoint>();
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

    // 상태를 전환하는 유일한 통로
    public void SwitchState(PlayerBaseState newState)
    {
        CurrentState?.ExitState();
        CurrentState = newState;
        CurrentState.EnterState();

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
        if (context.ReadValue<float>() > 0 && !grappleController.GrappleReady)
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
            targetVelocity *= maxGroundSpeed;
            velocityChange = targetVelocity - horizontalVelocity;
            RotateDirection(worldDirection);
        }
        velocityChange.y = 0f;
        mainRb.AddForce(velocityChange * groundSpeed, ForceMode.Acceleration);
    }

    public void HandleAirMove()
    {
        UpdateMoveInfo();

        Vector3 velocityChange;

        if (worldDirection.sqrMagnitude > 0.01f)
        {
            targetVelocity *= maxAirSpeed;
            velocityChange = targetVelocity - horizontalVelocity;
            velocityChange *= airSpeed;
            RotateDirectionSmooth(worldDirection, airTurnSpeed);
        }
        else
        {
            velocityChange = -horizontalVelocity * airBrakeForce;
        }
        mainRb.AddForce(velocityChange, ForceMode.Acceleration);
    }

    public void HandleAirMove2()
    {
        UpdateMoveInfo();

        Vector3 velocityChange;

        if (worldDirection.sqrMagnitude > 0.01f)
        {
            targetVelocity *= maxAirSpeed;
            velocityChange = targetVelocity - horizontalVelocity;
            velocityChange *= airSpeed;
            //RotateDirectionSmooth(worldDirection, airTurnSpeed);
            //ApplyGlidingForce(worldDirection);
            //RotateForGliding(worldDirection);

        }
        else
        {
            velocityChange = -horizontalVelocity * airBrakeForce;
        }
        HandleGlidingRotation(worldDirection);
        mainRb.AddForce(velocityChange, ForceMode.Acceleration);
    }

    public void HandleSwingMovement()
    {
        UpdateMoveInfo();

        mainRb.AddForce(worldDirection.normalized * swingMoveForce, ForceMode.Acceleration);
        RotateDirection(mainRb.linearVelocity);
    }


    /// <summary>
    /// Gliding related
    /// </summary>
    public void HandleDashingMovement(Vector3 dashDir)
    {
        ApplyGlidingForce(dashDir);
        RotateForGliding(dashDir);
        AntiGravity(targetGlideGravity);
    }

    public void HandleStandardGlidingMovement(Vector3 direction)
    {
        Vector3 targetDir = direction;
        targetDir.y = 0f;
        targetDir.Normalize();

        Vector3 currentForward = moveFrame.forward;
        currentForward.y = 0f;
        currentForward.Normalize();
        Vector3 newForward = Vector3.Slerp(currentForward, targetDir, glideTurnSpeed * Time.fixedDeltaTime);

        ApplyGlidingForce(newForward);
        RotateForGliding(newForward);
        AntiGravity(targetGlideGravity);
        //AntiGravity(targetGlideGravity + CurrentGlideBoost);
    }

    public void HandleDivingMovement()
    {
        Vector3 currentDirection = mainRb.linearVelocity.sqrMagnitude > 0.1f ? mainRb.linearVelocity.normalized : Vector3.down;
        float dot = Vector3.Dot(currentDirection, Vector3.down);
        float dampingFactor = Mathf.Clamp01(1.0f - dot) * diveDamper;

        Vector3 targetDirection = Vector3.Slerp(currentDirection, Vector3.down, diveSteeringFactor * Time.fixedDeltaTime);
        Vector3 targetVel = targetDirection * maxDiveSpeed;
        Vector3 velocityDiff = targetVel - mainRb.linearVelocity;

        mainRb.AddForce(velocityDiff * diveForce * dampingFactor, ForceMode.Acceleration);

        if (mainRb.linearVelocity.sqrMagnitude > 0.1f)
        {
            RotateForGliding(mainRb.linearVelocity);
        }
    }

    public void HandleTransitionMovement(Vector3 transitionDirection)
    {
        ApplyGlidingForce(transitionDirection);
        RotateForGliding(transitionDirection);
        AntiGravity(targetGlideGravity);
    }

    private void ApplyGlidingForce(Vector3 direction)
    {
        float currentMaxSpeed = maxGlideSpeed + CurrentGlideBoost;
        Vector3 targetVelocity = direction * currentMaxSpeed;
        Vector3 velocityChange = targetVelocity - mainRb.linearVelocity;
        velocityChange.y = 0;
        Vector3 finalForce = velocityChange;
        if (velocityChange.sqrMagnitude > 1f)
        {
            finalForce *= glideAccel;
        }
        mainRb.AddForce(finalForce, ForceMode.Acceleration);
    }


    /// <summary>
    /// 공용 움직임 관련 변수들
    /// </summary>
    public void JumpControl(float force)
    {
        ReduceMomentum(0.1f);
        mainRb.AddForce(Vector3.up * force, ForceMode.Impulse);
    }

    public void RotateDirection(Vector3 worldDirection)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude > 0.01f)
        {
            float targetYaw = Quaternion.LookRotation(worldDirection.normalized, Vector3.up).eulerAngles.y;
            mainJoint.targetRotation = Quaternion.Euler(0, targetYaw, 0);
        }
    }
    public void RotateDirectionSmooth(Vector3 worldDirection, float turnSpeed)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude > 0.01f)
        {
            Vector3 currentEuler = moveFrame.eulerAngles;
            float targetYaw = Quaternion.LookRotation(worldDirection.normalized, Vector3.up).eulerAngles.y;
            float newYaw = Mathf.LerpAngle(currentEuler.y, targetYaw, turnSpeed * Time.fixedDeltaTime);
            mainJoint.targetRotation = Quaternion.Euler(0, newYaw, 0);
        }
    }

    public void RotateForGliding(Vector3 worldDirection)
    {
        Quaternion targetWorldRotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up) * Quaternion.Euler(55, 0, 0);
        mainJoint.SetTargetRotationLocal(targetWorldRotation, Quaternion.identity);
    }

    public float glidingYawSpeed = 5f;
    public float glidingPitchSpeed = 5f;
    public Vector2 glidingVelocityRange = new Vector2(-20f, 10f);
    public Vector2 glidingPitchAngleRange = new Vector2(-45f, 30f);
    public void HandleGlidingRotation(Vector3 worldDirection)
    {
        Quaternion currRotation = Quaternion.LookRotation(moveFrame.forward);
        Quaternion targetRotation = currRotation;
        if (worldDirection.sqrMagnitude > 0.01f)
        {
            targetRotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
        }
        currRotation = Quaternion.Slerp(currRotation, targetRotation, glidingYawSpeed * Time.fixedDeltaTime);

        float verticalVelocity = mainRb.linearVelocity.y;
        float velocityRatio = Mathf.InverseLerp(glidingVelocityRange.x, glidingVelocityRange.y, verticalVelocity);

        float targetPitch = Mathf.Lerp(glidingPitchAngleRange.x, glidingPitchAngleRange.y, velocityRatio);
        float newPitch = Mathf.LerpAngle(mainJoint.targetRotation.eulerAngles.x, targetPitch, glidingPitchSpeed * Time.fixedDeltaTime);

        //currRotation *= Quaternion.Euler(newPitch, 0, 0);
        mainJoint.SetTargetRotationLocal(currRotation, Quaternion.identity);
    }

    private void AntiGravity(float targetAntiGravity)
    {
        // 반중력 값 보정(안정적)
        float currentVerticalSpeed = mainRb.linearVelocity.y;
        float speedDifference = targetAntiGravity - currentVerticalSpeed;
        Vector3 correctionForce = Vector3.up * speedDifference * verticalCorrectionForce;
        mainRb.AddForce(correctionForce, ForceMode.Acceleration);
    }

    public void MultiflyGravity()
    {
        foreach (var rb in allRigidbodies)
        {
            rb.AddForce(Vector3.down * additionalGravity, ForceMode.Acceleration);
        }
    }

    public void MultiflyHorizontalforce()
    {
        Vector3 horizontalDir = mainRb.linearVelocity.normalized;
        horizontalDir.y = 0f;
        mainRb.AddForce(horizontalDir * swingEndDashSpeed, ForceMode.VelocityChange);
    }

    public void ReduceMomentum(float amount) //예측불가능하게 움직일 수 있으니 꼭 필요한 상황외엔 사용X
    {
        if (allRigidbodies == null) return;

        foreach (var rb in allRigidbodies)
        {
            Vector3 reducedVelocity = rb.linearVelocity;
            reducedVelocity.y *= amount;
            rb.linearVelocity = reducedVelocity;
        }
    }

    public bool IsGrounded()
    {
        Vector3 origin = moveFrame.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }

}