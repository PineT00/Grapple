using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RopeMeshGenerator : MonoBehaviour
{
    [Header("Rope Settings")]
    [Tooltip("로프 메시에 적용할 재질")]
    public Material material;
    [Tooltip("로프 단면의 원형 해상도 (값이 높을수록 부드러움)")]
    [Range(3, 16)] public int sides = 8;
    public float radius = 0.1f;

    [Header("Texture Settings")]
    public float textureTiling = 1f;

    [Header("Wobble Effect")]
    [Tooltip("출렁임 효과를 표현할 정점의 수")]
    public int quality = 200;
    public float damper = 15;
    public float strength = 800;
    public float initialVelocity = 20;
    public float waveCount = 3;
    public float waveHeight = 1;
    public AnimationCurve affectCurve;

    private Mesh mesh;
    private Spring spring;

    // 메시 생성 데이터 리스트
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();


    //테스트
    public float continuousWobbleSpeed = 5f;
    [Tooltip("지속적인 출렁임 효과의 강도")]
    public float continuousWobbleMagnitude = 0.5f;

    private bool isWobblingContinuously = false;

    void Awake()
    {
        // 메시 컴포넌트 초기화
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = material;

        // 스프링 초기화
        spring = gameObject.AddComponent<Spring>();
        spring.SetTarget(0);
        spring.SetDamper(damper);
        spring.SetStrength(strength);
    }

    public void ActivateRope(bool active)
    {
        if (active)
        {
            spring.Reset();
            spring.SetVelocity(initialVelocity);
        }
        else
        {
            spring.Reset();
            ClearMesh();
        }
    }

    public void UpdateRopeVisuals(Vector3 firePoint, List<BendPoint> bendPoints, Transform cameraTransform)
    {
        if (bendPoints == null || bendPoints.Count == 0)
        {
            ClearMesh();
            return;
        }

        if (isWobblingContinuously)
        {
            float wobbleTarget = Mathf.Sin(Time.time * continuousWobbleSpeed) * continuousWobbleMagnitude;
            spring.SetTarget(wobbleTarget);
        }

        spring.Calculate(Time.deltaTime);

        List<Vector3> ropePositions = new List<Vector3> { firePoint };
        ropePositions.AddRange(bendPoints.Select(p => p.position).Reverse());

        List<Vector3> resampledPositions = ResamplePath(ropePositions, quality);

        List<Vector3> finalRopePositions = CalculateWobble(resampledPositions, cameraTransform);

        GenerateMesh(finalRopePositions);
    }

    //--추가: 경로를 세분화하는 함수
    private List<Vector3> ResamplePath(List<Vector3> path, int numPoints)
    {
        if (path.Count < 2) return path;

        var resampled = new List<Vector3>();
        float totalLength = 0f;

        // 전체 경로 길이 계산
        for (int i = 0; i < path.Count - 1; i++)
        {
            totalLength += Vector3.Distance(path[i], path[i + 1]);
        }

        float segmentLength = totalLength / (numPoints - 1);
        float currentDist = 0f;
        int pathIndex = 0;

        resampled.Add(path[0]); // 시작점 추가

        for (int i = 1; i < numPoints - 1; i++)
        {
            float targetDist = i * segmentLength;

            while (currentDist + Vector3.Distance(path[pathIndex], path[pathIndex + 1]) < targetDist)
            {
                currentDist += Vector3.Distance(path[pathIndex], path[pathIndex + 1]);
                pathIndex++;
            }

            float ratio = (targetDist - currentDist) / Vector3.Distance(path[pathIndex], path[pathIndex + 1]);
            resampled.Add(Vector3.Lerp(path[pathIndex], path[pathIndex + 1], ratio));
        }

        resampled.Add(path.Last()); // 끝점 추가
        return resampled;
    }


    private List<Vector3> CalculateWobble(List<Vector3> basePositions, Transform cameraTransform)
    {
        // 안정적인 출렁임 방향 계산
        Vector3 stableUpVector = Vector3.Cross((basePositions.Last() - basePositions.First()).normalized, cameraTransform.right).normalized;

        float totalRopeLength = 0;
        for (int i = 0; i < basePositions.Count - 1; i++)
        {
            totalRopeLength += Vector3.Distance(basePositions[i], basePositions[i + 1]);
        }

        if (totalRopeLength <= 0f) return basePositions;

        List<Vector3> wobbledPositions = new List<Vector3>();
        float distanceCovered = 0;

        // 경로의 각 지점에 출렁임 오프셋
        for (int i = 0; i < basePositions.Count; i++)
        {
            if (i > 0)
            {
                distanceCovered += Vector3.Distance(basePositions[i - 1], basePositions[i]);
            }
            float delta = distanceCovered / totalRopeLength;

            var offset = stableUpVector * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);
            wobbledPositions.Add(basePositions[i] + offset);
        }
        return wobbledPositions;
    }

    private void GenerateMesh(List<Vector3> points)
    {
        mesh.Clear();
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        if (points.Count < 2) return;

        float distanceCovered = 0f;

        // 경로의 각 지점을 따라 순회하며 원형 단면(Ring)을 생성하고 연결
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 currentPoint = points[i];
            Vector3 direction = (i < points.Count - 1) ? (points[i + 1] - currentPoint).normalized : (currentPoint - points[i - 1]).normalized;

            Quaternion rotation = Quaternion.LookRotation(direction);

            if (i > 0)
            {
                distanceCovered += Vector3.Distance(points[i - 1], points[i]);
            }

            // 링 생성
            for (int j = 0; j < sides; j++)
            {
                float angle = (float)j / sides * 2 * Mathf.PI;
                Vector3 vertexOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                vertices.Add(currentPoint + rotation * vertexOffset);

                float u = (float)j / sides;
                float v = distanceCovered * textureTiling;
                uvs.Add(new Vector2(u, v));
            }

            // 이전 링과 현재 링을 삼각형으로 연결
            if (i > 0)
            {
                for (int j = 0; j < sides; j++)
                {
                    int baseIndex = (i - 1) * sides;
                    int currentIndex = i * sides;

                    int p1 = baseIndex + j;
                    int p2 = baseIndex + (j + 1) % sides;
                    int p3 = currentIndex + j;
                    int p4 = currentIndex + (j + 1) % sides;

                    // 첫 번째 삼각형
                    triangles.Add(p1);
                    triangles.Add(p2);
                    triangles.Add(p3);

                    // 두 번째 삼각형
                    triangles.Add(p2);
                    triangles.Add(p4);
                    triangles.Add(p3);
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals(); // 조명을 올바르게 받기 위해 법선 재계산
        mesh.RecalculateBounds();
    }

    private void ClearMesh()
    {
        if (mesh != null)
        {
            mesh.Clear();
        }
    }


    public void SetContinuousWobble(bool enabled)
    {
        isWobblingContinuously = enabled;
        if (!enabled)
        {
            // 출렁임이 멈추면 스프링이 0으로 자연스럽게 수렴하도록 타겟을 재설정합니다.
            spring.SetTarget(0);
        }
    }
}
