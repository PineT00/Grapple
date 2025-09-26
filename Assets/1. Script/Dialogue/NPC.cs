using UnityEngine;

/// <summary>
/// NPC 오브젝트에 부착하여 다이얼로그를 트리거하는 간단한 스크립트입니다.
/// </summary>
public class NPC : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [Tooltip("이 NPC와 상호작용 시 시작할 Yarn 노드의 이름")]
    public string startNode = "Start";

    // 에디터에서 상호작용을 쉽게 테스트하기 위한 예시 함수입니다.
    // 실제 게임에서는 플레이어의 입력 시스템이나 상호작용 매니저를 통해 호출하는 것이 좋습니다.
    private void OnMouseDown()
    {
        Debug.Log($"{gameObject.name} 클릭됨. {startNode} 대화 시작 시도.");
        // 다이얼로그 매니저를 통해 대화를 시작합니다.
        DialogueManager.Instance.StartDialogue(startNode);
    }
}
