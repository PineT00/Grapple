using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RagdollAnimator : MonoBehaviour
{
    //애니메이터 본의 타겟과 비중을 조절하고
    //애니메이터 본의 회전방향을 조정함
    //'이동'을 제외한 모든 애니메이팅 총괄(타켓통제, 관절 스프링강도 조정)

    [Header("Body Parts")]
    public Transform moveFrame;
    public Transform animHipTrans;


    [Header("Joints")]
    [SerializeField] private ConfigurableJoint mainHipJoint;
    [SerializeField] private ConfigurableJoint spineJoint;
    [SerializeField] private ConfigurableJoint headJoint;
    [SerializeField] private ConfigurableJoint leftLegJoint;
    [SerializeField] private ConfigurableJoint rightLegJoint;
    [SerializeField] private ConfigurableJoint leftCarfJoint;
    [SerializeField] private ConfigurableJoint rightCarfJoint;
    [SerializeField] private ConfigurableJoint leftArmJoint;
    [SerializeField] private ConfigurableJoint rightArmJoint;
    [SerializeField] private ConfigurableJoint leftForeArmJoint;
    [SerializeField] private ConfigurableJoint rightForeArmJoint;

    [Header("Rigs")]
    public Rig normalRig;
    public Rig glideRig;
    public Rig swingRig;
    private Quaternion leftInitialRotation;
    private Quaternion rightInitialRotation;
    private Quaternion headInitialRotation;
    private Quaternion hipInitialRotation;

    [Header("Targets")]
    public Transform leftTarget;
    public Transform rightTarget;

    [Header("스텝 설정")]
    public float stepAngle = 30f; // 다리를 내미는 각도
    public float stepSpeed = 2f;
    private float offsetBetweenLegs = Mathf.PI;

    [Header("일반 상태 설정")]
    public float standBodyDrive = 3000f;
    public float fallDrive = 100f;
    public float normalArmDrive = 30f;
    public float glideArmDrive = 999f;

    [Header("스윙 액션 설정")]
    public Transform swingTarget;
    public float armReachSpeed = 10f;
    private PlayerState currState;
    private float timeCounter = 0f;

    void Awake()
    {
        leftInitialRotation = leftLegJoint.transform.localRotation;
        rightInitialRotation = rightLegJoint.transform.localRotation;
    }

    void FixedUpdate()
    {
        switch (currState)
        {
            case PlayerState.Walking:
                Walking();
                break;
            case PlayerState.Swinging:
                Swinging();
                break;
            case PlayerState.Reeling:
                Swinging();
                break;
            case PlayerState.Gliding:
                break;
        }
    }

    public void ReelingToward(Quaternion targetRot)
    {
        //mainHipJoint.targetRotation = targetRot;
    }

    public void SetHookTarget(Vector3 targetPos)
    {
        swingTarget.position = targetPos;
    }

    public void RotateDirection(Vector3 worldDirection, float turnSpeed)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude > 0.01f)
        {
            Vector3 currentEuler = moveFrame.eulerAngles;
            float targetYaw = Quaternion.LookRotation(worldDirection.normalized, Vector3.up).eulerAngles.y;
            float newYaw = Mathf.MoveTowardsAngle(currentEuler.y, targetYaw, turnSpeed * Time.fixedDeltaTime);
            //mainHipJoint.targetRotation = Quaternion.Euler(0, newYaw, 0);
            animHipTrans.localRotation = Quaternion.Euler(0, newYaw, 0);
        }
    }
    public void SmoothRotate(Vector3 worldDirection, float turnSmoothing)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude > 0.01f)
        {
            float targetYaw = Quaternion.LookRotation(worldDirection.normalized, Vector3.up).eulerAngles.y;
            float currentYaw = moveFrame.eulerAngles.y;
            float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, turnSmoothing * Time.fixedDeltaTime);
            animHipTrans.localRotation = Quaternion.Euler(0, newYaw, 0);
        }
    }

    public void RotateForGliding(Vector3 worldDirection)
    {
        Quaternion targetWorldRotation = Quaternion.LookRotation(worldDirection) * Quaternion.Euler(70, 0, 0);
        animHipTrans.localRotation = targetWorldRotation;
    }

    private void Walking()
    {
        timeCounter += Time.deltaTime * stepSpeed * Mathf.PI * 2f;

        // 사인파 기반 회전
        float leftAngle = Mathf.Sin(timeCounter) * stepAngle;
        float rightAngle = Mathf.Sin(timeCounter + offsetBetweenLegs) * stepAngle;

        leftLegJoint.transform.localRotation = leftInitialRotation * Quaternion.Euler(leftAngle, 0f, 0f);
        rightLegJoint.transform.localRotation = rightInitialRotation * Quaternion.Euler(rightAngle, 0f, 0f);
    }

    private void Swinging()
    {
        animHipTrans.localRotation = mainHipJoint.transform.localRotation;
        SwayWithHand();
    }

    public void SwayWithHand()
    {
        Vector3 toTarget = swingTarget.position - rightForeArmJoint.transform.position;
        if (toTarget.sqrMagnitude < 0.001f)
            return;
        Quaternion lookRot = Quaternion.LookRotation(toTarget, Vector3.up);

        // 조인트 축 보정
        Quaternion correction = Quaternion.Euler(90f, 0f, 0f);
        Quaternion targetRot = lookRot * correction;

        rightArmJoint.transform.rotation = Quaternion.Slerp(rightArmJoint.transform.rotation, targetRot, Time.deltaTime * armReachSpeed);
        rightForeArmJoint.transform.rotation = Quaternion.Slerp(rightForeArmJoint.transform.rotation, targetRot, Time.deltaTime * armReachSpeed);
    }

    public void SetAnimation(PlayerState state)
    {
        currState = state;
        JointDrive hipDrive = mainHipJoint.slerpDrive;
        JointDrive armDrive = leftArmJoint.slerpDrive;
        JointDrive legDrive = leftLegJoint.slerpDrive;

        switch (state)
        {
            case PlayerState.Standing:
            case PlayerState.Walking:
                hipDrive.positionSpring = standBodyDrive;
                mainHipJoint.slerpDrive = hipDrive;

                armDrive.positionSpring = normalArmDrive;
                leftArmJoint.slerpDrive = armDrive;
                leftForeArmJoint.slerpDrive = armDrive;
                rightArmJoint.slerpDrive = armDrive;
                rightForeArmJoint.slerpDrive = armDrive;
                leftLegJoint.slerpDrive = armDrive;
                rightLegJoint.slerpDrive = armDrive;

                normalRig.weight = 1.0f;
                swingRig.weight = 0f;
                glideRig.weight = 0f;
                break;
            case PlayerState.Swinging:
                hipDrive.positionSpring = fallDrive;
                mainHipJoint.slerpDrive = hipDrive;

                armDrive.positionSpring = normalArmDrive;
                leftArmJoint.slerpDrive = armDrive;
                leftForeArmJoint.slerpDrive = armDrive;
                rightArmJoint.slerpDrive = armDrive;
                rightForeArmJoint.slerpDrive = armDrive;

                normalRig.weight = 0f;
                swingRig.weight = 1.0f;
                glideRig.weight = 0f;
                break;
            case PlayerState.Reeling:
                hipDrive.positionSpring = fallDrive;
                mainHipJoint.slerpDrive = hipDrive;
                break;
            case PlayerState.Gliding:
                hipDrive.positionSpring = standBodyDrive;
                spineJoint.slerpDrive = hipDrive;
                mainHipJoint.slerpDrive = hipDrive;

                armDrive.positionSpring = glideArmDrive;
                leftArmJoint.slerpDrive = armDrive;
                leftForeArmJoint.slerpDrive = armDrive;
                rightArmJoint.slerpDrive = armDrive;
                rightForeArmJoint.slerpDrive = armDrive;

                leftLegJoint.slerpDrive = armDrive;
                rightLegJoint.slerpDrive = armDrive;

                normalRig.weight = 0f;
                swingRig.weight = 0f;
                glideRig.weight = 1.0f;
                break;

        }
    }

}
