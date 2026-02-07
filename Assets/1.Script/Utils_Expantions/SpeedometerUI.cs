using UnityEngine;

public class SpeedometerUI : MonoBehaviour
{
    [SerializeField] private RectTransform needle; // Image 컴포넌트의 RectTransform
    [SerializeField] private float maxSpeed = 200f;
    [SerializeField] private float minAngle = -120f; // 시작 각도
    [SerializeField] private float maxAngle = 120f;  // 끝 각도

    private float currentSpeed;
    public RagdollCharacterController characterController;
    private Rigidbody rb;
    private Vector3 horizontalVelocity;

    private void Start()
    {
        if (characterController == null)
        {
            characterController = FindFirstObjectByType<RagdollCharacterController>();
        }
        rb = characterController.mainRb;
    }

    void Update()
    {
        horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;
        float speedNormalized = Mathf.Clamp01(horizontalVelocity.magnitude / maxSpeed);
        float angle = Mathf.Lerp(minAngle, maxAngle, speedNormalized);
        needle.localEulerAngles = new Vector3(0, 0, -angle);
    }
}
