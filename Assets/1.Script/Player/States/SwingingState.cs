using System.Collections.Generic;
using UnityEngine.InputSystem;

public class SwingingState : PlayerBaseState
{
    public SwingingState(RagdollCharacterController controller) : base(controller) { }
    private readonly List<GrappleController> activeGrappleList = new(2);
    public override void EnterState()
    {
        _controller.CalculateMomentumBonus();
        _ragdollAnimator.SetAnimation(PlayerAnimState.Swinging);
    }

    public override void FixedUpdateState()
    {
        activeGrappleList.Clear();

        //모든 과정을 좌우 따로 검사해 돌아가게 하면 됌.
        switch (_controller.grappleController_Left.CurrentState)
        {
            case GrappleState.None:
                break;
            case GrappleState.Launching:
                _ragdollAnimator.ApplyGrappleArmCorrection(false, _controller.grappleController_Left.GetGrapplePoint(), 200f);
                break;
            case GrappleState.Attached:
                _ragdollAnimator.ApplyGrappleArmCorrection(false, _controller.grappleController_Left.GetGrapplePoint(), 200f);
                activeGrappleList.Add(_controller.grappleController_Left);
                _controller.grappleController_Left.HandleRopePhysics();
                break;
            case GrappleState.Reeling:
                _ragdollAnimator.ApplyGrappleArmCorrection(false, _controller.grappleController_Left.GetGrapplePoint(), 200f);
                _controller.grappleController_Left.ShortenRope();
                _controller.grappleController_Left.HandleRopePhysics();
                break;
        }

        switch (_controller.grappleController_Right.CurrentState)
        {
            case GrappleState.None:
                break;
            case GrappleState.Launching:
                _ragdollAnimator.ApplyGrappleArmCorrection(true, _controller.grappleController_Right.GetGrapplePoint(), 200f);
                break;
            case GrappleState.Attached:
                _ragdollAnimator.ApplyGrappleArmCorrection(true, _controller.grappleController_Right.GetGrapplePoint(), 200f);
                activeGrappleList.Add(_controller.grappleController_Right);
                _controller.grappleController_Right.HandleRopePhysics();
                break;
            case GrappleState.Reeling:
                _ragdollAnimator.ApplyGrappleArmCorrection(true, _controller.grappleController_Right.GetGrapplePoint(), 200f);
                _controller.grappleController_Right.ShortenRope();
                _controller.grappleController_Right.HandleRopePhysics();
                break;
        }
        _controller.HandleSwingMovement(activeGrappleList);
        _controller.MultiflyGravity();
    }

    public override void ExitState()
    {
        _controller.grappleController_Left.ReleaseGrapple();
        _controller.grappleController_Right.ReleaseGrapple();
        //_controller.MultiflyHorizontalforce();
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
            if (oppositeGrapple.CurrentState == GrappleState.None)
            {
                _controller.MultiflyForce(currentGrapple);
                currentGrapple.ReleaseGrapple();
                _controller.SwitchState(new OnAirState(_controller));
            }
            else
            {
                currentGrapple.ReleaseGrapple();
            }
        }
    }

    public override void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (_controller.grappleController_Left.CurrentState == GrappleState.Attached)
            {
                _controller.grappleController_Left.StartReeling();
                _controller.ragdollAnimator.SetAnimation(PlayerAnimState.Reeling);
            }
            if (_controller.grappleController_Right.CurrentState == GrappleState.Attached)
            {
                _controller.grappleController_Right.StartReeling();
                _controller.ragdollAnimator.SetAnimation(PlayerAnimState.Reeling);
            }
        }
        else if (context.canceled)
        {
            if (_controller.grappleController_Left.CurrentState == GrappleState.Attached)
            {
                _controller.grappleController_Left.StopReeling();
                _controller.ragdollAnimator.SetAnimation(PlayerAnimState.Swinging);
            }
            if (_controller.grappleController_Right.CurrentState == GrappleState.Attached)
            {
                _controller.grappleController_Right.StopReeling();
                _controller.ragdollAnimator.SetAnimation(PlayerAnimState.Swinging);
            }
        }
    }
}
