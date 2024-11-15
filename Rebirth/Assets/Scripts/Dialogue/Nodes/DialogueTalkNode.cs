using System.Collections.Generic;

[System.Serializable]
public class ConditionStringPair
{
    public Condition condition;
    public string nodeID;

    public ConditionStringPair(Condition cond, string id)
    {
        condition = cond;
        nodeID = id;
    }
}

[System.Serializable]
public class DialogueTalkNode : DialogueNode
{
    public List<ConditionStringPair> nextNodeIDs;
    public string defaultNextNodeID;
    public override DialogueNodeType NodeType => DialogueNodeType.Talk;

    public string GetNextNodeID()
    {
        if (nextNodeIDs == null || nextNodeIDs.Count == 0)
            return defaultNextNodeID;
        foreach (var entry in nextNodeIDs)
        {
            if (entry.condition.IsConditionMet())
            {
                return entry.nodeID;
            }
        }
        return defaultNextNodeID;
    }
}

