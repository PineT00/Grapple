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
    private bool isReeling = false;
    private float maxRope = 0f;
    private SpringJoint joint;
    public float reelSpeed = 25f;
    public float arrivalThreshold = 1.5f;

    void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        characterContoller = GetComponent<RagdollCharacterController>();
        lineRenderer = Instantiate(lineRendererPrefab);
        lineRenderer.positionCount = 0;
    }

    void FixedUpdate()
    {
        if (isReeling)
        {
            ReelingToTarget();
            DrawRope();
        }
        else if (isGrappling)
        {
            AdjustGrapplePoint();
            DrawRope();
        }
    }
    public void OnGrapple()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = cam.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grappleLayerMask))
        {
            characterContoller.SetPlayerState(PlayerState.Swinging);
            grapplePoint = hit.point;
            maxRope = Vector3.Distance(bodyTrans.position, hit.point);
            isGrappling = true;

            joint = rb.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            joint.maxDistance = maxRope * 0.9f;
            joint.minDistance = maxRope * 0.25f;

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
        characterContoller.SetPlayerState(PlayerState.OnAir);
        if (joint != null)
        {
            Destroy(joint);
        }
        isGrappling = false;
        maxRope = 0f;
        if (!isReeling)
        {
            lineRenderer.positionCount = 0;
        }
    }
    public void StartReeling()
    {
        isReeling = true;
        rb.linearVelocity = Vector3.zero;

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }
    }
    public void StopReeling()
    {
        isReeling = false;
        if (isGrappling)
        {
            rb.linearVelocity = Vector3.zero;
            joint = rb.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            joint.maxDistance = maxRope * 0.9f;
            joint.minDistance = maxRope * 0.25f;

            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;
        }
        else
        {
            if (joint != null)
            {
                Destroy(joint);
                joint = null;
            }
        }
    }
    private void ReelingToTarget()
    {
        Vector3 targetDir = (grapplePoint - firePoint.position).normalized;
        float distance = Vector3.Distance(firePoint.position, grapplePoint);

        rb.linearVelocity = targetDir * reelSpeed;

        if (distance <= arrivalThreshold)
        {
            StopReeling();
        }
    }

    private void DrawRope()
    {
        lineRenderer.SetPosition(0, firePoint.position);
        lineRenderer.SetPosition(1, grapplePoint);
    }

    private void AdjustGrapplePoint()
    {
        Vector3 from = firePoint.position;
        Vector3 to = grapplePoint;
        Vector3 dir = (to - from).normalized;
        float dist = Vector3.Distance(from, to);

        if (Physics.Raycast(from, dir, out RaycastHit hit, dist, grappleLayerMask))
        {
            grapplePoint = hit.point;

            joint.connectedAnchor = grapplePoint;
            float newRope = Vector3.Distance(bodyTrans.position, grapplePoint);
            joint.maxDistance = newRope * 0.9f;
            joint.minDistance = newRope * 0.25f;

            maxRope = newRope;
            DrawRope();
        }
    }
}
