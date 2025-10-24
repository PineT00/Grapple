using TMPro;
using UnityEngine;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI interactionText;

    [Header("Settings")]
    [SerializeField] private float heightOffset = 2f; // 오브젝트 위 얼마나 띄울지
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSpeed = 10f;

    private Camera mainCamera;
    private Transform currentTarget;
    private bool isShowing = false;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCamera = Camera.main;

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    void LateUpdate()
    {
        // UI가 보이는 동안 계속 위치와 회전 업데이트
        if (isShowing && currentTarget != null)
        {
            UpdateUIPosition();
            UpdateUIRotation();
        }
    }

    /// <summary>
    /// 상호작용 UI 표시
    /// </summary>
    public void Show(Transform target, string text = "[E]")
    {
        if (interactionPrompt == null || interactionText == null)
        {
            Debug.LogError("InteractionUIManager: UI 참조가 설정되지 않았습니다!");
            return;
        }

        currentTarget = target;
        interactionText.text = text;
        interactionPrompt.SetActive(true);
        isShowing = true;

        UpdateUIPosition();
        UpdateUIRotation();
    }

    /// <summary>
    /// 위치만 업데이트 (다른 오브젝트로 이동)
    /// </summary>
    public void UpdateTarget(Transform newTarget)
    {
        currentTarget = newTarget;
    }

    /// <summary>
    /// 텍스트만 변경
    /// </summary>
    public void UpdateText(string newText)
    {
        if (interactionText != null)
        {
            interactionText.text = newText;
        }
    }

    /// <summary>
    /// 상호작용 UI 숨기기
    /// </summary>
    public void Hide()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        isShowing = false;
        currentTarget = null;
    }

    /// <summary>
    /// UI 위치 업데이트
    /// </summary>
    private void UpdateUIPosition()
    {
        if (currentTarget != null && interactionPrompt != null)
        {
            Vector3 targetPosition = currentTarget.position + Vector3.up * heightOffset;
            interactionPrompt.transform.position = targetPosition;
        }
    }

    /// <summary>
    /// UI가 카메라를 향하도록 회전
    /// </summary>
    private void UpdateUIRotation()
    {
        if (mainCamera == null || interactionPrompt == null)
            return;

        // 카메라와 같은 방향을 바라보게
        if (smoothRotation)
        {
            interactionPrompt.transform.rotation = Quaternion.Slerp(
                interactionPrompt.transform.rotation,
                mainCamera.transform.rotation,
                Time.deltaTime * rotationSpeed
            );
        }
        else
        {
            interactionPrompt.transform.rotation = mainCamera.transform.rotation;
        }
    }

    /// <summary>
    /// 높이 오프셋 설정
    /// </summary>
    public void SetHeightOffset(float offset)
    {
        heightOffset = offset;
    }
}
