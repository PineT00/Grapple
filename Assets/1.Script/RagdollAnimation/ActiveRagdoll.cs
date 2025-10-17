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
    private Joint hipJoint;

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
                    startLocalRotation = joint.transform.localRotation
                };
                jointDataList.Add(data);
                Debug.Log(joint.name);
            }
        }
        hipJoint = ragdollHips.GetComponent<ConfigurableJoint>();
    }

    void FixedUpdate()
    {
        Vector3 bodyPositionOffset = ragdollHips.position - animationHips.position;
        animationRoot.position += bodyPositionOffset;
        AnimationSynchro();
    }

    private void AnimationSynchro()
    {
        foreach (var data in jointDataList)
        {
            // Hip 관절도 동기화하되, 과도한 회전 방지
            if (data.joint == hipJoint)
            {
                // Hip은 물리에 더 많이 맡기되, 애니메이션 방향도 일부 반영
                //Quaternion targetRotation = Quaternion.Slerp(data.joint.transform.localRotation, data.animationBone.localRotation, 0.3f);
                //ConfigurableJointExtensions.SetTargetRotationLocal(data.joint, targetRotation, data.startLocalRotation);
                data.animationBone.localRotation = data.joint.transform.localRotation;
            }
            else
            {
                // 다른 관절은 애니메이션을 정확히 추적
                ConfigurableJointExtensions.SetTargetRotationLocal(data.joint, data.animationBone.localRotation, data.startLocalRotation);
            }
        }
    }
}
