using System;
using MoonSharp.Interpreter;

public delegate void FurnitureUpdateEventHandler(object sender, FurnitureUpdateEventArgs args);

[MoonSharpUserData]
public class FurnitureUpdateEventArgs : EventArgs
{
    public readonly Furniture Furniture;
    public readonly float DeltaTime;

    public FurnitureUpdateEventArgs(Furniture furniture, float deltaTime) : base()
    {
        Furniture = furniture;
        DeltaTime = deltaTime;
    }
}
