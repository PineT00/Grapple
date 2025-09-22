using UnityEngine;
using UnityEngine.InputSystem;

public class OnAirState : PlayerBaseState
{
    public OnAirState(RagdollCharacterController controller) : base(controller) { }

    public override void EnterState()
    {
        _ragdollAnimator.SetAnimation(PlayerAnimState.OnAir);
    }

    public override void FixedUpdateState()
    {
        if (_controller.IsGrounded())
        {
            _controller.SwitchState(new StandingState(_controller));
            return;
        }

        _controller.HandleAirMove();
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
}
