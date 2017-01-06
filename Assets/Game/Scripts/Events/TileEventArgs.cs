using System;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class TileEventArgs : EventArgs
{
    public readonly Tile Tile;

    public TileEventArgs(Tile tile) : base()
    {
        Tile = tile;
    }
}