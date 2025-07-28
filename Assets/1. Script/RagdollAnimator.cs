using UnityEngine;

public class RagdollAnimator : MonoBehaviour
{

    [Header("좌/우 다리 조인트")]
    public ConfigurableJoint leftHipJoint;
    public ConfigurableJoint rightHipJoint;
    public Quaternion leftInitialRotation;
    public Quaternion rightInitialRotation;

    [Header("스텝 설정")]
    public float stepAngle = 30f; // 다리를 내미는 각도
    public float stepDuration = 0.3f;

    private float stepTimer = 0f;
    private bool isLeftStep = true;


    void Start()
    {

    }

    void FixedUpdate()
    {
        stepTimer += Time.fixedDeltaTime;
        if (stepTimer >= stepDuration)
        {
            stepTimer = 0f;
            DoStep();
            isLeftStep = !isLeftStep;
        }
    }
    
    
    void DoStep()
    {
        // 앞쪽으로 뻗는 회전값 (local 기준)
        Quaternion forwardRot = Quaternion.Euler(-stepAngle, 0f, 0f);
        Quaternion neutralRot = Quaternion.identity;

        if (isLeftStep)
        {
            leftHipJoint.targetRotation = Quaternion.Inverse(leftInitialRotation) * forwardRot;
            rightHipJoint.targetRotation = Quaternion.Inverse(rightInitialRotation) * neutralRot;
        }
        else
        {
            leftHipJoint.targetRotation = Quaternion.Inverse(leftInitialRotation) * neutralRot;
            rightHipJoint.targetRotation = Quaternion.Inverse(rightInitialRotation) * forwardRot;
        }
    }
}
