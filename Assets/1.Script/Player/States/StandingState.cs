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
    public override void OnClick(InputAction.CallbackContext context)
    {
        HandleInput(context, _controller.grappleController_Left, _controller.grabController_Left, _controller.grappleController_Right);
    }

    public override void OnRightClick(InputAction.CallbackContext context)
    {
        HandleInput(context, _controller.grappleController_Right, _controller.grabController_Right, _controller.grappleController_Left);
    }

    private void HandleInput(InputAction.CallbackContext context, GrappleController currentGrapple,
    GrabController currentGrab, GrappleController oppositeGrapple)
    {
        bool isPressed = context.ReadValue<float>() > 0;
        if (isPressed)
        {
            HandlePress(currentGrapple, currentGrab);
        }
        else
        {
            HandleRelease(currentGrapple, currentGrab, oppositeGrapple);
        }
    }

    private void HandlePress(GrappleController grapple, GrabController grab)
    {
        if (grapple.CurrentState != GrappleState.None) return;
        if (grab.CurrentState == GrabState.Attached) return;

        if (_controller.GetGrabReady(false))
        {
            grab.SetGrab(true);
        }
        else if (_controller.GetGrappleCheck())
        {
            _controller.StartGrapple(grapple);
            _controller.SwitchState(new SwingingState(_controller));
        }
    }

    private void HandleRelease(GrappleController currentGrapple, GrabController grab, GrappleController oppositeGrapple)
    {
        if (grab.CurrentState == GrabState.Attached)
        {
            grab.SetGrab(false);
        }
        else if (currentGrapple.CurrentState != GrappleState.None)
        {
            currentGrapple.ReleaseGrapple();
        }
    }
}
