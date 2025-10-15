using UnityEngine;

public class AutoMove : MonoBehaviour
{
    public float speed = 20f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position += Vector3.forward * Time.fixedDeltaTime * speed;
    }
}
