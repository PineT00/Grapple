using UnityEngine;

public class GrappleController : MonoBehaviour
{
    [Header("필수")]
    public Camera cam;
    public Transform firePoint;
    public LayerMask grappleLayerMask;
    public LineRenderer lineRendererPrefab;
    private LineRenderer lineRenderer;
    private CharacterContoller characterContoller;

    [Header("파라미터")]
    public float maxRayDistance = 30f;
    public float spring = 70f;
    public float damper = 7f;
    public float massScale = 4.5f;
    public float pullForce = 4.5f;

    private SpringJoint joint;
    private Vector3 grapplePoint;
    private Rigidbody rb;
    private bool isGrappling = false;
    public float maxRope = 0f;

    void Start()
    {
        characterContoller = GetComponent<CharacterContoller>();
        rb = GetComponent<Rigidbody>();
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
            maxRope = Vector3.Distance(transform.position, hit.point);
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
        if (joint != null)
        {
            Destroy(joint);
        }
        isGrappling = false;
        lineRenderer.positionCount = 0;
        characterContoller.SetPlayerState(PlayerState.Walking);
        maxRope = 0f;
    }

    private void MoveTowardToTarget()
    {
        float restLength = maxRope * 0.2f; // "느슨한 상태" 길이
        float distance = Vector3.Distance(transform.position, grapplePoint);
        float stretch = distance - restLength;

        if (stretch > 0f)
        {
            // Hook이 자연 길이보다 늘어났을 때만 탄성력 발생
            Vector3 dir = (grapplePoint - transform.position).normalized;
            // Hook 힘 곡선 (선형/제곱/조절 가능)
            float springForce = pullForce * stretch; // 선형
            // float springForce = pullForce * stretch * stretch; // 제곱(더 팽팽하게)
            rb.AddForce(dir * springForce, ForceMode.Acceleration);
        }
    }

    private void DrawRope()
    {
        lineRenderer.SetPosition(0, firePoint.position);
        lineRenderer.SetPosition(1, grapplePoint);
    }
}
