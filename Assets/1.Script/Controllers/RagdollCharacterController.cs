using System.Collections.Generic;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerAnimState
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
    [HideInInspector] public RagdollAnimator ragdollAnimator;
    [HideInInspector] public Rigidbody mainRb;
    [HideInInspector] public ConfigurableJoint mainJoint;
    public GrappleChecker grappleChecker;
    public GrappleController grappleController_Left;
    public GrappleController grappleController_Right;
    public GrabController grabController_Left;
    public GrabController grabController_Right;

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

    [Header("Air Settings")]
    public float jumpForce = 7f;
    public float airControl = 10f;
    public float maxAirControl = 15f;
    public float airTurnSpeed = 3f;
    public float airBrake = 1.3f;
    public float additionalGravity = 10f;
    public float spinSpeed = 200f;

    [Header("Swing Settings")]
    public float swingForce = 10f;
    public float maxSwingForce = 15f;
    public float swingTurnSpeed = 300f;
    public float swingEndDashSpeed = 15f;
    public float minSpeedForMultiply = 5f;
    public float maxSpeedForMultiply = 20f;
    public float minDashMultiplier = 0.5f;
    public float maxDashMultiplier = 1.5f;
    [Range(0f, 1f)]
    public float swingSteeringAssistance = 0.5f;

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

    private Rigidbody[] allRigidbodies;
    Vector3 inputDirection;
    Vector3 horizontalVelocity;
    Vector3 targetVelocity;
    [HideInInspector] public Vector3 worldDirection;

    void Awake()
    {
        mainRb = charCenterPart.GetComponent<Rigidbody>();
        mainJoint = charCenterPart.GetComponent<ConfigurableJoint>();
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
        GrabArmAdjustment();
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
    public void OnClick(InputAction.CallbackContext context)
    {
        CurrentState?.OnClick(context);
    }
    public void OnRightClick(InputAction.CallbackContext context)
    {
        CurrentState?.OnRightClick(context);
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

    public void HandleSwingMovement(List<GrappleController> activeGrapples)
    {
        if (activeGrapples.Count == 0)
            return;

        UpdateMoveInfo();

        Vector3 averageGrapplePoint = Vector3.zero;
        foreach (var grapple in activeGrapples)
        {
            averageGrapplePoint += grapple.GetGrapplePoint();
        }
        averageGrapplePoint /= activeGrapples.Count;
        Vector3 toGrapplePoint = averageGrapplePoint - mainRb.position;
        Vector3 ropeDirection = toGrapplePoint.normalized;

        // 현재 속도의 접선 방향 계산 (로프에 수직인 방향)
        Vector3 currentVelocity = mainRb.linearVelocity;
        Vector3 radialVelocity = Vector3.Project(currentVelocity, ropeDirection);
        Vector3 tangentVelocity = currentVelocity - radialVelocity;
        float currentSpeed = tangentVelocity.magnitude;

        if (worldDirection.sqrMagnitude > 0.01f)
        {
            // 1. 순수 접선 방향 (로프 중심 회전)
            Vector3 tangentDirection = tangentVelocity.normalized;

            // 2. 입력 방향 (접선 평면에 투영)
            Vector3 inputTangent = Vector3.ProjectOnPlane(worldDirection, ropeDirection).normalized;

            // 3. Steering Assistance 비율로 섞기
            Vector3 finalDirection = Vector3.Lerp(
                tangentDirection,           // 물리적 진자 방향
                inputTangent,               // 플레이어 입력 방향
                swingSteeringAssistance
            );

            // 4. 속도 크기는 유지하되 방향만 finalDirection으로
            Vector3 desiredVelocity = finalDirection * Mathf.Max(currentSpeed, maxSwingForce);
            Vector3 velocityChange = desiredVelocity - tangentVelocity;

            mainRb.AddForce(velocityChange * swingForce, ForceMode.Acceleration);

            // 회전은 최종 방향으로
            RotateDirection(finalDirection);
        }
        else
        {
            // 입력 없을 때는 접선 속도 방향으로 회전
            if (tangentVelocity.sqrMagnitude > 0.1f)
            {
                RotateDirection(tangentVelocity);
            }
        }
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
        float velocityMultiplier = 1.15f - forceRatio; // 반비례: force 증가 시 속도 감소

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

    public void MultiflyForce(GrappleController currentGrapple)
    {
        Vector3 ropeDirection = currentGrapple.GetGrapplePoint().normalized;

        // 현재 속도의 접선 방향 계산 (로프에 수직인 방향)
        Vector3 currentVelocity = mainRb.linearVelocity;
        Vector3 radialVelocity = Vector3.Project(currentVelocity, ropeDirection);
        float currentSpeed = currentVelocity.magnitude;

        // 현재 속도에 따라 배율 계산 (느리면 적게, 빠르면 많이)
        float speedRatio = Mathf.InverseLerp(minSpeedForMultiply, maxSpeedForMultiply, currentSpeed);
        float speedMultiplier = Mathf.Lerp(minDashMultiplier, maxDashMultiplier, speedRatio);

        Vector3 targetDir = radialVelocity.normalized;
        float finalDashSpeed = swingEndDashSpeed * speedMultiplier;

        mainRb.AddForce(targetDir * finalDashSpeed, ForceMode.VelocityChange);
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

    /// <summary>
    /// Check Ready
    /// </summary>
    /// 
    public bool GetGrabReady(bool isRightHand)
    {
        if (isRightHand)
        {
            return grabController_Right.CurrentState == GrabState.Ready;
        }
        else
        {
            return grabController_Left.CurrentState == GrabState.Ready;
        }
    }

    public bool GetGrappleCheck()
    {
        return grappleChecker.GetGrappleCheck();
    }
    public void StartGrapple(GrappleController currentGrappleController)
    {
        currentGrappleController.StartGrapple(grappleChecker.GetBendPoint());
    }

    public void GrabArmAdjustment()
    {
        if (grabController_Left.CurrentState == GrabState.Attached)
            ragdollAnimator.ApplyGrappleArmCorrection(false, grabController_Left.GetGrabPoint(), 200f);

        if (grabController_Right.CurrentState == GrabState.Attached)
            ragdollAnimator.ApplyGrappleArmCorrection(true, grabController_Right.GetGrabPoint(), 200f);
    }
}