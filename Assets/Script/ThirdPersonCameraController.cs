using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Target Setup")]
    public Transform followTarget;
    public Transform aimTarget;

    [Header("Rotation Settings")]
    public float mouseSensitivity = 1.5f;
    public float pitchMin = -40f;
    public float pitchMax = 80f;

    private float yaw;
    private float pitch;

    private CinemachineCamera cinemachineCam;

    void Awake()
    {
        cinemachineCam = GetComponent<CinemachineCamera>();

        if (followTarget == null) Debug.LogError("No FollowTarget");
        if (aimTarget == null) Debug.LogError("No AimTarget");
    }

    void FixedUpdate()
    {
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);

        aimTarget.rotation = cameraRotation;
        followTarget.rotation = cameraRotation;
    }

    void LateUpdate()
    {
        if (cinemachineCam != null)
        {
            cinemachineCam.Follow = followTarget;
            cinemachineCam.LookAt = aimTarget;
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();
        yaw += lookInput.x * mouseSensitivity * 0.01f;
        pitch -= lookInput.y * mouseSensitivity * 0.01f;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
    }
}
