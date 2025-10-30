using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 프리팹 생성 헬퍼 - 에디터에서 빠르게 프리팹 생성
/// </summary>
public class PlantPrefabCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("GameObject/Plant Generator/Create Root Prefab", false, 10)]
    static void CreateRootPrefab()
    {
        // Root 프리팹 생성
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "IvyRoot";
        root.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
        
        // 재질 설정
        Material rootMat = new Material(Shader.Find("Standard"));
        rootMat.color = new Color(0.3f, 0.2f, 0.1f); // 갈색
        root.GetComponent<Renderer>().material = rootMat;

        // 프리팹으로 저장
        string path = "Assets/Prefabs/IvyRoot.prefab";
        EnsureDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, path);
        
        DestroyImmediate(root);
        
        Debug.Log($"<color=green>Root 프리팹이 생성되었습니다: {path}</color>");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    }

    [MenuItem("GameObject/Plant Generator/Create Branch Prefab", false, 11)]
    static void CreateBranchPrefab()
    {
        // Branch 프리팹 생성 (더 정교한 버전)
        GameObject branch = new GameObject("IvyBranch");
        
        // 메인 가지
        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "Stem";
        stem.transform.SetParent(branch.transform);
        stem.transform.localPosition = new Vector3(0, 0.4f, 0);
        stem.transform.localRotation = Quaternion.Euler(90, 0, 0);
        stem.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);
        
        // 재질 설정
        Material stemMat = new Material(Shader.Find("Standard"));
        stemMat.color = new Color(0.2f, 0.5f, 0.2f); // 녹색
        stem.GetComponent<Renderer>().material = stemMat;
        
        // 잎 추가 (옵션)
        for (int i = 0; i < 3; i++)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Quad);
            leaf.name = $"Leaf_{i}";
            leaf.transform.SetParent(branch.transform);
            
            float t = (float)i / 2f;
            leaf.transform.localPosition = new Vector3(
                Random.Range(-0.1f, 0.1f),
                0.2f + t * 0.4f,
                Random.Range(-0.1f, 0.1f)
            );
            leaf.transform.localRotation = Quaternion.Euler(
                Random.Range(-30f, 30f),
                Random.Range(0f, 360f),
                Random.Range(-30f, 30f)
            );
            leaf.transform.localScale = new Vector3(0.15f, 0.2f, 1f);
            
            // 잎 재질
            Material leafMat = new Material(Shader.Find("Standard"));
            leafMat.color = new Color(0.3f, 0.6f, 0.3f);
            leaf.GetComponent<Renderer>().material = leafMat;
            
            // Collider 제거 (잎은 충돌 불필요)
            DestroyImmediate(leaf.GetComponent<Collider>());
        }
        
        // 피벗 조정 (가지의 시작점이 원점에 오도록)
        branch.transform.position = Vector3.zero;
        
        // 프리팹으로 저장
        string path = "Assets/Prefabs/IvyBranch.prefab";
        EnsureDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(branch, path);
        
        DestroyImmediate(branch);
        
        Debug.Log($"<color=green>Branch 프리팹이 생성되었습니다: {path}</color>");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    }

    [MenuItem("GameObject/Plant Generator/Create Test Scene", false, 12)]
    static void CreateTestScene()
    {
        // 테스트용 벽 생성
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "TestWall";
        wall.transform.position = new Vector3(0, 2.5f, 0);
        wall.transform.localScale = new Vector3(10, 5, 0.5f);
        
        // 레이어 설정
        int layer = CreateOrGetLayer("PlantSurface");
        wall.layer = layer;
        
        // 재질
        Material wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = new Color(0.7f, 0.7f, 0.7f);
        wall.GetComponent<Renderer>().material = wallMat;
        
        // Plant Generator 생성
        GameObject generator = new GameObject("PlantGenerator");
        generator.transform.position = new Vector3(0, 0.5f, 0.8f);
        
        // 스크립트 추가 시도
        if (System.Type.GetType("PlantGenerator") != null)
        {
            generator.AddComponent(System.Type.GetType("PlantGenerator"));
        }
        else if (System.Type.GetType("AdvancedPlantGenerator") != null)
        {
            generator.AddComponent(System.Type.GetType("AdvancedPlantGenerator"));
        }
        
        // Trigger Collider 추가
        BoxCollider trigger = generator.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(2, 2, 2);
        
        // 카메라 설정
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 2.5f, 8);
            mainCam.transform.LookAt(wall.transform.position);
        }
        
        Debug.Log("<color=green>테스트 씬이 생성되었습니다!</color>");
        Debug.Log("1. PlantGenerator에 Root와 Branch 프리팹을 할당하세요.");
        Debug.Log("2. Surface Layer를 'PlantSurface'로 설정하세요.");
        Debug.Log("3. Generate Plant 버튼을 클릭하세요!");
        
        Selection.activeGameObject = generator;
    }

    [MenuItem("GameObject/Plant Generator/Create Advanced Branch Prefab", false, 13)]
    static void CreateAdvancedBranchPrefab()
    {
        // 더 복잡한 가지 프리팹
        GameObject branch = new GameObject("IvyBranch_Advanced");
        
        // 곡선 가지를 위한 여러 세그먼트
        int segments = 3;
        for (int i = 0; i < segments; i++)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            segment.name = $"Segment_{i}";
            segment.transform.SetParent(branch.transform);
            
            float t = (float)i / segments;
            float height = 0.3f;
            
            // 약간의 곡선 추가
            segment.transform.localPosition = new Vector3(
                Mathf.Sin(t * Mathf.PI * 0.5f) * 0.05f,
                t * height * segments,
                0
            );
            segment.transform.localRotation = Quaternion.Euler(90 + t * 15f, 0, 0);
            segment.transform.localScale = new Vector3(
                0.04f * (1 - t * 0.3f), // 끝으로 갈수록 가늘어짐
                height,
                0.04f * (1 - t * 0.3f)
            );
            
            // 재질
            Material segmentMat = new Material(Shader.Find("Standard"));
            segmentMat.color = Color.Lerp(
                new Color(0.2f, 0.4f, 0.2f),
                new Color(0.3f, 0.6f, 0.3f),
                t
            );
            segment.GetComponent<Renderer>().material = segmentMat;
        }
        
        // 여러 크기의 잎 추가
        int leafCount = 5;
        for (int i = 0; i < leafCount; i++)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Quad);
            leaf.name = $"Leaf_{i}";
            leaf.transform.SetParent(branch.transform);
            
            float t = (float)i / (leafCount - 1);
            float height = 0.3f * segments;
            
            leaf.transform.localPosition = new Vector3(
                Random.Range(-0.15f, 0.15f),
                t * height,
                Random.Range(-0.15f, 0.15f)
            );
            
            leaf.transform.localRotation = Quaternion.Euler(
                Random.Range(-45f, 45f),
                Random.Range(0f, 360f),
                Random.Range(-30f, 30f)
            );
            
            float leafSize = Random.Range(0.12f, 0.2f);
            leaf.transform.localScale = new Vector3(leafSize, leafSize * 1.3f, 1f);
            
            // 잎 재질 (그라데이션)
            Material leafMat = new Material(Shader.Find("Standard"));
            leafMat.color = new Color(
                Random.Range(0.25f, 0.35f),
                Random.Range(0.55f, 0.65f),
                Random.Range(0.25f, 0.35f)
            );
            leafMat.SetFloat("_Smoothness", 0.2f);
            leaf.GetComponent<Renderer>().material = leafMat;
            
            DestroyImmediate(leaf.GetComponent<Collider>());
        }
        
        // 프리팹 저장
        string path = "Assets/Prefabs/IvyBranch_Advanced.prefab";
        EnsureDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(branch, path);
        
        DestroyImmediate(branch);
        
        Debug.Log($"<color=green>고급 Branch 프리팹이 생성되었습니다: {path}</color>");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parentFolder = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(path);
            
            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                EnsureDirectory(parentFolder);
            }
            
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }

    private static int CreateOrGetLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
        );
        SerializedProperty layers = tagManager.FindProperty("layers");
        
        // 기존 레이어 찾기
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (layer.stringValue == layerName)
            {
                return i;
            }
        }
        
        // 빈 슬롯에 새 레이어 추가
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"<color=green>레이어 '{layerName}'가 추가되었습니다.</color>");
                return i;
            }
        }
        
        Debug.LogWarning($"레이어를 추가할 빈 슬롯이 없습니다.");
        return 0;
    }
#endif
}
