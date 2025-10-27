using UnityEngine;
using UnityEngine.UI;

public class GrappleChecker : MonoBehaviour
{
    [Header("필수")]
    public Camera cam;
    public LayerMask grappleLayerMask;

    [Header("UI")]
    public Image grappleIndicatorUI; // 화면 중앙의 점 이미지
    public Image subIndicatorUI_1;
    public Image subIndicatorUI_2;
    public RectTransform grappleDistancePanel; // 거리 표시용 패널 (양쪽으로 벌어짐)
    public Color grappleableColor = Color.green; // 그래플 가능할 때
    public Color nonGrappleableColor = Color.white; // 그래플 불가능할 때

    [Header("수치")]
    public float maxRayDistance = 30f; // 레이캐스트 최대 거리
    public float grappleableDistance = 15f; // 그래플 실제 가능 거리
    public int grappleCoyoteFrames = 5;
    private int coyoteFrameCounter = 0;


    [Header("거리 UI 설정")]
    public float minPanelWidth = 35f;  // 최소 폭 (가장 가까울 때)
    public float maxPanelWidth = 300f; // 최대 폭 (최대 거리일 때)
    public float uiSmoothSpeed = 10f;  // UI 변화 부드러움 정도

    private bool grappleCheck = false;
    private bool surfaceDetected = false; // 표면 감지 여부
    private float currentDistance = 0f;
    private float targetPanelWidth = 0f;
    private float currentPanelWidth = 0f;

    private Vector3 potentialGrapplePoint;
    private Vector3 potentialGrappleNormal;
    private Collider potentialGrappleCollider;

    void FixedUpdate()
    {
        CheckForGrapplePoint();
        UpdateGrappleIndicator();
    }

    void Update()
    {
        UpdateDistancePanel();
    }

    public void CheckForGrapplePoint()
    {
        Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));

        Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, grappleCheck ? Color.green : Color.red, 0.1f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grappleLayerMask))
        {
            // 표면 감지 성공
            surfaceDetected = true;
            currentDistance = Vector3.Distance(ray.origin, hit.point);

            potentialGrapplePoint = hit.point;
            potentialGrappleNormal = hit.normal;
            potentialGrappleCollider = hit.collider;

            // 그래플 가능 거리 안에 들어왔는지 체크
            if (currentDistance <= grappleableDistance)
            {
                grappleCheck = true;
                coyoteFrameCounter = grappleCoyoteFrames;
            }
            else
            {
                // 표면은 감지되었지만 그래플 가능 거리 밖
                if (coyoteFrameCounter > 0)
                {
                    coyoteFrameCounter--;
                    grappleCheck = true;
                }
                else
                {
                    grappleCheck = false;
                }
            }
        }
        else
        {
            // 표면 감지 실패
            surfaceDetected = false;

            // 코요테 타임 카운터 감소
            if (coyoteFrameCounter > 0)
            {
                coyoteFrameCounter--;
                grappleCheck = true;
            }
            else
            {
                grappleCheck = false;
            }
        }
    }

    private void UpdateGrappleIndicator()
    {
        if (grappleIndicatorUI == null) return;
        grappleIndicatorUI.color = grappleCheck ? grappleableColor : nonGrappleableColor;
        subIndicatorUI_1.color = grappleCheck ? grappleableColor : nonGrappleableColor;
        subIndicatorUI_2.color = grappleCheck ? grappleableColor : nonGrappleableColor;
    }

    private void UpdateDistancePanel()
    {
        if (grappleDistancePanel == null) return;

        if (surfaceDetected)
        {
            if (currentDistance >= grappleableDistance)
            {
                float distanceRange = maxRayDistance - grappleableDistance;
                float excessDistance = currentDistance - grappleableDistance;
                float normalizedDistance = Mathf.Clamp01(excessDistance / distanceRange);

                targetPanelWidth = Mathf.Lerp(minPanelWidth, maxPanelWidth, normalizedDistance);
            }
            else
            {
                targetPanelWidth = minPanelWidth;
            }
        }
        else
        {
            targetPanelWidth = maxPanelWidth;
        }

        currentPanelWidth = Mathf.Lerp(currentPanelWidth, targetPanelWidth, Time.deltaTime * uiSmoothSpeed);
        grappleDistancePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentPanelWidth);
    }

    public bool GetGrappleCheck()
    {
        return grappleCheck;
    }

    public BendPoint GetBendPoint()
    {
        return new BendPoint
        {
            position = potentialGrapplePoint,
            normal = potentialGrappleNormal,
            attachedCollider = potentialGrappleCollider
        };
    }
}
