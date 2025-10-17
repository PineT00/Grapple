using UnityEngine;

public class SimpleFollow : MonoBehaviour
{
    [Tooltip("따라갈 타겟 Transform")]
    public Transform target;

    [Header("추적 옵션")]
    [Tooltip("타겟의 위치를 따라갈지 여부")]
    public bool followPosition = true;

    [Header("회전 축 옵션")]
    public bool followRotX = false;
    public bool followRotY = true;
    public bool followRotZ = false;

    void FixedUpdate()
    {
        if (!target)
            return;

        // 위치 추적
        if (followPosition)
        {
            transform.position = target.position;
        }

        Vector3 currentEulerAngles = transform.eulerAngles;

        float newRotX = currentEulerAngles.x;
        float newRotY = currentEulerAngles.y;
        float newRotZ = currentEulerAngles.z;

        if (followRotX)
            newRotX = target.eulerAngles.x;

        if (followRotY)
        {
            Vector3 flatForward = target.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude > 0.0001f)
            {
                Quaternion yawRotation = Quaternion.LookRotation(flatForward, Vector3.up);
                newRotY = yawRotation.eulerAngles.y;

                if (Vector3.Dot(target.up, Vector3.up) <= 0f)
                {
                    newRotY = (newRotY + 180f) % 360f;
                }
            }
        }

        if (followRotZ)
            newRotZ = target.eulerAngles.z;

        Quaternion targetRotation = Quaternion.Euler(newRotX, newRotY, newRotZ);
        transform.rotation = targetRotation;
    }
}