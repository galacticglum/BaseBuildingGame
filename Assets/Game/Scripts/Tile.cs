using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

public class Tile : IXmlSerializable
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
    public Inventory Inventory { get; protected set; }

    public Room Room { get; set; }

    public Job PendingFurnitureJob;

    public readonly int X;
    public readonly int Y;

    public float MovementCost
    {
        get
        {
            if(Type == TileType.Empty)
            {
                return 0;
            }
            return Furniture == null ? 1 : Furniture.MovementCost;
        }
    }

    public event TileChangedEventHandler TileChanged;
    public void OnTileChanged(TileChangedEventArgs args)
    {
        TileChangedEventHandler tileChanged = TileChanged;
        if (tileChanged != null)
        {
            tileChanged(this, args);
        }
    }

    private TileType type = TileType.Empty;
    public TileType Type
    {
        get { return type; }
        set
        {
            TileType oldType = type;
            type = value;

            if (oldType != type)
            {
                OnTileChanged(new TileChangedEventArgs(this));
            }
        }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="Tile"/> class.
    /// </summary>
    /// <param name="world">A World instance.</param>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public Tile(World world, int x, int y)
    {
        World = world;
        X = x;
        Y = y;
    }

    public bool PlaceFurniture(Furniture furniture)
    {
        if (furniture == null)
        {
            Furniture = null;
            return true;
        }

        if (Furniture != null)
        {
            Debug.LogError("Tile::PlaceFurniture: trying to assign a furniture to a tile that already has one!");
            return false;
        }

        Furniture = furniture;
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
            // There's already inventory on this tile, maybe we can combine a stack?
            if (Inventory.Type != inventory.Type)
            {
                Debug.LogError("Tile::PlaceInventory: trying to assign a inventory to a tile that already has inventory of different type!");
                return false;
            }

            int stackSize = inventory.StackSize;
            if (Inventory.StackSize + stackSize > Inventory.MaxStackSize)
            {
                stackSize = Inventory.MaxStackSize - Inventory.StackSize;
            }

            Inventory.StackSize += stackSize;
            Inventory.StackSize -= stackSize;

            return true;
        }

        Inventory = inventory.Clone();
        Inventory.Tile = this;
        inventory.StackSize = 0;

        return true;
    }

    public bool IsNeighbour(Tile tile, bool diagonal = false)
    {
        return (Mathf.Abs(X - tile.X) + Mathf.Abs(Y - tile.Y)) == 1 || (diagonal && (Mathf.Abs(X - tile.X) == 1 && Mathf.Abs(Y - tile.Y) == 1));
    }

    public Tile[] GetNeighbours(bool diagonal = false)
    {
        Tile[] neighbours = new Tile[diagonal ? 8 : 4];

        neighbours[0] = World.GetTileAt(X, Y + 1);
        neighbours[1] = World.GetTileAt(X + 1, Y);
        neighbours[2] = World.GetTileAt(X, Y - 1);
        neighbours[3] = World.GetTileAt(X - 1, Y);

        if (!diagonal) return neighbours;
        neighbours[4] = World.GetTileAt(X + 1, Y + 1);
        neighbours[5] = World.GetTileAt(X + 1, Y - 1); 
        neighbours[6] = World.GetTileAt(X - 1, Y - 1); 
        neighbours[7] = World.GetTileAt(X - 1, Y + 1);

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

        return TileEnterability.Yes;
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
