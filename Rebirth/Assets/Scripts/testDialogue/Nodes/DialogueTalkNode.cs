[System.Serializable]
public class DialogueTalkNode : DialogueNode
{
    public string[] nextNodeIDs;
    public override DialogueNodeType NodeType => DialogueNodeType.Talk;

    public void InitializeNextNodeIDs(int conditionCount)
    {
        nextNodeIDs = new string[conditionCount];
    }
}


