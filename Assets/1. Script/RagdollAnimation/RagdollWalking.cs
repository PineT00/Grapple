using UnityEngine;

public class RagdollWalking : MonoBehaviour
{
    [Header("오브젝트 참조")]
    [SerializeField] private Transform hipTransform;
    [SerializeField] private LegStepper leftLeg;
    [SerializeField] private LegStepper rightLeg;

    [Header("걷기 리듬 설정")]
    [Tooltip("초당 걸음 수. 2로 설정하면 1초에 두 걸음을 걷습니다.")]
    public float stepFrequency = 2.0f;
    [Tooltip("이동 방향으로 얼마나 멀리 발을 내딛을지 결정합니다.")]
    public float stepPredictionFactor = 0.2f;

    // 내부 리듬 및 순서 제어 변수
    private float walkCycleTimer;
    private bool isLeftLegTurn = true;

    // 각 다리의 상태와 로직을 관리하는 내부 클래스
    [System.Serializable]
    private class LegStepper
    {
        // ... [LegStepper 클래스 내부는 이전과 거의 동일합니다] ...
        [Header("IK 타겟")]
        public Transform ikTarget;
        public Transform homeTransform;

        [Header("보행 설정")]
        public float stepDuration = 0.3f; // 스텝 속도는 이제 Frequency로 제어되므로, 이 값은 스텝 동작의 부드러움을 조절합니다.
        public float stepHeight = 0.4f;

        private bool isStepping = false;
        private Vector3 plantedPosition;
        private Vector3 stepStartPosition;
        private Vector3 stepTargetPosition;
        private float stepTimer;

        public void Init() { plantedPosition = ikTarget.position; }
        public bool IsStepping() => isStepping;

        public void StartStep(Vector3 targetPos)
        {
            if (isStepping) return; // 이미 걷는 중이면 다시 시작하지 않음

            isStepping = true;
            stepStartPosition = ikTarget.position;
            stepTargetPosition = targetPos;
            stepTimer = 0f;
        }

        public void UpdateStep()
        {
            if (!isStepping) return;
            stepTimer += Time.deltaTime / stepDuration;
            if (stepTimer >= 1f)
            {
                ikTarget.position = stepTargetPosition;
                plantedPosition = stepTargetPosition;
                isStepping = false;
                return;
            }
            Vector3 position = Vector3.Lerp(stepStartPosition, stepTargetPosition, stepTimer);
            position.y += Mathf.Sin(stepTimer * Mathf.PI) * stepHeight;
            ikTarget.position = position;
        }

        public void UpdateStanding()
        {
            ikTarget.position = Vector3.Lerp(ikTarget.position, homeTransform.position, Time.deltaTime * 5f);
            plantedPosition = ikTarget.position;
        }
    }

    private void Awake()
    {
        leftLeg.Init();
        rightLeg.Init();
    }

    // --- 외부 호출용 Public API ---

    public void UpdateWalking(Vector3 moveDirection, float moveSpeed)
    {
        HandleRhythmicWalking(moveDirection, moveSpeed);

        leftLeg.UpdateStep();
        rightLeg.UpdateStep();
    }

    public void UpdateStanding()
    {
        HandleStanding();

        leftLeg.UpdateStep();
        rightLeg.UpdateStep();
    }

    // --- 핵심 로직: 리듬 기반 걷기 ---

    private void HandleRhythmicWalking(Vector3 moveDirection, float moveSpeed)
    {
        // 1. 걷기 타이머를 흐르게 합니다.
        walkCycleTimer += Time.deltaTime * stepFrequency;

        // 2. 타이머가 한 사이클(1.0)을 넘으면, 다음 스텝을 내딛을 시간입니다.
        if (walkCycleTimer >= 1.0f)
        {
            walkCycleTimer -= 1.0f; // 타이머 리셋

            // 3. 어느 발 차례인지 결정합니다.
            LegStepper legToMove = isLeftLegTurn ? leftLeg : rightLeg;

            // 4. 다음 발 디딜 위치를 '예측'하여 계산합니다.
            // 기본 위치 + 이동방향 * 예측 계수
            Vector3 targetPosition = legToMove.homeTransform.position + (moveDirection * moveSpeed * stepPredictionFactor);

            // 5. 해당 발의 스텝을 시작합니다.
            legToMove.StartStep(targetPosition);

            // 6. 다음 차례는 반대쪽 발로 넘깁니다.
            isLeftLegTurn = !isLeftLegTurn;
        }
    }

    private void HandleStanding()
    {
        // 서 있을 때는 타이머와 순서를 초기화합니다.
        walkCycleTimer = 0f;
        isLeftLegTurn = true;

        leftLeg.UpdateStanding();
        rightLeg.UpdateStanding();
    }
}
