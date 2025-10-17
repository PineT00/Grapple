using UnityEngine;

public class SimpleTurn : MonoBehaviour
{
    public float turnSpeed = 10f;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up * turnSpeed * Time.deltaTime);
    }
}
