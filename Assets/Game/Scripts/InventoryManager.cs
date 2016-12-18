using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager
{
    public Dictionary<string, List<Inventory>> Inventories { get; protected set; }

    public InventoryManager()
    {
        Inventories = new Dictionary<string, List<Inventory>>();
    }

    private void CleanupInventory(Inventory inventory)
    {
        if (inventory.StackSize == 0)
        {
            if (Inventories.ContainsKey(inventory.Type))
            {
                Inventories[inventory.Type].Remove(inventory);
            }

            if (inventory.Tile != null)
            {
                inventory.Tile.Inventory = null;
                inventory.Tile = null;
            }

            if (inventory.Character != null)
            {
                inventory.Character.inventory = null;
                inventory.Character = null;
            }
        }
    }

    public bool PlaceInventory(Tile tile, Inventory sourceInventory)
    {
        bool tileWasEmpty = tile.Inventory == null;

        if (tile.PlaceInventory(sourceInventory) == false)
        {
            return false;
        }

        CleanupInventory(sourceInventory);

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

    public bool PlaceInventory(Job job, Inventory sourceInventory)
    {
        if (job.InventoryRequirements.ContainsKey(sourceInventory.Type) == false)
        {
            Debug.LogError("InventoryManager::PlaceInventory: Attempted to add inventory to a job that doesn't require it.");
            return false;
        }

        job.InventoryRequirements[sourceInventory.Type].StackSize += sourceInventory.StackSize;
        if (job.InventoryRequirements[sourceInventory.Type].MaxStackSize < job.InventoryRequirements[sourceInventory.Type].StackSize)
        {
            sourceInventory.StackSize = job.InventoryRequirements[sourceInventory.Type].StackSize -
                                  job.InventoryRequirements[sourceInventory.Type].MaxStackSize;

            job.InventoryRequirements[sourceInventory.Type].StackSize = job.InventoryRequirements[sourceInventory.Type].MaxStackSize;
        }
        else
        {
            sourceInventory.StackSize = 0;
        }

        CleanupInventory(sourceInventory);
        return true;
    }

    public bool PlaceInventory(Character character, Inventory sourceInventory, int amount = -1)
    {
        if (amount < 0)
        {
            amount = sourceInventory.StackSize;
        }
        else
        {
            amount = Mathf.Min(amount, sourceInventory.StackSize);
        }

        if (character.inventory == null)
        {
            character.inventory = sourceInventory.Clone();
            character.inventory.StackSize = 0;

            Inventories[character.inventory.Type].Add(character.inventory);
        }
        else if (character.inventory.Type != sourceInventory.Type)
        {
            Debug.LogError("InventoryManager::PlaceInventory(Character, Inventory, int): Mismatched inventory type.");
            return false;
        }

        character.inventory.StackSize += amount;
        if (character.inventory.MaxStackSize < character.inventory.StackSize)
        {
            sourceInventory.StackSize = character.inventory.StackSize - character.inventory.MaxStackSize;
            character.inventory.StackSize = character.inventory.MaxStackSize;
        }
        else
        {
            sourceInventory.StackSize -= amount;
        }

        CleanupInventory(sourceInventory);

        return true;
    }

    public Inventory GetClosestInventoryOfType(string type, Tile tile, int amountRequired)
    {
        /* FIXME:
         *     a) We aren't ACTUALLY returning the closest item.
         *     b) Optimize method of returning closest item. This
         *        requires that the inventory database become more
         *        sophisticated.
         */

        if (Inventories.ContainsKey(type))
        {
            return Inventories[type].FirstOrDefault(inventory => inventory.Tile != null);
        }

        // If we get to this point it means that we couldn't find an inventory in our database.
        Debug.LogError("InventoryManager::GetClosestInventoryOfType: No inventory exists for type: '" + type + "'!");
        return null;
    }
}