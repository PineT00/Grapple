using UnityEngine;

public class CopyMotion : MonoBehaviour
{
    public Transform targetLimb;
    public ConfigurableJoint joint;
    public bool isMirror = false;
    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
    }

    void Update()
    {
        if (!isMirror)
        {
            joint.targetRotation = targetLimb.rotation;
        }
        else
        {
            joint.targetRotation = Quaternion.Inverse(targetLimb.rotation);
        }
    }
}
