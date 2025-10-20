using UnityEngine;
using UnityEngine.InputSystem;

public class SwingingState : PlayerBaseState
{
    public SwingingState(RagdollCharacterController controller) : base(controller) { }

    bool isReeling = false;

    public override void EnterState()
    {
        _controller.CalculateMomentumBonus();
        _controller.grappleController.OnGrapple();
        _ragdollAnimator.SetAnimation(PlayerAnimState.Swinging);
    }

    public override void FixedUpdateState()
    {
        if (!_controller.grappleController.IsAttached)
        {
            _ragdollAnimator.ApplyGrappleArmCorrection(true, _grappleController.GetGrapplePoint(), 200f);
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

        if (!isReeling)
        {
            _controller.MovementControl();
        }
        else
        {
            _controller.grappleController.ShortenRope();
        }
        _controller.MultiflyGravity();
        _controller.grappleController.HandleRopePhysics();
        _ragdollAnimator.ApplyGrappleArmCorrection(true, _grappleController.GetGrapplePoint(), 200f);
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

    public override void ExitState()
    {
        _controller.grappleController.OnRelease();
        _controller.MultiflyHorizontalforce();
    }

    public override void OnClick(InputAction.CallbackContext context)
    {
        if (_controller.GetGrabReady(false))
        {
            _controller.grabController_Left.OnGrab(context);
        }
    }
    public override void OnRightClick(InputAction.CallbackContext context)
    {
        if (_controller.GetGrabReady(true))
        {
            _controller.grabController_Right.OnGrab(context);
        }
    }

    public override void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isReeling = true;
            _controller.grappleController.StartReeling();
            _controller.ragdollAnimator.SetAnimation(PlayerAnimState.Reeling);
        }
        else if (context.canceled)
        {
            isReeling = false;
            _controller.grappleController.StopReeling();
            _controller.ragdollAnimator.SetAnimation(PlayerAnimState.Swinging);
        }
    }
}
