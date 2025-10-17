using UnityEngine;
using Yarn.Unity;
using System.Collections.Generic;

/// <summary>
/// 다이얼로그 시스템의 전반을 관리하는 싱글톤 매니저입니다.
/// 커스텀 커맨드 등록, 다이얼로그 시작 등의 API를 제공합니다.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Yarn Spinner Core")]
    [SerializeField] private DialogueRunner dialogueRunner;

    // 게임의 다른 시스템 (인벤토리, 퀘스트 등)에 대한 참조
    // public InventorySystem inventorySystem;
    // public QuestSystem questSystem;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
            if (dialogueRunner == null) Debug.LogError("Scene에 DialogueRunner가 없습니다!");
        }

        // Yarn 커스텀 커맨드를 등록합니다.
        RegisterCommands();
    }

    /// <summary>
    /// .yarn 파일에서 사용할 커스텀 커맨드를 정의하고 등록합니다.
    /// </summary>
    private void RegisterCommands()
    {
        dialogueRunner.AddCommandHandler<string, int>("give_item", GiveItem);
        dialogueRunner.AddCommandHandler<string, int>("start_quest", StartQuest);
    }

    /// <summary>
    /// 다이얼로그를 시작합니다.
    /// </summary>
    /// <param name="targetNode">.yarn 파일에 정의된 시작 노드 이름</param>
    public void StartDialogue(string targetNode)
    {
        if (dialogueRunner.IsDialogueRunning)
        {
            Debug.LogWarning("이미 다른 다이얼로그가 실행 중입니다.");
            return;
        }
        dialogueRunner.StartDialogue(targetNode);
    }

    // --- Custom Command Implementations ---

    private void GiveItem(string itemName, int amount)
    {
        Debug.Log($"아이템 획득: {itemName}, {amount}개");
        // 여기에 실제 인벤토리 시스템과 연동하는 코드를 작성합니다.
        // inventorySystem.AddItem(itemName, amount);
    }

    private void StartQuest(string questID, int requiredAmount)
    {
        Debug.Log($"퀘스트 시작: {questID}, 목표: {requiredAmount}");
        // 여기에 실제 퀘스트 시스템과 연동하는 코드를 작성합니다.
        // questSystem.StartNewQuest(questID, requiredAmount);
    }
}
