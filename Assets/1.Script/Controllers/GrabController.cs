using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GrabController : MonoBehaviour
{
    private enum GrabState { None, Attached }
    private GrabState currentState = GrabState.None;
    public bool IsGrabbing => currentState == GrabState.Attached;

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

    public bool GrabReady { get; private set; }
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
        CheckForGrabbableObject();
    }

    void LateUpdate()
    {
        UpdateGrabIndicator();

        if (currentState == GrabState.Attached && currentGrabRb != null)
        {
            UpdateRopeVisuals();
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
                GrabReady = true;
                potentialGrabPoint = hit.point;
            }
            else
            {
                GrabReady = false;
                Debug.LogWarning("Grabbable object must have a Rigidbody!");
            }
        }
        else
        {
            GrabReady = false;
        }
    }

    public void OnGrab(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0)
        {
            if (!GrabReady || currentState != GrabState.None) return;

            StartGrab();
        }
        else
        {
            ReleaseGrab();
        }
    }

    private void StartGrab()
    {
        currentGrabRb = potentialGrabRb;
        currentState = GrabState.Attached;

        joint.connectedBody = currentGrabRb;
        joint.connectedAnchor = currentGrabRb.transform.InverseTransformPoint(potentialGrabPoint);

        SetJoint(true);
        activeRopeRender.ActivateRope(true);
    }

    private void ReleaseGrab()
    {
        if (currentState == GrabState.None) return;

        currentState = GrabState.None;
        currentGrabRb = null;

        SetJoint(false);
        activeRopeRender.ActivateRope(false);
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
        grabIndicatorUI.gameObject.SetActive(GrabReady);

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
