using UnityEngine;

public class RagdollAnimator : MonoBehaviour
{

    [Header("Body Parts")]
    public Transform moveFrame;
    public Transform head;
    public Transform hip;
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform leftArm;
    public Transform leftForeArm;
    public Transform rightArm;
    public Transform rightForeArm;

    private ConfigurableJoint headJoint;
    private ConfigurableJoint mainHipJoint;
    private ConfigurableJoint leftLegJoint;
    private ConfigurableJoint rightLegJoint;
    private ConfigurableJoint leftArmJoint;
    private ConfigurableJoint rightArmJoint;
    private ConfigurableJoint leftForeArmJoint;
    private ConfigurableJoint rightForeArmJoint;

    private Quaternion leftInitialRotation;
    private Quaternion rightInitialRotation;
    private Quaternion headInitialRotation;
    private Quaternion hipInitialRotation;


    [Header("스텝 설정")]
    public float stepAngle = 30f; // 다리를 내미는 각도
    public float stepSpeed = 2f;
    private float offsetBetweenLegs = Mathf.PI;

    [Header("오뚝이 설정")]
    public float standDrive = 2000f;
    public float fallDrive = 100f;

    [Header("스윙 액션 설정")]
    public float armReachSpeed = 10f;
    private PlayerState currState;
    private Vector3 curHookTargetPos;
    private float timeCounter = 0f;


    void Awake()
    {
        headJoint = head.GetComponent<ConfigurableJoint>();
        mainHipJoint = hip.GetComponent<ConfigurableJoint>();
        leftLegJoint = leftLeg.GetComponent<ConfigurableJoint>();
        rightLegJoint = rightLeg.GetComponent<ConfigurableJoint>();
        leftArmJoint = leftArm.GetComponent<ConfigurableJoint>();
        rightArmJoint = rightArm.GetComponent<ConfigurableJoint>();
        leftForeArmJoint = leftForeArm.GetComponent<ConfigurableJoint>();
        rightForeArmJoint = rightForeArm.GetComponent<ConfigurableJoint>();

        leftInitialRotation = leftLeg.localRotation;
        rightInitialRotation = rightLeg.localRotation;
        hipInitialRotation = mainHipJoint.transform.localRotation;
        headInitialRotation = headJoint.transform.localRotation;
    }

    void FixedUpdate()
    {
        switch (currState)
        {
            case PlayerState.Walking:
                Walking();
                break;
            case PlayerState.Swinging:
                SwayWithHand();
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
        curHookTargetPos = targetPos;
    }

    public void RotateDirection(Vector3 worldDirection, float turnSpeed)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude > 0.01f)
        {
            Vector3 currentEuler = moveFrame.eulerAngles;
            float targetYaw = Quaternion.LookRotation(worldDirection.normalized, Vector3.up).eulerAngles.y;
            float newYaw = Mathf.MoveTowardsAngle(currentEuler.y, targetYaw, turnSpeed * Time.fixedDeltaTime);

            //yaw만 갱신
            mainHipJoint.targetRotation = Quaternion.Euler(0, newYaw, 0);
        }
    }

    public void RotateForGliding(Vector3 worldDirection, float turnSpeed)
    {
        float step = turnSpeed * Time.fixedDeltaTime;
        Vector3 flatDirection = worldDirection;
        flatDirection.y = 0;

        Quaternion targetWorldRotation = Quaternion.LookRotation(flatDirection) * Quaternion.Euler(70, 0, 0);
        Quaternion currentLocalRotation = mainHipJoint.transform.rotation;

        Quaternion newLocalRotation = Quaternion.RotateTowards(currentLocalRotation, targetWorldRotation, step);
        ConfigurableJointExtensions.SetTargetRotationLocal(mainHipJoint, newLocalRotation, hipInitialRotation);
    }

    public void SwayWithHand()
    {
        Vector3 toTarget = curHookTargetPos - rightForeArm.position;
        if (toTarget.sqrMagnitude < 0.001f)
            return;
        Quaternion lookRot = Quaternion.LookRotation(toTarget, Vector3.up);

        // 조인트 축 보정
        Quaternion correction = Quaternion.Euler(90f, 0f, 0f);
        Quaternion targetRot = lookRot * correction;

        rightArm.rotation = Quaternion.Slerp(rightForeArm.rotation, targetRot, Time.deltaTime * armReachSpeed);
        rightForeArm.rotation = Quaternion.Slerp(rightForeArm.rotation, targetRot, Time.deltaTime * armReachSpeed);
    }

    private void Walking()
    {
        timeCounter += Time.deltaTime * stepSpeed * Mathf.PI * 2f;

        // 사인파 기반 회전
        float leftAngle = Mathf.Sin(timeCounter) * stepAngle;
        float rightAngle = Mathf.Sin(timeCounter + offsetBetweenLegs) * stepAngle;

        leftLeg.localRotation = leftInitialRotation * Quaternion.Euler(leftAngle, 0f, 0f);
        rightLeg.localRotation = rightInitialRotation * Quaternion.Euler(rightAngle, 0f, 0f);
    }

    public void SetAnimation(PlayerState state)
    {
        currState = state;
        JointDrive drive = mainHipJoint.slerpDrive;
        Vector3 currentEuler = mainHipJoint.targetRotation.eulerAngles;
        switch (state)
        {
            case PlayerState.Standing:
            case PlayerState.Walking:
                drive.positionSpring = standDrive;
                mainHipJoint.slerpDrive = drive;
                mainHipJoint.targetRotation = Quaternion.Euler(0, currentEuler.y, currentEuler.z);
                break;
            case PlayerState.Swinging:
                drive.positionSpring = fallDrive;
                mainHipJoint.slerpDrive = drive;
                break;
            case PlayerState.Reeling:
                drive.positionSpring = standDrive;
                mainHipJoint.slerpDrive = drive;
                break;
            case PlayerState.Gliding:
                drive.positionSpring = standDrive;
                mainHipJoint.slerpDrive = drive;
                break;

        }
    }

}
