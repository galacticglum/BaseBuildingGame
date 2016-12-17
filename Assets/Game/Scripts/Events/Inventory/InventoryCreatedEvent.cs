public delegate void InventoryCreatedEventHandler(object sender, InventoryCreatedEventArgs args);
public class InventoryCreatedEventArgs : InventoryEventArgs
{
    public InventoryCreatedEventArgs(Inventory inventory) : base(inventory)
    {
    }
}