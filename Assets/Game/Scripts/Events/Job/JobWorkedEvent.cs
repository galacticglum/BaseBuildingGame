public delegate void JobWorkedEventHandler(object sender, JobWorkedEventArgs args);
public class JobWorkedEventArgs : JobEventArgs
{
    public JobWorkedEventArgs(Job job) : base(job)
    {
    }
}