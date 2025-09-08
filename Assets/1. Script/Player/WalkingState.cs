using UnityEngine;
using UnityEngine.InputSystem;

public class WalkingState : PlayerBaseState
{
    public WalkingState(RagdollCharacterController controller) : base(controller) { }

    public override void EnterState()
    {
        _ragdollAnimator.SetAnimation(PlayerState.Walking);
    }

    public override void FixedUpdateState()
    {
        if (!_controller.IsGrounded())
        {
            _controller.SwitchState(new OnAirState(_controller));
            return;
        }

        if (_controller.moveInput.sqrMagnitude < 0.1f)
        {
            _controller.SwitchState(new StandingState(_controller));
            return;
        }

        _controller.HandleGroundMovement();
    }

    public override void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _controller.JumpControl(_controller.jumpForce);
            _controller.SwitchState(new OnAirState(_controller));
        }
    }
}
