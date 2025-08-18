using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Target Setup")]
    public Transform followTarget;
    public CinemachineCamera cinemachineCam;

    [Header("Rotation Settings")]
    public float mouseSensitivity = 1.5f;
    public float pitchMin = -40f;
    public float pitchMax = 80f;

    private float yaw;
    private float pitch;



    void Awake()
    {
        followTarget = transform;
        
        if (cinemachineCam == null)
        {
            cinemachineCam = FindAnyObjectByType<CinemachineCamera>();
        }

        if (followTarget == null)
        {
            Debug.LogError("No FollowTarget");
        }
        else
        {
            cinemachineCam.Follow = followTarget;   
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
