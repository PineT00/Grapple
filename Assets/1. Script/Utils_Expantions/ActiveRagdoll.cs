using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActiveRagdoll : MonoBehaviour
{
    [Header("두 뼈대의 루트")]
    public Transform animationRoot;
    public Transform ragdollRoot;

    [Tooltip("중심 뼈")]
    public Transform animationHips;
    public Transform ragdollHips;

    // 조인트와 초기 회전 값을 함께 저장할 내부 클래스
    private class JointData
    {
        public ConfigurableJoint joint;
        public Quaternion startWorldRotation; // 게임 시작 시의 초기 회전 값을 저장할 변수
    }

    private Dictionary<Transform, JointData> jointMap;

    void Awake()
    {
        jointMap = new Dictionary<Transform, JointData>();

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
                    startWorldRotation = joint.transform.rotation
                };
                jointMap.Add(matchingBone, data);
            }
        }
    }

    void FixedUpdate()
    {
        Vector3 bodyPositionOffset = ragdollHips.position - animationHips.position;
        animationRoot.position += bodyPositionOffset;

        foreach (var item in jointMap)
        {
            Transform animationBone = item.Key;
            JointData data = item.Value;

            // 목표 회전 값(애니메이션 뼈대)과 저장해둔 초기 회전 값 전달
            data.joint.SetTargetRotation(animationBone.rotation, data.startWorldRotation);
        }
    }
}
