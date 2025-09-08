using UnityEngine;
using UnityEngine.InputSystem;

public class OnAirState : PlayerBaseState
{
    public OnAirState(RagdollCharacterController controller) : base(controller)
    {
    }

    public override void EnterState()
    {
        _ragdollAnimator.SetAnimation(PlayerState.OnAir);
    }

    public override void FixedUpdateState()
    {
        if (_controller.IsGrounded())
        {
            _controller.SwitchState(new StandingState(_controller));
            return;
        }

        _controller.HandleAirMovement();
    }
}
