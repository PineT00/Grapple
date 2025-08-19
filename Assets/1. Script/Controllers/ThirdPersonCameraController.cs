using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Target Setup")]
    public Transform followTarget;
    public CinemachineCamera cineCam;


    [Header("Rotation Settings")]
    public float mouseSensitivity = 1.5f;
    public float pitchMin = -40f;
    public float pitchMax = 80f;

    [Header("Distance Adjustment")]
    public float maxCameraDistance = 7f;
    public float minCameraDistance = 2f;
    public float distanceSmoothSpeed = 5f;

    private CinemachinePositionComposer camPositionCompser;
    private RagdollCharacterController playerController;
    private float yaw;
    private float pitch;
    private bool distCheck = false;

    void Awake()
    {
        followTarget = transform;

        if (cineCam == null)
        {
            cineCam = FindAnyObjectByType<CinemachineCamera>();
        }

        if (followTarget == null)
        {
            Debug.LogError("No FollowTarget");
        }
        else
        {
            cineCam.Follow = followTarget;
        }
        playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<RagdollCharacterController>();

        camPositionCompser = cineCam.GetComponent<CinemachinePositionComposer>();
    }

    void FixedUpdate()
    {
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        followTarget.rotation = cameraRotation;
        //AdjustCameraDistance();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();
        yaw += lookInput.x * mouseSensitivity * 0.01f;
        pitch -= lookInput.y * mouseSensitivity * 0.01f;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
    }

    private void AdjustCameraDistance()
    {
        if (camPositionCompser == null)
            return;

        float targetDistance;

        if (playerController.CurrState == PlayerState.Walking || playerController.CurrState == PlayerState.Standing)
        {
            distCheck = true;
        }
        else
        {
            distCheck = false;
        }

        if (pitch < 0 && distCheck)
        {
            float t = Mathf.InverseLerp(0f, pitchMin, pitch);
            targetDistance = Mathf.Lerp(maxCameraDistance, minCameraDistance, t);
        }
        else
        {
            targetDistance = maxCameraDistance;
        }

        camPositionCompser.CameraDistance = Mathf.Lerp(
            camPositionCompser.CameraDistance,
            targetDistance,
            Time.deltaTime * distanceSmoothSpeed
        );
    }
}
