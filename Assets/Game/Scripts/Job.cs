using System.Collections.Generic;
using System;
using System.Linq;

public class Job
{ 
	public Tile Tile { get; set; }
    public Furniture Furniture { get; set; }
    public string Type { get; protected set; }

    public Dictionary<string, Inventory> InventoryRequirements;

    public float WorkTime { get; protected set; }
	public bool AcceptsAnyInventoryItem { get; set; }
	public bool CanTakeFromStockpile { get; set; }

    public event JobCompleteEventHandler JobComplete;
    public void OnJobComplete(JobCompleteEventArgs args)
    {
        if (JobComplete != null)
        {
            JobComplete(this, args);
        }
    }

    public event JobCancelEventHandler JobCancel;
    public void OnJobCancel(JobCancelEventArgs args)
    {
        if (JobCancel != null)
        {
            JobCancel(this, args);
        }
    }

    public event JobWorkedEventHandler JobWorked;
    public void OnJobWorked(JobWorkedEventArgs args)
    {
        if (JobWorked != null)
        {
            JobWorked(this, args);
        }
    }

    public Job (Tile tile, string type, Action<object, JobCompleteEventArgs> jobComplete, float workTime, Inventory[] inventoryRequirements )
    {
		Tile = tile;
		Type = type;
		WorkTime = workTime;
        AcceptsAnyInventoryItem = false;
        CanTakeFromStockpile = true;

        if (jobComplete != null)
        {
            JobComplete += (sender, args) => { jobComplete(sender, args); };
        }

        InventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements == null) return;
        foreach(Inventory inventory in inventoryRequirements)
        {
            InventoryRequirements[inventory.Type] = inventory.Clone();
        }
    }

	protected Job(Job job)
    {
		Tile = job.Tile;
		Type = job.Type;
		WorkTime = job.WorkTime;
        AcceptsAnyInventoryItem = job.AcceptsAnyInventoryItem;
        CanTakeFromStockpile = job.CanTakeFromStockpile;
        JobComplete = job.JobComplete;

        InventoryRequirements = new Dictionary<string, Inventory>();
        if (InventoryRequirements == null) return;
        foreach(Inventory inventory in job.InventoryRequirements.Values)
        {
            InventoryRequirements[inventory.Type] = inventory.Clone();
        }
    }

	public virtual Job Clone()
    {
		return new Job(this);
	}

	public void DoWork(float workTime)
    {
		if(HasAllMaterials() == false)
        {
            OnJobWorked(new JobWorkedEventArgs(this));

            return;
		}

		WorkTime -= workTime;

        OnJobWorked(new JobWorkedEventArgs(this));

        if (!(WorkTime <= 0)) return;
        OnJobComplete(new JobCompleteEventArgs(this));
    }

	public void CancelJob()
    {
        OnJobCancel(new JobCancelEventArgs(this));
		Tile.World.JobQueue.Remove(this);
	}

	public bool HasAllMaterials()
	{
	    return InventoryRequirements.Values.All(inventory => inventory.MaxStackSize <= inventory.StackSize);
	}

	public int GetDesiredInventoryAmount(Inventory inventory)
    {
		if(AcceptsAnyInventoryItem)
        {
			return inventory.MaxStackSize;
		}

		if(InventoryRequirements.ContainsKey(inventory.Type) == false)
        {
			return 0;
		}

		if(InventoryRequirements[inventory.Type].StackSize >= InventoryRequirements[inventory.Type].MaxStackSize)
        {
			return 0;
		}

		return InventoryRequirements[inventory.Type].MaxStackSize - InventoryRequirements[inventory.Type].StackSize;
	}

	public Inventory GetFirstDesiredInventory()
	{
	    return InventoryRequirements.Values.FirstOrDefault(inv => inv.MaxStackSize > inv.StackSize);
	}
		
}
