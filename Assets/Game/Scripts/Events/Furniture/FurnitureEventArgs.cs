using System;

public class FurnitureEventArgs : EventArgs
{
    public readonly Furniture Furniture;

    public FurnitureEventArgs(Furniture furniture) : base()
    {
        Furniture = furniture;
    }
}
