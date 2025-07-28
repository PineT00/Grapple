using UnityEngine;

public class ManualGrappleController : MonoBehaviour
{
    public Vector3 GrapplePoint { get; private set; }
    public bool IsGrappling { get; private set; }

    [Header("파라미터")]
    public Camera cam;
    public Rigidbody playerRb;
    public float reelInAcceleration = 30f;
    public float minRopeLength = 1.5f;
    public float maxRayDistance = 50f;
    public LayerMask grappleLayerMask;
    public LineRenderer lineRendererPrefab;
    public float mass = 20f;

    private LineRenderer lineRenderer;
    private float ropeLength;
    private float reelInSpeed;
    private bool isReelingIn;
    private bool isRopeInTension;

    void Start()
    {
        lineRenderer = Instantiate(lineRendererPrefab);
        lineRenderer.positionCount = 0;
    }

    public void StartGrapple()
    {
        Debug.Log("Grapple Start");
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = cam.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grappleLayerMask))
        {
            GrapplePoint = hit.point;
            IsGrappling = true;
            ropeLength = Vector3.Distance(playerRb.position, GrapplePoint);
            reelInSpeed = 0f;
            isReelingIn = false;

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                DrawRope();
            }
            Debug.Log("Hit!");
        }
    }

    public void StopGrapple()
    {
        Debug.Log("Grapple Stop");
        lineRenderer.positionCount = 0;
        IsGrappling = false;
    }

    void FixedUpdate()
    {
        if (!IsGrappling) return;

        DrawRope();

        Vector3 ropeDir = GrapplePoint - playerRb.position;
        float dist = ropeDir.magnitude;
        isRopeInTension = dist > ropeLength;



        // (3) 장력 및 물리 힘 적용
        if (isRopeInTension)
        {
            Vector3 ropeDirNorm = ropeDir.normalized;

            // 원운동 장력 계산(중력, 구심력)
            float theta = Vector3.Angle(ropeDirNorm, Vector3.up) * Mathf.Deg2Rad;
            float centripetalAccel = playerRb.linearVelocity.sqrMagnitude / ropeLength;
            Vector3 tension =
                mass * (centripetalAccel + Physics.gravity.magnitude * Mathf.Cos(theta))
                * ropeDirNorm;

            playerRb.AddForce(tension, ForceMode.Force);

            // (4) 릴인 중이면 추가 힘
            if (isReelingIn)
            {
                playerRb.AddForce(mass * reelInAcceleration * ropeDirNorm, ForceMode.Force);
            }
        }

        //(5) 로프 길이 초과 상태일 때 강제 위치/속도 보정 (줄을 뚫고 나가지 않게)
        if (isRopeInTension && Vector3.Dot(playerRb.linearVelocity, ropeDir) > 0f)
        {
            Vector3 tangentVel = Vector3.ProjectOnPlane(playerRb.linearVelocity, ropeDir.normalized);
            playerRb.linearVelocity = tangentVel;
        }

        // (2) 릴인(줄 감기) 예시: 외부에서 isReelingIn true로 설정 시 동작
        if (isReelingIn && ropeLength > minRopeLength)
        {
            reelInSpeed += reelInAcceleration * Time.fixedDeltaTime;
            ropeLength = Mathf.Max(ropeLength - reelInSpeed * Time.fixedDeltaTime, minRopeLength);
        }
        else
        {
            reelInSpeed = 0f;
        }
    }

    public void StartReelIn()
    {
        isReelingIn = true;
    }

    public void StopReelIn()
    {
        isReelingIn = false;
    }
        private void DrawRope()
    {
        lineRenderer.SetPosition(0, playerRb.position);
        lineRenderer.SetPosition(1, GrapplePoint);
    }
}
