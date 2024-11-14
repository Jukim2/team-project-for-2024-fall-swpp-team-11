using System.Collections.Generic;

[System.Serializable]
public class DialogueEndNode : DialogueNode
{
    public List<DialogueChoice> choices;
    public override DialogueNodeType NodeType => DialogueNodeType.End;
}