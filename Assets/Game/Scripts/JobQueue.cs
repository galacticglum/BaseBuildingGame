using System.Collections.Generic;

public class JobQueue
{
    public event JobCreatedEventHandler JobCreated;
    public void OnJobCreated(JobCreatedEventArgs args)
    {
        JobCreatedEventHandler jobCreated = JobCreated;
        if (jobCreated != null)
        {
            jobCreated(this, args);
        }
    }

    private readonly Queue<Job> queue;

    public JobQueue()
    {
        queue = new Queue<Job>();
    }
    
    public void Enqueue(Job job)
    {
        queue.Enqueue(job);
        OnJobCreated(new JobCreatedEventArgs(job));
    }

    public Job Dequeue()
    {
        return queue.Count == 0 ? null : queue.Dequeue();
    }
}
