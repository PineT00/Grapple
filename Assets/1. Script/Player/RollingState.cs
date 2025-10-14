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

        //_controller.HandleAirMove();
        _controller.MovementControl();
        _controller.MultiflyGravity();
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
        if (context.ReadValue<float>() > 0)
        {
            _controller.SwitchState(new SwingingState(_controller));
        }
    }

    public override void OnGrab(InputAction.CallbackContext context)
    {
        _controller.grabController.OnGrab(context);
    }
}
