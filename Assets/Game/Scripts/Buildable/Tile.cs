using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Tile :IXmlSerializable, ISelectable
{
    public Tile North { get { return World.Current.GetTileAt(X, Y + 1); } }
    public Tile South { get { return World.Current.GetTileAt(X, Y - 1); } }
    public Tile East { get { return World.Current.GetTileAt(X + 1, Y); } }
    public Tile West { get { return World.Current.GetTileAt(X - 1, Y); } }

    public int X { get; private set; }
    public int Y { get; private set; }
    public float MovementCost { get { return (float)Lua.Call(Type.MovementCostLua, this).Number; } }

    private TileType type = TileType.Empty;
    public TileType Type
    {
        get { return type; }
        set
        {
            if (type == value) return;
            type = value;

            OnTileChanged(new TileEventArgs(this));
        }
    }

    public Inventory Inventory { get; set; }
    public Furniture Furniture { get; private set; }
    public Job PendingBuildJob { get; set; }

    public Room Room { get; set; }
    public List<Character> Characters { get; private set; }

    public bool IsSelected { get; set; }

    public event TileChangedEventHandler TileChanged;
    public void OnTileChanged(TileEventArgs args)
    {
        TileChangedEventHandler tileChanged = TileChanged;
        if (tileChanged != null)
        {
            tileChanged(this, args);
        }
    }

    public Tile(int x, int y)
    {
        X = x;
        Y = y;
        Characters = new List<Character>();
    }

    public bool PlaceFurniture(Furniture furniture)
    {
        if (furniture == null)
        {
            return RemoveFurniture();
        }

        if (furniture.IsValidPosition(this) == false)
        {
            return false;
        }

        for (int xOffset = X; xOffset < (X + furniture.Width); xOffset++)
        {
            for (int yOffset = Y; yOffset < (Y + furniture.Height); yOffset++)
            {
                Tile tileAt = World.Current.GetTileAt(xOffset, yOffset);
                tileAt.Furniture = furniture;

            }
        }

        return true;
    }

    public bool RemoveFurniture()
    {
        if (Furniture == null)
        {
            return false;
        }

        Furniture furniture = Furniture;
        for (int xOffset = X; xOffset < X + furniture.Width; xOffset++)
        {
            for (int yOffset = Y; yOffset < (Y + furniture.Height); yOffset++)
            {
                Tile tileAt = World.Current.GetTileAt(xOffset, yOffset);
                tileAt.Furniture = null;
            }
        }

        return true;
    }

    public bool PlaceInventory(Inventory inventory)
    {
        if (inventory == null)
        {
            Inventory = null;
            return true;
        }

        if (Inventory != null)
        {
            if (Inventory.Type != inventory.Type)
            {
                return false;
            }

            int stack = inventory.StackSize;
            if (Inventory.StackSize + stack > Inventory.MaxStackSize)
            {
                stack = Inventory.MaxStackSize - Inventory.StackSize;
            }

            Inventory.StackSize += stack;
            inventory.StackSize -= stack;

            return true;
        }

        Inventory = inventory.Clone();
        Inventory.Tile = this;
        inventory.StackSize = 0;

        return true;
    }

    // Tells us if two tiles are adjacent.
    public bool IsNeighbour(Tile tile, bool diagonal = false)
    {
        return Mathf.Abs(X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 || 
            diagonal && (Mathf.Abs(X - tile.X) == 1 && Mathf.Abs(Y - tile.Y) == 1); 
    }

    public Tile[] GetNeighbours(bool diagonal = false)
    {
        Tile[] neighbours = diagonal == false ? new Tile[4] : new Tile[8];

        Tile tileAt = World.Current.GetTileAt(X, Y + 1);
        neighbours[0] = tileAt;	
        tileAt = World.Current.GetTileAt(X + 1, Y);
        neighbours[1] = tileAt;
        tileAt = World.Current.GetTileAt(X, Y - 1);
        neighbours[2] = tileAt;	
        tileAt = World.Current.GetTileAt(X - 1, Y);
        neighbours[3] = tileAt;	

        if (diagonal != true) return neighbours;

        tileAt = World.Current.GetTileAt(X + 1, Y + 1);
        neighbours[4] = tileAt;
        tileAt = World.Current.GetTileAt(X + 1, Y - 1);
        neighbours[5] = tileAt;
        tileAt = World.Current.GetTileAt(X - 1, Y - 1);
        neighbours[6] = tileAt;	
        tileAt = World.Current.GetTileAt(X - 1, Y + 1);
        neighbours[7] = tileAt;	

        return neighbours;
    }

    public bool HasNeighboursOfType(TileType tileType)
    {
        return GetNeighbours(true).Any(tile => tile.Type == tileType);
    }

    public TileEnterability TryEnter()
    {
        if (MovementCost == 0)
        {
            return TileEnterability.Never;
        }

        return Furniture != null ? Furniture.TryEnter() : TileEnterability.Immediate;
    }

    public void EqualizeGas(float leakFactor)
    {
        Room.EqualizeGas(this, leakFactor);
    }

    public static void OnJobComplete(object sender, JobEventArgs args)
    {
        args.Job.Tile.Type = args.Job.TileType;
        args.Job.Tile.PendingBuildJob = null;
    }

    public string GetName()
    {
        return "tile_" + type;
    }

    public string GetDescription()
    {
        return "tile_" + type + "_desc";
    }

    public string GetHitPointString()
    {
        return "";
    }

    public string GetJobDescription()
    {
        return "";
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", X.ToString());
        writer.WriteAttributeString("Y", Y.ToString());
        writer.WriteAttributeString("RoomID", Room == null ? "-1" : Room.Index.ToString());
        writer.WriteAttributeString("Type", Type.Type);
    }

    public void ReadXml(XmlReader reader)
    {
        Room = World.Current.RoomManager.Get(int.Parse(reader.GetAttribute("RoomID")));
        if (Room != null)
        {
            Room.AssignTile(this);
        }
        Type = TileType.Parse(reader.GetAttribute("Type"));
    }
}
