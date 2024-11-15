using UnityEngine;

public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueDataSO dialogueData;
    [SerializeField] private DialogueState dialogueCondition;
    private Outline outline;

    private void Awake()
    {
        outline = GetComponent<Outline>();
    }
    public void Interact()
    {
		DialogueManager.Instance.StartDialogue(dialogueData, dialogueCondition);
    }

    public void OnFocus()
    {
        outline.enabled = true;
    }
    public void OnDefocus()
    {
        outline.enabled = false;
    }

    public void HandleDialogueEvent(DialogueEventType eventType, string parameter)
    {
        switch (eventType)
        {
            case DialogueEventType.AcceptQuest:
                // 퀘스트 매니저에 퀘스트 추가
                Debug.Log($"Quest accepted: {parameter}");
                break;
                
            case DialogueEventType.GiveItem:
                // 인벤토리에 아이템 추가
                Debug.Log($"Gave item: {parameter}");
                break;
                
            case DialogueEventType.StartTrade:
                // 상점 UI 열기
                Debug.Log("Opening shop...");
                break;
        }
    }
}