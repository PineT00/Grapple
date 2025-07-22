using UnityEngine;

public class SimpleFollow : MonoBehaviour
{
    public Transform target;

    void FixedUpdate()
    {
        if (target != null)
        {
            transform.position = target.position;
        }
    }
}
