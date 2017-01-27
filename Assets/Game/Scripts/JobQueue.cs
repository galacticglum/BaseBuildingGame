using UnityEngine;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityUtilities.Generic;

[MoonSharpUserData]
public class JobQueue
{
    public LuaEventManager EventManager { get; set; }
    public event JobCreatedEventHandler JobCreated;
    public void OnJobCreated(JobEventArgs args)
    {
        JobCreatedEventHandler jobCreated = JobCreated;
        if (jobCreated != null)
        {
            jobCreated(this, args);
        }

        EventManager.Trigger("JobCreated", this, args);
    }

    private SortedList<JobPriority, Job> jobQueue;

    public JobQueue()
    {
		jobQueue = new SortedList<JobPriority, Job>(new DuplicateKeyComparer<JobPriority>(true));
        EventManager = new LuaEventManager("JobCreated");
	}

    public void PrintQueue()
    {
        Debug.Log(jobQueue.Count);
    }

	public void Enqueue(Job job)
    {
		if(job.WorkTime < 0)
        {
			job.DoWork(0);
			return;
		}

		jobQueue.Add(job.Priority, job);
        OnJobCreated(new JobEventArgs(job));
	}

	public Job Dequeue()
	{
	    if (jobQueue.Count == 0) return null;

	    Job job = jobQueue.Values[0];
	    jobQueue.RemoveAt(0);
	    return job;
	}

	public void Remove(Job job)
	{
	    if (jobQueue.ContainsValue(job) == false) return;
        jobQueue.RemoveAt(jobQueue.IndexOfValue(job));
	}
}
