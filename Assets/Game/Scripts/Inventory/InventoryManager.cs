using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class InventoryManager
{
    public Dictionary<string, List<Inventory>> Inventories { get; private set; }

    public InventoryManager()
    {
        Inventories = new Dictionary<string, List<Inventory>>();
    }

    private void Cleanup(Inventory inventory)
    {
        if (inventory.StackSize != 0) return;
        if (Inventories.ContainsKey(inventory.Type))
        {
            Inventories[inventory.Type].Remove(inventory);
        }

        if (inventory.Tile != null)
        {
            inventory.Tile.Inventory = null;
            inventory.Tile = null;
        }

        if (inventory.Character == null) return;
        inventory.Character.Inventory = null;
        inventory.Character = null;
    }

    public bool Place(Tile tile, Inventory inventory)
    {
        bool wasEmpty = tile.Inventory == null;
        if (tile.PlaceInventory(inventory) == false)
        {
            return false;
        }

        Cleanup(inventory);
        if (!wasEmpty) return true;

        if (Inventories.ContainsKey(tile.Inventory.Type) == false)
        {
            Inventories[tile.Inventory.Type] = new List<Inventory>();
        }

        Inventories[tile.Inventory.Type].Add(tile.Inventory);
        World.Current.OnInventoryCreated(new InventoryEventArgs(tile.Inventory));

        return true;
    }

    public bool Place(Job job, Inventory inventory)
    {
        if (job.InventoryRequirements.ContainsKey(inventory.Type) == false)
        {
            Debug.LogError("InventoryManager::Place: Attempted to add inventory to a job that doesn't require it.");
            return false;
        }

        job.InventoryRequirements[inventory.Type].StackSize += inventory.StackSize;
        if (job.InventoryRequirements[inventory.Type].MaxStackSize < job.InventoryRequirements[inventory.Type].StackSize)
        {
            inventory.StackSize = job.InventoryRequirements[inventory.Type].StackSize - job.InventoryRequirements[inventory.Type].MaxStackSize;
            job.InventoryRequirements[inventory.Type].StackSize = job.InventoryRequirements[inventory.Type].MaxStackSize;
        }
        else
        {
            inventory.StackSize = 0;
        }

        Cleanup(inventory);
        return true;
    }

    public bool Place(Character character, Inventory sourceInventory, int amount = -1)
    {
        amount = amount < 0 ? sourceInventory.StackSize : Mathf.Min(amount, sourceInventory.StackSize);
        if (character.Inventory == null)
        {
            character.Inventory = sourceInventory.Clone();
            character.Inventory.StackSize = 0;
            Inventories[character.Inventory.Type].Add(character.Inventory);
        }
        else if (character.Inventory.Type != sourceInventory.Type)
        {
            Debug.LogError("InventoryManager::Place(Character, Inventory, int): Mismatched inventory type.");
            return false;
        }

        character.Inventory.StackSize += amount;
        if (character.Inventory.MaxStackSize < character.Inventory.StackSize)
        {
            sourceInventory.StackSize = character.Inventory.StackSize - character.Inventory.MaxStackSize;
            character.Inventory.StackSize = character.Inventory.MaxStackSize;
        }
        else
        {
            sourceInventory.StackSize -= amount;
        }

        Cleanup(sourceInventory);
        return true;
    }

    public bool QuickCheck(string type)
    {
        if (Inventories.ContainsKey(type) == false)
        {
            return false;
        }

        return Inventories[type].Count != 0;
    }

    public Inventory GetClosestInventoryOfType(string type, Tile tile, int desiredAmount, bool canTakeFromStockpile)
    {
        Pathfinder path = GetPathToClosestInventoryOfType(type, tile, desiredAmount, canTakeFromStockpile);
        return path.DestinationTile.Inventory;
    }

    public Pathfinder GetPathToClosestInventoryOfType(string type, Tile tile, int desiredAmount, bool canTakeFromStockpile)
    {
        QuickCheck(type);
        if (!canTakeFromStockpile && Inventories[type].TrueForAll(i => i.Tile != null && i.Tile.Furniture != null && i.Tile.Furniture.IsStockpile()))
        {
            return null;
        }

        if (Inventories[type].TrueForAll(i => i.Tile != null && i.Tile.Furniture != null && i.Tile.Inventory.Locked))
        {
            return null;
        }

        if (Inventories[type].Find(i => i.Tile != null) == null)
        {
            return null;
        }

        Pathfinder path = new Pathfinder(World.Current, tile, null, type, desiredAmount, canTakeFromStockpile);
        return path;
    }
}
