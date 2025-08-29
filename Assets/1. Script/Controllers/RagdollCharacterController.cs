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
    public PlayerState CurrState { get; private set; }

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

    [Header("New Gliding Mechanics")]
    [Tooltip("급강하 시 중력배수")]
    public float diveGravityMultiplier = 2.5f;

    [Tooltip("급강하의 수직 속도를 활강의 수평 속도로 전환하는 정도")]
    public float diveToGlideSpeedConversion = 0.8f;

    [Tooltip("급강하 전환으로 얻을 수 있는 최대 추가 속도")]
    public float maxDiveSpeedBonus = 25f;

    [Tooltip("급강하에서 활강으로 방향이 전환되는 데 걸리는 시간. 길수록 부드럽고 큰 곡선.")]
    public float glideTransitionDuration = 0.6f;

    [Tooltip("급강하로 얻은 추가 속도가 점차 줄어드는 속도.")]
    public float glideBoostDecayRate = 2f;

    private float dashTimer = 0f;
    private Vector3 dashDir = Vector3.zero;
    private float currentGlideBoost = 0f;
    private float transitionProgress = 0f;
    private Vector3 diveDirection;
    private Vector3 targetGlideDirection;
    private Rigidbody mainRb;
    private Vector2 moveInput;
    private GrappleController grappleController;
    private RagdollAnimator ragdollAnimator;
    private Rigidbody[] allRigidbodies;

    void Awake()
    {
        mainRb = charCenterPart.GetComponent<Rigidbody>();
        grappleController = GetComponent<GrappleController>();
        ragdollAnimator = GetComponent<RagdollAnimator>();

        SetPlayerState(PlayerState.Standing);

        Cursor.lockState = CursorLockMode.Confined; //Mouse Screen Lock

        allRigidbodies = GetComponentsInChildren<Rigidbody>();
    }

    void FixedUpdate()
    {
        // 부스트 지속시간 관리
        if (currentGlideBoost > 0)
        {
            currentGlideBoost -= glideBoostDecayRate * Time.fixedDeltaTime;
            if (currentGlideBoost < 0) currentGlideBoost = 0;
        }

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
            if (CurrState != PlayerState.Gliding)
                return;

            SetPlayerState(PlayerState.OnAir);
        }
    }

    public void SetPlayerState(PlayerState state)
    {
        CurrState = state;
        switch (state)
        {
            case PlayerState.Standing:
                ragdollAnimator.SetAnimation(PlayerState.Standing);
                break;
            case PlayerState.Swinging:
                ragdollAnimator.SetHookTarget(grappleController.GetGrapplePoint());
                ragdollAnimator.SetAnimation(PlayerState.Swinging);
                break;
            case PlayerState.OnAir:
                ragdollAnimator.SetAnimation(PlayerState.Standing);
                break;
            case PlayerState.Walking:
                ragdollAnimator.SetAnimation(PlayerState.Walking);
                break;
            case PlayerState.Reeling:
                ragdollAnimator.SetAnimation(PlayerState.Reeling);
                break;
            case PlayerState.Gliding:
                if (moveInput.sqrMagnitude < 0.01f)
                {
                    currentGlideState = GlideState.Gliding;
                    StopAllMotion();
                }
                else
                {
                    currentGlideState = GlideState.Diving;
                }
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
                    mainRb.AddForce(worldDirection.normalized * swingMoveForce, ForceMode.Acceleration);
                    if (mainRb.linearVelocity.magnitude > maxAirSpeed)
                    {
                        Vector3 limitedVelocity = mainRb.linearVelocity.normalized * maxAirSpeed;
                        mainRb.linearVelocity = limitedVelocity;
                    }
                    break;
                }
            case PlayerState.Gliding:
                {
                    GlidingWithDiving(worldDirection);
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

    public void ReduceMomentum(float amount)
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

    private bool IsGrounded()
    {
        Vector3 origin = mainRb.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }

    public enum GlideState
    {
        Dashing,
        Gliding,
        Diving,
        Transitioning
    }
    private GlideState currentGlideState;

    private void GlidingWithDiving(Vector3 worldDirection)
    {
        bool hasInput = worldDirection.sqrMagnitude > 0.01f;

        // 입력이 없으면 무조건 Diving
        if (!hasInput)
        {
            currentGlideState = GlideState.Diving;
        }
        // 입력이 있는데, 이전 상태가 Diving 이었다면 Transitioning 시작
        else if (currentGlideState == GlideState.Diving)
        {
            currentGlideState = GlideState.Transitioning;

            // Transitioning 상태에 진입하는 순간, 필요한 값들을 딱 한 번 설정합니다.
            transitionProgress = 0f;
            diveDirection = mainRb.linearVelocity.normalized;
            targetGlideDirection = worldDirection.normalized;

            float diveBonus = -mainRb.linearVelocity.y * diveToGlideSpeedConversion;
            currentGlideBoost += Mathf.Clamp(diveBonus, 0, maxDiveSpeedBonus);
            ReduceMomentum(0.25f);
        }
        else if (currentGlideState != GlideState.Transitioning)
        {
            currentGlideState = GlideState.Gliding;
        }

        // --- 상태별 실제 행동 로직 ---
        switch (currentGlideState)
        {
            case GlideState.Gliding:
                Vector3 glideDirection = worldDirection.normalized;
                ApplyGlidingForce(glideDirection);
                break;

            case GlideState.Diving:
                // 급강하 로직
                currentGlideBoost = 0;
                dashTimer = 0; // 다이빙 시작 시 대시도 취소
                dashDir = Vector3.zero;

                mainRb.AddForce(Physics.gravity * (diveGravityMultiplier - 1f), ForceMode.Acceleration);

                if (mainRb.linearVelocity.sqrMagnitude > 0.1f)
                {
                    ragdollAnimator.RotateForGliding(mainRb.linearVelocity.normalized, glideTurnSpeed * 1.5f);
                }
                break;

            case GlideState.Transitioning:
                // 전환 로직
                transitionProgress += Time.fixedDeltaTime / glideTransitionDuration;
                Vector3 transitionDirection = Vector3.Slerp(diveDirection, targetGlideDirection, transitionProgress).normalized;

                ApplyGlidingForce(transitionDirection);

                ReduceMomentum(0.8f);

                if (transitionProgress >= 1.0f)
                {
                    // 전환이 끝나면 일반 활강 상태로 변경
                    currentGlideState = GlideState.Gliding;
                }
                break;
        }
    }

    private void ApplyGlidingForce(Vector3 direction)
    {
        ragdollAnimator.RotateForGliding(direction, glideTurnSpeed);

        float currentMaxSpeed = maxAirSpeed + currentGlideBoost;
        Vector3 targetVelocity = direction * currentMaxSpeed;
        Vector3 currentHorizontalVelocity = new Vector3(mainRb.linearVelocity.x, 0, mainRb.linearVelocity.z);
        Vector3 velocityChange = targetVelocity - currentHorizontalVelocity;

        Vector3 finalForce = velocityChange * glideSpeed;
        Vector3 antiGravity = Physics.gravity * -reducedGravity;

        mainRb.AddForce(finalForce, ForceMode.Acceleration);
        mainRb.AddForce(antiGravity, ForceMode.Acceleration);
    }
}