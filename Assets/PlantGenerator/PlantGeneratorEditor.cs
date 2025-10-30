using UnityEngine;
using UnityEditor;

/// <summary>
/// PlantGenerator 커스텀 에디터 - Inspector UI 개선
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(PlantGenerator))]
public class PlantGeneratorEditor : Editor
{
    private PlantGenerator generator;
    private bool showHelp = false;

    private void OnEnable()
    {
        generator = (PlantGenerator)target;
    }

    public override void OnInspectorGUI()
    {
        // 헤더
        EditorGUILayout.Space(10);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 16;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        EditorGUILayout.LabelField("🌿 담쟁이 식물 생성기", headerStyle);
        EditorGUILayout.Space(5);

        // 도움말 버튼
        showHelp = EditorGUILayout.Foldout(showHelp, "📖 사용 가이드", true);
        if (showHelp)
        {
            EditorGUILayout.HelpBox(
                "1. Root와 Branch 프리팹을 할당하세요\n" +
                "2. Surface Layer를 설정하세요\n" +
                "3. Trigger Collider를 추가하세요\n" +
                "4. 플레이 모드 또는 'Generate Plant' 버튼으로 생성",
                MessageType.Info
            );
        }

        EditorGUILayout.Space(10);

        // 필수 설정 체크
        CheckRequiredSettings();

        // 기본 Inspector 그리기
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // 생성 버튼
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🌱 Generate Plant", GUILayout.Height(40)))
        {
            if (ValidateSettings())
            {
                generator.RegeneratePlant();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        // 클리어 버튼
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🗑️ Clear Plant", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("식물 제거", 
                "생성된 식물을 모두 제거하시겠습니까?", "제거", "취소"))
            {
                ClearPlant();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // 통계 정보
        ShowStatistics();
    }

    private void CheckRequiredSettings()
    {
        bool hasErrors = false;

        if (generator.rootPrefab == null)
        {
            EditorGUILayout.HelpBox("⚠️ Root Prefab이 할당되지 않았습니다!", MessageType.Error);
            hasErrors = true;
        }

        if (generator.branchPrefab == null)
        {
            EditorGUILayout.HelpBox("⚠️ Branch Prefab이 할당되지 않았습니다!", MessageType.Error);
            hasErrors = true;
        }

        if (generator.surfaceLayer == 0)
        {
            EditorGUILayout.HelpBox("⚠️ Surface Layer가 설정되지 않았습니다!", MessageType.Warning);
            hasErrors = true;
        }

        Collider collider = generator.GetComponent<Collider>();
        if (collider == null)
        {
            EditorGUILayout.HelpBox("⚠️ Collider가 없습니다! Trigger Collider를 추가하세요.", MessageType.Warning);
            hasErrors = true;
        }
        else if (!collider.isTrigger)
        {
            EditorGUILayout.HelpBox("⚠️ Collider의 'Is Trigger'를 활성화하세요!", MessageType.Warning);
            hasErrors = true;
        }

        if (!hasErrors)
        {
            EditorGUILayout.HelpBox("✅ 모든 설정이 완료되었습니다!", MessageType.Info);
        }
    }

    private bool ValidateSettings()
    {
        if (generator.rootPrefab == null)
        {
            EditorUtility.DisplayDialog("오류", "Root Prefab을 할당하세요!", "확인");
            return false;
        }

        if (generator.branchPrefab == null)
        {
            EditorUtility.DisplayDialog("오류", "Branch Prefab을 할당하세요!", "확인");
            return false;
        }

        if (generator.surfaceLayer == 0)
        {
            EditorUtility.DisplayDialog("경고", "Surface Layer를 설정하는 것이 좋습니다.", "확인");
        }

        return true;
    }

    private void ShowStatistics()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📊 생성 예상치", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField($"총 가지 수: {generator.totalBranchCount}");
        EditorGUILayout.LabelField($"노드당 최대 가지: {generator.maxBranchesPerNode}");
        
        float estimatedTime = generator.totalBranchCount * 0.05f;
        EditorGUILayout.LabelField($"예상 생성 시간: ~{estimatedTime:F1}초");
        
        EditorGUILayout.EndVertical();
    }

    private void ClearPlant()
    {
        // Plant Root 찾아서 삭제
        Transform plantRoot = generator.transform.Find("Plant_Root");
        if (plantRoot != null)
        {
            DestroyImmediate(plantRoot.gameObject);
        }

        // Ivy로 시작하는 모든 자식 삭제
        for (int i = generator.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = generator.transform.GetChild(i);
            if (child.name.StartsWith("IvyPlant") || child.name.StartsWith("Plant"))
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    // Scene 뷰에 기즈모 그리기
    private void OnSceneGUI()
    {
        Collider collider = generator.GetComponent<Collider>();
        if (collider != null && collider.isTrigger)
        {
            Handles.color = new Color(0, 1, 0, 0.2f);
            
            if (collider is BoxCollider box)
            {
                Handles.matrix = generator.transform.localToWorldMatrix;
                Handles.DrawWireCube(box.center, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Handles.DrawWireDisc(generator.transform.position, Vector3.up, sphere.radius);
                Handles.DrawWireDisc(generator.transform.position, Vector3.right, sphere.radius);
                Handles.DrawWireDisc(generator.transform.position, Vector3.forward, sphere.radius);
            }
        }
    }
}

/// <summary>
/// AdvancedPlantGenerator 커스텀 에디터
/// </summary>
[CustomEditor(typeof(AdvancedPlantGenerator))]
public class AdvancedPlantGeneratorEditor : Editor
{
    private AdvancedPlantGenerator generator;
    private bool showPresets = false;

    private void OnEnable()
    {
        generator = (AdvancedPlantGenerator)target;
    }

    public override void OnInspectorGUI()
    {
        // 헤더
        EditorGUILayout.Space(10);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 16;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        EditorGUILayout.LabelField("🌿 고급 담쟁이 식물 생성기", headerStyle);
        EditorGUILayout.Space(5);

        // 프리셋 버튼
        showPresets = EditorGUILayout.Foldout(showPresets, "🎨 프리셋", true);
        if (showPresets)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("사실적 담쟁이"))
            {
                ApplyRealisticPreset();
            }
            if (GUILayout.Button("빽빽한 덤불"))
            {
                ApplyDensePreset();
            }
            if (GUILayout.Button("예술적 스타일"))
            {
                ApplyArtisticPreset();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);

        // 기본 Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // 생성 버튼
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🌱 Generate Plant", GUILayout.Height(40)))
        {
            generator.GeneratePlant();
        }
        GUI.backgroundColor = Color.white;
    }

    private void ApplyRealisticPreset()
    {
        Undo.RecordObject(generator, "Apply Realistic Preset");
        
        generator.totalBranchCount = 80;
        generator.maxBranchesPerNode = 2;
        generator.maxGeneration = 6;
        generator.branchLength = 0.6f;
        generator.branchLengthVariation = 0.15f;
        generator.maxAngleFromParent = 50f;
        generator.directionRandomness = 30f;
        generator.upwardBias = 0.3f;
        generator.minDistanceBetweenBranches = 0.3f;
        
        EditorUtility.SetDirty(generator);
    }

    private void ApplyDensePreset()
    {
        Undo.RecordObject(generator, "Apply Dense Preset");
        
        generator.totalBranchCount = 150;
        generator.maxBranchesPerNode = 3;
        generator.maxGeneration = 8;
        generator.branchLength = 0.4f;
        generator.branchLengthVariation = 0.1f;
        generator.maxAngleFromParent = 60f;
        generator.directionRandomness = 45f;
        generator.upwardBias = 0.2f;
        generator.minDistanceBetweenBranches = 0.15f;
        
        EditorUtility.SetDirty(generator);
    }

    private void ApplyArtisticPreset()
    {
        Undo.RecordObject(generator, "Apply Artistic Preset");
        
        generator.totalBranchCount = 40;
        generator.maxBranchesPerNode = 2;
        generator.maxGeneration = 5;
        generator.branchLength = 1.2f;
        generator.branchLengthVariation = 0.3f;
        generator.maxAngleFromParent = 70f;
        generator.directionRandomness = 60f;
        generator.upwardBias = 0.1f;
        generator.minDistanceBetweenBranches = 0.5f;
        
        EditorUtility.SetDirty(generator);
    }
}
#endif
