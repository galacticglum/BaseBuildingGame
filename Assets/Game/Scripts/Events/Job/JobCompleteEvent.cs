public delegate void JobCompleteEventHandler(object sender, JobCompleteEventArgs args);
public class JobCompleteEventArgs : JobEventArgs
{
    public JobCompleteEventArgs(Job job) : base(job)
    {
    }
}
