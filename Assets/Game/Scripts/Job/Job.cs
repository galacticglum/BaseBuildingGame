﻿using System.Collections.Generic;
using System;
using System.Linq;
using System.Xml;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class Job
{ 
	public Tile Tile { get; set; }
    public Furniture FurniturePrototype { get; set; }
    public Furniture Furniture { get; set; }
    public string Type { get; protected set; }

    public Dictionary<string, Inventory> InventoryRequirements;

    public JobPriority Priority { get; protected set; }
    public float WorkTime { get; protected set; }
    public bool WorkAdjacent { get; protected set; }
	public bool AcceptsAnyInventoryItem { get; set; }
	public bool CanTakeFromStockpile { get; set; }

    public LuaEventManager EventManager { get; set; }
    public event JobCompletedEventHandler JobCompleted;
    public void OnJobCompleted(JobEventArgs args)
    {
        JobCompletedEventHandler jobCompleted = JobCompleted;
        if (jobCompleted != null)
        {
            jobCompleted(this, args);
        }

        EventManager.Trigger("JobCompleted", this, args);
    }

    public event JobStoppedEventHandler JobStopped;
    public void OnJobStopped(JobEventArgs args)
    {
        JobStoppedEventHandler jobStopped = JobStopped;
        if (jobStopped != null)
        {
            jobStopped(this, args);
        }

        EventManager.Trigger("JobStopped", this, args);
    }

    public event JobWorkedEventHandler JobWorked;
    public void OnJobWorked(JobEventArgs args)
    {
        JobWorkedEventHandler jobWorked = JobWorked;
        if (jobWorked != null)
        {
            jobWorked(this, args);
        }

        EventManager.Trigger("JobWorked", this, args);
    }

    private float requiredWorkTime;
    private bool repeatingJob;

    public Job(Tile tile, string type, float workTime, JobPriority priority, Closure jobCompleted, Inventory[] inventoryRequirements, bool repeatingJob, bool workAdjacent)
    {
        Initialize(tile, type, workTime, priority, null, inventoryRequirements, repeatingJob, workAdjacent);
        EventManager.AddHandler("JobCompleted", jobCompleted);
    }

    public Job (Tile tile, string type, float workTime, JobPriority priority, JobCompletedEventHandler jobCompleted, Inventory[] inventoryRequirements, bool repeatingJob = false, bool workAdjacent = false)
    {
        Initialize(tile, type, workTime, priority, jobCompleted, inventoryRequirements, repeatingJob, workAdjacent);
    }

    protected Job(Job job)
    {
		Tile = job.Tile;
		Type = job.Type;
        Priority = job.Priority;

        WorkAdjacent = job.WorkAdjacent;
		WorkTime = job.WorkTime;
        AcceptsAnyInventoryItem = job.AcceptsAnyInventoryItem;
        CanTakeFromStockpile = job.CanTakeFromStockpile;

        JobCompleted = job.JobCompleted;
        EventManager = new LuaEventManager("JobCompleted", "JobStopped", "JobWorked");

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

    private void Initialize(Tile tile, string type, float workTime, JobPriority priority, JobCompletedEventHandler jobCompleted, Inventory[] inventoryRequirements, bool repeatingJob, bool workAdjacent)
    {
        this.repeatingJob = repeatingJob;
        WorkAdjacent = workAdjacent;

        CanTakeFromStockpile = true;
        requiredWorkTime = WorkTime = workTime;

        Tile = tile;
        Type = type;
        Priority = priority;
        AcceptsAnyInventoryItem = false;
        CanTakeFromStockpile = true;

        JobCompleted = jobCompleted;
        EventManager = new LuaEventManager("JobCompleted", "JobStopped", "JobWorked");

        InventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements == null) return;
        foreach (Inventory inventory in inventoryRequirements)
        {
            InventoryRequirements[inventory.Type] = inventory.Clone();
        }
    }

	public void DoWork(float workTime)
    {
        if (HasAllMaterials() == false)
        {
            OnJobWorked(new JobEventArgs(this));
            return;
		}

		WorkTime -= workTime;
        OnJobWorked(new JobEventArgs(this));

        if (!(WorkTime <= 0)) return;

        OnJobCompleted(new JobEventArgs(this));
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
        World.Current.JobQueue.Remove(this);
	}

    public void DropPriority()
    {
        Priority = (JobPriority) Mathf.Min((int) JobPriority.Low, (int) Priority + 1);
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