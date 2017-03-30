using System;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class JobEventArgs : EventArgs
{
    public readonly Job Job;
    public JobEventArgs(Job job) : base()
    {
        Job = job;
    }
}
