using UnityEngine;
using System.Collections;

public class Inventory
{
    public string Type { get; set; }
    public int MaxStackSize { get; set; }
    public int StackSize { get; set; }

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
