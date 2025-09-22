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
        {
            return;
        }

        if (followPosition)
        {
            transform.position = target.position;
        }

        Vector3 currentEulerAngles = transform.eulerAngles;
        Vector3 targetEulerAngles = target.eulerAngles;

        float newRotX = followRotX ? targetEulerAngles.x : currentEulerAngles.x;
        float newRotY = followRotY ? targetEulerAngles.y : currentEulerAngles.y;
        float newRotZ = followRotZ ? targetEulerAngles.z : currentEulerAngles.z;

        Quaternion targetRotation = Quaternion.Euler(newRotX, newRotY, newRotZ);
        transform.rotation = targetRotation;
    }
}