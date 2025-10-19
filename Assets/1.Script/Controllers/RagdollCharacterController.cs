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
    WallRunning,
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
    [HideInInspector] public GrabController grabController;
    [HideInInspector] public RagdollAnimator ragdollAnimator;

    [Header("Input")]
    [HideInInspector] public Vector2 moveInput;
    [Header("Effects")]
    public MMF_Player jumpFeedback;
    public MMF_Player airTrailFeedback;
    public MMF_Player speedLineFeedback;

    [Header("Ground Settings")]
    public float groundSpeed = 30f;
    public float maxGroundSpeed = 5f;
    public float groundTurnSpeed = 5f;
    public float groundBrake = 1.3f;
    public float groundCheckDistance = 0.7f;

    [Header("Jump Settings")]
    public float jumpForce = 7f;
    public float jumpControl = 10f;
    public float maxJumpControl = 15f;
    public float jumpTurnSpeed = 3f;
    public float jumpBrake = 1.3f;
    public float additionalGravity = 10f;

    [Header("Air Settings")]
    public float airControl = 10f;
    public float maxAirControl = 15f;
    public float airTurnSpeed = 3f;
    public float airBrake = 1.3f;
    public float spinSpeed = 200f;

    [Header("Swing Settings")]
    public float swingMoveForce = 10f;
    public float swingTurnSpeed = 300f;
    public float swingEndDashSpeed = 15f;
    public float minSpeedForMultiply = 5f;
    public float maxSpeedForMultiply = 20f;
    public float minDashMultiplier = 0.5f;
    public float maxDashMultiplier = 1.5f;

    [Header("Glide Settings")]
    public float glideAngle = 60f;
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
    public float CurrentGlideBoost { get; set; }
    public Vector3 DashDir { get; set; }

    [Header("Ascend Settings")]
    public float ascendAngle = 30f;
    public float ascendInitialForce = 2f;
    public float ascendTargetForce = 15f;
    public float ascendDuration = 1.5f;

    [Header("Momentum System")]
    public float momentumBonus = 0f;
    public float momentumDecayRate = 2f;
    public float momentumConversionRatio = 0.3f;
    public float maxMomentumBonus = 20f;
    public float minSpeedForMomentum = 5f;

    [Header("Hovering Settings")]
    public float hoverHeight = 2f;
    public float hoverForce = 50f;
    public float hoverDamping = 10f;
    public float hoverRaycastDistance = 10f;

    [Header("Wall Run Settings")]
    public float maxWallRunTime = 3f;
    public float wallRunGravity = 3f;
    public float wallRunSpeedDecay = 2f;
    public float minWallRunSpeed = 3f;
    public float wallStickForce = 5f;
    public float wallDetectionDistance = 1.5f;
    public float minSpeedForWallRun = 8f;

    private Rigidbody[] allRigidbodies;
    Vector3 inputDirection;
    Vector3 horizontalVelocity;
    Vector3 targetVelocity;
    [HideInInspector] public Vector3 worldDirection;

    void Awake()
    {
        mainRb = charCenterPart.GetComponent<Rigidbody>();
        mainJoint = charCenterPart.GetComponent<ConfigurableJoint>();
        grappleController = GetComponent<GrappleController>();
        grabController = GetComponent<GrabController>();
        ragdollAnimator = GetComponent<RagdollAnimator>();
        allRigidbodies = GetComponentsInChildren<Rigidbody>();

        Cursor.lockState = CursorLockMode.Confined;

        // 초기 상태
        SwitchState(new StandingState(this));
    }

    void FixedUpdate()
    {
        CurrentState?.FixedUpdateState();
        UpdateMomentumDecay();
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
        CurrentState?.OnGrapple(context);
    }
    public void OnRighClick(InputAction.CallbackContext context)
    {
        CurrentState?.OnGrab(context);
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

    public void MovementControl()
    {
        UpdateMoveInfo();

        switch (CurrentState)
        {
            case StandingState:
            case WalkingState:
                HandleMovement(groundSpeed, maxGroundSpeed, groundTurnSpeed, groundBrake);
                break;
            case OnAirState:
                HandleMovement(jumpControl, maxJumpControl, jumpTurnSpeed, jumpBrake);
                break;
            case RollingState:
                HandleMovement(airControl, maxAirControl, airTurnSpeed, airBrake);
                break;
        }
    }

    public void HandleMovement(float force, float maxForce, float turnSpeed, float breakForce)
    {
        Vector3 velocityChange = Vector3.zero;
        float effectiveMaxSpeed = maxForce + momentumBonus;

        if (worldDirection.sqrMagnitude > 0.01f)
        {
            Vector3 desiredVelocity = worldDirection.normalized * effectiveMaxSpeed;
            velocityChange = (desiredVelocity - horizontalVelocity) * force;
            RotateDirectionSmooth(worldDirection, turnSpeed);
        }
        else
        {
            velocityChange = -horizontalVelocity * breakForce;
        }
        velocityChange.y = 0f;
        mainRb.AddForce(velocityChange, ForceMode.Acceleration);
    }

    float steeringForce = 30f;
    private void HandleHighSpeedMovement(float currentSpeed)
    {
        // 현재 속도 방향 유지하면서 입력 방향으로 서서히 조정
        Vector3 currentDir = horizontalVelocity.normalized;
        Vector3 inputDir = worldDirection.normalized;

        // 입력 방향으로 보조 힘 추가 (속도를 크게 바꾸지 않고 방향만 조정)
        Vector3 steeringVelocity = Vector3.Lerp(currentDir, inputDir, 0.3f) * currentSpeed;
        Vector3 steeringForceVec = (steeringVelocity - horizontalVelocity) * steeringForce;
        steeringForceVec.y = 0f;

        mainRb.AddForce(steeringForceVec, ForceMode.Acceleration);
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

    public void HandleAscendingMovement(Vector3 ascendDirection, float currentAscendForce, float progress)
    {
        // currentAscendForce에 반비례하여 속도 감소 계산
        float forceRatio = Mathf.Clamp01(currentAscendForce / ascendTargetForce); // 0~1 범위로 정규화
        float velocityMultiplier = 1.1f - forceRatio; // 반비례: force 증가 시 속도 감소

        // 수평속도 계산
        Vector3 targetVelocity = ascendDirection.normalized *
            (maxGlideSpeed + CurrentGlideBoost + momentumBonus) * velocityMultiplier;

        Vector3 velocityChange = targetVelocity - mainRb.linearVelocity;

        // 수평 성분 가속
        Vector3 horizontalChange = velocityChange;
        horizontalChange.y = 0f;
        mainRb.AddForce(horizontalChange * glideAccel, ForceMode.Acceleration);

        // AntiGravity를 이용한 안정적 상승
        AntiGravity(currentAscendForce);

        // progress에 따라 수평→상승 방향으로 회전
        Vector3 horizontalDirection = ascendDirection;
        horizontalDirection.y = 0f;
        horizontalDirection.Normalize();
        RotateForAscending(horizontalDirection, progress);
    }

    private void ApplyGlidingForce(Vector3 direction)
    {
        float currentMaxSpeed = maxGlideSpeed + CurrentGlideBoost + momentumBonus;
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
        Quaternion targetWorldRotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up) * Quaternion.Euler(glideAngle, 0, 0);
        mainJoint.SetTargetRotationLocal(targetWorldRotation, Quaternion.identity);
    }

    public void RotateForAscending(Vector3 baseDirection, float progress)
    {
        // 수평(0도)에서 목표 각도(60도)까지 progress에 따라 보간
        float currentPitch = Mathf.Lerp(glideAngle, ascendAngle, progress);
        Quaternion targetWorldRotation = Quaternion.LookRotation(baseDirection.normalized, Vector3.up) * Quaternion.Euler(currentPitch, 0, 0);
        mainJoint.SetTargetRotationLocal(targetWorldRotation, Quaternion.identity);
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
        Vector3 horizontalVel = mainRb.linearVelocity;
        horizontalVel.y = 0f;
        float currentSpeed = horizontalVel.magnitude;

        // 현재 속도에 따라 배율 계산 (느리면 적게, 빠르면 많이)
        float speedRatio = Mathf.InverseLerp(minSpeedForMultiply, maxSpeedForMultiply, currentSpeed);
        float speedMultiplier = Mathf.Lerp(minDashMultiplier, maxDashMultiplier, speedRatio);

        Vector3 horizontalDir = horizontalVel.normalized;
        float finalDashSpeed = swingEndDashSpeed * speedMultiplier;

        mainRb.AddForce(horizontalDir * finalDashSpeed, ForceMode.VelocityChange);
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

    public void ApplyHovering()
    {
        Vector3 rayOrigin = moveFrame.position + Vector3.up * 0.1f;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, hoverRaycastDistance, groundLayer))
        {
            float currentHeight = hit.distance;
            float heightError = hoverHeight - currentHeight;
            float verticalVelocity = mainRb.linearVelocity.y;

            float force = (heightError * hoverForce) - (verticalVelocity * hoverDamping);
            mainRb.AddForce(Vector3.up * force, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Momentum System
    /// </summary>
    public void CalculateMomentumBonus()
    {
        Vector3 horizontalVel = mainRb.linearVelocity;
        horizontalVel.y = 0f;
        float currentSpeed = horizontalVel.magnitude;

        if (currentSpeed > minSpeedForMomentum)
        {
            float bonus = currentSpeed * momentumConversionRatio;
            momentumBonus = Mathf.Clamp(bonus, 0f, maxMomentumBonus);
        }
        else
        {
            momentumBonus = 0f;
        }
    }

    private void UpdateMomentumDecay()
    {
        if (momentumBonus > 0f)
        {
            float decayRate = momentumDecayRate;
            switch (CurrentState)
            {
                case StandingState:
                case WalkingState:
                    decayRate *= 1.5f;
                    break;
            }

            momentumBonus -= decayRate * Time.fixedDeltaTime;
            if (momentumBonus < 0f)
            {
                momentumBonus = 0f;
            }
        }
    }

}