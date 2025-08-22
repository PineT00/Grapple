using UnityEngine;

public class TestWaking : MonoBehaviour
{
    public RagdollWalking ragdollWalking;
    public Rigidbody mainRb;
    public float speed = 10f;

    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        mainRb.AddForce(Vector3.forward * speed * Time.fixedDeltaTime, ForceMode.Acceleration);
        if (mainRb.linearVelocity.magnitude < 0.1f)
        {
            ragdollWalking.UpdateStanding();
        }
        else
        {
            ragdollWalking.UpdateWalking(mainRb.linearVelocity.normalized, mainRb.linearVelocity.magnitude);
        }
    }
}
