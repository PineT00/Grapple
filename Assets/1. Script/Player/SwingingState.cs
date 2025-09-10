using UnityEngine;
using UnityEngine.InputSystem;

public class SwingingState : PlayerBaseState
{
    public SwingingState(RagdollCharacterController controller) : base(controller) { }

    public override void EnterState()
    {
        _controller.grappleController.OnGrapple();
        _ragdollAnimator.SetHookTarget(_controller.grappleController.GetGrapplePoint());
        _ragdollAnimator.SetAnimation(PlayerState.Swinging);
    }

    public override void FixedUpdateState()
    {
        _controller.HandleSwingMovement();
    }

    public override void ExitState()
    {
        _controller.grappleController.OnRelease();
    }

    public override void OnGrapple(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() <= 0)
        {
            _controller.SwitchState(new OnAirState(_controller));
        }
    }
}
