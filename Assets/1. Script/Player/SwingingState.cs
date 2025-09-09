using UnityEngine;

public class SwingingState : PlayerBaseState
{
    public SwingingState(RagdollCharacterController controller) : base(controller){}

    public override void EnterState()
    {
        _ragdollAnimator.SetAnimation(PlayerState.Swinging);
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
