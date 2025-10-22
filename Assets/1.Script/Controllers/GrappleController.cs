using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct BendPoint
{
    public Vector3 position;
    public Vector3 normal;
    public Collider attachedCollider;
}

public enum GrappleState { None, Launching, Attached, Reeling }

public class GrappleController : MonoBehaviour
{
    public GrappleState CurrentState { get; private set; } = GrappleState.None;

    [Header("필수")]
    public Rigidbody anchorRb;
    public Camera cam;
    public Transform firePoint;
    public GameObject ropePrefab;
    public Transform visualAnchor;
    private RopeMeshGenerator activeRopeRender;

    [Header("파라미터")]
    public LayerMask grappleLayerMask;
    public float ropeLaunchSpeed = 80f; // 로프 발사 속도
    public float spring = 70f;
    public float damper = 7f;
    public float massScale = 4.5f;
    public float reelSpeed = 25f;
    [SerializeField]
    private float maxRope = 0.7f;
    [SerializeField]
    private float minRope = 0.4f;

    [Header("로프 물리")]
    [Tooltip("로프가 새로 꺾이기 위해 필요한 최소 거리")]
    public float minNewBendDistance = 0.5f;
    [Tooltip("로프가 풀리기 시작하는 각도의 임계값 (Dot Product)")]
    [Range(-1f, 1f)]
    public float ropeUnwrapAngleThreshold = 0.1f;
    [Tooltip("Raycast가 자기 자신을 감지하지 않도록 주는 오프셋")]
    public float raycastOffset = 0.1f;
    [Tooltip("새 꺾임 지점을 벽에서 살짝 띄우는 거리")]
    public float bendPointOffset = 0.1f;
    private SpringJoint joint;

    private List<BendPoint> bendPoints = new List<BendPoint>();
    private float currentRopeLength;
    private float launchProgress;
    private Vector3 launchPoint;

    void Start()
    {
        joint = anchorRb.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = joint.transform.InverseTransformPoint(firePoint.position);

        activeRopeRender = Instantiate(ropePrefab).GetComponent<RopeMeshGenerator>();

        SetJoint(false);
    }

    private void LateUpdate()
    {
        if (CurrentState == GrappleState.Launching)
        {
            HandleRopeLaunchVisuals();
        }
        else if (CurrentState == GrappleState.Attached || CurrentState == GrappleState.Reeling)
        {
            activeRopeRender.UpdateRopeVisuals(visualAnchor.position, bendPoints, cam.transform);
        }
    }

    public void StartGrapple(BendPoint bendPoint)
    {
        // 그래플 발사 시작
        CurrentState = GrappleState.Launching;
        launchProgress = 0f;

        bendPoints.Clear();
        bendPoints.Add(bendPoint);
        activeRopeRender.ActivateRope(true);
        launchPoint = bendPoint.position;
    }

    public void ReleaseGrapple()
    {
        if (CurrentState == GrappleState.None) return;

        SwitchGrappleState(GrappleState.None);
        bendPoints.Clear();
        SetJoint(false);
        activeRopeRender.ActivateRope(false);
    }

    public void StartReeling()
    {
        SwitchGrappleState(GrappleState.Reeling);
        joint.spring = 100f;
        joint.damper = 20f;
    }

    public void StopReeling()
    {
        SwitchGrappleState(GrappleState.Attached);
        SetJoint(true); // 원래 Spring, Damper 값으로 복원
    }

    public Vector3 GetGrapplePoint()
    {
        return bendPoints.Last().position;
    }

    public void ShortenRope()
    {
        currentRopeLength -= reelSpeed * Time.fixedDeltaTime;
        if (currentRopeLength <= 0)
        {
            currentRopeLength = 0;
        }
    }

    public void HandleRopePhysics()
    {
        if (bendPoints.Count <= 0) return;

        // 1. 움직이는 발사 지점 업데이트
        //UpdateGrappledObjectPosition();

        HandleRopeUnwrapping();
        HandleRopeBending();
        UpdateJoint();
    }

    private void UpdateGrappledObjectPosition()
    {
        //if (grappledObjectTransform != null)
        //{
        //    var firstBend = bendPoints[0];
        //    firstBend.position = grappledObjectTransform.TransformPoint(grappleOffset);
        //    bendPoints[0] = firstBend;
        //}
    }
    private void HandleRopeUnwrapping()
    {
        if (bendPoints.Count <= 1) return;

        Vector3 lastPoint = bendPoints.Last().position;
        Vector3 prevPoint = bendPoints[bendPoints.Count - 2].position;
        Vector3 dirToPlayer = (firePoint.position - lastPoint).normalized;
        Vector3 dirToPrev = (prevPoint - lastPoint).normalized;

        if (Vector3.Dot(dirToPlayer, dirToPrev) > ropeUnwrapAngleThreshold)
        {
            float distToPrev = Vector3.Distance(firePoint.position, prevPoint);
            if (!Physics.Raycast(firePoint.position, (prevPoint - firePoint.position).normalized, distToPrev - raycastOffset, grappleLayerMask))
            {
                bendPoints.RemoveAt(bendPoints.Count - 1);
            }
        }
    }

    private void HandleRopeBending()
    {
        Vector3 lastBendPosition = bendPoints.Last().position;
        Vector3 playerToLastPointDir = (lastBendPosition - firePoint.position).normalized;
        float distToLastPoint = Vector3.Distance(firePoint.position, lastBendPosition);

        Vector3 rayStartPoint = firePoint.position + playerToLastPointDir * raycastOffset;

        if (Physics.Raycast(rayStartPoint, playerToLastPointDir, out RaycastHit hit, distToLastPoint - raycastOffset, grappleLayerMask))
        {
            if (Vector3.Distance(hit.point, lastBendPosition) > minNewBendDistance)
            {
                bendPoints.Add(new BendPoint
                {
                    position = hit.point + hit.normal * bendPointOffset,
                    normal = hit.normal,
                    attachedCollider = hit.collider
                });
            }
        }
    }

    private void UpdateJoint()
    {
        joint.connectedAnchor = bendPoints.Last().position;

        float wrappedLength = 0;
        if (bendPoints.Count > 1)
        {
            for (int i = 0; i < bendPoints.Count - 1; i++)
            {
                wrappedLength += Vector3.Distance(bendPoints[i].position, bendPoints[i + 1].position);
            }
        }

        joint.maxDistance = (currentRopeLength - wrappedLength) * maxRope;
        joint.minDistance = (currentRopeLength - wrappedLength) * minRope;
    }

    [SerializeField] private float referenceLength = 10f; // 기준 길이
    [SerializeField] private float springScalingPower = 1f;

    private void SetJoint(bool active)
    {
        if (active)
        {
            float lengthRatio = referenceLength / Mathf.Max(currentRopeLength, 0.1f);
            float springScale = Mathf.Pow(lengthRatio, springScalingPower);

            joint.spring = spring * springScale;
            joint.damper = damper * springScale;
            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;
            UpdateJoint();
        }
        else
        {
            joint.spring = 0;
            joint.damper = 0;
            joint.massScale = 0;
            joint.connectedAnchor = anchorRb.position;
        }
    }

    public void HandleRopeLaunchVisuals()
    {
        Vector3 startPoint = firePoint.position;
        float totalDistance = Vector3.Distance(startPoint, launchPoint);

        if (totalDistance > 0)
        {
            launchProgress += (ropeLaunchSpeed * Time.deltaTime) / totalDistance;
        }
        launchProgress = Mathf.Clamp01(launchProgress);

        Vector3 currentTipPosition = Vector3.Lerp(startPoint, launchPoint, launchProgress);

        bendPoints[0] = new BendPoint { position = currentTipPosition };

        activeRopeRender.UpdateRopeVisuals(visualAnchor.position, bendPoints, cam.transform);

        if (launchProgress >= 1f)
        {
            SwitchGrappleState(GrappleState.Attached);
            currentRopeLength = Vector3.Distance(firePoint.position, launchPoint);
            bendPoints[0] = new BendPoint { position = launchPoint };
            SetJoint(true); // 물리 조인트
        }
    }

    private void SwitchGrappleState(GrappleState state)
    {
        CurrentState = state;
    }

}
