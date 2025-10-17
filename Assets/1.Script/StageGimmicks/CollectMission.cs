using TMPro;
using UnityEngine;

public class CollectMission : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI collectCountText;

    [Header("Settings")]
    public Collider[] triggerColiders;
    public string targetTag = "Gold";

    private int collectedCount = 0;

    void Start()
    {
        UpdateUI();
        RegisterChildTriggers();
    }

    void RegisterChildTriggers()
    {
        foreach (var col in triggerColiders)
        {
            if (col.isTrigger)
            {
                var forwarder = col.gameObject.GetComponent<TriggerForwarder>();
                if (forwarder == null)
                {
                    forwarder = col.gameObject.AddComponent<TriggerForwarder>();
                }
                forwarder.collectMission = this;
            }
        }
    }

    public void OnChildTriggerEnter(Collider other)
    {
        HandleTrigger(other);
    }

    void HandleTrigger(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            collectedCount++;
            UpdateUI();
            Destroy(other.gameObject);
        }
    }

    void UpdateUI()
    {
        if (collectCountText != null)
        {
            collectCountText.text = collectedCount.ToString();
        }
    }
}
