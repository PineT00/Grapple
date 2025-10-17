using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일자형 무한 발판을 절차적으로 생성하고 관리하는 스크립트입니다.
/// Kinematic Rigidbody의 MovePosition을 사용하여 외부 충격에 영향을 받지 않으면서도
/// 위에 올라탄 다른 Rigidbody 객체(플레이어 등)를 함께 이동시킵니다.
/// </summary>
public class InfinitePlatformGenerator : MonoBehaviour
{
    [Header("프리팹 및 풀링 설정")]
    [Tooltip("발판으로 사용할 프리팹. Rigidbody가 없으면 자동으로 추가됩니다.")]
    public GameObject platformPrefab;
    [Tooltip("오브젝트 풀의 크기. 씬에 존재할 최대 발판 수.")]
    public int poolSize = 20;
    [Tooltip("플레이어의 Transform. 발판 생성 및 제거의 기준점.")]
    public Transform playerTransform;

    [Header("생성 설정")]
    [Tooltip("발판이 생성되는 시간 간격 (초)")]
    public float spawnInterval = 1.0f;
    [Tooltip("첫 발판이 생성될 위치 오프셋")]
    public Vector3 spawnOriginOffset = new Vector3(0, -5, 0);
    [Tooltip("발판이 생성될 좌우(X), 상하(Y) 범위")]
    public Vector2 spawnArea = new Vector2(10f, 5f);
    [Tooltip("발판 사이의 최소 Z축 간격")]
    public float minSpawnGap = 2f;
    [Tooltip("발판 사이의 최대 Z축 간격")]
    public float maxSpawnGap = 5f;

    [Header("발판 크기 설정")]
    [Tooltip("생성될 발판의 최소 크기 (X, Y, Z)")]
    public Vector3 minPlatformSize = new Vector3(5, 1, 10);
    [Tooltip("생성될 발판의 최대 크기 (X, Y, Z)")]
    public Vector3 maxPlatformSize = new Vector3(15, 1, 25);

    [Header("발판 이동 설정")]
    [Tooltip("발판이 플레이어를 향해 이동하는 방향")]
    public Vector3 moveDirection = Vector3.back;
    [Tooltip("발판의 이동 속도")]
    public float moveSpeed = 5f;
    [Tooltip("플레이어 뒤쪽으로 이 거리만큼 지나가면 발판이 비활성화됩니다.")]
    public float deactivateDistanceBehind = 20f;

    private Queue<Rigidbody> platformPool;
    private List<Rigidbody> activePlatforms;
    private Vector3 nextSpawnPosition;
    private float lastPlatformZSize;

    void Start()
    {
        if (platformPrefab == null || playerTransform == null)
        {
            Debug.LogError("Platform Prefab 또는 Player Transform이 설정되지 않았습니다!");
            this.enabled = false;
            return;
        }

        InitializePool();
        activePlatforms = new List<Rigidbody>();

        nextSpawnPosition = playerTransform.position + spawnOriginOffset;
        lastPlatformZSize = 0;

        // 시간 간격에 따라 발판을 생성하는 코루틴을 시작합니다.
        StartCoroutine(SpawnPlatformRoutine());
    }

    void FixedUpdate()
    {
        ManagePlatforms();
    }

    /// <summary>
    /// Kinematic Rigidbody를 사용하는 오브젝트 풀을 초기화합니다.
    /// </summary>
    void InitializePool()
    {
        platformPool = new Queue<Rigidbody>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject platformObj = Instantiate(platformPrefab, transform);
            Rigidbody rb = platformObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = platformObj.AddComponent<Rigidbody>();
                Debug.LogWarning($"'{platformPrefab.name}' 프리팹에 Rigidbody가 없어 자동으로 추가했습니다.");
            }
            
            rb.useGravity = false;
            rb.isKinematic = true; // Rigidbody를 항상 Kinematic으로 유지합니다.
            platformObj.SetActive(false);
            platformPool.Enqueue(rb);
        }
    }

    /// <summary>
    /// 지정된 시간 간격으로 발판을 계속 생성하는 코루틴입니다.
    /// </summary>
    private IEnumerator SpawnPlatformRoutine()
    {
        while (true)
        {
            // 풀에 재사용할 발판이 있을 경우에만 생성합니다.
            if (platformPool.Count > 0)
            {
                SpawnPlatform();
            }
            // 지정된 시간만큼 대기합니다.
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// 풀에서 발판을 꺼내 위치와 크기를 설정하고 활성화합니다.
    /// </summary>
    void SpawnPlatform()
    {
        if (platformPool.Count == 0) return;

        Rigidbody platformRb = platformPool.Dequeue();
        GameObject platform = platformRb.gameObject;

        float randomXSize = Random.Range(minPlatformSize.x, maxPlatformSize.x);
        float randomYSize = Random.Range(minPlatformSize.y, maxPlatformSize.y);
        float randomZSize = Random.Range(minPlatformSize.z, maxPlatformSize.z);
        platform.transform.localScale = new Vector3(randomXSize, randomYSize, randomZSize);

        float randomXPos = Random.Range(-spawnArea.x / 2, spawnArea.x / 2);
        float randomYPos = Random.Range(-spawnArea.y / 2, spawnArea.y / 2);
        float randomZGap = Random.Range(minSpawnGap, maxSpawnGap);

        nextSpawnPosition.z += (lastPlatformZSize / 2) + (randomZSize / 2) + randomZGap;
        platform.transform.position = new Vector3(nextSpawnPosition.x + randomXPos, nextSpawnPosition.y + randomYPos, nextSpawnPosition.z);

        lastPlatformZSize = randomZSize;

        platform.SetActive(true);
        activePlatforms.Add(platformRb);
    }

    /// <summary>
    /// 활성화된 발판들을 MovePosition으로 이동시키고 재활용 여부를 관리합니다.
    /// </summary>
    void ManagePlatforms()
    {
        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            Rigidbody platformRb = activePlatforms[i];

            Vector3 newPosition = platformRb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            platformRb.MovePosition(newPosition);

            // 재활용 로직에서 직접적인 스폰 호출을 제거합니다.
            if (platformRb.position.z < playerTransform.position.z - deactivateDistanceBehind)
            {
                ReturnToPool(platformRb);
            }
        }
    }

    /// <summary>
    /// 사용이 끝난 발판을 풀에 반환합니다.
    /// </summary>
    void ReturnToPool(Rigidbody platformRb)
    {
        platformRb.gameObject.SetActive(false);
        activePlatforms.Remove(platformRb);
        platformPool.Enqueue(platformRb);
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Vector3 spawnZoneCenter = new Vector3(playerTransform.position.x, playerTransform.position.y + spawnOriginOffset.y, nextSpawnPosition.z);
        Gizmos.DrawWireCube(spawnZoneCenter, new Vector3(spawnArea.x, spawnArea.y, 1));

        Gizmos.color = new Color(1, 0, 0, 0.8f);
        float deactivateZ = playerTransform.position.z - deactivateDistanceBehind;
        Vector3 deactivateLineStart = new Vector3(playerTransform.position.x - 100, playerTransform.position.y, deactivateZ);
        Vector3 deactivateLineEnd = new Vector3(playerTransform.position.x + 100, playerTransform.position.y, deactivateZ);
        Gizmos.DrawLine(deactivateLineStart, deactivateLineEnd);
    }
}
