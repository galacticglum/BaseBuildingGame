using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class World : IXmlSerializable
{
    public static World Current { get; protected set; }

    public TileGraph TileGraph { get; set; }
    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public RoomManager RoomManager { get; protected set; }
    public CharacterManager CharacterManager { get; protected set; }
    public InventoryManager InventoryManager { get; protected set; }
    public FurnitureManager FurnitureManager { get; protected set; }
    public Dictionary<string, Job> FurnitureJobPrototypes { get; protected set; }

    public JobQueue JobQueue { get; protected set; }

    public LuaEventManager EventManager { get; set; }

    public event TileChangedEventHandler TileChanged;
    public void OnTileChanged(TileEventArgs args)
    {
        TileChangedEventHandler tileChanged = TileChanged;
        if (tileChanged != null)
        {
            tileChanged(this, args);
        }

        EventManager.Trigger("TileChanged", this, args);
    }

    private Tile[,] tiles;

    public World() { }
    public World(int width, int height)
    {
		Initialize(width, height);
		CharacterManager.Create(GetTileAt(Width / 2, Height / 2));
        CharacterManager.Create(GetTileAt(Width / 2 + 2, Height / 2));
        CharacterManager.Create(GetTileAt(Width / 2 + 4, Height / 2));
    }

    private void Initialize(int width, int height)
    {
        // DEBUG
        CharacterNameManager.Load(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, Path.Combine("Data", "CharacterNames.xml"))));

        Current = this;

        EventManager = new LuaEventManager("TileChanged", "InventoryCreated");

		Width = width;
		Height = height;

		tiles = new Tile[Width,Height];
        JobQueue = new JobQueue();

        RoomManager = new RoomManager();
        CharacterManager = new CharacterManager();
        InventoryManager = new InventoryManager();

        for (int x = 0; x < Width; x++)
        {
			for (int y = 0; y < Height; y++)
            {
				tiles[x,y] = new Tile(x, y);
                tiles[x, y].TileChanged += OnTileChangedEvent;
				tiles[x,y].Room = RoomManager.OutsideRoom;
			}
		}

        CreateFurniturePrototypes();
    }

	public void Update(float deltaTime)
    {
        CharacterManager.Update(deltaTime);
        FurnitureManager.Update(deltaTime);
	}

    private void CreateFurniturePrototypes()
    {
        FurnitureManager = new FurnitureManager();
        FurnitureJobPrototypes = new Dictionary<string, Job>();

        string filePath = Path.Combine(Application.streamingAssetsPath, Path.Combine("Data", "Furnitures.xml"));
        PrototypeManager.Furnitures.Load(File.ReadAllText(filePath));
    }

	public void SetupPathfindingExample()
    {
		int length = Width / 2 - 5;
		int sBase = Height / 2 - 5;

		for (int x = length - 5; x < length + 15; x++)
        {
			for (int y = sBase - 5; y < sBase + 15; y++)
            {
				tiles[x,y].Type = TileType.Floor;
			    if (x != length && x != length + 9 && y != sBase && y != sBase + 9) continue;
			    if(x != length + 9 && y != sBase + 4)
                {
			        FurnitureManager.Place("Furniture_SteelWall", tiles[x,y]);
			    }
			}
		}
    }

    public Tile GetTileAt(int x, int y)
    {
		if( x >= Width || x < 0 || y >= Height || y < 0)
        {
			return null;
		}

		return tiles[x, y];
	}

    private void OnTileChangedEvent(object sender, TileEventArgs args)
    {
		OnTileChanged(new TileEventArgs(args.Tile));
		InvalidateTileGraph();
	}

	public void InvalidateTileGraph()
    {
		TileGraph = null;
	}

	public bool IsFurniturePlacementValid(string type, Tile tile)
    {
		return PrototypeManager.Furnitures[type].IsValidPosition(tile);
	}

	public Furniture GetFurniture(string type)
    {
        if (PrototypeManager.Furnitures.Contains(type)) return PrototypeManager.Furnitures[type];
        Debug.LogError("World::GetFurniture: No furniture with type: " + type + ".");
        return null;
    }

    public XmlSchema GetSchema()
    {
		return null;
	}

	public void WriteXml(XmlWriter writer)
    {
        // Save info here
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

        RoomManager.WriteXml(writer);

        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (tiles[x, y].Type == TileType.Empty) continue;
                writer.WriteStartElement("Tile");
                tiles[x, y].WriteXml(writer);
                writer.WriteEndElement();
            }
        }
        writer.WriteEndElement();

        FurnitureManager.WriteXml(writer);
        CharacterManager.WriteXml(writer);
	}

    public void ReadXml(XmlReader reader)
    {
        int width = int.Parse(reader.GetAttribute("Width"));
        int height = int.Parse(reader.GetAttribute("Height"));

        Initialize(width, height);

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Rooms":
                    RoomManager.ReadXml(reader);
                    break;

                case "Tiles":
                    ReadXmlTiles(reader);
                    break;
                    
                case "Furnitures":
                    FurnitureManager.ReadXml(reader);
                    break;

                case "Characters":
                    CharacterManager.ReadXml(reader);
                    break;
                    
            }
        }


        // DEBUGGING ONLY!  REMOVE ME LATER!
        // Create an Inventory Item
        Inventory inventory = new Inventory("Steel Plate", 50, 50);
        Tile tileAt = GetTileAt(Width / 2, Height / 2);
        InventoryManager.Place(tileAt, inventory);
        InventoryManager.OnInventoryCreated(new InventoryEventArgs(tileAt.Inventory));

        inventory = new Inventory("Steel Plate", 50, 40);
        tileAt = GetTileAt(Width / 2 + 2, Height / 2);
        InventoryManager.Place(tileAt, inventory);
        InventoryManager.OnInventoryCreated(new InventoryEventArgs(tileAt.Inventory));

        inventory = new Inventory("Steel Plate", 50, 10);
        tileAt = GetTileAt(Width / 2 + 1, Height / 2 + 2);
        InventoryManager.Place(tileAt, inventory);
        InventoryManager.OnInventoryCreated(new InventoryEventArgs(tileAt.Inventory));
    }

    private void ReadXmlTiles(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Tile")) return;

        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));
            tiles[x, y].ReadXml(reader);
        }
        while (reader.ReadToNextSibling("Tile"));
    }
}
