using System.Collections.Generic;

[System.Serializable]
public class DialogueEndNode : DialogueNode
{
    public List<DialogueChoice> choices;
    public override DialogueNodeType NodeType => DialogueNodeType.End;
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public DialogueEventType eventType;
    public string eventParameter;
    public UnityEngine.Object eventObjectParameter;
}
