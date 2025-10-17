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

    private float yaw;
    private float pitch;

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

    }

    void FixedUpdate()
    {
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        followTarget.rotation = cameraRotation;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();
        yaw += lookInput.x * mouseSensitivity * 0.01f;
        pitch -= lookInput.y * mouseSensitivity * 0.01f;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
    }
}
