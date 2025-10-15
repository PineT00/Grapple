using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [Tooltip("이 NPC와 상호작용 시 시작할 Yarn 노드의 이름")]
    public string startNode = "Start";

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            Debug.Log($"{gameObject.name} 접촉. {startNode} 대화 시작 시도.");
            DialogueManager.Instance.StartDialogue(startNode);
        }
    }
}
