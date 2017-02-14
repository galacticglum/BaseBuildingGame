using UnityEngine;

public struct MouseDragParameters
{
    public int RawStartX { get; private set; }
    public int RawEndX { get; private set; }
    public int RawStartY { get; private set; }
    public int RawEndY { get; private set; }

    public int StartX { get; private set; }
    public int EndX { get; private set; }
    public int StartY { get; private set; }
    public int EndY { get; private set; }

    public MouseDragParameters(int startX, int endX, int startY, int endY) : this()
    {
        RawStartX = startX;
        RawEndX = endX;
        RawStartY = startY;
        RawEndY = endY;

        StartX = Mathf.Min(startX, endX);
        EndX = Mathf.Max(startX, endX);
        StartY = Mathf.Min(startY, endY);
        EndY = Mathf.Max(startY, endY);
    }
}