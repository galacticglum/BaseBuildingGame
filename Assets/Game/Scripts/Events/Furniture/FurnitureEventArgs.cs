using System;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class FurnitureEventArgs : EventArgs
{
    public readonly Furniture Furniture;

    public FurnitureEventArgs(Furniture furniture)
    {
        Furniture = furniture;
    }
}
