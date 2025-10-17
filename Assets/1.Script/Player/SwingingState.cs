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
            Debug.Log("손뻗기");
            Debug.Log(_grappleController.GetGrapplePoint());
            return;
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
        _controller.MultiflyGravity();
        _ragdollAnimator.ApplyGrappleArmCorrection(true, _grappleController.GetGrapplePoint(), 200f);
    }

    public override void ExitState()
    {
        _controller.grappleController.OnRelease();
        _controller.MultiflyHorizontalforce();
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
            _controller.ragdollAnimator.SetAnimation(PlayerAnimState.Reeling);
        }
        else if (context.canceled)
        {
            isReeling = false;
            _controller.grappleController.StopReeling();
            _controller.ragdollAnimator.SetAnimation(PlayerAnimState.Swinging);
        }
    }

    public override void OnGrab(InputAction.CallbackContext context)
    {
        _controller.grabController.OnGrab(context);
    }
}
