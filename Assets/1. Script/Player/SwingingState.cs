using UnityEngine;
using UnityEngine.InputSystem;

public class SwingingState : PlayerBaseState
{
    public SwingingState(RagdollCharacterController controller) : base(controller) {}

    bool isReeling = false;

    public override void EnterState()
    {
        _controller.grappleController.OnGrapple();
        _ragdollAnimator.SetAnimation(PlayerState.Swinging);
    }

    public override void FixedUpdateState()
    {
        if (!_controller.grappleController.IsAttached)
        {
            return; // 아직 발사 중이면 아무것도 하지 않음
        }

        if (!isReeling)
        {
            _controller.HandleSwingMovement();
        }
        else
        {
            _controller.grappleController.ShortenRope();
        }
        _controller.grappleController.HandleRopePhysics();
    }

    public override void ExitState()
    {
        _controller.grappleController.OnRelease();
    }

    public override void OnGrapple(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() <= 0)
        {
            _controller.SwitchState(new RollingState(_controller));
        }
    }

    public override void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isReeling = true;
            _controller.grappleController.StartReeling();
        }
        else if (context.canceled)
        {
            isReeling = false;
            _controller.grappleController.StopReeling();
        }
    }
}
