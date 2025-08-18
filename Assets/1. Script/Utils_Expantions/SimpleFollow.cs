using UnityEngine;

public class SimpleFollow : MonoBehaviour
{
    public bool followRotX = false;
    public bool followRotY = false;
    public bool followRotZ = false;

    public Transform target;

    private Quaternion rot = Quaternion.identity;

    void FixedUpdate()
    {
        if (!target) return;

        transform.position = target.position;

        // 현재 회전의 오일러, 타깃 오일러
        Vector3 cur = transform.eulerAngles;
        Vector3 tgt = target.eulerAngles;

        if (followRotX) cur.x = tgt.x;
        if (followRotY) cur.y = tgt.y;
        if (followRotZ) cur.z = tgt.z;

        transform.rotation = Quaternion.Euler(cur);
    }
}
