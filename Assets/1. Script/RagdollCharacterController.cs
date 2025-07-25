using UnityEngine;
using UnityEngine.InputSystem;

public class RagdollCharacterController : MonoBehaviour
{
    [Header("Essencial")]
    public LayerMask groundLayer;
    public Transform camTarget;
    public Rigidbody mainRb;
    public Transform bodyTrans;
    public ConfigurableJoint mainJoint;

    [Header("Settings")]
    public float moveForce = 30f;
    public float maxSpeed = 5f;
    public float swingMoveForce = 10f;

    public float jumpForce = 7f;
    public float turnSpeed = 5f;
    public float groundCheckDistance = 0.2f;

    private Vector2 moveInput;
    private PlayerState currState;


    [Header("좌/우 다리 조인트")]
    public ConfigurableJoint leftHipJoint;
    public ConfigurableJoint rightHipJoint;
    public Quaternion leftInitialRotation;
    public Quaternion rightInitialRotation;

    [Header("스텝 설정")]
    public float stepAngle = 30f; // 다리를 내미는 각도
    public float stepDuration = 0.3f;

    private float stepTimer = 0f;
    private bool isLeftStep = true;

    void Awake()
    {
        if (mainRb == null)
        {
            mainRb = GetComponent<Rigidbody>();
        }
        currState = PlayerState.Walking;
        Cursor.lockState = CursorLockMode.Confined;
        //leftInitialRotation = leftHipJoint.transform.localRotation;
        //rightInitialRotation = rightHipJoint.transform.localRotation;
    }

    void FixedUpdate()
    {
        HandleMovement();
        directionCheck();

        stepTimer += Time.fixedDeltaTime;
        if (stepTimer >= stepDuration)
        {
            stepTimer = 0f;
            DoStep();
            isLeftStep = !isLeftStep;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (IsGrounded())
            {
                Vector3 vel = mainRb.linearVelocity;
                vel.y = 0;
                mainRb.linearVelocity = vel;
                mainRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
    }
    public void SetPlayerState(PlayerState state)
    {
        currState = state;
    }

    void HandleMovement()
    {
        if (moveInput.sqrMagnitude < 0.1f)
            return;

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 worldDirection = camTarget.TransformDirection(inputDirection);
        worldDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);

        switch (currState)
        {
            case PlayerState.Walking:
                {
                    Vector3 targetVelocity = worldDirection.normalized * maxSpeed;
                    Vector3 horizontalVelocity = mainRb.linearVelocity;
                    horizontalVelocity.y = 0f;

                    Vector3 velocityChange = targetVelocity - horizontalVelocity;
                    velocityChange.y = 0f;

                    mainRb.AddForce(velocityChange * moveForce, ForceMode.Acceleration);
                    break;
                }
            case PlayerState.Swinging:
                {
                    // 단순히 방향으로 힘을 더해주기
                    mainRb.AddForce(worldDirection.normalized * swingMoveForce, ForceMode.Acceleration);
                    break;
                }
        }
    }

    private void directionCheck()
    {
        float yCurrent = bodyTrans.rotation.eulerAngles.y;
        float yTarget = camTarget.rotation.eulerAngles.y;

        float diff = Mathf.DeltaAngle(yCurrent, yTarget);

        if (Mathf.Abs(diff) > 0.1f)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, -yTarget, 0f);
            mainJoint.targetRotation = targetRotation;
        }
    }

    bool IsGrounded()
    {
        Vector3 origin = bodyTrans.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }
    
    

    void DoStep()
    {
        // 앞쪽으로 뻗는 회전값 (local 기준)
        Quaternion forwardRot = Quaternion.Euler(-stepAngle, 0f, 0f);
        Quaternion neutralRot = Quaternion.identity;

        if (isLeftStep)
        {
            leftHipJoint.targetRotation = Quaternion.Inverse(leftInitialRotation) * forwardRot;
            rightHipJoint.targetRotation = Quaternion.Inverse(rightInitialRotation) * neutralRot;
        }
        else
        {
            leftHipJoint.targetRotation = Quaternion.Inverse(leftInitialRotation) * neutralRot;
            rightHipJoint.targetRotation = Quaternion.Inverse(rightInitialRotation) * forwardRot;
        }
    }
}