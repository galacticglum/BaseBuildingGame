using UnityEngine;
using System.Collections.Generic;

public class InventoryManager
{
    public Dictionary<string, List<Inventory>> Inventories { get; protected set; }

    public InventoryManager()
    {
        Inventories = new Dictionary<string, List<Inventory>>();
    }

    public bool PlaceInventory(Tile tile, Inventory inventory)
    {
        bool tileWasEmpty = (tile.Inventory == null);

        if (tile.PlaceInventory(inventory) == false)
        {
            return false;
        }

        if (inventory.StackSize == 0)
        {
            if (Inventories.ContainsKey(tile.Inventory.Type))
            {
                Inventories[inventory.Type].Remove(inventory);
            }
        }

        if (tileWasEmpty)
        {
            if (Inventories.ContainsKey(tile.Inventory.Type) == false)
            {
                Inventories[tile.Inventory.Type] = new List<Inventory>();
            }
            Inventories[tile.Inventory.Type].Add(tile.Inventory);
        }

        return true;
    }
}