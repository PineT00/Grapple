using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public struct BendPoint
{
    public Vector3 position;
    public Vector3 normal;
    public Collider attachedCollider;
}

public class GrappleController : MonoBehaviour
{
    private enum GrappleState { None, Launching, Attached }
    private GrappleState currentState = GrappleState.None;
    public bool IsAttached => currentState == GrappleState.Attached;

    [Header("필수")]
    public Rigidbody anchorRb;
    public Camera cam;
    public Transform firePoint;
    public LayerMask grappleLayerMask;
    public GameObject ropePrefab;
    public Transform visualAnchor;
    private RopeMeshGenerator activeRopeRender;

    [Header("UI")]
    public Image grappleIndicatorUI; // 화면 중앙의 점 이미지
    public Color grappleableColor = Color.green; // 그래플 가능할 때
    public Color nonGrappleableColor = Color.white; // 그래플 불가능할 때

    [Header("파라미터")]
    public float maxRayDistance = 30f;
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

    [Header("조준 코요테 타임")]
    [Tooltip("타겟을 잃은 후에도 그래플 가능한 프레임 수")]
    [Range(0, 30)]
    public int grappleCoyoteFrames = 5;

    public bool GrappleReady { get; private set; }
    private int coyoteFrameCounter = 0;
    private Vector3 potentialGrapplePoint;
    private Vector3 potentialGrappleNormal;
    private Collider potentialGrappleCollider;
    private SpringJoint joint;

    private List<BendPoint> bendPoints = new List<BendPoint>();
    private float currentRopeLength;
    private Vector3 launchTargetPoint;
    private float launchProgress;

    void Start()
    {
        joint = anchorRb.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = joint.transform.InverseTransformPoint(firePoint.position);

        activeRopeRender = Instantiate(ropePrefab).GetComponent<RopeMeshGenerator>();

        SetJoint(false);
    }

    private void FixedUpdate()
    {
        CheckForGrapplePoint();
    }

    private void LateUpdate()
    {
        UpdateGrappleIndicator();

        if (currentState == GrappleState.Launching)
        {
            HandleRopeLaunchVisuals();
        }
        else if (currentState == GrappleState.Attached)
        {
            activeRopeRender.UpdateRopeVisuals(visualAnchor.position, bendPoints, cam.transform);
        }
    }

    public void CheckForGrapplePoint()
    {
        Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));

        Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, GrappleReady ? Color.green : Color.red, 0.1f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grappleLayerMask))
        {
            //Debug.DrawLine(ray.origin, hit.point, Color.yellow, 0.1f);
            // 타겟 감지 성공
            GrappleReady = true;
            coyoteFrameCounter = grappleCoyoteFrames;
            potentialGrapplePoint = hit.point;
            potentialGrappleNormal = hit.normal;
            potentialGrappleCollider = hit.collider;
        }
        else
        {
            // 타겟 감지 실패: 코요테 타임 카운터 감소
            if (coyoteFrameCounter > 0)
            {
                coyoteFrameCounter--;
                GrappleReady = true;
            }
            else
            {
                GrappleReady = false;
            }
        }
    }

    public void OnGrapple()
    {
        // 그래플 발사 시작
        currentState = GrappleState.Launching;
        launchTargetPoint = potentialGrapplePoint;
        launchProgress = 0f;

        // 코요테 카운터 초기화 (발사 후에는 다시 타겟 감지 필요)
        coyoteFrameCounter = 0;

        bendPoints.Clear();
        bendPoints.Add(new BendPoint { position = potentialGrapplePoint, normal = potentialGrappleNormal, attachedCollider = potentialGrappleCollider });
        activeRopeRender.ActivateRope(true);
    }

    public void OnRelease()
    {
        if (currentState == GrappleState.None) return;

        currentState = GrappleState.None;
        bendPoints.Clear();
        SetJoint(false);
        activeRopeRender.ActivateRope(false);
    }

    public void StartReeling()
    {
        // Reeling 시 Spring, Damper 값 조절 (선택)
        joint.spring = 100f;
        joint.damper = 20f;

    }

    public void StopReeling()
    {
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

        // 2. 로프 풀기
        HandleRopeUnwrapping();

        // 4. 새로운 로프 꺾임점 추가
        HandleRopeBending();

        // 5. 조인트 업데이트
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

    private void UpdateGrappleIndicator()
    {
        if (grappleIndicatorUI == null) return;
        grappleIndicatorUI.color = GrappleReady ? grappleableColor : nonGrappleableColor;
    }
    private void HandleRopeLaunchVisuals()
    {
        Vector3 startPoint = firePoint.position;
        float totalDistance = Vector3.Distance(startPoint, launchTargetPoint);

        if (totalDistance > 0)
        {
            launchProgress += (ropeLaunchSpeed * Time.deltaTime) / totalDistance;
        }
        launchProgress = Mathf.Clamp01(launchProgress);

        Vector3 currentTipPosition = Vector3.Lerp(startPoint, launchTargetPoint, launchProgress);

        bendPoints[0] = new BendPoint { position = currentTipPosition };

        activeRopeRender.UpdateRopeVisuals(visualAnchor.position, bendPoints, cam.transform);

        if (launchProgress >= 1f)
        {
            currentState = GrappleState.Attached;
            currentRopeLength = Vector3.Distance(firePoint.position, launchTargetPoint);
            bendPoints[0] = new BendPoint { position = launchTargetPoint, normal = potentialGrappleNormal };
            SetJoint(true); // 물리 조인트
        }
    }

}
