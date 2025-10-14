using UnityEngine;

public class TriggerForwarder : MonoBehaviour
{
    [HideInInspector]
    public CollectMission collectMission;

    void OnTriggerEnter(Collider other)
    {
        if (collectMission != null)
        {
            collectMission.OnChildTriggerEnter(other);
        }
    }
}
