using System;

public class Job 
{
    public Tile Tile { get; protected set; }
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


    private float jobTime;

    public Job(Tile tile, string type, Action<object, JobCompleteEventArgs> jobComplete, float jobTime = 0.1f)
    {
        Tile = tile;
        Type = type;
        this.jobTime = jobTime;
        JobComplete += ((sender, args) => jobComplete(sender, args));   
    }

    public void DoWork(float workTime)
    {
        jobTime -= workTime;

        if (!(jobTime <= 0)) return;
        OnJobComplete(new JobCompleteEventArgs(this));
    }
}
