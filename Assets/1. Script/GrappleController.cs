using Unity.VisualScripting;
using UnityEngine;

public class GrappleController : MonoBehaviour
{
    [Header("필수")]
    public Rigidbody anchorRb;
    public Camera cam;
    public Transform firePoint;
    public LayerMask grappleLayerMask;
    public LineRenderer lineRendererPrefab;
    private LineRenderer lineRenderer;
    private RagdollCharacterController characterContoller;
    private GrapplingRope grapplingRope;

    [Header("파라미터")]
    public float maxRayDistance = 30f;
    public float spring = 70f;
    public float damper = 7f;
    public float massScale = 4.5f;
    public float pullForce = 4.5f;
    public float reelSpeed = 25f;
    public float arrivalThreshold = 1.5f;

    [SerializeField]
    private float maxRope = 8.5f;
    
    [SerializeField]
    private float minRope = 1.5f;

    private Vector3 grapplePoint;
    private bool isGrappling = false;
    private float ropeLength = 0f;
    private SpringJoint joint;


    void Start()
    {
        characterContoller = GetComponent<RagdollCharacterController>();
        lineRenderer = Instantiate(lineRendererPrefab);
        lineRenderer.transform.SetParent(anchorRb.transform);
        lineRenderer.positionCount = 0;

        joint = anchorRb.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = joint.transform.InverseTransformPoint(firePoint.position);

        grapplingRope = GetComponent<GrapplingRope>();
        grapplingRope.SetLineRenderer(lineRenderer);
        SetJoint(false);
    }

    void FixedUpdate()
    {
        switch (characterContoller.CurrState)
        {
            case PlayerState.Swinging:
                AdjustGrapplePoint();
                grapplingRope.DrawRope();
                break;
            case PlayerState.Reeling:
                ReelingToTarget();
                grapplingRope.DrawRope();
                break;
        }
    }
    public void OnGrapple()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = cam.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grappleLayerMask))
        {
            grapplePoint = hit.point;
            ropeLength = Vector3.Distance(anchorRb.position, hit.point);
            characterContoller.SetPlayerState(PlayerState.Swinging);
            SetJoint(true);
            grapplingRope.SetRope(true);
        }
    }

    public void OnRelease()
    {
        characterContoller.SetPlayerState(PlayerState.OnAir);
        ropeLength = 0f;
        SetJoint(false);
        grapplingRope.SetRope(false);
    }

    public void StartReeling()
    {
        anchorRb.linearVelocity = Vector3.zero;
        anchorRb.angularVelocity = Vector3.zero;
        characterContoller.SetPlayerState(PlayerState.Reeling);
        joint.spring = 0;
    }
    public void StopReeling()
    {
        if (isGrappling)
        {
            characterContoller.SetPlayerState(PlayerState.Swinging);
            ropeLength = Vector3.Distance(anchorRb.position, grapplePoint);
            joint.spring = spring;
        }
        else
        {
            characterContoller.SetPlayerState(PlayerState.OnAir);
            SetJoint(isGrappling);
            grapplingRope.SetRope(false);
        }
    }
    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }

    private void ReelingToTarget()
    {
        Vector3 toTarget = grapplePoint - firePoint.position;
        float distance = toTarget.magnitude;
        Vector3 dir = toTarget.normalized;

        // 파라미터
        float decelStartDist = 3f;
        float minSpeed = 2f;
        float maxSpeed = 100f;

        // 속도 보간: 거리 멀면 빠르게, 가까우면 감속
        float t = Mathf.InverseLerp(0f, decelStartDist, distance);
        float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);

        // 현재 속도에서 부드러운 가속 적용
        Vector3 currentVel = anchorRb.linearVelocity;
        Vector3 forwardVel = Vector3.Project(currentVel, dir);
        float currentSpeed = forwardVel.magnitude;
        float accelLerpRate = 10f;
        float finalSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * accelLerpRate);

        anchorRb.linearVelocity = dir * finalSpeed;

        // 회전 고정: 가속 방향으로 바라보게
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

        // 도착 처리
        if (distance <= arrivalThreshold)
        {
            anchorRb.linearVelocity = Vector3.zero;
            StopReeling();
        }
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
            ropeLength = Vector3.Distance(anchorRb.position, grapplePoint);
            joint.maxDistance = ropeLength * maxRope;
            joint.minDistance = ropeLength * minRope;
            grapplingRope.BendRope();
        }
    }

    private void SetJoint(bool active = true)
    {
        if (active)
        {
            isGrappling = true;
            joint.connectedAnchor = grapplePoint;
            joint.maxDistance = ropeLength * maxRope;
            joint.minDistance = ropeLength * minRope;
            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;
        }
        else
        {
            isGrappling = false;
            joint.connectedAnchor = anchorRb.position;
            joint.maxDistance = 0;
            joint.minDistance = 0;
            joint.spring = 0;
            joint.damper = 0;
            joint.massScale = 0;
        }
        
    }
}
