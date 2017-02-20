using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class Inventory : IXmlSerializable, ISelectable, IContextActionProvider
{
    public string Type;
    public int MaxStackSize { get; set; }
    public float BasePrice { get; set; }
    public bool Locked { get; set; }

    private int stackSize = 1;
    public int StackSize
    {
        get { return stackSize; }
        set
        {
            if (stackSize == value) return;
            stackSize = value;

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
    public bool IsSelected { get; set; }

    public Inventory()
    {
        MaxStackSize = 50;
        BasePrice = 1;
    }

    public Inventory(string type, int maxStackSize, int stackSize) : this()
    {
        Type = type;
        MaxStackSize = maxStackSize;
        StackSize = stackSize;
    }

    public Inventory(string type, int stackSize) : this()
    {
        Type = type;
        MaxStackSize = World.Current.InventoryPrototypes.ContainsKey(type) ? World.Current.InventoryPrototypes[type].MaxStackSize : 50;
        StackSize = stackSize;
    }

    protected Inventory(Inventory inventory)
    {
        Type = inventory.Type;
        MaxStackSize = inventory.MaxStackSize;
        StackSize = inventory.StackSize;
        Locked = inventory.Locked;
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
        return "A stack of inventory.";
    }

    public string GetHitPointString()
    {
        return string.Empty;  
    }

    public string GetJobDescription()
    {
        return "";
    }

    public IEnumerable<ContextMenuAction> GetContextMenuActions(ContextMenu contextMenu)
    {
        ContextMenuAction action = new ContextMenuAction
        {
            Text = "Sample Item Context action",
            RequiresCharacterSelection = true,
        };

        action.Action += (contextMenuAction, character) => Debug.Log("Sample menu action");
        yield return action;
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", Tile.X.ToString());
        writer.WriteAttributeString("Y", Tile.Y.ToString());
        writer.WriteAttributeString("objectType", Type);
        writer.WriteAttributeString("maxStackSize", MaxStackSize.ToString());
        writer.WriteAttributeString("stackSize", StackSize.ToString());
        writer.WriteAttributeString("basePrice", BasePrice.ToString());
    }

    public void ReadXml(XmlReader reader) { }
}
