using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ParticlePoolItem
{
    public string particleName;
    public GameObject prefab;
    public int initialPoolSize = 5;
}

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [SerializeField]
    private List<ParticlePoolItem> particlePoolItems;
    private Dictionary<string, Queue<ParticleSystem>> particlePools;

    // 원본 프리팹 빠르게 찾기
    private Dictionary<string, GameObject> particlePrefabs;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        particlePools = new Dictionary<string, Queue<ParticleSystem>>();
        particlePrefabs = new Dictionary<string, GameObject>();

        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var item in particlePoolItems)
        {
            Queue<ParticleSystem> newPool = new Queue<ParticleSystem>();
            particlePrefabs.Add(item.particleName, item.prefab);

            for (int i = 0; i < item.initialPoolSize; i++)
            {
                GameObject newParticleObj = Instantiate(item.prefab, transform);
                newParticleObj.SetActive(false);

                var returnToPool = newParticleObj.GetComponent<ReturnToPool>();
                if (returnToPool != null)
                {
                    returnToPool.poolName = item.particleName;
                }

                ParticleSystem ps = newParticleObj.GetComponent<ParticleSystem>();
                newPool.Enqueue(ps);
            }
            particlePools.Add(item.particleName, newPool);
        }
    }

    public ParticleSystem Play(string name, Vector3 position, Quaternion rotation)
    {
        if (!particlePools.ContainsKey(name))
        {
            Debug.LogWarning($"Particle Pool for '{name}' does not exist.");
            return null;
        }

        ParticleSystem particleToPlay;

        // 풀에 사용 가능한 파티클이 있으면 가져오고, 없으면 새로 생성
        if (particlePools[name].Count > 0)
        {
            particleToPlay = particlePools[name].Dequeue();
        }
        else
        {
            GameObject newParticleObj = Instantiate(particlePrefabs[name], transform);
            var returnToPool = newParticleObj.GetComponent<ReturnToPool>();
            if (returnToPool != null)
            {
                returnToPool.poolName = name;
            }
            particleToPlay = newParticleObj.GetComponent<ParticleSystem>();
        }

        particleToPlay.transform.position = position;
        particleToPlay.transform.rotation = rotation;
        particleToPlay.gameObject.SetActive(true);
        particleToPlay.Play();

        return particleToPlay;
    }

    // 길게 재생되는 파티클을 꺼야할때
    public void ReturnToPool(string name, ParticleSystem particle)
    {
        if (!particlePools.ContainsKey(name))
        {
            Debug.LogWarning($"Particle Pool for '{name}' does not exist.");
            Destroy(particle.gameObject);
            return;
        }

        Debug.Log("리턴작동");
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // 중지 및 잔여 파티클 제거
        particle.gameObject.SetActive(false);
        particlePools[name].Enqueue(particle);
    }
}