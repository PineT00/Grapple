using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    [Tooltip("점수가 쌓이기 시작하는 최소 속도")]
    public float minSpeedForScore = 10f;
    [Tooltip("최소 속도 이상일 때 초당 점수 증가량")]
    public float scorePerSecond = 1f;
    [Tooltip("스윙 포인트 연결 시 추가 점수")]
    public float swingPointBonus = 50f;
    [Tooltip("벽면 근접 시 점수 배율")]
    public float wallProximityMultiplier = 3f;

    [Header("Wall Proximity Detection")]
    [Tooltip("벽면 감지용 레이어")]
    public LayerMask wallLayer;
    [Tooltip("아슬아슬하다고 판단하는 벽면까지 거리")]
    public float proximityDistance = 2f;
    [Tooltip("벽면 감지 레이캐스트 방향 개수")]
    public int raycastDirections = 8;

    public TextMeshProUGUI scoreText;

    [Header("References")]
    public RagdollCharacterController playerController;

    // 점수 관련
    private float currentScore = 0f;
    private bool isNearWall = false;

    // 이벤트
    public UnityEvent<float> OnScoreChanged;
    public UnityEvent<float> OnSwingPointBonus;

    public float CurrentScore => currentScore;
    public bool IsNearWall => isNearWall;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<RagdollCharacterController>();
        }

        // GrappleController에 이벤트 연결
        if (playerController != null)
        {
            SubscribeToGrappleEvents();
        }
    }

    private void Update()
    {
        if (playerController == null) return;

        // 속도 기반 점수 계산
        CalculateSpeedScore();

        // 벽면 근접 감지
        CheckWallProximity();
    }

    private void CalculateSpeedScore()
    {
        float currentSpeed = playerController.mainRb.linearVelocity.magnitude;

        if (currentSpeed >= minSpeedForScore)
        {
            float baseScore = scorePerSecond * Time.deltaTime;

            // 벽면 근접 시 점수 곱
            if (isNearWall)
            {
                baseScore *= wallProximityMultiplier;
            }

            AddScore(baseScore);
        }
    }

    /// <summary>
    /// 벽면 근접 감지
    /// </summary>
    private void CheckWallProximity()
    {
        if (playerController == null || playerController.mainRb == null)
        {
            isNearWall = false;
            return;
        }

        Vector3 playerPosition = playerController.mainRb.position;
        bool foundNearbyWall = false;

        // 여러 방향으로 레이캐스트를 쏴서 벽면 감지
        for (int i = 0; i < raycastDirections; i++)
        {
            float angle = (360f / raycastDirections) * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            if (Physics.Raycast(playerPosition, direction, proximityDistance, wallLayer))
            {
                foundNearbyWall = true;
                break;
            }
        }

        // 위아래 방향도 체크
        if (!foundNearbyWall)
        {
            if (Physics.Raycast(playerPosition, Vector3.up, proximityDistance, wallLayer) ||
                Physics.Raycast(playerPosition, Vector3.down, proximityDistance, wallLayer))
            {
                foundNearbyWall = true;
            }
        }

        isNearWall = foundNearbyWall;
    }

    /// <summary>
    /// 점수 추가
    /// </summary>
    public void AddScore(float amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
        scoreText.text = currentScore.ToString("F0");
    }

    /// <summary>
    /// 스윙 포인트 보너스 점수 추가
    /// </summary>
    public void AddSwingPointBonus()
    {
        AddScore(swingPointBonus);
        OnSwingPointBonus?.Invoke(swingPointBonus);
    }

    /// <summary>
    /// 점수 리셋
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0f;
        OnScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// GrappleController 이벤트 구독
    /// 그래플이 붙을 때 보너스 점수 추가
    /// </summary>
    private void SubscribeToGrappleEvents()
    {
        // 참고: GrappleController에 이벤트가 없다면
        // StartGrapple 메서드를 수정하여 이벤트를 추가해야 합니다.
        // 현재는 플레이어가 스윙 상태로 전환될 때를 감지하는 방식으로 구현했습니다.
    }

    private PlayerBaseState lastState = null;

    private void LateUpdate()
    {
        // 스윙 상태로 전환되었을 때 보너스 점수
        if (playerController != null && playerController.CurrentState != lastState)
        {
            if (playerController.CurrentState is SwingingState && lastState is not SwingingState)
            {
                // 스윙 상태로 진입 시 보너스
                AddSwingPointBonus();
            }
            lastState = playerController.CurrentState;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (playerController == null || playerController.mainRb == null) return;

        Vector3 playerPosition = playerController.mainRb.position;

        // 벽면 감지 레이캐스트 시각화
        Gizmos.color = isNearWall ? Color.red : Color.green;
        for (int i = 0; i < raycastDirections; i++)
        {
            float angle = (360f / raycastDirections) * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Gizmos.DrawRay(playerPosition, direction * proximityDistance);
        }

        Gizmos.DrawRay(playerPosition, Vector3.up * proximityDistance);
        Gizmos.DrawRay(playerPosition, Vector3.down * proximityDistance);
    }
#endif
}
