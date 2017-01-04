using System;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Inventory
{
	public string Type { get; set; }
    public int MaxStackSize { get; set; }

    protected int localStackSize = 1;
	public int StackSize
    {
		get { return localStackSize; }
		set
        {
            if (localStackSize == value) return;
            localStackSize = value;

            OnInventoryChanged(new InventoryEventArgs(this));
        }
	}

    public Tile Tile { get; set; }
    public Character Character { get; set; }

    public event InventoryChangedEventHandler InventoryChanged;
    public void OnInventoryChanged(InventoryEventArgs args)
    {
        InventoryChangedEventHandler inventoryChanged = InventoryChanged;
        if (inventoryChanged != null)
        {
            inventoryChanged(this, args);
        }
    }

    public Inventory()
    {
        Type = "Steel Plate";
        MaxStackSize = 50;
        StackSize = 1;
    }

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
}
