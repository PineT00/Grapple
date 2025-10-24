using UnityEngine;

public class EnergyConnector : MonoBehaviour
{
    public Material offMat;
    public Material onMat;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            InteractionUIManager.Instance.Show(transform);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            InteractionUIManager.Instance.Hide();
        }
    }
}
