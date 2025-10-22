using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum GrabState { None, Ready, Attached }

public class GrabController : MonoBehaviour
{
    public GrabState CurrentState { get; private set; } = GrabState.None;

    [Header("필수")]
    public Rigidbody playerRb;
    public Camera cam;
    public Transform firePoint;
    public LayerMask grabbableLayerMask;
    public GameObject ropePrefab;
    public Transform visualAnchor;
    private RopeMeshGenerator activeRopeRender;

    [Header("UI")]
    public Image grabIndicatorUI;

    [Header("파라미터")]
    public float maxRayDistance = 30f;
    public float pullForce = 50f;
    public float spring = 70f;
    public float damper = 7f;
    public float massScale = 4.5f;

    private Vector3 potentialGrabPoint;
    private Rigidbody potentialGrabRb;
    private Rigidbody currentGrabRb;
    private SpringJoint joint;

    void Start()
    {
        joint = playerRb.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = joint.transform.InverseTransformPoint(firePoint.position);
        joint.minDistance = 0.5f;

        activeRopeRender = Instantiate(ropePrefab).GetComponent<RopeMeshGenerator>();

        SetJoint(false);
    }

    void FixedUpdate()
    {
        if (CurrentState == GrabState.Attached)
            return;

        CheckForGrabbableObject();
    }

    void LateUpdate()
    {
        if (CurrentState == GrabState.Attached)
        {
            UpdateRopeVisuals();
        }
        else
        {
            UpdateGrabIndicator();
        }
    }

    private void CheckForGrabbableObject()
    {
        Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, grabbableLayerMask))
        {
            potentialGrabRb = hit.rigidbody;
            if (potentialGrabRb != null)
            {
                CurrentState = GrabState.Ready;
                potentialGrabPoint = hit.point;
            }
            else
            {
                CurrentState = GrabState.None;
                Debug.LogWarning("Grabbable object must have a Rigidbody!");
            }
        }
        else
        {
            CurrentState = GrabState.None;
        }
    }

    public void SetGrab(bool isActive)
    {
        if (isActive)
        {
            StartGrab();
        }
        else
        {
            ReleaseGrab();
        }
    }

    public void StartGrab()
    {
        currentGrabRb = potentialGrabRb;
        CurrentState = GrabState.Attached;

        joint.connectedBody = currentGrabRb;
        joint.connectedAnchor = currentGrabRb.transform.InverseTransformPoint(potentialGrabPoint);

        SetJoint(true);
        activeRopeRender.ActivateRope(true);
        UpdateGrabIndicator();
    }

    public void ReleaseGrab()
    {
        CurrentState = GrabState.None;
        currentGrabRb = null;

        SetJoint(false);
        activeRopeRender.ActivateRope(false);
    }

    public Vector3 GetGrabPoint()
    {
        return currentGrabRb.transform.position;
    }

    private void SetJoint(bool active)
    {
        if (active)
        {
            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;
        }
        else
        {
            joint.spring = 0;
            joint.damper = 0;
            joint.massScale = 0;
            joint.connectedBody = null;
        }
    }

    private void UpdateGrabIndicator()
    {
        if (grabIndicatorUI == null) return;
        //grabIndicatorUI.color = GrabReady ? grabbableColor : nonGrabbableColor;

        grabIndicatorUI.gameObject.SetActive(CurrentState == GrabState.Ready);
    }

    private void UpdateRopeVisuals()
    {
        if (currentGrabRb == null) return;

        Vector3 grabPoint = currentGrabRb.transform.TransformPoint(joint.connectedAnchor);
        var bendPoints = new System.Collections.Generic.List<BendPoint>
        {
            new BendPoint { position = grabPoint }
        };

        activeRopeRender.UpdateRopeVisuals(visualAnchor.position, bendPoints, cam.transform);
    }
}
