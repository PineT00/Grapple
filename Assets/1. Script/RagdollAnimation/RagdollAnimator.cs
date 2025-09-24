using System.Collections.Generic;
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
    public Transform ragdollHipTrans;


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
    public float rigTransitionSpeed = 0.5f;
    public Rig normalRig;
    public Rig glideRig;
    public Rig swingRig;
    public Rig rollingRig;

    [Header("스텝 설정")]
    public float stepAngle = 30f; // 다리를 내미는 각도
    public float stepSpeed = 2f;

    [Header("Drive 설정")]
    public float standBodyDrive = 3000f;
    public float fallDrive = 100f;
    public float normalArmDrive = 30f;
    public float normalLegDrive = 30f;
    public float swingArmDrive = 999f;
    public float glideDrive = 999f;
    public float normalDamper = 20f;
    public float glideDamper = 100f;


    [Header("스윙 액션 설정")]
    public Transform swingTarget;
    private PlayerAnimState currState;

    private Dictionary<Rig, float> targetRigWeights = new Dictionary<Rig, float>();
    private List<Rig> allRigs;

    void Awake()
    {
        newYawAngle = transform.eulerAngles.y;

        allRigs = new List<Rig> { normalRig, swingRig, rollingRig, glideRig };
        foreach (var rig in allRigs)
        {
            if (rig != null) targetRigWeights[rig] = rig.weight;
        }

        initialLeftFootPos = leftFootTarget.transform.localPosition;
        initialRightFootPos = rightFootTarget.transform.localPosition;
    }

    void FixedUpdate()
    {
        switch (currState)
        {
            case PlayerAnimState.Walking:
                Walking();
                break;
            case PlayerAnimState.Swinging:
                //Swinging();
                break;
            case PlayerAnimState.Reeling:
                //Swinging();
                break;
            case PlayerAnimState.Gliding:
                break;
        }
    }

    void Update()
    {
        SmoothlyUpdateRigWeights();
    }

    public void SetHookTarget(Vector3 targetPos)
    {
        swingTarget.position = targetPos;
    }

    public float maxSpinSpeed = 360f; // 초당 회전할 각도 (360이면 1초에 한 바퀴)
    private float currentSpinAngle = 0f; // 현재까지 회전한 각도를 저장할 변수
    private float newYawAngle;
    public void SmoothRotateAndSpin(Vector3 worldDirection, float turnSmoothing)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude > 0.01f)
        {
            float targetYaw = Quaternion.LookRotation(worldDirection.normalized, Vector3.up).eulerAngles.y;
            float currentYaw = moveFrame.eulerAngles.y;
            newYawAngle = Mathf.LerpAngle(currentYaw, targetYaw, turnSmoothing * Time.fixedDeltaTime);
        }
        // 공중제비
        currentSpinAngle += maxSpinSpeed * Time.fixedDeltaTime;

        Quaternion yawRotation = Quaternion.Euler(0f, newYawAngle, 0f);
        Quaternion spinRotation = Quaternion.Euler(currentSpinAngle, 0f, 0f);

        animHipTrans.rotation = yawRotation * spinRotation;
    }


    float stepTimer = 0f;
    public float stepTime = 1f;
    public float stepDistance = 1f;
    public float stepHeight = 1f;
    Vector3 initialLeftFootPos;
    Vector3 initialRightFootPos;
    public Transform leftFootTarget;
    public Transform rightFootTarget;

    private void Walking()
    {
        stepTimer += Time.deltaTime;
        float progress = Mathf.PingPong(stepTimer, stepTime) / stepTime;
        float zOffset = Mathf.Cos(progress * 2 * Mathf.PI) * stepDistance;

        Vector3 newLeftPos = initialLeftFootPos + Vector3.forward * zOffset;
        Vector3 newRightPos = initialRightFootPos - Vector3.forward * zOffset;

        float leftHeight = Mathf.Sin(progress * 2 * Mathf.PI + Mathf.PI) * 0.5f + 0.5f;
        float rightHeight = Mathf.Sin(progress * 2 * Mathf.PI) * 0.5f + 0.5f;

        newLeftPos.y += leftHeight * stepHeight;
        newRightPos.y += rightHeight * stepHeight;

        // 계산된 최종 위치를 각 타겟의 localPosition에 적용
        if (leftFootTarget) leftFootTarget.localPosition = newLeftPos;
        if (rightFootTarget) rightFootTarget.localPosition = newRightPos;
    }

    public void SetAnimation(PlayerAnimState state)
    {
        currState = state;
        SetJointDrivesForState(state);
        SetTargetRigWeightsForState(state);
    }
    private void SetJointDrivesForState(PlayerAnimState state)
    {
        switch (state)
        {
            case PlayerAnimState.Standing:
            case PlayerAnimState.Walking:
                SetTorsoDrives(standBodyDrive);
                SetLimbDrives(normalArmDrive, normalLegDrive); // 팔, 다리
                break;
            case PlayerAnimState.OnAir:
                SetTorsoDrives(standBodyDrive);
                SetLimbDrives(normalArmDrive, normalLegDrive); // 팔, 다리
                break;
            case PlayerAnimState.Rolling:
                SetTorsoDrives(standBodyDrive); // 척추 포함
                SetLimbDrives(glideDrive, glideDrive); // 팔, 다리
                currentSpinAngle = animHipTrans.localEulerAngles.x;
                break;
            case PlayerAnimState.Gliding:
                SetTorsoDrives(standBodyDrive, true); // 척추 포함
                SetLimbDrives(glideDrive, glideDrive); // 팔, 다리
                break;
            case PlayerAnimState.Swinging:
                SetTorsoDrives(fallDrive);
                SetLimbDrives(swingArmDrive, normalArmDrive);
                break;
            case PlayerAnimState.Reeling:
                SetTorsoDrives(fallDrive);
                SetLimbDrives(swingArmDrive, normalArmDrive);
                break;
        }
    }

    private void SetTargetRigWeightsForState(PlayerAnimState state)
    {
        targetRigWeights[normalRig] = (state == PlayerAnimState.Standing || state == PlayerAnimState.Walking || state == PlayerAnimState.OnAir) ? 1f : 0f;
        targetRigWeights[swingRig] = (state == PlayerAnimState.Swinging || state == PlayerAnimState.Reeling) ? 1f : 0f;
        targetRigWeights[rollingRig] = (state == PlayerAnimState.Rolling) ? 1f : 0f;
        targetRigWeights[glideRig] = (state == PlayerAnimState.Gliding) ? 1f : 0f;
    }

    private void SmoothlyUpdateRigWeights()
    {
        foreach (var rig in allRigs)
        {
            if (rig != null && targetRigWeights.ContainsKey(rig))
            {
                rig.weight = Mathf.MoveTowards(rig.weight, targetRigWeights[rig], rigTransitionSpeed * Time.deltaTime);
            }
        }
    }

    // 물리 설정 헬퍼들
    private void SetTorsoDrives(float hipSpring, bool includeSpine = false)
    {
        SetJointDrive(mainHipJoint, hipSpring);
        if (includeSpine)
        {
            SetJointDrive(spineJoint, hipSpring);
        }
    }

    private void SetLimbDrives(float armSpring, float legSpring)
    {
        // 팔
        SetJointDrive(leftArmJoint, armSpring);
        SetJointDrive(leftForeArmJoint, armSpring);
        SetJointDrive(rightArmJoint, armSpring);
        SetJointDrive(rightForeArmJoint, armSpring);

        // 다리
        SetJointDrive(leftLegJoint, legSpring);
        SetJointDrive(rightLegJoint, legSpring);
        SetJointDrive(leftCarfJoint, legSpring);
        SetJointDrive(rightCarfJoint, legSpring);
    }

    private void SetJointDrive(ConfigurableJoint joint, float springValue)
    {
        if (joint == null || springValue <= 0) return; // 유효성 검사

        JointDrive drive = joint.slerpDrive;
        drive.positionSpring = springValue;
        joint.slerpDrive = drive;
    }

    private void SetLimbDamper(float armDamper, float legDamper)
    {
        // 팔
        SetJointDamper(leftArmJoint, armDamper);
        SetJointDamper(leftForeArmJoint, armDamper);
        SetJointDamper(rightArmJoint, armDamper);
        SetJointDamper(rightForeArmJoint, armDamper);

        // 다리
        SetJointDamper(leftLegJoint, legDamper);
        SetJointDamper(rightLegJoint, legDamper);
        SetJointDamper(leftCarfJoint, legDamper);
        SetJointDamper(rightCarfJoint, legDamper);
    }

    private void SetJointDamper(ConfigurableJoint joint, float DamperValue)
    {
        if (joint == null || DamperValue <= 0) return; // 유효성 검사

        JointDrive drive = joint.slerpDrive;
        drive.positionDamper = DamperValue;
        joint.slerpDrive = drive;
    }

}
