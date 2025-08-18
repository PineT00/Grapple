using UnityEngine;

public class RandomDistributer : MonoBehaviour
{
    public GameObject[] objectsToSpawn;
    public Vector3 areaSize = new Vector3(10f, 10f, 10f);
    public float setUp_Y_Pos = 0f;
    public int spawnCount = 0;
    public float minScale = 1f;
    public float maxScale = 2f;
    public bool randRotation = false;
    void Start()
    {
        SpawnObjectsByCount();
    }

    void SpawnObjectsByCount()
    {
        if (objectsToSpawn == null || objectsToSpawn.Length == 0)
        {
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject randomPrefab = objectsToSpawn[Random.Range(0, objectsToSpawn.Length)];

            float randomX = transform.position.x + Random.Range(-areaSize.x / 2, areaSize.x / 2);
            float randomY = transform.position.y + Random.Range(-areaSize.y / 2, areaSize.y / 2);
            float randomZ = transform.position.z + Random.Range(-areaSize.z / 2, areaSize.z / 2);

            Vector3 spawnPosition = new Vector3(randomX, setUp_Y_Pos + randomY, randomZ);

            float randomScale = Random.Range(minScale, maxScale);
            Vector3 newScale = randomPrefab.transform.localScale * randomScale;
            Quaternion objRotation = randomPrefab.transform.rotation;

            if (randRotation)
                objRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            GameObject spawnedObject = Instantiate(randomPrefab, spawnPosition, objRotation);
            spawnedObject.transform.localScale = newScale;
            spawnedObject.transform.SetParent(transform);

            if(!spawnedObject.activeSelf)
            {
                spawnedObject.SetActive(true);
            }
        }
    }

}
