public delegate void InventoryChangedEventHandler(object sender, InventoryChangedEventArgs args);
public class InventoryChangedEventArgs : InventoryEventArgs
{
    public InventoryChangedEventArgs(Inventory inventory) : base(inventory)
    {
    }
}   