using System.Collections.Generic;
using System;
using System.Linq;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Job
{ 
	public Tile Tile { get; set; }
    public Furniture FurniturePrototype { get; set; }
    public Furniture Furniture { get; set; }
    public string Type { get; protected set; }

    public Dictionary<string, Inventory> InventoryRequirements;

    public float WorkTime { get; protected set; }
	public bool AcceptsAnyInventoryItem { get; set; }
	public bool CanTakeFromStockpile { get; set; }

    private readonly float requiredWorkTime;
    private readonly bool repeatingJob;

    public Callback<JobEventArgs> JobCompleted;
    public Callback<JobEventArgs> JobStopped;
    public Callback<JobEventArgs> JobWorked;

    public Job(Tile tile, string type, float workTime, Closure jobCompleted, Inventory[] inventoryRequirements, bool repeatingJob = false)
    {
        this.repeatingJob = repeatingJob;
        requiredWorkTime = WorkTime = workTime;

        Initialize(tile, type, new Callback<JobEventArgs>(jobCompleted), inventoryRequirements);
    }

    public Job (Tile tile, string type, float workTime, CallbackHandler<JobEventArgs> jobCompleted, Inventory[] inventoryRequirements, bool repeatingJob = false)
    {
        this.repeatingJob = repeatingJob;
        requiredWorkTime = WorkTime = workTime;

        Initialize(tile, type, new Callback<JobEventArgs>(jobCompleted), inventoryRequirements);
    }

    protected Job(Job job)
    {
		Tile = job.Tile;
		Type = job.Type;

		WorkTime = job.WorkTime;
        AcceptsAnyInventoryItem = job.AcceptsAnyInventoryItem;
        CanTakeFromStockpile = job.CanTakeFromStockpile;

        JobCompleted = job.JobCompleted;
        JobStopped = new Callback<JobEventArgs>();     
        JobWorked = new Callback<JobEventArgs>();

        requiredWorkTime = job.requiredWorkTime;

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

    private void Initialize(Tile tile, string type, Callback<JobEventArgs> jobCompleted, Inventory[] inventoryRequirements)
    {
        Tile = tile;
        Type = type;
        AcceptsAnyInventoryItem = false;
        CanTakeFromStockpile = true;

        JobCompleted = jobCompleted;
        JobStopped = new Callback<JobEventArgs>();
        JobWorked = new Callback<JobEventArgs>();

        InventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements == null) return;
        foreach (Inventory inventory in inventoryRequirements)
        {
            InventoryRequirements[inventory.Type] = inventory.Clone();
        }
    }

	public void DoWork(float workTime)
    {
		if(HasAllMaterials() == false)
        {
            JobWorked.Invoke(new JobEventArgs(this));

            return;
		}

		WorkTime -= workTime;

        JobWorked.Invoke(new JobEventArgs(this));

        if (!(WorkTime <= 0)) return;

        JobCompleted.Invoke(new JobEventArgs(this));
        if (repeatingJob == false)
        {
            JobStopped.Invoke(new JobEventArgs(this));
        }
        else
        {
            WorkTime += requiredWorkTime;
        }
    }

	public void CancelJob()
    {
        JobStopped.Invoke(new JobEventArgs(this));
        World.Current.JobQueue.Remove(this);
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
