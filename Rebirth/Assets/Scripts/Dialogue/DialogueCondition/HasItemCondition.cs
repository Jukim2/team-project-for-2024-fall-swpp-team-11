[ConditionName("Has Item")]
public class HasItemCondition : Condition
{
    public ItemData itemData;

    public override bool IsConditionMet()
    {
        // return InventorySystem.Instance.HasItem(itemID);
        return true;
    }
}