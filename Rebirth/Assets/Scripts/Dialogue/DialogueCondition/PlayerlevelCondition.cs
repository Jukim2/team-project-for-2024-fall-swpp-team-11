[ConditionName("Player Level")]
public class PlayerlevelCondition : Condition
{
    public int requiredLevel;
    public bool isGreaterThan;

    override public bool IsConditionMet()
    {
        // Your condition logic here
        return true;
    }
}