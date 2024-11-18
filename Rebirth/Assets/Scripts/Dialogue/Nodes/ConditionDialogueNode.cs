using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Plain Next Dialogue Node", menuName = "Dialogue/Condition Dialogue Node")]
public class ConditionDialogueNode : DialogueNode
{
    public DialogueNode conditionMetNextNode;
    public DialogueNode conditionNotMetNextNode;
    public List<DialogueCondition> conditions;

    private void OnEnable()
    {
        options.Clear();

        DialogueOption option = new DialogueOption
        {
            optionText = "Continue",
            nextNode = conditionMetNextNode,
            fallbackNode = conditionNotMetNextNode,
            conditions = conditions
        };

        options.Add(option);
    }
}
