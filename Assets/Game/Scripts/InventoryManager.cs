﻿using UnityEngine;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class InventoryManager
{
	public Dictionary< string, List<Inventory>> Inventories { get; protected set; }

    public LuaEventManager EventManager { get; protected set; }
    public event InventoryCreatedEventHandler InventoryCreated;
    public void OnInventoryCreated(InventoryEventArgs args)
    {
        InventoryCreatedEventHandler inventoryCreated = InventoryCreated;
        if (inventoryCreated != null)
        {
            inventoryCreated(this, args);
        }

        EventManager.Trigger("InventoryCreated", this, args);
    }

    public InventoryManager()
    {
        EventManager = new LuaEventManager("InventoryCreated");
		Inventories = new Dictionary< string, List<Inventory> >();
	}

    private void Cleanup(Inventory inventory)
    {
        if (inventory.StackSize != 0) return;
        if( Inventories.ContainsKey(inventory.Type) )
        {
            Inventories[inventory.Type].Remove(inventory);
        }

        if(inventory.Tile != null)
        {
            inventory.Tile.Inventory = null;
            inventory.Tile = null;
        }

        if (inventory.Character == null) return;
        inventory.Character.Inventory = null;
        inventory.Character = null;
    }

	public bool Place(Tile tile, Inventory sourceInventory)
    {
		bool tileWasEmpty = tile.Inventory == null;

		if( tile.PlaceInventory(sourceInventory) == false )
        {
			return false;
		}

		Cleanup(sourceInventory);
        if (!tileWasEmpty) return true;

        if( Inventories.ContainsKey(tile.Inventory.Type) == false )
        {
            Inventories[tile.Inventory.Type] = new List<Inventory>();
        }

        Inventories[tile.Inventory.Type].Add(tile.Inventory);
        OnInventoryCreated(new InventoryEventArgs(tile.Inventory));

        return true;
	}

	public bool Place(Job job, Inventory sourceInventory)
    {
		if ( job.InventoryRequirements.ContainsKey(sourceInventory.Type) == false )
        {
			Debug.LogError("InventoryManager::PlaceInventory: Attempted to add inventory to a job that doesn't require it.");
			return false;
		}

		job.InventoryRequirements[sourceInventory.Type].StackSize += sourceInventory.StackSize;

		if(job.InventoryRequirements[sourceInventory.Type].MaxStackSize < job.InventoryRequirements[sourceInventory.Type].StackSize)
        {
			sourceInventory.StackSize = job.InventoryRequirements[sourceInventory.Type].StackSize - job.InventoryRequirements[sourceInventory.Type].MaxStackSize;
			job.InventoryRequirements[sourceInventory.Type].StackSize = job.InventoryRequirements[sourceInventory.Type].MaxStackSize;
		}
		else
        {
			sourceInventory.StackSize = 0;
		}

		Cleanup(sourceInventory);

		return true;
	}

	public bool Place(Character character, Inventory sourceInventory, int amount = -1)
    {
		amount = amount < 0 ? sourceInventory.StackSize : Mathf.Min( amount, sourceInventory.StackSize );

		if(character.Inventory == null)
        {
			character.Inventory = sourceInventory.Clone();
			character.Inventory.StackSize = 0;
			Inventories[character.Inventory.Type].Add(character.Inventory);
		}
		else if(character.Inventory.Type != sourceInventory.Type)
        {
			Debug.LogError("InventoryManager::PlaceInventory(Character, Inventory, int): Mismatched inventory type.");
			return false;
		}

		character.Inventory.StackSize += amount;

		if(character.Inventory.MaxStackSize < character.Inventory.StackSize)
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

	public Inventory GetClosestInventoryOfType(string type, Tile tile, bool canTakeFromStockpile)
	{
	    Tile destinationTile = GetPathToClosestInventoryOfType(type, tile, canTakeFromStockpile).DestinationTile;
	    return destinationTile != null ? destinationTile.Inventory : null;
	}

    public Pathfinder GetPathToClosestInventoryOfType(string type, Tile tile, bool canTakeFromStockpile)
    {
        if (Inventories.ContainsKey(type) == false
            || !canTakeFromStockpile && Inventories[type].TrueForAll(inventory => inventory.Tile != null && 
            inventory.Tile.Furniture != null && inventory.Tile.Furniture.IsStockpile())) 
        {
            return null;
        }

        return new Pathfinder(tile, type, canTakeFromStockpile);
    }
}
