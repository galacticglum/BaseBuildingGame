using System;
using UnityEngine;

public delegate void CameraMovedEventHandler(object sender, CameraMovedEventArgs args);
public class CameraMovedEventArgs : EventArgs
{
    public readonly Bounds CameraBounds;
    public CameraMovedEventArgs(Bounds cameraBounds)
    {
        CameraBounds = cameraBounds;
    }
}