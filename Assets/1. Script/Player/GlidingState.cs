using UnityEngine;
using UnityEngine.InputSystem;

public class GlidingState : PlayerBaseState
{
    public enum GlideSubState { Dashing, Gliding, Diving, Transitioning }
    private GlideSubState _currentSubState;

    private float _glideDashTimer;
    private float _transitionProgress;
    private Vector3 _diveDirection;
    private Vector3 _targetGlideDirection;

    public GlidingState(RagdollCharacterController controller) : base(controller) { }

    public override void EnterState()
    {
        _ragdollAnimator.SetAnimation(PlayerAnimState.Gliding);

        _controller.UpdateMoveInfo();
        _controller.CalculateMomentumBonus();

        _currentSubState = GlideSubState.Dashing;
        _glideDashTimer = _controller.glideDashTime;

        //_controller.CurrentGlideBoost = _controller.dashSpeed;
        _controller.CurrentGlideBoost = _controller.momentumBonus;

        if (_controller.worldDirection.sqrMagnitude > 0.1f)
        {
            _controller.DashDir = _controller.worldDirection;
        }
        else
        {
            Vector3 initialDashDir = _controller.camTarget.forward;
            initialDashDir.y = 0f;
            _controller.DashDir = initialDashDir.normalized;
        }
        _controller.airTrailFeedback.PlayFeedbacks();
    }

    public override void FixedUpdateState()
    {
        _controller.UpdateMoveInfo();
        bool hasInput = _controller.moveInput.sqrMagnitude > 0.01f;

        if (_currentSubState == GlideSubState.Dashing)
        {
            _glideDashTimer -= Time.fixedDeltaTime;
            if (_glideDashTimer <= 0f)
            {
                _currentSubState = GlideSubState.Gliding;
                _controller.worldDirection = _controller.DashDir;
                _glideDashTimer = 0f;
            }
        }

        if (_controller.CurrentGlideBoost > 0)
        {
            _controller.CurrentGlideBoost -= _controller.glideBoostDecayRate * Time.fixedDeltaTime;
            if (_controller.CurrentGlideBoost < 0) _controller.CurrentGlideBoost = 0;
        }

        // 입력 여부에 따른 상태 전환
        if (hasInput)
        {
            if (_currentSubState == GlideSubState.Diving)
            {
                _currentSubState = GlideSubState.Transitioning;
                _transitionProgress = 0f;
                _diveDirection = _controller.mainRb.linearVelocity.normalized;
                _targetGlideDirection = _controller.worldDirection;

                // 다이빙으로 얻은 수직 속도를 수평 부스트로 전환
                float diveBonus = -_controller.mainRb.linearVelocity.y * _controller.diveToGlideSpeedConversion;
                _controller.CurrentGlideBoost = Mathf.Clamp(diveBonus, 0, _controller.maxDiveSpeedBoost);
            }
        }
        else // 방향 입력이 없으면
        {
            if (_currentSubState != GlideSubState.Dashing)
            {
                _currentSubState = GlideSubState.Diving;
            }
        }
        _controller.currGlideStateUI.text = _currentSubState.ToString();


        switch (_currentSubState)
        {
            case GlideSubState.Dashing:
                _controller.HandleDashingMovement(_controller.DashDir);
                break;

            case GlideSubState.Gliding:
                _controller.HandleStandardGlidingMovement(_controller.worldDirection);
                break;

            case GlideSubState.Diving:
                _controller.HandleDivingMovement();
                break;

            case GlideSubState.Transitioning:
                _transitionProgress += Time.fixedDeltaTime / _controller.glideTransitionDuration;
                Vector3 transitionDirection = Vector3.Slerp(_diveDirection, _targetGlideDirection, _transitionProgress).normalized;
                _controller.HandleTransitionMovement(transitionDirection);

                if (_transitionProgress >= 1.0f)
                {
                    _currentSubState = GlideSubState.Gliding;
                }
                break;
        }

        if (_controller.IsGrounded())
        {
            _controller.SwitchState(new StandingState(_controller));
        }
    }

    public override void ExitState()
    {
        _controller.CurrentGlideBoost = 0f;
        _controller.currGlideStateUI.text = "";
        _controller.airTrailFeedback.StopFeedbacks();
    }

    public override void OnGlide(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            _controller.SwitchState(new RollingState(_controller));
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
