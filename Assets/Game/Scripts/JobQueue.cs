using UnityEngine;
using System.Collections.Generic;
using System;

public class JobQueue
{
    private Queue<Job> jobQueue;

    public event JobCreatedEventHandler JobCreated;
    public void OnJobCreated(JobEventArgs args)
    {
        JobCreatedEventHandler jobCreated = JobCreated;
        if (jobCreated != null)
        {
            jobCreated(this, args);
        }
    }

    public JobQueue()
    {
		jobQueue = new Queue<Job>();
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

		jobQueue.Enqueue(job);
        OnJobCreated(new JobEventArgs(job));
	}

	public Job Dequeue()
	{
	    return jobQueue.Count == 0 ? null : jobQueue.Dequeue();
	}

	public void Remove(Job job)
    {
		List<Job> jobs = new List<Job>(jobQueue);

		if(jobs.Contains(job) == false)
        {
			return;
		}

		jobs.Remove(job);
		jobQueue = new Queue<Job>(jobs);
	}
}
