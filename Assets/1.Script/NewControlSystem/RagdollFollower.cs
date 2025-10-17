
using UnityEngine;

/// <summary>
/// 랙돌이 ControlRoot 오브젝트를 따라가도록 물리력을 적용합니다.
/// FixedUpdate에서 작동하여 안정적인 물리 시뮬레이션을 보장합니다.
/// </summary>
public class RagdollFollower : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform controlRootTarget; // 따라갈 ControlRoot의 Transform

    [Header("Ragdoll Parts")]
    public Rigidbody hipRigidbody; // 랙돌의 중심이 되는 Hip Rigidbody

    [Header("Follow Force Settings")]
    public float positionForce = 1000f; // 위치를 따라가는 힘의 세기
    public float rotationForce = 50f;   // 회전을 따라가는 힘의 세기

    private Quaternion initialHipRotation; // 초기 Hip의 로컬 회전 값

    void Start()
    {
        if (hipRigidbody == null)
        {
            Debug.LogError("Hip Rigidbody가 할당되지 않았습니다.");
            this.enabled = false;
            return;
        }

        // Hip 본(Bone)은 모델링 자세에 따라 약간의 초기 회전 값을 가질 수 있습니다.
        // 이 초기 회전 값을 보정해주어야 캐릭터가 올바르게 서게 됩니다.
        initialHipRotation = hipRigidbody.transform.localRotation;
    }

    void FixedUpdate()
    {
        if (controlRootTarget == null) return;

        FollowPosition();
        FollowRotation();
    }

    /// <summary>
    /// Hip의 위치가 ControlRoot의 위치를 따라가도록 힘을 가합니다.
    /// </summary>
    private void FollowPosition()
    {
        // 목표 위치와 현재 Hip 위치의 차이를 계산합니다.
        Vector3 positionDifference = controlRootTarget.position - hipRigidbody.position;

        // 목표 위치로 향하는 힘을 계산하고 적용합니다.
        // 이 힘은 일종의 스프링처럼 작동하여 부드러운 따라가기 효과를 만듭니다.
        Vector3 force = positionDifference * positionForce;
        hipRigidbody.AddForce(force * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Hip의 회전이 ControlRoot의 회전을 따라가도록 토크를 가합니다.
    /// </summary>
    private void FollowRotation()
    {
        // ControlRoot의 목표 회전에 모델의 초기 회전 값을 곱하여 최종 목표 회전을 계산합니다.
        Quaternion targetRotation = controlRootTarget.rotation * initialHipRotation;

        // 현재 Hip의 회전과 목표 회전의 차이를 계산합니다.
        Quaternion rotationDifference = targetRotation * Quaternion.Inverse(hipRigidbody.rotation);

        // Quaternion을 Axis-Angle 형태로 변환하여 토크를 적용할 축과 각도를 구합니다.
        rotationDifference.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180) angle -= 360;

        // 목표 회전으로 향하는 토크를 계산하고 적용합니다.
        Vector3 torque = axis.normalized * (angle * Mathf.Deg2Rad) * rotationForce;
        hipRigidbody.AddTorque(torque * Time.fixedDeltaTime);
    }
}
