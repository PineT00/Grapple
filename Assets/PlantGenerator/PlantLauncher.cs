using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlantLauncher : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject seedPrefab;
    public GameObject plantGeneratorPrefab;

    [Header("Launch Settings")]
    public Transform launchPos;
    public float launchForce = 20f;
    public int seedPoolSize = 5;
    public int generatorPoolSize = 10;

    [Header("Seed Settings")]
    public float seedLifetime = 10f; // 착탄 없을 경우 자동 회수 시간

    private Camera cam;
    private Queue<SeedProjectile> seedPool;
    private Queue<GameObject> generatorPool;
    private List<SeedProjectile> activeSeeds;

    void Start()
    {
        cam = Camera.main;
        InitializePools();
    }

    void InitializePools()
    {
        // Seed Pool 초기화
        seedPool = new Queue<SeedProjectile>();
        activeSeeds = new List<SeedProjectile>();

        for (int i = 0; i < seedPoolSize; i++)
        {
            GameObject seedObj = Instantiate(seedPrefab, transform);
            SeedProjectile seed = seedObj.GetComponent<SeedProjectile>();

            if (seed == null)
            {
                seed = seedObj.AddComponent<SeedProjectile>();
            }

            seed.Initialize(this);
            seedObj.SetActive(false);
            seedPool.Enqueue(seed);
        }

        // Generator Pool 초기화
        generatorPool = new Queue<GameObject>();

        for (int i = 0; i < generatorPoolSize; i++)
        {
            GameObject generator = Instantiate(plantGeneratorPrefab, transform);
            generator.SetActive(false);
            generatorPool.Enqueue(generator);
        }
    }

    public void OnLaunch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            LaunchSeed();
        }
    }

    void LaunchSeed()
    {
        if (seedPool.Count == 0)
        {
            Debug.LogWarning("씨앗 풀이 비어있습니다. 풀 크기를 늘려주세요.");
            return;
        }

        SeedProjectile seed = seedPool.Dequeue();
        activeSeeds.Add(seed);

        // 씨앗 위치 및 상태 초기화
        Transform seedTransform = seed.transform;
        seedTransform.position = launchPos.position;
        seedTransform.rotation = launchPos.rotation;

        Rigidbody rb = seed.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        seed.gameObject.SetActive(true);

        // 발사
        Vector3 force = cam.transform.forward * launchForce;
        rb.AddForce(force, ForceMode.Impulse);

        // 자동 회수 타이머 시작
        seed.StartLifetimeTimer(seedLifetime);
    }

    public void OnSeedLanded(SeedProjectile seed, Vector3 landingPos)
    {
        // Generator 활성화
        if (generatorPool.Count > 0)
        {
            GameObject generator = generatorPool.Dequeue();
            generator.transform.position = landingPos;
            generator.SetActive(true);

            // Generator가 자동으로 비활성화되면 풀로 반환하도록 설정
            // PlantGeneratorController genController = generator.GetComponent<PlantGeneratorController>();
            // if (genController != null)
            // {
            //     genController.SetReturnCallback(() => ReturnGenerator(generator));
            // }
        }

        // Seed 풀로 반환
        ReturnSeed(seed);
    }

    public void ReturnSeed(SeedProjectile seed)
    {
        if (activeSeeds.Contains(seed))
        {
            activeSeeds.Remove(seed);
        }

        seed.gameObject.SetActive(false);
        seedPool.Enqueue(seed);
    }

    public void ReturnGenerator(GameObject generator)
    {
        generator.SetActive(false);
        generatorPool.Enqueue(generator);
    }

    void OnDestroy()
    {
        // 활성화된 모든 씨앗 정리
        foreach (var seed in activeSeeds)
        {
            if (seed != null)
            {
                seed.StopAllCoroutines();
            }
        }
    }
}
