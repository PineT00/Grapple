using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 담쟁이 식물 생성기 - 표면을 따라 자라는 식물을 생성합니다
/// </summary>
public class PlantGenerator : MonoBehaviour
{
    [Header("프리팹 설정")]
    [Tooltip("뿌리 프리팹")]
    public GameObject rootPrefab;
    
    [Tooltip("가지 프리팹")]
    public GameObject branchPrefab;

    [Header("생성 설정")]
    [Tooltip("감지할 표면 레이어")]
    public LayerMask surfaceLayer;
    
    [Tooltip("생성할 총 가지 수")]
    [Range(1, 100)]
    public int totalBranchCount = 20;
    
    [Tooltip("한 노드에서 뻗을 최대 가지 수")]
    [Range(1, 5)]
    public int maxBranchesPerNode = 2;

    [Header("가지 설정")]
    [Tooltip("가지 길이")]
    [Range(0.1f, 5f)]
    public float branchLength = 1f;
    
    [Tooltip("가지 방향 랜덤성 (도)")]
    [Range(0f, 180f)]
    public float directionRandomness = 45f;
    
    [Tooltip("표면 감지 거리")]
    [Range(0.1f, 2f)]
    public float surfaceDetectionDistance = 0.5f;

    [Header("충돌 회피")]
    [Tooltip("가지 간 최소 거리")]
    [Range(0.1f, 2f)]
    public float minDistanceBetweenBranches = 0.3f;
    
    [Tooltip("충돌 검사 반경")]
    [Range(0.1f, 1f)]
    public float collisionCheckRadius = 0.2f;

    [Header("디버그")]
    public bool showDebugRays = true;

    // 내부 변수
    private List<BranchNode> allBranches = new List<BranchNode>();
    private int currentBranchCount = 0;
    private Transform plantRoot;

    private void Start()
    {
        GeneratePlant();
    }

    /// <summary>
    /// 식물 생성 시작
    /// </summary>
    public void GeneratePlant()
    {
        // 기존 식물 제거
        ClearPlant();

        // Root 생성 시도
        if (TryCreateRoot(out Vector3 rootPosition, out Vector3 rootNormal))
        {
            CreateRootNode(rootPosition, rootNormal);
            
            // 가지 생성 시작
            StartCoroutine(GrowPlant());
        }
        else
        {
            Debug.LogWarning("표면을 감지하지 못했습니다. Plant Generator의 트리거를 확인하세요.");
        }
    }

    /// <summary>
    /// Root 생성 위치와 법선 찾기 - 트리거 콜라이더 기반
    /// </summary>
    private bool TryCreateRoot(out Vector3 position, out Vector3 normal)
    {
        position = Vector3.zero;
        normal = Vector3.up;

        // 자신의 트리거 콜라이더 가져오기
        Collider triggerCollider = GetComponent<Collider>();
        
        if (triggerCollider == null || !triggerCollider.isTrigger)
        {
            Debug.LogError("PlantGenerator에 Trigger Collider가 필요합니다!");
            return false;
        }

        // 트리거 범위 내의 모든 콜라이더 찾기
        Collider[] overlappingColliders = Physics.OverlapBox(
            triggerCollider.bounds.center,
            triggerCollider.bounds.extents,
            transform.rotation,
            surfaceLayer
        );

        if (overlappingColliders.Length == 0)
        {
            Debug.LogWarning("트리거 범위 내에 표면이 감지되지 않았습니다. Surface Layer를 확인하세요.");
            return false;
        }

        // 가장 가까운 표면 찾기
        float closestDistance = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;
        Collider closestCollider = null;

        foreach (Collider col in overlappingColliders)
        {
            // 트리거는 무시
            if (col.isTrigger) continue;

            // 가장 가까운 점 계산
            Vector3 closest = col.ClosestPoint(transform.position);
            float distance = Vector3.Distance(transform.position, closest);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = closest;
                closestCollider = col;
            }
        }

        if (closestCollider == null)
        {
            Debug.LogWarning("유효한 표면 콜라이더를 찾지 못했습니다.");
            return false;
        }

        // 정확한 표면 위치와 노멀을 얻기 위해 레이캐스트
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = (closestPoint - transform.position).normalized;
        float rayDistance = closestDistance + 1f; // 여유있게

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, rayDistance, surfaceLayer))
        {
            position = hit.point;
            normal = hit.normal;

            if (showDebugRays)
            {
                Debug.DrawLine(rayOrigin, hit.point, Color.cyan, 2f);
                Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.green, 2f);
            }

            return true;
        }
        else
        {
            // 레이캐스트가 실패한 경우, ClosestPoint 결과를 사용하고 노멀을 추정
            position = closestPoint;
            
            // 콜라이더 중심에서 표면으로의 방향으로 노멀 추정
            normal = (closestPoint - closestCollider.bounds.center).normalized;
            
            // 더 정확한 노멀을 위해 주변에서 레이캐스트 시도
            Vector3[] searchOffsets = new Vector3[]
            {
                Vector3.up * 0.1f,
                Vector3.down * 0.1f,
                Vector3.left * 0.1f,
                Vector3.right * 0.1f,
                Vector3.forward * 0.1f,
                Vector3.back * 0.1f
            };

            foreach (Vector3 offset in searchOffsets)
            {
                Vector3 searchOrigin = closestPoint + offset;
                Vector3 searchDir = -offset.normalized;
                
                if (Physics.Raycast(searchOrigin, searchDir, out RaycastHit searchHit, 0.2f, surfaceLayer))
                {
                    position = searchHit.point;
                    normal = searchHit.normal;
                    
                    if (showDebugRays)
                    {
                        Debug.DrawRay(searchHit.point, searchHit.normal * 0.5f, Color.yellow, 2f);
                    }
                    
                    return true;
                }
            }

            if (showDebugRays)
            {
                Debug.DrawRay(position, normal * 0.5f, Color.red, 2f);
            }

            Debug.LogWarning("정확한 노멀을 찾지 못했습니다. 추정된 노멀을 사용합니다.");
            return true;
        }
    }

    /// <summary>
    /// Root 노드 생성
    /// </summary>
    private void CreateRootNode(Vector3 position, Vector3 normal)
    {
        if (rootPrefab == null)
        {
            Debug.LogError("Root Prefab이 할당되지 않았습니다!");
            return;
        }

        // Root 오브젝트 생성
        plantRoot = new GameObject("Plant_Root").transform;
        plantRoot.position = position;
        
        // Root 프리팹 인스턴스 생성
        GameObject rootInstance = Instantiate(rootPrefab, position, Quaternion.identity, plantRoot);
        
        // 표면 법선에 맞춰 회전 (로컬 Y축이 법선 방향)
        rootInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        
        // 초기 가지 노드들 생성
        for (int i = 0; i < maxBranchesPerNode && currentBranchCount < totalBranchCount; i++)
        {
            CreateInitialBranch(position, normal, rootInstance.transform);
        }
    }

    /// <summary>
    /// Root에서 초기 가지 생성
    /// </summary>
    private void CreateInitialBranch(Vector3 rootPosition, Vector3 rootNormal, Transform parent)
    {
        // XZ 평면에서 랜덤 방향 (표면을 따라)
        Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, rootNormal);
        float randomAngle = Random.Range(0f, 360f);
        Vector3 randomDir = surfaceRotation * Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
        
        // 표면을 따라 이동할 방향
        Vector3 tangentDir = Vector3.ProjectOnPlane(randomDir, rootNormal).normalized;
        
        BranchNode branch = new BranchNode
        {
            position = rootPosition,
            normal = rootNormal,
            direction = tangentDir,
            generation = 0
        };

        allBranches.Add(branch);
        currentBranchCount++;
    }

    /// <summary>
    /// 식물 성장 코루틴
    /// </summary>
    private IEnumerator GrowPlant()
    {
        Queue<BranchNode> branchQueue = new Queue<BranchNode>();
        
        // 초기 가지들을 큐에 추가
        foreach (var branch in allBranches)
        {
            branchQueue.Enqueue(branch);
        }

        while (branchQueue.Count > 0 && currentBranchCount < totalBranchCount)
        {
            BranchNode currentBranch = branchQueue.Dequeue();
            
            // 다음 가지 위치 계산
            if (TryGrowBranch(currentBranch, out BranchNode newBranch))
            {
                // 가지 생성 성공
                allBranches.Add(newBranch);
                currentBranchCount++;

                // 다음 세대 가지 생성 가능 여부 확인
                int childBranchCount = Random.Range(1, maxBranchesPerNode + 1);
                
                for (int i = 0; i < childBranchCount && currentBranchCount < totalBranchCount; i++)
                {
                    branchQueue.Enqueue(newBranch);
                }

                yield return new WaitForSeconds(0.05f); // 시각적 효과를 위한 딜레이
            }
        }

        Debug.Log($"식물 생성 완료! 총 {currentBranchCount}개의 가지가 생성되었습니다.");
    }

    /// <summary>
    /// 가지 성장 시도
    /// </summary>
    private bool TryGrowBranch(BranchNode parentBranch, out BranchNode newBranch)
    {
        newBranch = null;

        // 1단계: 다음 가지 위치를 먼저 계산
        Vector3 growthDirection = CalculateGrowthDirection(parentBranch);
        Vector3 targetPosition = parentBranch.position + growthDirection * branchLength;

        // 2단계: 표면 추적으로 실제 다음 위치 확정
        if (TraceSurface(parentBranch.position, targetPosition, parentBranch.normal,
            out Vector3 nextPosition, out Vector3 nextNormal))
        {
            // 3단계: 충돌 체크
            if (!CheckCollisionWithOtherBranches(nextPosition))
            {
                // 4단계: 현재 위치에서 다음 위치로 향하는 방향 계산
                Vector3 currentToNextDir = (nextPosition - parentBranch.position).normalized;
                Vector3 branchForward = Vector3.ProjectOnPlane(currentToNextDir, parentBranch.normal).normalized;

                if (branchForward.magnitude < 0.1f)
                {
                    branchForward = Vector3.ProjectOnPlane(Vector3.forward, parentBranch.normal).normalized;
                }

                // 5단계: 현재 위치에 가지 프리팹 생성 (다음 위치를 향하도록)
                GameObject branchInstance = Instantiate(branchPrefab, parentBranch.position, Quaternion.identity, plantRoot);
                branchInstance.transform.rotation = Quaternion.LookRotation(branchForward, parentBranch.normal);

                // 6단계: 다음 노드 정보 생성 (다음 위치에 대한 정보)
                Vector3 nextDirection = Vector3.ProjectOnPlane(currentToNextDir, nextNormal).normalized;
                if (nextDirection.magnitude < 0.1f)
                {
                    nextDirection = Vector3.ProjectOnPlane(Vector3.forward, nextNormal).normalized;
                }

                newBranch = new BranchNode
                {
                    position = nextPosition,
                    normal = nextNormal,
                    direction = nextDirection,
                    generation = parentBranch.generation + 1,
                    gameObject = branchInstance
                };

                if (showDebugRays)
                {
                    Debug.DrawLine(parentBranch.position, nextPosition, Color.cyan, 10f);
                    Debug.DrawRay(parentBranch.position, branchForward * 0.5f, Color.green, 10f);
                    Debug.DrawRay(nextPosition, nextNormal * 0.3f, Color.yellow, 10f);
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 성장 방향 계산
    /// </summary>
    private Vector3 CalculateGrowthDirection(BranchNode branch)
    {
        // 표면을 따라 이동하는 방향 (접선 방향)
        Vector3 tangent = branch.direction;
        
        // 랜덤 편차 추가
        Quaternion randomRotation = Quaternion.AngleAxis(
            Random.Range(-directionRandomness, directionRandomness), 
            branch.normal
        );
        
        Vector3 newDirection = randomRotation * tangent;
        return Vector3.ProjectOnPlane(newDirection, branch.normal).normalized;
    }

    /// <summary>
    /// 표면 추적
    /// </summary>
    private bool TraceSurface(Vector3 startPos, Vector3 targetPos, Vector3 currentNormal, 
        out Vector3 finalPosition, out Vector3 finalNormal)
    {
        finalPosition = targetPos;
        finalNormal = currentNormal;

        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        // 전진 방향으로 레이캐스트
        if (Physics.Raycast(startPos + currentNormal * 0.1f, direction, out RaycastHit forwardHit, 
            distance + surfaceDetectionDistance, surfaceLayer))
        {
            // 새로운 표면 발견
            finalPosition = forwardHit.point;
            finalNormal = forwardHit.normal;
            return true;
        }

        // 현재 표면을 유지하면서 이동
        Vector3 checkPosition = targetPos + currentNormal * surfaceDetectionDistance;
        
        if (Physics.Raycast(checkPosition, -currentNormal, out RaycastHit downHit, 
            surfaceDetectionDistance * 2f, surfaceLayer))
        {
            finalPosition = downHit.point;
            finalNormal = downHit.normal;
            return true;
        }

        // 표면을 찾지 못한 경우 주변 탐색
        Vector3[] searchDirections = new Vector3[]
        {
            -currentNormal,
            Quaternion.AngleAxis(45, Vector3.Cross(currentNormal, direction)) * -currentNormal,
            Quaternion.AngleAxis(-45, Vector3.Cross(currentNormal, direction)) * -currentNormal
        };

        foreach (Vector3 searchDir in searchDirections)
        {
            if (Physics.Raycast(targetPos, searchDir, out RaycastHit searchHit, 
                surfaceDetectionDistance * 2f, surfaceLayer))
            {
                finalPosition = searchHit.point;
                finalNormal = searchHit.normal;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 다른 가지들과의 충돌 체크
    /// </summary>
    private bool CheckCollisionWithOtherBranches(Vector3 position)
    {
        foreach (var branch in allBranches)
        {
            if (Vector3.Distance(position, branch.position) < minDistanceBetweenBranches)
            {
                return true; // 충돌 발생
            }
        }
        return false;
    }

    /// <summary>
    /// 기존 식물 제거
    /// </summary>
    private void ClearPlant()
    {
        if (plantRoot != null)
        {
            Destroy(plantRoot.gameObject);
        }
        
        allBranches.Clear();
        currentBranchCount = 0;
    }

    /// <summary>
    /// 가지 노드 클래스
    /// </summary>
    private class BranchNode
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 direction;
        public int generation;
        public GameObject gameObject;
    }

    // 에디터에서 재생성
    [ContextMenu("Regenerate Plant")]
    public void RegeneratePlant()
    {
        GeneratePlant();
    }
}
