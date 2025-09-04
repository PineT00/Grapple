using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrapplingRope : MonoBehaviour
{
    [Header("Rope Visuals")]
    public int quality = 200; // 로프의 부드러움 정도
    public float damper = 15;
    public float strength = 800;
    public float initialVelocity = 20; // 로프 발사 시 출렁임의 초기 속도
    public float waveCount = 3;
    public float waveHeight = 1;
    public AnimationCurve affectCurve; // 로프의 어느 부분에 출렁임 효과를 줄지 결정하는 커브

    private Spring spring;
    private LineRenderer lineRenderer;

    void Awake()
    {
        spring = gameObject.AddComponent<Spring>();
        spring.SetTarget(0);
        spring.SetDamper(damper);
        spring.SetStrength(strength);
    }

    public void SetLineRenderer(LineRenderer lineRenderer)
    {
        this.lineRenderer = lineRenderer;
    }

    public void ActivateRope(bool active)
    {
        if (active)
        {
            spring.Reset();
            spring.SetVelocity(initialVelocity); // 활성화 시 출렁임 효과 시작
        }
        else
        {
            spring.Reset();
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
        }
    }

    public void UpdateRopeVisuals(Vector3 firePoint, List<Vector3> bendPoints)
    {
        if (lineRenderer == null || bendPoints == null || bendPoints.Count == 0)
        {
            if (lineRenderer != null) lineRenderer.positionCount = 0;
            return;
        }

        spring.Calculate(Time.deltaTime);
        lineRenderer.positionCount = bendPoints.Count + quality;

        // 고정된 구간(앵커 ~ 마지막 꺾임 지점) 직선
        for (int i = 0; i < bendPoints.Count; i++)
        {
            lineRenderer.SetPosition(i, bendPoints[bendPoints.Count - 1 - i]);
        }

        // 3. 마지막 구간(마지막 꺾임 지점 ~ 플레이어) 출렁임
        Vector3 lastPoint = bendPoints.Last();
        Vector3 ropeDirection = (firePoint - lastPoint).normalized;
        Vector3 up = Quaternion.LookRotation(ropeDirection) * Vector3.right;

        for (var i = 0; i < quality + 1; i++)
        {
            var delta = i / (float)quality;
            var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);

            Vector3 position = Vector3.Lerp(lastPoint, firePoint, delta);

            lineRenderer.SetPosition(bendPoints.Count - 1 + i, position + offset);
        }
    }
}
