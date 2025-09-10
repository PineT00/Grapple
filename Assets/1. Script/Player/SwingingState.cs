using UnityEngine;
using UnityEngine.InputSystem;

public class SwingingState : PlayerBaseState
{
    public SwingingState(RagdollCharacterController controller) : base(controller) {}

    bool isReeling = false;

    public override void EnterState()
    {
        _controller.grappleController.OnGrapple();
        _ragdollAnimator.SetHookTarget(_controller.grappleController.GetGrapplePoint());
        _ragdollAnimator.SetAnimation(PlayerState.Swinging);
    }

    public override void FixedUpdateState()
    {
        if(!isReeling)
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
        }
        else if (context.canceled)
        {
            isReeling = false;
        }
    }
}
