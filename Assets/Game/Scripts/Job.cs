using System;
using System.Collections.Generic;

public class Job 
{
    public Tile Tile { get; set; }
    public string Type { get; protected set; }

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

    private Dictionary<string, Inventory> inventoryRequirements;
    private float jobTime;

    public Job(Tile tile, string type, Action<object, JobCompleteEventArgs> jobComplete, float jobTime, IEnumerable<Inventory> inventoryRequirements = null)
    {
        Tile = tile;
        Type = type;
        JobComplete += (sender, args) => jobComplete(sender, args);

        this.jobTime = jobTime;
        this.inventoryRequirements = new Dictionary<string, Inventory>();

        if (inventoryRequirements == null) return;
        foreach (Inventory inventoryRequirement in inventoryRequirements)
        {
            this.inventoryRequirements[inventoryRequirement.Type] = inventoryRequirement.Clone();
        }
    }

    protected Job(Job job)
    {
        Tile = job.Tile;
        Type = job.Type;
        JobComplete += (sender, args) => job.JobComplete(sender, args);

        jobTime = job.jobTime;
        inventoryRequirements = new Dictionary<string, Inventory>();

        if (inventoryRequirements == null) return;
        foreach (Inventory inventoryRequirement in job.inventoryRequirements.Values)
        {
            inventoryRequirements[inventoryRequirement.Type] = inventoryRequirement.Clone();
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
}
