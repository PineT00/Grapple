using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public struct BendPoint
{
    public Vector3 position;
    public Vector3 normal;
}

public class GrappleController : MonoBehaviour
{
    [Header("필수")]
    public Rigidbody anchorRb;
    public Camera cam;
    public Transform firePoint;
    public LayerMask grappleLayerMask;
    public GameObject ropePrefab;
    public Transform visualAnchor;
    private RagdollCharacterController characterContoller;
    private RopeMeshGenerator activeRopeRender;

    [Header("UI")]
    public Image grappleIndicatorUI; // 화면 중앙의 점 역할을 할 UI 이미지
    public Color grappleableColor = Color.green; // 그래플 가능할 때의 색상
    public Color nonGrappleableColor = Color.white; // 그래플 불가능할 때의 색상

    [Header("파라미터")]
    public float maxRayDistance = 30f;
    public float spring = 70f;
    public float damper = 7f;
    public float massScale = 4.5f;
    public float reelSpeed = 25f;

    [SerializeField]
    private float maxRope = 0.7f;

    [SerializeField]
    private float minRope = 0.4f;

    private Vector3 potentialGrapplePoint;
    private Vector3 potentialGrappleNormal;
    private bool isGrappleable = false;
    private bool isGrappling = false;
    private SpringJoint joint;
    private List<BendPoint> bendPoints = new List<BendPoint>();
    private float currentRopeLength;

    void Start()
    {
        characterContoller = GetComponent<RagdollCharacterController>();

        joint = anchorRb.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = joint.transform.InverseTransformPoint(firePoint.position);
        joint.minDistance = minRope;

        activeRopeRender = Instantiate(ropePrefab).GetComponent<RopeMeshGenerator>();

        SetJoint(false);
    }

    void FixedUpdate()
    {
        CheckForGrapplePoint();
        UpdateGrappleIndicator();

        if (isGrappling)
        {
            HandleRopePhysics();
            activeRopeRender.UpdateRopeVisuals(visualAnchor.position, bendPoints, cam.transform);
        }

        if (characterContoller.CurrState == PlayerState.Reeling)
        {
            ShortenRope();
        }
    }

    private void CheckForGrapplePoint()
    {
        Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grappleLayerMask))
        {
            isGrappleable = true;
            potentialGrapplePoint = hit.point;
            potentialGrappleNormal = hit.normal;
        }
        else
        {
            isGrappleable = false;
        }
    }

    public void OnGrapple()
    {
        if (!isGrappleable) return;
        isGrappling = true;

        bendPoints.Clear();
        bendPoints.Add(new BendPoint { position = potentialGrapplePoint, normal = potentialGrappleNormal });

        currentRopeLength = Vector3.Distance(firePoint.position, potentialGrapplePoint);

        characterContoller.SetPlayerState(PlayerState.Swinging);
        SetJoint(true);
        activeRopeRender.ActivateRope(isGrappling);
    }

    public void OnRelease()
    {
        if (!isGrappling) return;
        isGrappling = false;

        characterContoller.SetPlayerState(PlayerState.OnAir);
        SetJoint(false);
        activeRopeRender.ActivateRope(isGrappling);
    }

    public void StartReeling()
    {
        if (!isGrappling) return;
        characterContoller.SetPlayerState(PlayerState.Reeling);
        // Reeling 시 Spring, Damper 값 조절 (선택)
        joint.spring = 200f;
        joint.damper = 50f;
    }

    public void StopReeling()
    {
        if (!isGrappling) return;
        characterContoller.SetPlayerState(PlayerState.Swinging);
        SetJoint(true); // 원래 Spring, Damper 값으로 복원
    }

    public Vector3 GetGrapplePoint()
    {
        return bendPoints.Last().position;
    }

    private void ShortenRope()
    {
        currentRopeLength -= reelSpeed * Time.fixedDeltaTime;
        currentRopeLength = Mathf.Max(currentRopeLength, 0.1f); // 최소 길이
    }

    private void HandleRopePhysics()
    {
        // 로프 풀기
        if (bendPoints.Count > 1)
        {
            Vector3 lastPoint = bendPoints.Last().position;
            Vector3 prevPoint = bendPoints[bendPoints.Count - 2].position;

            // 로프가 둔각으로 펴졌을 때만 풀림 검사
            Vector3 dirToPlayer = (firePoint.position - lastPoint).normalized;
            Vector3 dirToPrev = (prevPoint - lastPoint).normalized;
            if (Vector3.Dot(dirToPlayer, dirToPrev) < 0)
            {
                // 플레이어와 이전 꺾임점 사이에 장애물이 없다면
                float distToPrev = Vector3.Distance(firePoint.position, prevPoint);
                if (!Physics.Raycast(firePoint.position, (prevPoint - firePoint.position).normalized, distToPrev - 0.1f, grappleLayerMask))
                {
                    bendPoints.RemoveAt(bendPoints.Count - 1);
                }
            }
        }

        // 로프 꺾기
        Vector3 lastBendPosition = bendPoints.Last().position;
        Vector3 playerToLastPointDir = (lastBendPosition - firePoint.position).normalized;
        float distToLastPoint = Vector3.Distance(firePoint.position, lastBendPosition);

        if (Physics.Raycast(firePoint.position, playerToLastPointDir, out RaycastHit hit, distToLastPoint - 0.1f, grappleLayerMask))
        {
            if (Vector3.Distance(hit.point, lastBendPosition) > 0.5f)
            {
                Vector3 lastNormal = bendPoints.Last().normal;
                Vector3 newNormal = hit.normal;
                Vector3 offsetDirection = (lastNormal + newNormal).normalized;
                Vector3 finalPoint = hit.point + offsetDirection * 0.18f;

                bendPoints.Add(new BendPoint { position = finalPoint, normal = newNormal });
            }
        }

        UpdateJoint();
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
    }

    private void SetJoint(bool active)
    {
        if (active)
        {
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
        grappleIndicatorUI.color = isGrappleable ? grappleableColor : nonGrappleableColor;
    }
}
