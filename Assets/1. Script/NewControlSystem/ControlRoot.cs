
using UnityEngine;

/// <summary>
/// 플레이어의 입력을 받아 움직이는 보이지 않는 컨트롤러.
/// 물리 시뮬레이션의 영향을 받지 않으며, 랙돌이 따라가야 할 목표(Target) 역할을 합니다.
/// </summary>
public class ControlRoot : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f; // 캐릭터가 방향을 전환하는 속도

    [Header("References")]
    public Transform cameraTransform; // 메인 카메라의 Transform

    private CharacterController characterController;
    private Vector3 moveDirection;

    void Awake()
    {
        // 이 오브젝트에 CharacterController를 추가하여 사용합니다.
        // CharacterController는 물리 충돌은 감지하지만 Rigidbody의 물리 시뮬레이션은 따르지 않아
        // 안정적인 이동 처리에 적합합니다.
        characterController = gameObject.AddComponent<CharacterController>();
        characterController.radius = 0.5f;
        characterController.height = 1.8f;
        characterController.center = new Vector3(0, 0.9f, 0);

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleRotation();
    }

    /// <summary>
    /// 플레이어 입력을 읽어옵니다.
    /// </summary>
    private void HandleInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 카메라의 정면과 오른쪽 방향을 기준으로 이동 방향을 계산합니다.
        Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;

        moveDirection = (camForward * vertical + camRight * horizontal).normalized;
    }

    /// <summary>
    /// 계산된 이동 방향으로 CharacterController를 움직입니다.
    /// </summary>
    private void HandleMovement()
    {
        // CharacterController.Move는 월드 좌표계 기준입니다.
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 캐릭터가 움직이는 방향을 바라보도록 회전시킵니다.
    /// </summary>
    private void HandleRotation()
    {
        if (moveDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
