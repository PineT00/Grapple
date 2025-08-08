using UnityEngine;

public class RagdollAnimator : MonoBehaviour
{

    [Header("좌/우 다리 조인트")]
    public ConfigurableJoint mainHipJoint;
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform rightShoulderBone;
    public Transform rightArmBone;
    public Quaternion leftInitialRotation;
    public Quaternion rightInitialRotation;

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

    void Start()
    {
        leftInitialRotation = leftLeg.localRotation;
        rightInitialRotation = rightLeg.localRotation;
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
        }
    }

    public void ReelingToward(Quaternion targetRot)
    {
        mainHipJoint.targetRotation = targetRot;
    }

    public void SetHookTarget(Vector3 targetPos)
    {
        curHookTargetPos = targetPos;
    }

    public void SwayWithHand()
    {
        Vector3 toTarget = curHookTargetPos - rightArmBone.position;
        if (toTarget.sqrMagnitude < 0.001f)
            return;
        Quaternion lookRot = Quaternion.LookRotation(toTarget, Vector3.up);

        // Z축 → Y축 보정 (Z가 전방인 LookRotation 결과를 Y축 전방인 구조에 맞춤)
        Quaternion correction = Quaternion.Euler(90f, 0f, 0f);
        Quaternion targetRot = lookRot * correction;

        // rightArmBone.rotation = targetRot;
        // rightShoulderBone.rotation = targetRot;

        rightShoulderBone.rotation = Quaternion.Slerp(rightArmBone.rotation, targetRot, Time.deltaTime * armReachSpeed);
        rightArmBone.rotation = Quaternion.Slerp(rightArmBone.rotation, targetRot, Time.deltaTime * armReachSpeed);
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
        switch (state)
        {
            case PlayerState.Standing:
            case PlayerState.Walking:
                drive.positionSpring = standDrive;
                mainHipJoint.slerpDrive = drive;
                break;
            case PlayerState.Swinging:
                drive.positionSpring = fallDrive;
                mainHipJoint.slerpDrive = drive;
                break;
            case PlayerState.Reeling:
                drive.positionSpring = standDrive;
                mainHipJoint.slerpDrive = drive;
                break;
                
        }
    }

}
