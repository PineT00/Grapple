using UnityEngine;

public class GrappleController : MonoBehaviour
{
    [Header("필수")]
    public Rigidbody rb;
    public Camera cam;
    public Transform bodyTrans;
    public Transform firePoint;
    public LayerMask grappleLayerMask;
    public LineRenderer lineRendererPrefab;
    private LineRenderer lineRenderer;
    private RagdollCharacterController characterContoller;

    [Header("파라미터")]
    public float maxRayDistance = 30f;
    public float spring = 70f;
    public float damper = 7f;
    public float massScale = 4.5f;
    public float pullForce = 4.5f;

    private Vector3 grapplePoint;
    private bool isGrappling = false;
    private float maxRope = 0f;

    void Start()
    {
        if(rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        characterContoller = GetComponent<RagdollCharacterController>();
        lineRenderer = Instantiate(lineRendererPrefab);
        lineRenderer.positionCount = 0;
    }

    void Update()
    {
        if (isGrappling)
        {
            MoveTowardToTarget();
            DrawRope();
        }
    }
    public void OnGrapple()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = cam.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grappleLayerMask))
        {
            grapplePoint = hit.point;
            maxRope = Vector3.Distance(bodyTrans.position, hit.point);
            isGrappling = true;
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                DrawRope();
            }

            characterContoller.SetPlayerState(PlayerState.Swinging);
        }
    }

    public void OnRelease()
    {
        isGrappling = false;
        lineRenderer.positionCount = 0;
        characterContoller.SetPlayerState(PlayerState.Walking);
        maxRope = 0f;
    }

    private void MoveTowardToTarget()
    {
        float restLength = maxRope * 0.2f;
        float distance = Vector3.Distance(bodyTrans.position, grapplePoint);
        float stretch = distance - restLength;

        if (stretch > 0f)
        {
            Vector3 dir = (grapplePoint - bodyTrans.position).normalized;
            float springForce = pullForce * stretch; // 선형
            rb.AddForce(dir * springForce, ForceMode.Acceleration);
        }
    }

    private void DrawRope()
    {
        lineRenderer.SetPosition(0, firePoint.position);
        lineRenderer.SetPosition(1, grapplePoint);
    }
}
