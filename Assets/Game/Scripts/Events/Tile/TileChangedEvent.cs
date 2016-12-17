using System;

public delegate void TileChangedEventHandler(object sender, TileChangedEventArgs args);
public class TileChangedEventArgs : EventArgs
{
    public readonly Tile Tile;

    public TileChangedEventArgs(Tile tile) : base()
    {
        Tile = tile;
    }
}