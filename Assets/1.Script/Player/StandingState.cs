using UnityEngine;
using UnityEngine.InputSystem;

public class StandingState : PlayerBaseState
{
    public StandingState(RagdollCharacterController controller) : base(controller) { }

    public override void EnterState()
    {
        _controller.CalculateMomentumBonus();

        Quaternion standRot = _ragdollAnimator.animHipTrans.localRotation;
        standRot.x = 0f;
        standRot.z = 0f;
        _ragdollAnimator.animHipTrans.localRotation = standRot;
        _ragdollAnimator.SetAnimation(PlayerAnimState.Standing);
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

        _grappleController.CheckForGrapplePoint();
        _controller.MovementControl();
        _controller.ApplyHovering();
    }

    public override void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _controller.JumpControl(_controller.jumpForce);
            _controller.jumpFeedback.PlayFeedbacks();
            _controller.SwitchState(new OnAirState(_controller));
        }
    }

    public override void OnGrapple(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0 && _grappleController.GrappleReady)
        {
            _controller.SwitchState(new SwingingState(_controller));
        }
    }

    public override void OnGrab(InputAction.CallbackContext context)
    {
        _controller.grabController.OnGrab(context);
    }
}
