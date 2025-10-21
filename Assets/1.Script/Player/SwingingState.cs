using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwingingState : PlayerBaseState
{
    public SwingingState(RagdollCharacterController controller) : base(controller) { }

    bool isReeling = false;

    public override void EnterState()
    {
        _controller.CalculateMomentumBonus();
        _ragdollAnimator.SetAnimation(PlayerAnimState.Swinging);
    }

    public override void FixedUpdateState()
    {
        //모든 과정을 좌우 따로 검사해 돌아가게 하면 됌.

        switch (_controller.grappleController_Left.CurrentState)
        {
            case GrappleState.None:
                break;
            case GrappleState.Launching:
                _ragdollAnimator.ApplyGrappleArmCorrection(true, _controller.grappleController_Left.GetGrapplePoint(), 200f);
                break;
            case GrappleState.Attached:
                _ragdollAnimator.ApplyGrappleArmCorrection(true, _controller.grappleController_Left.GetGrapplePoint(), 200f);
                _controller.MovementControl();
                _controller.grappleController_Left.HandleRopePhysics();
                break;
            case GrappleState.Reeling:
                _ragdollAnimator.ApplyGrappleArmCorrection(true, _controller.grappleController_Left.GetGrapplePoint(), 200f);
                _controller.grappleController_Left.ShortenRope();
                break;
        }

        _controller.MultiflyGravity();
    }

    public override void ExitState()
    {
        _controller.grappleController_Left.OnRelease();
        _controller.grappleController_Right.OnRelease();
        _controller.MultiflyHorizontalforce();
    }

    public override void OnClick(InputAction.CallbackContext context)
    {
        _controller.grabController_Left.OnGrab(context);
    }
    public override void OnRightClick(InputAction.CallbackContext context)
    {
        _controller.grabController_Right.OnGrab(context);
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
