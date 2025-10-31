using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 고급 담쟁이 식물 생성기 - 더 정교한 표면 추적과 가지 분기
/// </summary>
public class AdvancedPlantGenerator : MonoBehaviour
{
    [Header("프리팹 설정")]
    public GameObject rootPrefab;
    public GameObject branchPrefab;

    [Header("생성 설정")]
    public LayerMask surfaceLayer;
    [Range(1, 200)]
    public int totalBranchCount = 50;

    [Header("분화 설정")]
    [Tooltip("뿌리에서 생성되는 초기 가지 개수")]
    [Range(1, 5)]
    public int initialRootBranches = 2;
    [Tooltip("한 노드 끝당 최대 분화 개수")]
    [Range(1, 5)]
    public int maxBranchesPerNode = 2;
    [Tooltip("노드가 분화할 확률 (0~1)")]
    [Range(0f, 1f)]
    public float branchProbability = 0.7f;
    [Range(1, 20)]
    public int maxGeneration = 5;

    [Header("가지 크기")]
    [Range(0.1f, 3f)]
    public float branchLength = 0.8f;
    [Range(0.1f, 1.0f)]
    public float branchLengthVariation = 0.2f;

    [Header("방향 설정")]
    [Tooltip("부모 가지와의 최소 각도")]
    [Range(0f, 90f)]
    public float minAngleFromParent = 10f;
    [Tooltip("부모 가지와의 최대 각도")]
    [Range(0f, 90f)]
    public float maxAngleFromParent = 60f;
    [Range(0f, 180f)]
    public float directionRandomness = 30f;
    [Range(0f, 1f)]
    public float upwardBias = 0.3f; // 위로 자라는 경향

    [Header("표면 추적")]
    [Range(0.05f, 1f)]
    public float surfaceOffset = 0.05f; // 표면으로부터의 거리
    [Range(0.1f, 2f)]
    public float surfaceDetectionRange = 0.5f;
    [Range(3, 10)]
    public int surfaceCheckSteps = 5; // 표면 추적 정밀도

    [Header("충돌 회피")]
    [Range(0.1f, 2f)]
    public float minDistanceBetweenBranches = 0.4f;
    [Range(0.05f, 0.5f)]
    public float collisionCheckRadius = 0.15f;
    public bool avoidSelfIntersection = true;

    [Header("생성 제어")]
    [Range(0f, 0.2f)]
    public float generationDelay = 0.02f;
    public bool generateOnStart = true;
    public bool animateGrowth = true;

    [Header("디버그")]
    public bool showDebugGizmos = true;
    public bool showSurfaceNormals = true;
    public bool showGrowthDirections = true;

    // 내부 데이터
    private Transform plantRoot;
    private List<PlantNode> allNodes = new List<PlantNode>();
    private Dictionary<int, List<PlantNode>> generationMap = new Dictionary<int, List<PlantNode>>();
    private int currentBranchCount = 0;
    private bool isGrowing = false;

    private void OnEnable()
    {
        if (generateOnStart)
        {
            GeneratePlant();
        }
    }

    [ContextMenu("Generate Plant")]
    public void GeneratePlant()
    {
        if (isGrowing)
        {
            Debug.LogWarning("식물이 이미 생성 중입니다.");
            return;
        }

        StartCoroutine(GeneratePlantCoroutine());
    }

    private IEnumerator GeneratePlantCoroutine()
    {
        isGrowing = true;
        ClearPlant();

        // Root 생성
        if (!TryFindSurface(out Vector3 rootPos, out Vector3 rootNormal))
        {
            Debug.LogError("표면을 찾을 수 없습니다!");
            isGrowing = false;
            yield break;
        }

        CreateRoot(rootPos, rootNormal);

        // 세대별로 처리 (0세대는 이미 뿌리에서 생성했으므로 1세대부터 시작)
        for (int gen = 0; gen < maxGeneration && currentBranchCount < totalBranchCount; gen++)
        {
            if (!generationMap.ContainsKey(gen))
                break;

            // 0세대는 분화 로직 건너뛰고 자식만 생성
            if (gen == 0)
            {
                List<PlantNode> rootNodes = generationMap[0];
                foreach (PlantNode node in rootNodes)
                {
                    if (currentBranchCount >= totalBranchCount)
                        break;

                    if (node.generation + 1 >= maxGeneration)
                        continue;

                    // 0세대는 각각 1개씩만 자식 생성 (분화 없음)
                    if (TryCreateBranch(node, out PlantNode newNode))
                    {
                        allNodes.Add(newNode);
                        currentBranchCount++;
                        node.currentBranchCount++;

                        if (!generationMap.ContainsKey(newNode.generation))
                        {
                            generationMap[newNode.generation] = new List<PlantNode>();
                        }
                        generationMap[newNode.generation].Add(newNode);

                        if (animateGrowth && generationDelay > 0)
                        {
                            yield return new WaitForSeconds(generationDelay);
                        }
                    }
                }
                continue; // 0세대는 여기서 끝
            }

            // 이 세대의 모든 노드가 최대치 도달할 때까지 반복
            while (currentBranchCount < totalBranchCount)
            {
                bool anyBranchCreated = false;
                List<PlantNode> nodesInGeneration = generationMap[gen];

                // 1단계: 확률적으로 모든 노드 순회하며 서브노드 생성 시도
                foreach (PlantNode node in nodesInGeneration)
                {
                    if (currentBranchCount >= totalBranchCount)
                        break;

                    // 자식 세대가 최대를 초과하면 스킵
                    if (node.generation + 1 >= maxGeneration)
                        continue;

                    // 최대 분화 수 도달하면 스킵
                    if (node.currentBranchCount >= maxBranchesPerNode)
                        continue;

                    // 확률적으로 서브노드 생성
                    if (Random.value <= branchProbability)
                    {
                        if (TryCreateBranch(node, out PlantNode newNode))
                        {
                            allNodes.Add(newNode);
                            currentBranchCount++;
                            node.currentBranchCount++;

                            if (!generationMap.ContainsKey(newNode.generation))
                            {
                                generationMap[newNode.generation] = new List<PlantNode>();
                            }
                            generationMap[newNode.generation].Add(newNode);

                            anyBranchCreated = true;

                            if (animateGrowth && generationDelay > 0)
                            {
                                yield return new WaitForSeconds(generationDelay);
                            }
                        }
                    }
                }

                // 2단계: 확률에 안 걸렸으면 강제로 1개 생성
                if (!anyBranchCreated)
                {
                    PlantNode targetNode = null;

                    // 최대치 안 찬 노드 찾기
                    foreach (PlantNode node in nodesInGeneration)
                    {
                        if (node.generation + 1 < maxGeneration &&
                            node.currentBranchCount < maxBranchesPerNode)
                        {
                            targetNode = node;
                            break;
                        }
                    }

                    if (targetNode != null && currentBranchCount < totalBranchCount)
                    {
                        if (TryCreateBranch(targetNode, out PlantNode newNode))
                        {
                            allNodes.Add(newNode);
                            currentBranchCount++;
                            targetNode.currentBranchCount++;

                            if (!generationMap.ContainsKey(newNode.generation))
                            {
                                generationMap[newNode.generation] = new List<PlantNode>();
                            }
                            generationMap[newNode.generation].Add(newNode);

                            anyBranchCreated = true;

                            if (animateGrowth && generationDelay > 0)
                            {
                                yield return new WaitForSeconds(generationDelay);
                            }
                        }
                    }
                }

                // 3단계: 그래도 생성 못했으면 이 세대는 끝
                if (!anyBranchCreated)
                {
                    break;
                }
            }
        }

        Debug.Log($"<color=green>식물 생성 완료!</color> 총 {currentBranchCount}개 가지, {generationMap.Count}세대");
        isGrowing = false;
    }

    private bool TryFindSurface(out Vector3 position, out Vector3 normal)
    {
        position = Vector3.zero;
        normal = Vector3.up;

        // 자신의 트리거 콜라이더 가져오기
        Collider trigger = GetComponent<Collider>();

        if (trigger == null || !trigger.isTrigger)
        {
            Debug.LogError("PlantGenerator에 Trigger Collider가 필요합니다!");
            return false;
        }

        // 트리거 범위 내의 모든 콜라이더 찾기
        Collider[] colliders = Physics.OverlapBox(
            trigger.bounds.center,
            trigger.bounds.extents,
            transform.rotation,
            surfaceLayer
        );

        if (colliders.Length == 0)
        {
            Debug.LogWarning("트리거 범위 내에 표면이 감지되지 않았습니다.");
            return false;
        }

        // 가장 가까운 표면 찾기
        float closestDistance = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;
        Collider closestCollider = null;

        foreach (Collider col in colliders)
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
        float rayDistance = closestDistance + 1f;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, rayDistance, surfaceLayer))
        {
            position = hit.point;
            normal = hit.normal;

            if (showDebugGizmos)
            {
                Debug.DrawLine(rayOrigin, hit.point, Color.cyan, 2f);
                Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.green, 2f);
            }

            return true;
        }
        else
        {
            // 레이캐스트 실패 시 ClosestPoint 사용 및 노멀 추정
            position = closestPoint;
            normal = (closestPoint - closestCollider.bounds.center).normalized;

            // 더 정확한 노멀을 위한 주변 탐색
            Vector3[] searchOffsets = new Vector3[]
            {
                Vector3.up * 0.1f, Vector3.down * 0.1f,
                Vector3.left * 0.1f, Vector3.right * 0.1f,
                Vector3.forward * 0.1f, Vector3.back * 0.1f
            };

            foreach (Vector3 offset in searchOffsets)
            {
                Vector3 searchOrigin = closestPoint + offset;
                Vector3 searchDir = -offset.normalized;

                if (Physics.Raycast(searchOrigin, searchDir, out RaycastHit searchHit, 0.2f, surfaceLayer))
                {
                    position = searchHit.point;
                    normal = searchHit.normal;

                    if (showDebugGizmos)
                    {
                        Debug.DrawRay(searchHit.point, searchHit.normal * 0.5f, Color.yellow, 2f);
                    }

                    return true;
                }
            }

            if (showDebugGizmos)
            {
                Debug.DrawRay(position, normal * 0.5f, Color.red, 2f);
            }

            return true;
        }
    }

    private void CreateRoot(Vector3 position, Vector3 normal)
    {
        // Root 컨테이너
        plantRoot = new GameObject($"IvyPlant_{System.DateTime.Now.Ticks}").transform;
        plantRoot.position = position;

        // Root 프리팹 인스턴스
        if (rootPrefab != null)
        {
            GameObject rootObj = Instantiate(rootPrefab, position, Quaternion.identity, plantRoot);
            rootObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        }

        // 초기 가지 노드 생성 (여러 방향)
        int numInitialBranches = Mathf.Min(initialRootBranches, totalBranchCount);
        float angleStep = 360f / numInitialBranches;

        // 0세대 generationMap 초기화
        generationMap[0] = new List<PlantNode>();

        for (int i = 0; i < numInitialBranches; i++)
        {
            float angle = angleStep * i + Random.Range(-angleStep * 0.3f, angleStep * 0.3f);
            Vector3 direction = CalculateSurfaceTangent(normal, angle);

            PlantNode node = new PlantNode
            {
                position = position + normal * surfaceOffset,
                surfaceNormal = normal,
                growthDirection = direction,
                generation = 0,
                parentNode = null
            };

            allNodes.Add(node);
            generationMap[0].Add(node);
            currentBranchCount++;
        }
    }

    private bool TryCreateBranch(PlantNode parent, out PlantNode newNode)
    {
        newNode = null;

        // 1단계: 다음 가지 위치를 먼저 계산
        Vector3 growthDir = CalculateGrowthDirection(parent);
        float length = branchLength + Random.Range(-branchLengthVariation, branchLengthVariation);

        // 2단계: 표면 추적하며 다음 위치 확정
        if (TraceSurfacePath(parent.position, growthDir, length, parent.surfaceNormal,
            out Vector3 nextPos, out Vector3 nextNormal, out Vector3 nextDir))
        {
            // 3단계: 충돌 체크
            if (avoidSelfIntersection && IsPositionOccupied(nextPos))
            {
                return false;
            }

            // 4단계: 현재 위치에서 다음 위치로 향하는 방향 계산
            Vector3 currentToNextDir = (nextPos - parent.position).normalized;
            Vector3 branchForward = Vector3.ProjectOnPlane(currentToNextDir, parent.surfaceNormal).normalized;

            if (branchForward.magnitude < 0.1f)
            {
                branchForward = Vector3.ProjectOnPlane(Vector3.forward, parent.surfaceNormal).normalized;
            }

            // 5단계: 현재 위치에 가지 프리팹 생성 (다음 위치를 향하도록)
            GameObject branchObj = null;
            if (branchPrefab != null)
            {
                branchObj = Instantiate(branchPrefab, parent.position, Quaternion.identity, plantRoot);

                // 회전 설정: Y축=법선, Z축=다음 위치 방향
                Quaternion rotation = Quaternion.LookRotation(branchForward, parent.surfaceNormal);
                branchObj.transform.rotation = rotation;
            }

            // 6단계: 다음 노드 정보 생성 (다음 위치에 대한 정보)
            newNode = new PlantNode
            {
                position = nextPos,
                surfaceNormal = nextNormal,
                growthDirection = nextDir,
                generation = parent.generation + 1,
                parentNode = parent,
                gameObject = branchObj
            };

            return true;
        }

        return false;
    }

    private Vector3 CalculateGrowthDirection(PlantNode parent)
    {
        // 부모 방향을 기준으로
        Vector3 baseDir = parent.growthDirection;

        // 표면 접선 방향으로 제한
        baseDir = Vector3.ProjectOnPlane(baseDir, parent.surfaceNormal).normalized;

        // 랜덤 회전 추가
        Quaternion randomRot = Quaternion.AngleAxis(
            Random.Range(-directionRandomness, directionRandomness),
            parent.surfaceNormal
        );
        Vector3 newDir = randomRot * baseDir;

        // 위쪽 편향 추가
        if (upwardBias > 0)
        {
            Vector3 upDirection = Vector3.up;
            newDir = Vector3.Lerp(newDir, upDirection, upwardBias * Random.value);
            newDir = Vector3.ProjectOnPlane(newDir, parent.surfaceNormal).normalized;
        }

        // 부모와의 각도 제한 (최소/최대)
        float angle = Vector3.Angle(parent.growthDirection, newDir);
        Vector3 axis = Vector3.Cross(parent.growthDirection, newDir);

        // 최대 각도 초과 시 제한
        if (angle > maxAngleFromParent)
        {
            if (axis.magnitude > 0.01f)
            {
                newDir = Quaternion.AngleAxis(maxAngleFromParent, axis) * parent.growthDirection;
            }
        }
        // 최소 각도 미달 시 강제로 벌림
        else if (angle < minAngleFromParent)
        {
            if (axis.magnitude > 0.01f)
            {
                newDir = Quaternion.AngleAxis(minAngleFromParent, axis) * parent.growthDirection;
            }
            else
            {
                // 방향이 거의 같으면 임의의 방향으로 최소 각도만큼 벌림
                Vector3 perpendicular = Vector3.Cross(parent.growthDirection, parent.surfaceNormal);
                if (perpendicular.magnitude < 0.01f)
                {
                    perpendicular = Vector3.Cross(parent.growthDirection, Vector3.up);
                }
                newDir = Quaternion.AngleAxis(minAngleFromParent, perpendicular.normalized) * parent.growthDirection;
            }
        }

        return newDir.normalized;
    }

    private bool TraceSurfacePath(Vector3 startPos, Vector3 direction, float distance,
        Vector3 currentNormal, out Vector3 finalPosition, out Vector3 finalNormal,
        out Vector3 finalDirection)
    {
        finalPosition = startPos;
        finalNormal = currentNormal;
        finalDirection = direction;

        Vector3 currentPos = startPos;
        Vector3 currentDir = direction;
        Vector3 normal = currentNormal;

        float stepSize = distance / surfaceCheckSteps;

        for (int i = 0; i < surfaceCheckSteps; i++)
        {
            // 다음 위치 계산
            Vector3 nextPos = currentPos + currentDir * stepSize;

            bool foundSurface = false;

            // 1단계: 전방 레이캐스트 (다른 오브젝트 표면 감지)
            if (Physics.Raycast(currentPos + normal * surfaceOffset, currentDir, out RaycastHit forwardHit,
                stepSize + surfaceDetectionRange, surfaceLayer))
            {
                currentPos = forwardHit.point + forwardHit.normal * surfaceOffset;
                normal = forwardHit.normal;
                currentDir = Vector3.ProjectOnPlane(currentDir, normal).normalized;
                foundSurface = true;
            }
            // 2단계: 아래 방향 표면 탐지 (현재 표면 유지)
            else
            {
                Vector3 searchStart = nextPos + normal * surfaceDetectionRange;

                if (Physics.Raycast(searchStart, -normal, out RaycastHit downHit,
                    surfaceDetectionRange * 2f, surfaceLayer))
                {
                    currentPos = downHit.point + downHit.normal * surfaceOffset;
                    normal = downHit.normal;
                    currentDir = Vector3.ProjectOnPlane(currentDir, normal).normalized;
                    foundSurface = true;
                }
            }

            // 3단계: 표면을 못 찾았으면 주변 탐색
            if (!foundSurface)
            {
                Vector3[] searchDirs = {
                    -normal,
                    currentDir,
                    Quaternion.AngleAxis(45, normal) * currentDir,
                    Quaternion.AngleAxis(-45, normal) * currentDir,
                    Quaternion.AngleAxis(45, currentDir) * -normal,
                    Quaternion.AngleAxis(-45, currentDir) * -normal
                };

                foreach (Vector3 searchDir in searchDirs)
                {
                    if (Physics.Raycast(nextPos, searchDir, out RaycastHit searchHit,
                        surfaceDetectionRange * 1.5f, surfaceLayer))
                    {
                        currentPos = searchHit.point + searchHit.normal * surfaceOffset;
                        normal = searchHit.normal;
                        currentDir = Vector3.ProjectOnPlane(currentDir, normal).normalized;
                        foundSurface = true;
                        break;
                    }
                }

                if (!foundSurface)
                {
                    return false; // 표면을 완전히 잃음
                }
            }
        }

        finalPosition = currentPos;
        finalNormal = normal;
        finalDirection = currentDir;
        return true;
    }

    private bool IsPositionOccupied(Vector3 position)
    {
        foreach (var node in allNodes)
        {
            if (Vector3.Distance(position, node.position) < minDistanceBetweenBranches)
            {
                return true;
            }
        }
        return false;
    }

    private Vector3 CalculateSurfaceTangent(Vector3 normal, float angle)
    {
        // 법선에 수직인 접선 벡터 생성
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.magnitude < 0.1f)
        {
            tangent = Vector3.Cross(normal, Vector3.forward);
        }
        tangent.Normalize();

        // 각도만큼 회전
        Quaternion rotation = Quaternion.AngleAxis(angle, normal);
        return rotation * tangent;
    }

    private int CalculateChildCount(PlantNode parent)
    {
        // 세대가 높을수록 가지 개수 감소
        float generationFactor = 1f - ((float)parent.generation / maxGeneration);
        int maxChildren = Mathf.Max(1, Mathf.RoundToInt(maxBranchesPerNode * generationFactor));

        return Random.Range(1, maxChildren + 1);
    }

    private void ClearPlant()
    {
        if (plantRoot != null)
        {
            DestroyImmediate(plantRoot.gameObject);
        }

        allNodes.Clear();
        generationMap.Clear();
        currentBranchCount = 0;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || allNodes == null) return;

        // 노드 표시
        foreach (var node in allNodes)
        {
            // 세대별 색상
            Color color = Color.Lerp(Color.green, Color.red, (float)node.generation / maxGeneration);
            Gizmos.color = color;
            Gizmos.DrawWireSphere(node.position, collisionCheckRadius);

            // 표면 법선
            if (showSurfaceNormals)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(node.position, node.surfaceNormal * 0.2f);
            }

            // 성장 방향
            if (showGrowthDirections)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(node.position, node.growthDirection * 0.3f);
            }

            // 부모 연결선
            if (node.parentNode != null)
            {
                Gizmos.color = color * 0.5f;
                Gizmos.DrawLine(node.parentNode.position, node.position);
            }
        }
    }

    [System.Serializable]
    private class PlantNode
    {
        public Vector3 position;
        public Vector3 surfaceNormal;
        public Vector3 growthDirection;
        public int generation;
        public PlantNode parentNode;
        public GameObject gameObject;
        public int currentBranchCount = 0; // 현재까지 생성한 자식 가지 수
    }
}
