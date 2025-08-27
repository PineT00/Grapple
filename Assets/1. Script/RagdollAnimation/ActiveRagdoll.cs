using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActiveRagdoll : MonoBehaviour
{
    [Header("참조: 두 뼈대의 루트")]
    public Transform animationRoot;
    public Transform ragdollRoot;

    [Header("동기화 기준 뼈")]
    public Transform animationHips;
    public Transform ragdollHips;

    // 조인트와 초기 '로컬' 회전 값을 저장할 내부 클래스
    private class JointData
    {
        public ConfigurableJoint joint;
        public Transform animationBone;
        public Quaternion startLocalRotation; // 초기 로컬 회전 값을 저장
    }

    private List<JointData> jointDataList;

    void Awake()
    {
        jointDataList = new List<JointData>();
        ConfigurableJoint[] allJoints = ragdollRoot.GetComponentsInChildren<ConfigurableJoint>();
        Transform[] allAnimationBones = animationRoot.GetComponentsInChildren<Transform>();

        foreach (var joint in allJoints)
        {
            Transform matchingBone = allAnimationBones.FirstOrDefault(bone => bone.name == joint.name);
            if (matchingBone != null)
            {
                var data = new JointData
                {
                    joint = joint,
                    animationBone = matchingBone,
                    // 게임 시작 시의 '로컬' 회전 값을 기준점으로 저장
                    startLocalRotation = joint.transform.localRotation
                };
                jointDataList.Add(data);
            }
        }
    }

    void FixedUpdate()
    {
        // 1. 위치 및 회전 동기화
        Vector3 bodyPositionOffset = ragdollHips.position - animationHips.position;
        animationRoot.position += bodyPositionOffset;

        AnimationSynchro();
    }

    private void AnimationSynchro()
    {
        foreach (var data in jointDataList)
        {
            ConfigurableJointExtensions.SetTargetRotationLocal(data.joint, data.animationBone.localRotation, data.startLocalRotation);
        }
    }
}
