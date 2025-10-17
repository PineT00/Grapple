using UnityEngine;
using UnityEngine.InputSystem;

public class WallRunState : PlayerBaseState
{
    // 벽타기 파라미터
    private Vector3 wallNormal;
    private Vector3 wallRunDirection;
    private float wallRunTimer;
    private float currentGravityReduction;
    private Collider currentWall;

    // 설정값 (RagdollCharacterController에서 가져옴)
    private float maxWallRunTime => _controller.maxWallRunTime;
    private float initialGravityReduction => _controller.wallRunGravity;
    private float gravityReductionDecay => _controller.wallRunSpeedDecay;
    private float minGravityReduction => _controller.minWallRunSpeed;
    private float wallStickForce => _controller.wallStickForce;
    private float wallDetectionDistance => _controller.wallDetectionDistance;

    public WallRunState(RagdollCharacterController controller, Vector3 wallNormal, Vector3 velocity)
        : base(controller)
    {
        this.wallNormal = wallNormal;

        // 진입 시 속도를 기반으로 벽타기 방향 계산
        Vector3 forward = Vector3.ProjectOnPlane(velocity, wallNormal).normalized;
        wallRunDirection = forward;

        // 초기 중력 감소율 설정
        currentGravityReduction = initialGravityReduction;
    }

    public override void EnterState()
    {
        _ragdollAnimator.SetAnimation(PlayerAnimState.WallRunning);
        wallRunTimer = 0f;

        // 스윙 중 벽에 부딪혔다면 모멘텀 유지
        _controller.CalculateMomentumBonus();
    }

    public override void FixedUpdateState()
    {
        // 1. 벽 감지 체크
        if (!CheckWallContact())
        {
            _controller.SwitchState(new RollingState(_controller));
            return;
        }

        // 2. 타이머 및 중력 감소율 감소
        wallRunTimer += Time.fixedDeltaTime;
        currentGravityReduction -= gravityReductionDecay * Time.fixedDeltaTime;

        // 3. 중력 감소율이 최소치 이하거나 시간 초과 시 떨어짐
        if (currentGravityReduction < minGravityReduction || wallRunTimer > maxWallRunTime)
        {
            _controller.SwitchState(new RollingState(_controller));
            return;
        }

        // 4. 지면 도달 체크
        if (_controller.IsGrounded())
        {
            _controller.SwitchState(new StandingState(_controller));
            return;
        }

        // 5. 벽타기 움직임 적용
        ApplyWallRunMovement();
    }

    private bool CheckWallContact()
    {
        // 벽 방향으로 레이캐스트
        Vector3 rayOrigin = _controller.moveFrame.position;
        Vector3 rayDirection = -wallNormal;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit,
            wallDetectionDistance, _controller.groundLayer))
        {
            wallNormal = hit.normal;
            currentWall = hit.collider;
            return true;
        }
        return false;
    }

    private void ApplyWallRunMovement()
    {
        // 입력 방향 업데이트
        _controller.UpdateMoveInfo();

        // 플레이어 입력이 있으면 방향 조정 (제한적으로)
        if (_controller.worldDirection.sqrMagnitude > 0.01f)
        {
            Vector3 inputProjected = Vector3.ProjectOnPlane(_controller.worldDirection, wallNormal);
            wallRunDirection = Vector3.Slerp(wallRunDirection, inputProjected.normalized,
                0.3f * Time.fixedDeltaTime);
        }

        // 벽을 따라 이동 (현재 속도 유지)
        Vector3 horizontalVelocity = _controller.mainRb.linearVelocity;
        horizontalVelocity.y = 0;

        // 입력 방향으로 약간의 조향력 추가
        if (_controller.worldDirection.sqrMagnitude > 0.01f)
        {
            Vector3 inputProjected = Vector3.ProjectOnPlane(_controller.worldDirection, wallNormal);
            _controller.mainRb.AddForce(inputProjected.normalized * 5f, ForceMode.Acceleration);
        }

        // 벽에 붙어있는 힘 (벽에서 떨어지지 않도록)
        Vector3 stickForce = -wallNormal * wallStickForce;
        _controller.mainRb.AddForce(stickForce, ForceMode.Acceleration);

        // 중력 상쇄 (시간에 따라 감소)
        _controller.mainRb.AddForce(Vector3.up * currentGravityReduction, ForceMode.Acceleration);

        // 캐릭터 방향 회전
        if (horizontalVelocity.sqrMagnitude > 0.1f)
        {
            _controller.RotateDirection(horizontalVelocity);
        }
    }

    public override void ExitState()
    {
        // 벽타기 종료 시 약간의 벽에서 튕겨나가는 힘 (선택사항)
        // _controller.mainRb.AddForce(wallNormal * 3f, ForceMode.Impulse);
    }

    public override void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // 벽 점프: 벽에서 튕겨나가며 점프
            Vector3 jumpDirection = (wallNormal + Vector3.up).normalized;
            _controller.mainRb.AddForce(jumpDirection * _controller.jumpForce, ForceMode.Impulse);
            _controller.SwitchState(new OnAirState(_controller));
        }
    }

    public override void OnGrapple(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0)
        {
            _controller.SwitchState(new SwingingState(_controller));
        }
    }

    public override void OnGlide(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _controller.SwitchState(new GlidingState(_controller));
        }
    }

    public override void OnGrab(InputAction.CallbackContext context)
    {
        _controller.grabController.OnGrab(context);
    }
}
