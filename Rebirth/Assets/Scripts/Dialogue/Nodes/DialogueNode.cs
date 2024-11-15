using UnityEngine;

public enum DialogueNodeType
{
    Talk,
    End
}

public enum DialogueEventType
{
    None,
    AcceptQuest,
    RejectQuest,
    GiveItem,
    StartTrade,
    EndConversation
}

public enum DialogueState
{
    None,
    Normal,
    HasNotMet,
    HasMet,
    QuestIncomplete,
    QuestComplete,
    MAX
}

// 대화 노드 기본 클래스
[System.Serializable]
public class DialogueNode : ScriptableObject
{
    public string dialogueID;
    public string dialogueText;
    virtual public DialogueNodeType NodeType { get; }
}

