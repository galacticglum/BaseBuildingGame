using UnityEngine;
using System.Collections;

public class Inventory
{
    public string Type { get; set; }
    public int MaxStackSize { get; set; }

    private int stackSize = 1;
    public int StackSize
    {
        get { return stackSize; }
        set
        {
            if (stackSize == value) return;

            stackSize = value;
            if (Tile != null)
            {
                OnInventoryChanged(new InventoryChangedEventArgs(this));
            }
        }
    }

    public event InventoryChangedEventHandler InventoryChanged;
    public void OnInventoryChanged(InventoryChangedEventArgs args)
    {
        InventoryChangedEventHandler inventoryChanged = InventoryChanged;
        if (inventoryChanged != null)
        {
            inventoryChanged(this, args);
        }
    }

    public Tile Tile { get; set; }
    public Character Character { get; set; }

    public Inventory(string type, int maxStackSize, int stackSize)
    {
        Type = type;
        MaxStackSize = maxStackSize;
        StackSize = stackSize;
    }

    protected Inventory(Inventory inventory)
    {
        Type = inventory.Type;
        MaxStackSize = inventory.MaxStackSize;
        StackSize = inventory.StackSize;
    }

    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }

    public Inventory()
    {
        Type = "Steel Plate";
        MaxStackSize = 50;
        StackSize = 1;
    }
}
