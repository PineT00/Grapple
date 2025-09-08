using UnityEngine;
using UnityEngine.InputSystem;

public class StandingState : PlayerBaseState
{
    public StandingState(RagdollCharacterController controller) : base(controller) { }

    public override void EnterState()
    {
        _ragdollAnimator.SetAnimation(PlayerState.Standing);
    }

    public override void FixedUpdateState()
    {
        if (!_controller.IsGrounded())
        {
            _controller.SwitchState(new OnAirState(_controller));
            return;
        }

        if (_controller.moveInput.sqrMagnitude > 0.1f)
        {
            _controller.SwitchState(new WalkingState(_controller));
            return;
        }
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
