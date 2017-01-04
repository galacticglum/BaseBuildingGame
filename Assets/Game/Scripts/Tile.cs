using UnityEngine;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Tile : IXmlSerializable
{
    public Tile North
    {
        get { return World.Current.GetTileAt(X, Y + 1); }
    }

    public Tile South
    {
        get { return World.Current.GetTileAt(X, Y - 1); }
    }

    public Tile East
    {
        get { return World.Current.GetTileAt(X + 1, Y); }
    }

    public Tile West
    {
        get { return World.Current.GetTileAt(X - 1, Y); }
    }

    public Furniture Furniture { get; protected set; }
    public Inventory Inventory { get; set; }

	public Room Room { get; set; }
	public Job PendingFurnitureJob { get; set; }

	public readonly int X;
    public readonly int Y;

    private TileType type = TileType.Empty;
    public TileType Type
    {
        get { return type; }
        set
        {
            TileType oldType = type;
            type = value;

            if (oldType == type) return;
            OnTileChanged(new TileEventArgs(this));
        }
    }

    public float MovementCost
    {
        get
        {
            if (Type == TileType.Empty)
            {
                return 0;
            }

            return Furniture == null ? 1 : Furniture.MovementCost;
        }
    }

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
	}

	public bool PlaceFurniture(Furniture furniture)
	{
        if (furniture == null)
        {
            return RemoveFurniture();
        }

	    if (furniture.IsValidPosition(this) == false)
	    {
            Debug.LogError("Tile::PlaceFurniture: trying to assign a furniture to an invald tile!");
	        return false;
	    }

        for (int x = X; x < X + furniture.Width; x++)
        {
            for (int y = Y; y < Y + furniture.Height; y++)
            {
                Tile tileAt = World.Current.GetTileAt(x, y);
                tileAt.Furniture = furniture;
            }
        }

        return true;
    }

    public bool RemoveFurniture()
    {
        if (Furniture == null) return false;

        int width = Furniture.Width;
        int height = Furniture.Height;

        for (int x = X; x < X + width; x++)
        {
            for (int y = Y; y < Y + height; y++)
            {
                Tile tileAt = World.Current.GetTileAt(x, y);
                tileAt.Furniture = null;
            }
        }

        return true;
    }

    public bool PlaceInventory(Inventory inventory)
    {
		if(inventory == null)
        {
			Inventory = null;
			return true;
		}

		if(Inventory != null)
        {
			if(Inventory.Type != inventory.Type)
            {
				Debug.LogError("Tile::PlaceInventory: trying to assign a inventory to a tile that already has inventory of different type!");
				return false;
			}

			int stackSize = inventory.StackSize;
			if(Inventory.StackSize + stackSize > Inventory.MaxStackSize)
            {
				stackSize = Inventory.MaxStackSize - Inventory.StackSize;
			}

			Inventory.StackSize += stackSize;
			inventory.StackSize -= stackSize;

			return true;
		}

		Inventory = inventory.Clone();
		Inventory.Tile = this;
		inventory.StackSize = 0;

		return true;
	}

	public bool IsNeighbour(Tile tile, bool diagonal = false)
    {
		return Mathf.Abs(X - tile.X ) + Mathf.Abs( Y - tile.Y ) == 1 || (diagonal && Mathf.Abs( X - tile.X ) == 1 && Mathf.Abs( Y - tile.Y ) == 1);
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

    public TileEnterability TryEnter()
    {
        if (MovementCost == 0)
        {
            return TileEnterability.Never;
        }

        return Furniture != null ? Furniture.TryEnter() : TileEnterability.Immediate;
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", X.ToString());
        writer.WriteAttributeString("Y", Y.ToString());
        writer.WriteAttributeString("Type", ((int)Type).ToString());
        writer.WriteAttributeString("RoomIndex", Room == null ? "-1" : Room.Index.ToString());
    }

    public void ReadXml(XmlReader reader)
    {
        Type = (TileType)int.Parse(reader.GetAttribute("Type"));
        Room = World.Current.GetRoom(int.Parse(reader.GetAttribute("RoomIndex")));
    }
}
