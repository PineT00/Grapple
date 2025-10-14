using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBaseState
{
    protected RagdollCharacterController _controller;
    protected RagdollAnimator _ragdollAnimator;

    // 생성자: 상태가 생성될 때 컨트롤러의 참조를 받아옵니다.
    public PlayerBaseState(RagdollCharacterController controller)
    {
        _controller = controller;
        _ragdollAnimator = controller.ragdollAnimator;
    }

    public virtual void EnterState() { }
    public virtual void FixedUpdateState() { }
    public virtual void ExitState() { }

    public virtual void OnJump(InputAction.CallbackContext context) { }
    public virtual void OnGlide(InputAction.CallbackContext context) { }
    public virtual void OnGrapple(InputAction.CallbackContext context) { }
    public virtual void OnGrab(InputAction.CallbackContext context) { }
}
