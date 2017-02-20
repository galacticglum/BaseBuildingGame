using System;

public class UpdateEventArgs : EventArgs
{
    public readonly float DeltaTime;
    public UpdateEventArgs(float deltaTime)
    {
        DeltaTime = deltaTime;
    }
}