using System;

public class PowerEventArgs : EventArgs
{
    public readonly IPowerRelated PowerRelated;
    public PowerEventArgs(IPowerRelated powerRelated)
    {
        PowerRelated = powerRelated;
    }
}