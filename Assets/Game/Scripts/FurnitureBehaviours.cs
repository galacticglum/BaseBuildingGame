using UnityEngine;

public static class FurnitureBehaviours
{
	public static void UpdateDoor(Furniture furniture, float deltaTime)
    {
		if(furniture.GetParameter("is_opening") >= 1)
        {
			furniture.ModifyParameter("openness", deltaTime * 4);	
			if (furniture.GetParameter("openness") >= 1)
            {
				furniture.SetParameter("is_opening", 0);
			}
		}
		else
        {
			furniture.ModifyParameter("openness", deltaTime * -4);
		}

		furniture.SetParameter("openness", Mathf.Clamp01(furniture.GetParameter("openness")));
        furniture.OnFurnitureChanged(new FurnitureChangedEventArgs(furniture));
	}

	public static TileEnterability DoorTryEnter(Furniture furniture)
    {
		furniture.SetParameter("is_opening", 1);
		return furniture.GetParameter("openness") >= 1 ? TileEnterability.Immediate : TileEnterability.Soon;
    }

	public static void BuildFurniture(object sender, JobCompleteEventArgs args)
    {
		WorldController.Instance.World.PlaceFurniture(args.Job.Type, args.Job.Tile);
        args.Job.Tile.PendingFurnitureJob = null;
	}

	public static Inventory[] GetStockpileItemFilter()
    {
		// TODO: This should be reading from some kind of UI for this
		return new[] { new Inventory("Steel Plate", 50, 0) };
	}

	public static void UpdateStockpile(Furniture furniture, float deltaTime)
    {
		if( furniture.Tile.Inventory != null && furniture.Tile.Inventory.StackSize >= furniture.Tile.Inventory.MaxStackSize )
        {
			furniture.ClearJobs();
			return;
		}

		if(furniture.JobCount > 0)
        {
			return;
		}

		if( furniture.Tile.Inventory != null && furniture.Tile.Inventory.StackSize == 0 )
        {
			Debug.LogError("FurnitureBehaviours::UpdateStockpile: Stockpile has a zero-size stack.");
			furniture.ClearJobs();
			return;
		}

		Inventory[] itemFilter;
		if( furniture.Tile.Inventory == null )
        {
			itemFilter = GetStockpileItemFilter();
		}
		else
        {
			Inventory desiredInventory = furniture.Tile.Inventory.Clone();
			desiredInventory.MaxStackSize -= desiredInventory.StackSize;
			desiredInventory.StackSize = 0;

			itemFilter = new[] { desiredInventory };
		}

        Job job = new Job(furniture.Tile, null, null, 0, itemFilter) { CanTakeFromStockpile = false };
        job.JobWorked += StockpileJobWorked;
		furniture.AddJob(job);  
	}

    private static void StockpileJobWorked(object sender, JobWorkedEventArgs args)
    {
        args.Job.Tile.Furniture.RemoveJob(args.Job);

		// TODO: Change this when we figure out what we're doing for the all/any pickup job.
		foreach(Inventory inventory in args.Job.InventoryRequirements.Values)
        {
		    if (inventory.StackSize <= 0) continue;
            args.Job.Tile.World.InventoryManager.PlaceInventory(args.Job.Tile, inventory);

		    return;  
		}
	}

}
