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

    [Header("Drive 설정")]
    public float standBodyDrive = 3000f;
    public float fallDrive = 100f;
    public float normalArmDrive = 30f;
    public float normalLegDrive = 30f;
    public float swingArmDrive = 999f;
    public float glideDrive = 999f;

    [Header("Damper 설정")]
    public float standBodyDamper = 200f;
    public float fallDamper = 10f;
    public float normalArmDamper = 5f;
    public float normalLegDamper = 5f;
    public float swingArmDamper = 50f;
    public float glideDamper = 100f;

    [Header("MaxForce 설정")]
    public float bodyMaxForce = 50000f;
    public float limbMaxForce = 10000f;


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

    /// <summary>
    /// 발의 위치를 위상에 따라 업데이트 (전방 + 수직 아치)
    /// </summary>
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
                SetTorsoDrives(standBodyDrive, standBodyDamper, bodyMaxForce);
                SetLimbDrives(normalArmDrive, normalLegDrive, normalArmDamper, normalLegDamper, limbMaxForce);
                ResetToStanding();
                break;
            case PlayerAnimState.Walking:
                SetTorsoDrives(standBodyDrive, standBodyDamper, bodyMaxForce);
                SetLimbDrives(normalArmDrive, normalLegDrive, normalArmDamper, normalLegDamper, limbMaxForce);
                break;
            case PlayerAnimState.OnAir:
                SetTorsoDrives(standBodyDrive, standBodyDamper, bodyMaxForce);
                SetLimbDrives(normalArmDrive, normalLegDrive, normalArmDamper, normalLegDamper, limbMaxForce);
                break;
            case PlayerAnimState.Rolling:
                SetTorsoDrives(standBodyDrive, standBodyDamper, bodyMaxForce);
                SetLimbDrives(glideDrive, glideDrive, glideDamper, glideDamper, limbMaxForce);
                currentSpinAngle = animHipTrans.localEulerAngles.x;
                break;
            case PlayerAnimState.Gliding:
                SetTorsoDrives(standBodyDrive, standBodyDamper, bodyMaxForce);
                SetLimbDrives(glideDrive, glideDrive, glideDamper, glideDamper, limbMaxForce);
                break;
            case PlayerAnimState.Swinging:
                SetTorsoDrives(fallDrive, fallDamper, bodyMaxForce);
                SetLimbDrives(swingArmDrive, normalArmDrive, swingArmDamper, normalLegDamper, limbMaxForce);
                break;
            case PlayerAnimState.Reeling:
                SetTorsoDrives(fallDrive, fallDamper, bodyMaxForce);
                SetLimbDrives(swingArmDrive, normalArmDrive, swingArmDamper, normalLegDamper, limbMaxForce);
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
    private void SetTorsoDrives(float hipSpring, float hipDamper, float maxForce, bool includeSpine = false)
    {
        SetJointDrive(mainHipJoint, hipSpring, hipDamper, maxForce);
        if (includeSpine)
        {
            SetJointDrive(spineJoint, hipSpring, hipDamper, maxForce);
        }
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


}
