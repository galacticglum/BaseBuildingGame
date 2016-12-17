public delegate void JobCreatedEventHandler(object sender, JobCreatedEventArgs args);
public class JobCreatedEventArgs : JobEventArgs
{
    public JobCreatedEventArgs(Job job) : base(job)
    {
    }
}