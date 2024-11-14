using System.Collections.Generic;
using UnityEngine;

public enum DialogueNodeType
{
    Talk,
    End
}
// 이벤트 타입 열거형
public enum DialogueEventType
{
    None,
    AcceptQuest,
    RejectQuest,
    GiveItem,
    StartTrade,
    EndConversation
}

public enum DialogueCondition
{
    None,
    Normal,
    FirstMeeting,
    Acquaintance,
    FindingItem,
    FoundItem,
    QuestIncomplete,
    QuestComplete,
    MAX
}

public enum ItemDialogueCondition
{
    item1,
    item2,
    item3,
    MAX
}

// 대화 노드 기본 클래스
[System.Serializable]
public class DialogueNode : ScriptableObject
{
    public DialogueCondition dialogueCondition;
    public string dialogueID;
    public string dialogueText;
    virtual public DialogueNodeType NodeType { get; }
}

