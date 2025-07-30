using UnityEngine;

public class GrapplingRope : MonoBehaviour
{

    public GrappleController grapplingController;
    public Transform firePoint;
    public int quality;
    public float damper;
    public float strength;
    public float velocity;
    public float waveCount;
    public float waveHeight;
    public AnimationCurve affectCurve;
    private Spring spring;
    private LineRenderer lineRenderer;
    private Vector3 currentGrapplePosition;
    private Vector3 grapplePoint;


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

    public void SetRope(bool active)
    {
        if (active)
        {
            spring.SetVelocity(velocity);
            lineRenderer.positionCount = quality + 1;
            grapplePoint = grapplingController.GetGrapplePoint();
            currentGrapplePosition = firePoint.position;
        }
        else
        {
            spring.Reset();
            lineRenderer.positionCount = 0;
        }

    }

    public void BendRope()
    {
        grapplePoint = grapplingController.GetGrapplePoint();
    }

    public void DrawRope()
    {
        var up = Quaternion.LookRotation((grapplePoint - firePoint.position).normalized) * Vector3.right;
        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, grapplePoint, Time.deltaTime * 12f);
        spring.Calculate(Time.deltaTime);

        for (var i = 0; i < quality + 1; i++)
        {
            var delta = i / (float)quality;
            var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value *
                         affectCurve.Evaluate(delta);

            lineRenderer.SetPosition(i, Vector3.Lerp(firePoint.position, currentGrapplePosition, delta) + offset);
        }
    }
}
