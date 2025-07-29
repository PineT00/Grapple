using Unity.VisualScripting;
using UnityEngine;

public class GrappleController : MonoBehaviour
{
    [Header("필수")]
    public Rigidbody handRb;
    public Rigidbody subRb;
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
    public float reelSpeed = 25f;
    public float arrivalThreshold = 1.5f;

    private Vector3 grapplePoint;
    private bool isGrappling = false;
    private float maxRope = 0f;
    private SpringJoint joint;


    void Start()
    {
        if (handRb == null)
        {
            handRb = GetComponent<Rigidbody>();
        }
        characterContoller = GetComponent<RagdollCharacterController>();
        lineRenderer = Instantiate(lineRendererPrefab);
        lineRenderer.positionCount = 0;

        joint = handRb.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        SetRope(false);
    }

    void FixedUpdate()
    {
        switch (characterContoller.CurrState)
        {
            case PlayerState.Swinging:
                AdjustGrapplePoint();
                DrawRope();
                break;
            case PlayerState.Reeling:
                ReelingToTarget();
                DrawRope();
                break;
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
            SetRope(true);
        }
    }

    public void OnRelease()
    {
        characterContoller.SetPlayerState(PlayerState.OnAir);
        maxRope = 0f;
        SetRope(false);
    }

    public void StartReeling()
    {
        handRb.linearVelocity = Vector3.zero;
        characterContoller.SetPlayerState(PlayerState.Reeling);
        joint.spring = 0;
    }
    public void StopReeling()
    {
        if (isGrappling)
        {
            characterContoller.SetPlayerState(PlayerState.Swinging);
            maxRope = Vector3.Distance(bodyTrans.position, grapplePoint);
            joint.spring = spring;
        }
        else
        {
            characterContoller.SetPlayerState(PlayerState.OnAir);
            SetRope(isGrappling);
        }
    }
    private void ReelingToTarget()
    {
        Vector3 targetDir = (grapplePoint - firePoint.position).normalized;
        float distance = Vector3.Distance(firePoint.position, grapplePoint);

        Vector3 targetVelocity = targetDir * reelSpeed;
        float smoothing = 5f;
        handRb.linearVelocity = Vector3.Lerp(handRb.linearVelocity, targetVelocity, Time.fixedDeltaTime * smoothing);

        handRb.linearVelocity = targetDir * reelSpeed * 0.5f;
        subRb.linearVelocity = targetDir * reelSpeed * 0.5f;

        if (distance <= arrivalThreshold)
        {
            handRb.linearVelocity = Vector3.zero;
            subRb.linearVelocity = Vector3.zero;
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
            maxRope = Vector3.Distance(bodyTrans.position, grapplePoint);
            joint.maxDistance = maxRope * 0.9f;
            joint.minDistance = maxRope * 0.25f;
        }
    }

    private void SetRope(bool active)
    {
        if (active)
        {
            isGrappling = true;
            joint.connectedAnchor = grapplePoint;
            joint.maxDistance = maxRope * 0.9f;
            joint.minDistance = maxRope * 0.25f;
            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;

            lineRenderer.positionCount = 2;
        }
        else
        {
            isGrappling = false;
            joint.connectedAnchor = handRb.position;
            joint.maxDistance = 0;
            joint.minDistance = 0;
            joint.spring = 0;
            joint.damper = 0;
            joint.massScale = 0;

            lineRenderer.positionCount = 0;
        }
        
    }
}
