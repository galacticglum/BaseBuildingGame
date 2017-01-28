using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Inventory : ISelectable
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

    public LuaEventManager EventManager { get; set; }

    public event InventoryChangedEventHandler InventoryChanged;
    public void OnInventoryChanged(InventoryEventArgs args)
    {
        InventoryChangedEventHandler inventoryChanged = InventoryChanged;
        if (inventoryChanged != null)
        {
            inventoryChanged(this, args);
        }

        EventManager.Trigger("InventoryChanged", this, args);
    }

    public Inventory()
    {
        EventManager = new LuaEventManager("InventoryChanged");

        Type = "Steel Plate";
        MaxStackSize = 50;
        StackSize = 1;
    }

    public Inventory(string type, int maxStackSize, int stackSize) : this()
    {
		Type = type;
		MaxStackSize = maxStackSize;
		StackSize = stackSize;
	}

	protected Inventory(Inventory inventory) : this()
    {
		Type = inventory.Type;
		MaxStackSize = inventory.MaxStackSize;
		StackSize = inventory.StackSize;
	}

	public virtual Inventory Clone()
    {
		return new Inventory(this);
	}

    public string GetName()
    {
        return Type;
    }

    public string GetDescription()
    {
        return "An inventory.. (what else do you want!)";
    }

    public IEnumerable<string> GetAdditionalInfo()
    {
        return null;
    }
}
