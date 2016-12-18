using System;
using System.Collections.Generic;
using UnityEngine;

public class Job 
{
    public Tile Tile { get; set; }
    public string Type { get; protected set; }
    public Dictionary<string, Inventory> InventoryRequirements { get; protected set; }

    public event JobCompleteEventHandler JobComplete;
    public void OnJobComplete(JobCompleteEventArgs args)
    {
        JobCompleteEventHandler jobComplete = JobComplete;
        if (jobComplete != null)
        {
            jobComplete(this, args);
        }
    }

    public event JobCancelEventHandler JobCancel;
    public void OnJobCancel(JobCancelEventArgs args)
    {
        JobCancelEventHandler jobCancel = JobCancel;
        if (jobCancel != null)
        {
            jobCancel(this, args);
        }
    }

    private float jobTime;

    public Job(Tile tile, string type, Action<object, JobCompleteEventArgs> jobComplete, float jobTime, IEnumerable<Inventory> inventoryRequirements = null)
    {
        Tile = tile;
        Type = type;
        JobComplete += (sender, args) => jobComplete(sender, args);
        InventoryRequirements = new Dictionary<string, Inventory>();

        this.jobTime = jobTime;

        if (inventoryRequirements == null) return;
        foreach (Inventory inventoryRequirement in inventoryRequirements)
        {
            InventoryRequirements[inventoryRequirement.Type] = inventoryRequirement.Clone();
        }
    }

    protected Job(Job job)
    {
        Tile = job.Tile;
        Type = job.Type;
        JobComplete += (sender, args) => job.JobComplete(sender, args);

        jobTime = job.jobTime;
        InventoryRequirements = new Dictionary<string, Inventory>();

        if (InventoryRequirements == null) return;
        foreach (Inventory inventoryRequirement in job.InventoryRequirements.Values)
        {
            InventoryRequirements[inventoryRequirement.Type] = inventoryRequirement.Clone();
        }
    }

    public virtual Job Clone()
    {
        return new Job(this);
    }

    public void DoWork(float workTime)
    {
        jobTime -= workTime;

        if (!(jobTime <= 0)) return;
        OnJobComplete(new JobCompleteEventArgs(this));
    }

    public bool HasAllMaterials()
    {
        foreach (Inventory inventoryRequirement in InventoryRequirements.Values)
        {
            if (inventoryRequirement.MaxStackSize > inventoryRequirement.StackSize)
            {
                return false;
            }
        }
        return true;
    }

    public int GetRequiredInventoryAmount(Inventory inventory)
    {
        if (inventory == null) return 0;

        if (InventoryRequirements.ContainsKey(inventory.Type) == false)
        {
            return 0;
        }

        if (InventoryRequirements[inventory.Type].StackSize >= InventoryRequirements[inventory.Type].MaxStackSize)
        {
            return 0;
        }

        return InventoryRequirements[inventory.Type].MaxStackSize - InventoryRequirements[inventory.Type].StackSize;
    }

    public Inventory GetFirstRequiredInventory()
    {
        foreach (Inventory inventoryRequirement in InventoryRequirements.Values)
        {
            if (inventoryRequirement.MaxStackSize > inventoryRequirement.StackSize)
            {
                return inventoryRequirement;
            }
        }

        return null;
    }
}
