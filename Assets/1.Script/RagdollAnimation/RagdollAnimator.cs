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
    [SerializeField] private ConfigurableJoint leftHandJoint;
    [SerializeField] private ConfigurableJoint rightHandJoint;

    [Header("Rigs")]
    public float rigTransitionSpeed = 0.5f;
    public Rig normalRig;
    public Rig glideRig;
    public Rig swingRig;
    public Rig rollingRig;

    [Header("Stand")]
    public float standHipDrive = 3000f;
    public float standSpineDrive = 600f;
    public float standArmDrive = 30f;
    public float standLegDrive = 30f;
    public float standBodyDamper = 200f;
    public float normalArmDamper = 5f;
    public float normalLegDamper = 5f;

    [Header("Swing")]
    public float swingHipDrive = 3000f;
    public float swingSpineDrive = 999f;
    public float swingArmDrive = 999f;
    public float swingLegDrive = 999f;
    public float swingBodyDamper = 200f;
    public float swingArmDamper = 50f;
    public float swingLegDamper = 50f;

    [Header("glide")]
    public float glideHipDrive = 999f;
    public float glideSpineDrive = 800f;
    public float glideArmDrive = 800f;
    public float glideLegDrive = 800f;
    public float glideBodyDamper = 100f;
    public float glideArmDamper = 50f;
    public float glideLegDamper = 50f;

    [Header("MaxForce 설정")]
    public float bodyMaxForce = 50000f;
    public float limbMaxForce = 10000f;

    [Header("스윙 액션 설정")]
    public Transform swingTarget;
    private PlayerAnimState currState;

    [Header("그래플 손/팔 보정")]
    [Tooltip("손에 가할 보정 힘 비율")]
    public float handForceRatio = 0.15f;
    [Tooltip("팔꿈치에 가할 보정 힘 비율")]
    public float forearmForceRatio = 0.08f;
    [Tooltip("어깨에 가할 보정 힘 비율")]
    public float armForceRatio = 0.05f;

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


    [Header("걷기 애니메이션 설정")]
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    [Tooltip("걸음 주기 (초, 작을수록 빠름)")]
    public float stepCycleDuration = 0.5f;
    [Tooltip("발을 앞으로 내딛는 거리 (로컬 Z)")]
    public float stepForwardDistance = 0.3f;
    [Tooltip("발을 올리는 높이 (로컬 Y)")]
    public float stepHeight = 0.2f;
    [Tooltip("양발 겹침 허용 비율 (0.5 = 50%)")]
    public float overlapRatio = 0.5f;

    private Vector3 initialLeftFootPos;
    private Vector3 initialRightFootPos;
    private float leftLegPhase = 0f;  // 왼발의 애니메이션 진행도 (0~1)
    private float rightLegPhase = 0.5f; // 오른발의 애니메이션 진행도 (0~1, 0.5만큼 위상차)

    public void ResetToStanding()
    {
        leftFootTarget.localPosition = initialLeftFootPos;
        rightFootTarget.localPosition = initialRightFootPos;
    }

    private void Walking()
    {
        // 양발 위상 진행 (시간 기반)
        float phaseIncrement = Time.deltaTime / stepCycleDuration;
        leftLegPhase += phaseIncrement;
        rightLegPhase += phaseIncrement;

        // 위상 래핑 (0~1 범위 유지)
        if (leftLegPhase >= 1f) leftLegPhase -= 1f;
        if (rightLegPhase >= 1f) rightLegPhase -= 1f;

        // 각 발의 위치 계산
        UpdateFootPosition(leftFootTarget, leftLegPhase, initialLeftFootPos);
        UpdateFootPosition(rightFootTarget, rightLegPhase, initialRightFootPos);
    }

    private void UpdateFootPosition(Transform footTarget, float phase, Vector3 initialPos)
    {
        if (footTarget == null) return;

        // 전후 이동: 0~0.5 구간에서 앞으로, 0.5~1 구간에서 뒤로
        float forwardOffset;
        if (phase < 0.5f)
        {
            // 0~0.5: 뒤에서(-stepForwardDistance) 앞으로(+stepForwardDistance) 이동
            forwardOffset = Mathf.Lerp(-stepForwardDistance, stepForwardDistance, phase * 2f);
        }
        else
        {
            // 0.5~1: 앞에서 뒤로 밀려남 (착지 후 몸이 앞으로 이동하면서)
            forwardOffset = Mathf.Lerp(stepForwardDistance, -stepForwardDistance, (phase - 0.5f) * 2f);
        }

        // 수직 이동 (sin 곡선으로 0 ~ 1, 발이 공중에 있을 때만 올라감)
        float verticalOffset = Mathf.Sin(phase * Mathf.PI) * stepHeight;

        // 최종 위치 (로컬 좌표)
        Vector3 newPos = initialPos;
        newPos.z += forwardOffset; // 전방
        newPos.y += verticalOffset; // 높이

        footTarget.localPosition = newPos;
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
                SetTorsoDrives(standHipDrive, standSpineDrive, standBodyDamper, standBodyDamper, bodyMaxForce);
                SetLimbDrives(standArmDrive, standLegDrive, normalArmDamper, normalLegDamper, limbMaxForce);
                ResetToStanding();
                break;
            case PlayerAnimState.Walking:
                SetTorsoDrives(standHipDrive, standSpineDrive, standBodyDamper, standBodyDamper, bodyMaxForce);
                SetLimbDrives(standArmDrive, standLegDrive, normalArmDamper, normalLegDamper, limbMaxForce);
                break;
            case PlayerAnimState.OnAir:
                SetTorsoDrives(standHipDrive, standSpineDrive, standBodyDamper, standBodyDamper, bodyMaxForce);
                SetLimbDrives(standArmDrive, standLegDrive, normalArmDamper, normalLegDamper, limbMaxForce);
                break;
            case PlayerAnimState.Rolling:
                SetTorsoDrives(swingHipDrive, swingSpineDrive, swingBodyDamper, swingBodyDamper, bodyMaxForce);
                SetLimbDrives(swingArmDrive, swingLegDrive, swingArmDamper, swingLegDamper, limbMaxForce);
                currentSpinAngle = animHipTrans.localEulerAngles.x;
                break;
            case PlayerAnimState.Gliding:
                SetTorsoDrives(glideHipDrive, glideSpineDrive, glideBodyDamper, glideBodyDamper, bodyMaxForce);
                SetLimbDrives(glideArmDrive, glideLegDrive, glideArmDamper, glideLegDamper, limbMaxForce);
                break;
            case PlayerAnimState.Swinging:
                SetTorsoDrives(swingHipDrive, swingSpineDrive, swingBodyDamper, swingBodyDamper, bodyMaxForce);
                SetLimbDrives(swingArmDrive, swingLegDrive, swingArmDamper, swingLegDamper, limbMaxForce);
                break;
            case PlayerAnimState.Reeling:
                SetTorsoDrives(swingHipDrive, swingSpineDrive, swingBodyDamper, swingBodyDamper, bodyMaxForce);
                SetLimbDrives(swingArmDrive, glideLegDrive, swingArmDamper, swingLegDamper, limbMaxForce);
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
    private void SetTorsoDrives(float hipSpring, float spineSpring, float hipDamper, float spineDamper, float maxForce)
    {
        SetJointDrive(mainHipJoint, hipSpring, hipDamper, maxForce);
        SetJointDrive(spineJoint, spineSpring, spineDamper, maxForce);
    }

    private void SetLimbDrives(float armSpring, float legSpring, float armDamper, float legDamper, float maxForce)
    {
        // 팔
        SetJointDrive(leftArmJoint, armSpring, armDamper, maxForce);
        SetJointDrive(leftForeArmJoint, armSpring, armDamper, maxForce);
        SetJointDrive(rightArmJoint, armSpring, armDamper, maxForce);
        SetJointDrive(rightForeArmJoint, armSpring, armDamper, maxForce);

        // 다리
        SetJointDrive(leftLegJoint, legSpring, legDamper, maxForce);
        SetJointDrive(rightLegJoint, legSpring, legDamper, maxForce);
        SetJointDrive(leftCarfJoint, legSpring, legDamper, maxForce);
        SetJointDrive(rightCarfJoint, legSpring, legDamper, maxForce);
    }

    private void SetJointDrive(ConfigurableJoint joint, float springValue, float damperValue, float maxForceValue)
    {
        if (joint == null) return;

        JointDrive drive = joint.slerpDrive;
        drive.positionSpring = springValue;
        drive.positionDamper = damperValue;
        drive.maximumForce = maxForceValue;
        joint.slerpDrive = drive;
    }

    /// <summary>
    /// 그래플 포인트로 손과 팔에 체인식 보정 힘을 가합니다.
    /// </summary>
    /// 
    public void ApplyGrappleArmCorrection(bool isRight, Vector3 grapplePoint, float baseForce)
    {
        ConfigurableJoint handJoint = isRight ? rightHandJoint : leftHandJoint;
        ConfigurableJoint forearmJoint = isRight ? rightForeArmJoint : leftForeArmJoint;
        ConfigurableJoint armJoint = isRight ? rightArmJoint : leftArmJoint;

        // 손 (팔꿈치 조인트의 연결된 body = 손)
        if (handJoint != null)
        {
            Vector3 direction = (grapplePoint - handJoint.transform.position).normalized;
            handJoint.GetComponent<Rigidbody>().AddForce(direction * baseForce * handForceRatio, ForceMode.Force);
        }

        // 팔꿈치
        if (forearmJoint != null)
        {
            Vector3 direction = (grapplePoint - forearmJoint.transform.position).normalized;
            forearmJoint.GetComponent<Rigidbody>().AddForce(direction * baseForce * forearmForceRatio, ForceMode.Force);
        }

        // 어깨
        if (armJoint != null)
        {
            Vector3 direction = (grapplePoint - armJoint.transform.position).normalized;
            armJoint.GetComponent<Rigidbody>().AddForce(direction * baseForce * armForceRatio, ForceMode.Force);
        }
    }


}
