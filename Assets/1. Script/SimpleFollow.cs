using UnityEngine;

public class SimpleFollow : MonoBehaviour
{
    public Transform target;

    // Update is called once per frame
    void FixedUpdate()
    {
        if (target != null)
        {
            transform.position = target.position;
        }
    }
}
