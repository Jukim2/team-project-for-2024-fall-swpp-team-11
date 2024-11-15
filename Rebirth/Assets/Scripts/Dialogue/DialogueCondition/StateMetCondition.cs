[ConditionName("State Met")]
public class StateMetCondition : Condition
{
    public DialogueState dialogueState;

    public override bool IsConditionMet()
    {
        return DialogueManager.Instance.speakerState == dialogueState;
    }
}