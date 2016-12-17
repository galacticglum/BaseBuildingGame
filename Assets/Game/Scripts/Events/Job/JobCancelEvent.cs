public delegate void JobCancelEventHandler(object sender, JobCancelEventArgs args);
public class JobCancelEventArgs : JobEventArgs
{
    public JobCancelEventArgs(Job job) : base(job)
    {
    }
}
