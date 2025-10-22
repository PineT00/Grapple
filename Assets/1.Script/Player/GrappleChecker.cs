using UnityEngine;
using UnityEngine.UI;

public class GrappleChecker : MonoBehaviour
{
    [Header("필수")]
    public Camera cam;
    public LayerMask grappleLayerMask;

    [Header("UI")]
    public Image grappleIndicatorUI; // 화면 중앙의 점 이미지
    public Color grappleableColor = Color.green; // 그래플 가능할 때
    public Color nonGrappleableColor = Color.white; // 그래플 불가능할 때

    [Header("수치")]
    public float maxRayDistance = 30f;
    public int grappleCoyoteFrames = 5;
    private int coyoteFrameCounter = 0;

    private bool grappleCheck = false;

    private Vector3 potentialGrapplePoint;
    private Vector3 potentialGrappleNormal;
    private Collider potentialGrappleCollider;

    void FixedUpdate()
    {
        CheckForGrapplePoint();
        UpdateGrappleIndicator();
    }

    public void CheckForGrapplePoint()
    {
        Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));

        Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, grappleCheck ? Color.green : Color.red, 0.1f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grappleLayerMask))
        {
            //Debug.DrawLine(ray.origin, hit.point, Color.yellow, 0.1f);
            // 타겟 감지 성공
            grappleCheck = true;
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
