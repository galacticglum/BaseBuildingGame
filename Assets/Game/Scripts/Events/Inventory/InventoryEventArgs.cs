using System;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class InventoryEventArgs : EventArgs
{
    public readonly Inventory Inventory;

    public InventoryEventArgs(Inventory inventory) : base()
    {
        Inventory = inventory;
    }
}
