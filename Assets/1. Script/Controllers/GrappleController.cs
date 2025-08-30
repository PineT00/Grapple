using UnityEngine;
using UnityEngine.UI;

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

    [Header("UI")]
    public Image grappleIndicatorUI; // 화면 중앙의 점 역할을 할 UI 이미지
    public Color grappleableColor = Color.green; // 그래플 가능할 때의 색상
    public Color nonGrappleableColor = Color.white; // 그래플 불가능할 때의 색상

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

    private Vector3 potentialGrapplePoint;
    private Vector3 grapplePoint;
    private bool isGrappleable = false;
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
        CheckForGrapplePoint();
        UpdateGrappleIndicator();

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
            default:
                break;
        }
    }

    private void CheckForGrapplePoint()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = cam.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grappleLayerMask))
        {
            isGrappleable = true;
            potentialGrapplePoint = hit.point;
        }
        else
        {
            isGrappleable = false;
        }
    }

    public void OnGrapple()
    {
        if (!isGrappleable) return;

        grapplePoint = potentialGrapplePoint;
        ropeLength = Vector3.Distance(anchorRb.position, potentialGrapplePoint);
        characterContoller.SetPlayerState(PlayerState.Swinging);
        SetJoint(true);
        grapplingRope.SetRope(true);
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

        float decelStartDist = 3f;
        float minSpeed = 2f;
        float maxSpeed = 100f;

        float t = Mathf.InverseLerp(0f, decelStartDist, distance);
        float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);

        Vector3 currentVel = anchorRb.linearVelocity;
        Vector3 forwardVel = Vector3.Project(currentVel, dir);
        float currentSpeed = forwardVel.magnitude;
        float accelLerpRate = 10f;
        float finalSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * accelLerpRate);

        anchorRb.linearVelocity = dir * finalSpeed;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

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
    private void UpdateGrappleIndicator()
    {
        if (grappleIndicatorUI == null) return; // UI가 할당되지 않았으면 실행하지 않음

        if (isGrappleable)
        {
            grappleIndicatorUI.color = grappleableColor;
        }
        else
        {
            grappleIndicatorUI.color = nonGrappleableColor;
        }
    }
}
