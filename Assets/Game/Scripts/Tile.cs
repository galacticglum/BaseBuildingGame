using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Tile :IXmlSerializable
{
    public Tile North
    {
        get { return World.GetTileAt(X, Y + 1); }
    }

    public Tile South
    {
        get { return World.GetTileAt(X, Y - 1); }
    }

    public Tile East
    {
        get { return World.GetTileAt(X + 1, Y); }
    }

    public Tile West
    {
        get { return World.GetTileAt(X - 1, Y); }
    }

    public World World { get; protected set; }
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
            OnTileChanged(new TileChangedEventArgs(this));
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
    public void OnTileChanged(TileChangedEventArgs args)
    {
        if (TileChanged != null)
        {
            TileChanged(this, args);
        }
    }

    public Tile( World world, int x, int y)
    {
		World = world;
		X = x;
		Y = y;
	}

	public bool PlaceFurniture(Furniture furniture)
    {
		if(furniture == null)
        {
			Furniture = null;
			return true;
		}

		if(Furniture != null)
        {
			Debug.LogError("Tile::PlaceFurniture: trying to assign a furniture to a tile that already has one!");
			return false;
		}

		Furniture = furniture;
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
        Tile tileAt = World.GetTileAt(X, Y + 1);

		neighbours[0] = tileAt;	
		tileAt = World.GetTileAt(X + 1, Y);
		neighbours[1] = tileAt;	
		tileAt = World.GetTileAt(X, Y - 1);
		neighbours[2] = tileAt;	
		tileAt = World.GetTileAt(X - 1, Y);
		neighbours[3] = tileAt;

        if (diagonal != true) return neighbours;
        tileAt = World.GetTileAt(X + 1, Y + 1);
        neighbours[4] = tileAt;	
        tileAt = World.GetTileAt(X + 1, Y - 1);
        neighbours[5] = tileAt;	
        tileAt = World.GetTileAt(X - 1, Y - 1);
        neighbours[6] = tileAt;
        tileAt = World.GetTileAt(X - 1, Y + 1);
        neighbours[7] = tileAt;	

        return neighbours;
	}

    public TileEnterability TryEnter()
    {
        if (MovementCost == 0)
        {
            return TileEnterability.Never;
        }

        if (Furniture != null && Furniture.TryEnter != null)
        {
            return Furniture.TryEnter(Furniture);
        }

        return TileEnterability.Immediate;
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
    }

    public void ReadXml(XmlReader reader)
    {
        Type = (TileType)int.Parse(reader.GetAttribute("Type"));
    }
}
