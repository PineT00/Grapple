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

        Vector3 tgt = target.eulerAngles;

        float finalX = followRotX ? tgt.x : 0;
        float finalY = followRotY ? tgt.y : 0;
        float finalZ = followRotZ ? tgt.z : 0;

        if (target.up.y < 0)
        {
            if (followRotY) // Y축을 따라갈 때만 보정
            {
                finalY += 180f;
            }
        }

        transform.rotation = Quaternion.Euler(finalX, finalY, finalZ);
    }
}