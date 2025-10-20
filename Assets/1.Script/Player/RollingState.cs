using UnityEngine;
using UnityEngine.InputSystem;

public class RollingState : PlayerBaseState
{
    public RollingState(RagdollCharacterController controller) : base(controller) { }

    public override void EnterState()
    {
        _controller.CalculateMomentumBonus();
        _ragdollAnimator.SetAnimation(PlayerAnimState.Rolling);
    }

    public override void FixedUpdateState()
    {
        if (_controller.IsGrounded())
        {
            _controller.SwitchState(new StandingState(_controller));
            return;
        }

        // 벽 충돌 체크 추가
        if (CheckWallCollision(out Vector3 wallNormal))
        {
            float currentSpeed = _controller.mainRb.linearVelocity.magnitude;
            if (currentSpeed >= _controller.minSpeedForWallRun)
            {
                _controller.SwitchState(new WallRunState(_controller, wallNormal,
                    _controller.mainRb.linearVelocity));
                return;
            }
        }

        _controller.MovementControl();
        _controller.MultiflyGravity();
    }

    private bool CheckWallCollision(out Vector3 wallNormal)
    {
        wallNormal = Vector3.zero;
        Vector3 velocity = _controller.mainRb.linearVelocity;
        if (velocity.sqrMagnitude < 0.1f) return false;

        Vector3 rayOrigin = _controller.moveFrame.position;
        Vector3 rayDirection = velocity.normalized;
        float rayDistance = 2f;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit,
            rayDistance, _controller.groundLayer))
        {
            // 벽인지 확인 (법선이 수평에 가까운지)
            float verticalDot = Vector3.Dot(hit.normal, Vector3.up);
            if (Mathf.Abs(verticalDot) < 0.3f) // 거의 수직인 벽
            {
                wallNormal = hit.normal;
                return true;
            }
        }
        return false;
    }

    public override void OnGlide(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _controller.SwitchState(new GlidingState(_controller));
        }
    }

    public override void OnGrapple(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0 && _grappleController.GrappleReady)
        {
            _controller.SwitchState(new SwingingState(_controller));
        }
    }

    public override void OnGrab(InputAction.CallbackContext context)
    {
        _controller.grabController.OnGrab(context);
    }
}
