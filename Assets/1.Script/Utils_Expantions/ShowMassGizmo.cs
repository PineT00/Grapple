using UnityEngine;

public class ShowMassGizmo : MonoBehaviour
{
    [Tooltip("기즈모의 색상을 지정합니다.")]
    public Color gizmoColor = Color.red;

    [Tooltip("기즈모 구체의 크기를 지정합니다.")]
    public float gizmoRadius = 0.1f;

    private Rigidbody rb;

    private void OnDrawGizmos()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null) return;

        Gizmos.color = gizmoColor;
        Vector3 worldCenterOfMass = transform.TransformPoint(rb.centerOfMass);
        Gizmos.DrawSphere(worldCenterOfMass, gizmoRadius);
    }
}
