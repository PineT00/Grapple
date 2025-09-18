using UnityEngine;

public class SimpleFollow : MonoBehaviour
{
    public bool followRotX = false;
    public bool followRotY = false;
    public bool followRotZ = false;

    public Transform target;

    void FixedUpdate()
    {
        if (!target) return;

        transform.position = target.position;
        Vector3 targetForward = target.forward;
        targetForward.y = 0;

        if (targetForward.sqrMagnitude > 0.001f)
        {
            Quaternion newRotation = Quaternion.LookRotation(targetForward);

            transform.rotation = newRotation;
        }
    }
}