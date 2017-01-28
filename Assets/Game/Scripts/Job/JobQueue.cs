using UnityEngine;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityUtilities.Generic;

[MoonSharpUserData]
public class JobQueue
{
    public bool Empty { get { return jobQueue == null || jobQueue.Count == 0; } }
    public int Count { get { return jobQueue == null ? 0 : jobQueue.Count; } }

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

    private readonly SortedList<JobPriority, Job> jobQueue;

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

    public IEnumerable<Job> Peek()
    {
        return jobQueue.Values;
    }
}
