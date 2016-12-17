using System;

public class InventoryEventArgs : EventArgs
{
    public readonly Inventory Inventory;

    public InventoryEventArgs(Inventory inventory) : base()
    {
        Inventory = inventory;
    }
}
