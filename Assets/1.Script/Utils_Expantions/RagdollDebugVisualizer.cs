using UnityEngine;
using TMPro;

/// <summary>
/// Ragdoll 애니메이션의 동기화 품질을 시각화하고 모니터링하는 디버그 도구
/// </summary>
public class RagdollDebugVisualizer : MonoBehaviour
{
    [Header("참조")]
    public ActiveRagdoll activeRagdoll;
    public Transform animationRoot;
    public Transform ragdollRoot;

    [Header("시각화 설정")]
    public bool showIKTargets = true;
    public bool showJointAngles = true;
    public bool showGroundRaycasts = true;
    public Color ikTargetColor = Color.green;
    public Color jointAngleColor = Color.yellow;
    public Color raycastHitColor = Color.red;
    public Color raycastMissColor = Color.white;

    [Header("UI 참조")]
    public TextMeshProUGUI debugText;

    [Header("모니터링 설정")]
    public float angleErrorThreshold = 15f; // 경고를 표시할 각도 오차 임계값

    private ConfigurableJoint[] allJoints;
    private Transform[] allAnimationBones;

    void Start()
    {
        if (ragdollRoot != null)
        {
            allJoints = ragdollRoot.GetComponentsInChildren<ConfigurableJoint>();
        }
        if (animationRoot != null)
        {
            allAnimationBones = animationRoot.GetComponentsInChildren<Transform>();
        }
    }

    void Update()
    {
        if (debugText != null)
        {
            UpdateDebugText();
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (showIKTargets)
        {
            DrawIKTargets();
        }

        if (showJointAngles)
        {
            DrawJointAngles();
        }

        if (showGroundRaycasts)
        {
            DrawGroundRaycasts();
        }
    }

    private void DrawIKTargets()
    {
        // RagdollWalking의 IK 타겟 찾기
        var ragdollWalking = GetComponent<RagdollWalking>();
        if (ragdollWalking == null) return;

        // 리플렉션을 사용하여 private 필드 접근
        var leftLegField = ragdollWalking.GetType().GetField("leftLeg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rightLegField = ragdollWalking.GetType().GetField("rightLeg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (leftLegField != null)
        {
            var leftLeg = leftLegField.GetValue(ragdollWalking);
            var ikTargetField = leftLeg.GetType().GetField("ikTarget");
            if (ikTargetField != null)
            {
                Transform ikTarget = ikTargetField.GetValue(leftLeg) as Transform;
                if (ikTarget != null)
                {
                    Gizmos.color = ikTargetColor;
                    Gizmos.DrawWireSphere(ikTarget.position, 0.1f);
                    Gizmos.DrawLine(ikTarget.position, ikTarget.position + Vector3.up * 0.2f);
                }
            }
        }

        if (rightLegField != null)
        {
            var rightLeg = rightLegField.GetValue(ragdollWalking);
            var ikTargetField = rightLeg.GetType().GetField("ikTarget");
            if (ikTargetField != null)
            {
                Transform ikTarget = ikTargetField.GetValue(rightLeg) as Transform;
                if (ikTarget != null)
                {
                    Gizmos.color = ikTargetColor;
                    Gizmos.DrawWireSphere(ikTarget.position, 0.1f);
                    Gizmos.DrawLine(ikTarget.position, ikTarget.position + Vector3.up * 0.2f);
                }
            }
        }
    }

    private void DrawJointAngles()
    {
        if (allJoints == null || allAnimationBones == null) return;

        Gizmos.color = jointAngleColor;

        foreach (var joint in allJoints)
        {
            if (joint == null) continue;

            // 매칭되는 애니메이션 본 찾기
            Transform matchingBone = System.Array.Find(allAnimationBones, bone => bone.name == joint.name);
            if (matchingBone == null) continue;

            // 각도 차이 계산
            float angleDiff = Quaternion.Angle(joint.transform.localRotation, matchingBone.localRotation);

            // 임계값 초과 시 빨간색으로 표시
            if (angleDiff > angleErrorThreshold)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = jointAngleColor;
            }

            // 관절 위치에 작은 구 표시
            Gizmos.DrawWireSphere(joint.transform.position, 0.05f);

            // 목표 방향 표시
            Vector3 targetForward = matchingBone.forward * 0.15f;
            Gizmos.DrawLine(joint.transform.position, joint.transform.position + targetForward);
        }
    }

    private void DrawGroundRaycasts()
    {
        var ragdollWalking = GetComponent<RagdollWalking>();
        if (ragdollWalking == null) return;

        // 발의 homeTransform 위치에서 Raycast
        var leftLegField = ragdollWalking.GetType().GetField("leftLeg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rightLegField = ragdollWalking.GetType().GetField("rightLeg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        DrawGroundRaycastForLeg(leftLegField, ragdollWalking);
        DrawGroundRaycastForLeg(rightLegField, ragdollWalking);
    }

    private void DrawGroundRaycastForLeg(System.Reflection.FieldInfo legField, RagdollWalking ragdollWalking)
    {
        if (legField == null) return;

        var leg = legField.GetValue(ragdollWalking);
        var homeTransformField = leg.GetType().GetField("homeTransform");
        if (homeTransformField == null) return;

        Transform homeTransform = homeTransformField.GetValue(leg) as Transform;
        if (homeTransform == null) return;

        Vector3 origin = homeTransform.position + Vector3.up * 0.5f;
        float rayDistance = 1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance))
        {
            Gizmos.color = raycastHitColor;
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawWireSphere(hit.point, 0.05f);
        }
        else
        {
            Gizmos.color = raycastMissColor;
            Gizmos.DrawLine(origin, origin + Vector3.down * rayDistance);
        }
    }

    private void UpdateDebugText()
    {
        if (allJoints == null || allAnimationBones == null) return;

        string text = "=== Ragdoll 동기화 상태 ===\n";

        float totalAngleError = 0f;
        int jointCount = 0;
        int errorCount = 0;

        foreach (var joint in allJoints)
        {
            if (joint == null) continue;

            Transform matchingBone = System.Array.Find(allAnimationBones, bone => bone.name == joint.name);
            if (matchingBone == null) continue;

            float angleDiff = Quaternion.Angle(joint.transform.localRotation, matchingBone.localRotation);
            totalAngleError += angleDiff;
            jointCount++;

            if (angleDiff > angleErrorThreshold)
            {
                errorCount++;
                text += $"<color=red>⚠ {joint.name}: {angleDiff:F1}°</color>\n";
            }
        }

        float avgError = jointCount > 0 ? totalAngleError / jointCount : 0f;
        text += $"\n평균 각도 오차: {avgError:F2}°\n";
        text += $"경고 관절 수: {errorCount}/{jointCount}\n";

        // 물리 안정성 체크
        var controller = GetComponent<RagdollCharacterController>();
        if (controller != null && controller.mainRb != null)
        {
            float velocity = controller.mainRb.linearVelocity.magnitude;
            float angularVel = controller.mainRb.angularVelocity.magnitude;

            text += $"\n=== 물리 상태 ===\n";
            text += $"속도: {velocity:F2} m/s\n";
            text += $"각속도: {angularVel:F2} rad/s\n";

            if (velocity > 50f)
            {
                text += "<color=red>⚠ 속도 이상!</color>\n";
            }
            if (angularVel > 10f)
            {
                text += "<color=red>⚠ 회전 불안정!</color>\n";
            }
        }

        debugText.text = text;
    }
}
