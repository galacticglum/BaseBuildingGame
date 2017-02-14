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
    public TileType TileType { get; private set; }

    public Tile Tile { get; set; }
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

    public event Action<Job> cbJobCompleted;
    public event Action<Job> cbJobStopped;
    public event Action<Job> cbJobWorked;

    protected float requiredWorkTime;
    protected bool repeatingJob;

    private readonly List<string> cbJobCompletedLua;
    private readonly List<string> cbJobWorkedLua;

    public Job(Tile tile, string type, Action<Job> cbJobComplete, float workTime, Inventory[] inventoryRequirements, JobPriority priority, bool repeatingJob = false, bool isNeed = false, bool critical = false)
    {
        this.repeatingJob = repeatingJob;

        Tile = tile;
        Type = type;
        cbJobCompleted += cbJobComplete;
        requiredWorkTime = WorkTime = workTime;
        IsNeed = isNeed;
        Critical = critical;
        Priority = priority;
        Description = "job_error_missing_desc";

        cbJobWorkedLua = new List<string>();
        cbJobCompletedLua = new List<string>();

        InventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements == null) return;
        foreach (Inventory inventory in inventoryRequirements)
        {
            InventoryRequirements[inventory.Type] = inventory.Clone();
        }
    }

    public Job(Tile tile, TileType tileType, Action<Job> cbJobComplete, float workTime, Inventory[] inventoryRequirements, JobPriority priority, bool repeatingJob = false, bool workAdjacent = false)
    {
        this.repeatingJob = repeatingJob;
        Tile = tile;
        TileType = tileType;
        cbJobCompleted += cbJobComplete;
        requiredWorkTime = WorkTime = workTime;
        Priority = priority;
        WorkAdjacent = workAdjacent;
        Description = "job_error_missing_desc";

        cbJobWorkedLua = new List<string>();
        cbJobCompletedLua = new List<string>();

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
        cbJobCompleted = other.cbJobCompleted;
        WorkTime = other.WorkTime;
        Priority = other.Priority;
        WorkAdjacent = other.WorkAdjacent;
        Description = other.Description;
        AcceptsAnyInventoryItem = other.AcceptsAnyInventoryItem;

        cbJobWorkedLua = new List<string>(other.cbJobWorkedLua);
        cbJobCompletedLua = new List<string>(other.cbJobWorkedLua);

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
        if (cbJobWorked != null)
        {
            cbJobWorked(this);
        }

        if (cbJobWorkedLua != null)
        {
            foreach (string function in cbJobWorkedLua)
            {
                LuaUtilities.CallFunction(function, this);
            }
        }

        if (NeedsMaterial() == false)
        {
            return;
        }

        WorkTime -= workTime;
        if (!(WorkTime <= 0)) return;

        if (cbJobCompleted != null)
        {
            cbJobCompleted(this);
        }

        foreach (string function in cbJobCompletedLua)
        {
            LuaUtilities.CallFunction(function, this);
        }

        if (repeatingJob == false)
        {
            if (cbJobStopped != null)
            {
                cbJobStopped(this);
            }
        }
        else
        {
            WorkTime += requiredWorkTime;
        }
    }

    public void CancelJob()
    {
        if (cbJobStopped != null)
        {
            cbJobStopped(this);
        }

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

    public void RegisterJobCompletedCallback(string cb)
    {
        cbJobCompletedLua.Add(cb);
    }

    public void UnregisterJobCompletedCallback(string cb)
    {
        cbJobCompletedLua.Remove(cb);
    }
    
    public void RegisterJobWorkedCallback(string cb)
    {
        cbJobWorkedLua.Add(cb);
    }

    public void UnregisterJobWorkedCallback(string cb)
    {
        cbJobWorkedLua.Remove(cb);
    }
}
