using System;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class Job
{
    public string Description { get; set; }
    public string Type { get; private set; }

    public Tile Tile { get; set; }
    public TileType TileType { get; private set; }
    public Furniture FurniturePrototype { get; set; }
    public Furniture Furniture { get; set; }

    public Dictionary<string, Inventory> InventoryRequirements { get; set; }
    
    public JobPriority Priority { get; private set; }
    public float WorkTime { get; private set; }
    public bool WorkAdjacent { get; set; }

    public bool CanTakeFromStockpile { get; set; }
    public bool AcceptsAnyInventoryItem { get; set; }

    public bool IsNeed { get; private set; }
    public bool Critical { get; private set; }

    public LuaEventManager EventManager { get; private set; }
    public event JobCompletedEventHandler JobCompleted;
    public void OnJobCompleted(JobEventArgs args)
    {
        JobCompletedEventHandler jobCompleted = JobCompleted;
        if (jobCompleted != null)
        {
            jobCompleted(this, args);
        }
    }

    public event JobStoppedEventHandler JobStopped;
    public void OnJobStopped(JobEventArgs args)
    {
        JobStoppedEventHandler jobStopped = JobStopped;
        if (jobStopped != null)
        {
            jobStopped(this, args);
        }
    }

    public event JobWorkedEventHandler JobWorked;
    public void OnJobWorked(JobEventArgs args)
    {
        JobWorkedEventHandler jobWorked = JobWorked;
        if (jobWorked != null)
        {
            jobWorked(this, args);
        }
    }

    private readonly float requiredWorkTime;
    private readonly bool repeatingJob;

    //private readonly List<string> cbJobCompletedLua;
    //private readonly List<string> cbJobWorkedLua;

    public Job(Tile tile, string type, JobCompletedEventHandler jobCompleted, float workTime, Inventory[] inventoryRequirements, JobPriority priority, bool repeatingJob = false, bool isNeed = false, bool critical = false)
    {
        this.repeatingJob = repeatingJob;

        Tile = tile;
        Type = type;
        JobCompleted = jobCompleted;
        requiredWorkTime = WorkTime = workTime;
        IsNeed = isNeed;
        Critical = critical;
        Priority = priority;
        Description = "job_error_missing_desc";
        EventManager = new LuaEventManager();

        InventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements == null) return;
        foreach (Inventory inventory in inventoryRequirements)
        {
            InventoryRequirements[inventory.Type] = inventory.Clone();
        }
    }

    public Job(Tile tile, TileType tileType, JobCompletedEventHandler jobCompleted, float workTime, Inventory[] inventoryRequirements, JobPriority priority, bool repeatingJob = false, bool workAdjacent = false)
    {
        this.repeatingJob = repeatingJob;
        Tile = tile;
        TileType = tileType;
        JobCompleted = jobCompleted;
        requiredWorkTime = WorkTime = workTime;
        Priority = priority;
        WorkAdjacent = workAdjacent;
        Description = "job_error_missing_desc";

        EventManager = new LuaEventManager();
        InventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements == null) return;
        foreach (Inventory inventory in inventoryRequirements)
        {
            InventoryRequirements[inventory.Type] = inventory.Clone();
        }
    }

    protected Job(Job other)
    {
        Tile = other.Tile;
        Type = other.Type;
        TileType = other.TileType;
        JobCompleted = other.JobCompleted;
        WorkTime = other.WorkTime;
        Priority = other.Priority;
        WorkAdjacent = other.WorkAdjacent;
        Description = other.Description;
        AcceptsAnyInventoryItem = other.AcceptsAnyInventoryItem;

        EventManager = other.EventManager.Clone();
        InventoryRequirements = new Dictionary<string, Inventory>();
        if (InventoryRequirements == null) return;
        foreach (Inventory inventory in other.InventoryRequirements.Values)
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
        OnJobWorked(new JobEventArgs(this));

        EventManager.Trigger("JobWorked", this);
        if (NeedsMaterial() == false)
        {
            return;
        }

        WorkTime -= workTime;
        if (!(WorkTime <= 0)) return;

        OnJobCompleted(new JobEventArgs(this));

        EventManager.Trigger("JobCompleted", this);
        if (repeatingJob == false)
        {
            OnJobStopped(new JobEventArgs(this));
        }
        else
        {
            WorkTime += requiredWorkTime;
        }
    }

    public void CancelJob()
    {
        OnJobStopped(new JobEventArgs(this));

        World.Current.JobWaitingQueue.Remove(this);
        World.Current.JobQueue.Remove(this);
    }

    public void DropPriority()
    {
        Priority = (JobPriority)Mathf.Min((int)JobPriority.Low, (int)Priority + 1);
    }

    public bool HasAllMaterial()
    {
        return InventoryRequirements == null || InventoryRequirements.Values.All(inv => inv.MaxStackSize <= inv.StackSize);
    }

    public bool HasAnyMaterial()
    {
        return InventoryRequirements.Values.Any(inv => inv.StackSize > 0);
    }

    public bool NeedsMaterial()
    {
        if (AcceptsAnyInventoryItem && HasAnyMaterial())
        {
            return true;
        }

        return AcceptsAnyInventoryItem == false && HasAllMaterial();
    }

    public int GetDesiredInventoryAmount(string objectType)
    {
        if (InventoryRequirements.ContainsKey(objectType) == false)
        {
            return 0;
        }

        if (InventoryRequirements[objectType].StackSize >= InventoryRequirements[objectType].MaxStackSize)
        {
            return 0;
        }

        return InventoryRequirements[objectType].MaxStackSize - InventoryRequirements[objectType].StackSize;
    }

    public int GetDesiredInventoryAmount(Inventory inventory)
    {
        return GetDesiredInventoryAmount(inventory.Type);
    }

    public Inventory GetFirstDesiredInventory()
    {
        return InventoryRequirements.Values.FirstOrDefault(inv => inv.MaxStackSize > inv.StackSize);
    }
}
