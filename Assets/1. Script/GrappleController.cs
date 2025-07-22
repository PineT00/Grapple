using UnityEngine;

public class GrappleController : MonoBehaviour
{
    [Header("필수")]
    public Camera cam;
    public Transform firePoint;
    public LayerMask grappleLayerMask;
    public LineRenderer lineRendererPrefab;
    private LineRenderer lineRenderer;

    [Header("파라미터")]
    public float maxDistance = 30f;
    public float spring = 70f;
    public float damper = 7f;
    public float massScale = 4.5f;
    public float pullForce = 4.5f;

    private SpringJoint joint;
    private Vector3 grapplePoint;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lineRenderer = Instantiate(lineRendererPrefab);
        lineRenderer.positionCount = 0;
    }

    void Update()
    {
        if (joint != null)
        {
            MoveTowardToTarget();
            DrawRope();
        }
        else if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }
    public void OnGrapple()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = cam.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, grappleLayerMask))
        {
            grapplePoint = hit.point;

            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float dist = Vector3.Distance(firePoint.position, grapplePoint);
            joint.maxDistance = dist * 0.8f;
            joint.minDistance = dist * 0.25f;

            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                DrawRope();
            }
        }
    }

    public void OnRelease()
    {
        if (joint != null)
        {
            Destroy(joint);
        }
    }

    private void MoveTowardToTarget()
    {
        Vector3 dir = (grapplePoint - transform.position).normalized;
        rb.AddForce(dir * pullForce, ForceMode.Acceleration);
    }

    private void DrawRope()
    {
        lineRenderer.SetPosition(0, firePoint.position);
        lineRenderer.SetPosition(1, grapplePoint);
    }
}
