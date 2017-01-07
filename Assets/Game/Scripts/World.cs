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

	public List<Character> Characters { get; protected set; }
    public List<Furniture> Furnitures { get; protected set; }
    public List<Room> Rooms { get; protected set; }

    public Dictionary<string, Furniture> FurniturePrototypes { get; protected set; }
    public Dictionary<string, Job> FurnitureJobPrototypes { get; protected set; }

    public JobQueue JobQueue { get; protected set; }
    public InventoryManager InventoryManager { get; protected set; }
    public Room OutsideRoom { get { return Rooms[0]; } }

    public Callback<TileEventArgs> TileChanged;
    public Callback<FurnitureEventArgs> FurnitureCreated;
    public Callback<CharacterEventArgs> CharacterCreated;
    public Callback<InventoryEventArgs> InventoryCreated;

    private Tile[,] tiles;

    public World() { }
    public World(int width, int height)
    {
		Initialize(width, height);
		CreateCharacter(GetTileAt(Width / 2, Height / 2));
		CreateCharacter(GetTileAt(Width / 2 + 2, Height / 2));
		CreateCharacter(GetTileAt(Width / 2 + 4, Height / 2));
    }

    private void Initialize(int width, int height)
    {
        Current = this;

        TileChanged = new Callback<TileEventArgs>();
        FurnitureCreated = new Callback<FurnitureEventArgs>();
        CharacterCreated = new Callback<CharacterEventArgs>();
        InventoryCreated = new Callback<InventoryEventArgs>(); 

		Width = width;
		Height = height;

		tiles = new Tile[Width,Height];
        JobQueue = new JobQueue();

        Characters = new List<Character>();
        Furnitures = new List<Furniture>();
        InventoryManager = new InventoryManager();
        Rooms = new List<Room>
        {
            new Room()
        };

        for (int x = 0; x < Width; x++)
        {
			for (int y = 0; y < Height; y++)
            {
				tiles[x,y] = new Tile(x, y);
                tiles[x, y].TileChanged += OnTileChangedEvent;
				tiles[x,y].Room = OutsideRoom;
			}
		}

        CreateFurniturePrototypes();
    }

	public void Update(float deltaTime)
    {
		foreach(Character character in Characters)
        {
			character.Update(deltaTime);
		}

		foreach(Furniture furniture in Furnitures)
        {
			furniture.Update(deltaTime);
		}
	}

    public void AddRoom(Room room)
    {
        Rooms.Add(room);
    }

    public void DeleteRoom(Room room)
    {
        if (room == OutsideRoom)
        {
            Debug.LogError("World::DeleteRoom: tried to delete the 'outside' room!");
            return;
        }

        Rooms.Remove(room);
        room.ClearTiles();
    }

    public Character CreateCharacter(Tile tile)
    {
		Character characterInstance = new Character( tile ); 
		Characters.Add(characterInstance);
        CharacterCreated.Invoke(new CharacterEventArgs(characterInstance));

		return characterInstance;
	}

    private void CreateFurniturePrototypes()
    {
        FurniturePrototypes = new Dictionary<string, Furniture>();
        FurnitureJobPrototypes = new Dictionary<string, Job>();

        string filePath = Path.Combine(Application.streamingAssetsPath, Path.Combine("Data", "Furnitures.xml"));
        XmlTextReader reader = new XmlTextReader(new StringReader(File.ReadAllText(filePath)));

        if (reader.ReadToDescendant("Furnitures"))
        {
            if(reader.ReadToDescendant("Furniture"))
            {
                do
                {
                    Furniture furniturePrototype = new Furniture();
                    furniturePrototype.ReadXmlPrototype(reader);
                    FurniturePrototypes[furniturePrototype.Type] = furniturePrototype;
                }
                while (reader.ReadToNextSibling("Furniture"));
            }
            else
            {
                Debug.LogError("World::CreateFurniturePrototypes: Could not find 'Furniture' XML element in furniture prototype definition file!");
            }
        }
        else
        {
            Debug.LogError("World::CreateFurniturePrototypes: Could not find 'Furnitures' XML element in furniture prototype definition file!");
        }
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
			        PlaceFurniture("Furniture_SteelWall", tiles[x,y]);
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

	public Furniture PlaceFurniture(string type, Tile tile, bool doRoomFloodFill = true)
    {
		if( FurniturePrototypes.ContainsKey(type) == false )
        {
			Debug.LogError("World::PlaceFurniture: Dictionary<string, Furniture> 'furniturePrototypes' does not contain a prototype for key: " + type + ".");
			return null;
		}

		Furniture furnitureInstance = Furniture.Place(FurniturePrototypes[type], tile);

		if(furnitureInstance == null)
        {
			return null;
		}

		Furnitures.Add(furnitureInstance);
        furnitureInstance.FurnitureRemoved += OnFurnitureRemoved;

		if(doRoomFloodFill && furnitureInstance.RoomEnclosure)
        {
			Room.CalculateRooms(furnitureInstance.Tile);
		}

        if (FurnitureCreated == null) return furnitureInstance;
        FurnitureCreated.Invoke(new FurnitureEventArgs(furnitureInstance));

        if(furnitureInstance.MovementCost != 1)
        {
            InvalidateTileGraph();	
        }

        return furnitureInstance;
	}

    private void OnTileChangedEvent(object sender, TileEventArgs args)
    {
		TileChanged.Invoke(new TileEventArgs(args.Tile));
		InvalidateTileGraph();
	}

    private void OnFurnitureRemoved(object sender, FurnitureEventArgs args)
    {
        Furnitures.Remove(args.Furniture);
    }

	public void InvalidateTileGraph()
    {
		TileGraph = null;
	}

	public bool IsFurniturePlacementValid(string type, Tile tile)
    {
		return FurniturePrototypes[type].IsValidPosition(tile);
	}

	public Furniture GetFurniture(string type)
    {
        if (FurniturePrototypes.ContainsKey(type)) return FurniturePrototypes[type];
        Debug.LogError("World::GetFurniture: No furniture with type: " + type + ".");
        return null;
    }

    public int GetRoomIndex(Room room)
    {
        return Rooms.IndexOf(room);
    }

    public Room GetRoom(int index)
    {
        if (index < 0 || index > Rooms.Count - 1)
        {
            return null;
        }

        return Rooms[index];
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

        writer.WriteStartElement("Rooms");
        foreach (Room room in Rooms)
        {
            if (room == OutsideRoom) continue;

            writer.WriteStartElement("Room");
            room.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

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

        writer.WriteStartElement("Furnitures");
        foreach (Furniture furniture in Furnitures)
        {
            writer.WriteStartElement("Furniture");
            furniture.WriteXml(writer);
            writer.WriteEndElement();

        }
        writer.WriteEndElement();

        writer.WriteStartElement("Characters");
        foreach (Character c in Characters)
        {
            writer.WriteStartElement("Character");
            c.WriteXml(writer);
            writer.WriteEndElement();

        }
        writer.WriteEndElement();
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
                    ReadXmlRooms(reader);
                    break;

                case "Tiles":
                    ReadXmlTiles(reader);
                    break;
                    
                case "Furnitures":
                    ReadXmlFurnitures(reader);
                    break;

                case "Characters":
                    ReadXmlCharacters(reader);
                    break;
                    
            }
        }


        // DEBUGGING ONLY!  REMOVE ME LATER!
        // Create an Inventory Item
        Inventory inventory = new Inventory("Steel Plate", 50, 50);
        Tile tileAt = GetTileAt(Width / 2, Height / 2);
        InventoryManager.PlaceInventory(tileAt, inventory);
        InventoryCreated.Invoke(new InventoryEventArgs(tileAt.Inventory));

        inventory = new Inventory("Steel Plate", 50, 40);
        tileAt = GetTileAt(Width / 2 + 2, Height / 2);
        InventoryManager.PlaceInventory(tileAt, inventory);
        InventoryCreated.Invoke(new InventoryEventArgs(tileAt.Inventory));

        inventory = new Inventory("Steel Plate", 50, 10);
        tileAt = GetTileAt(Width / 2 + 1, Height / 2 + 2);
        InventoryManager.PlaceInventory(tileAt, inventory);
        InventoryCreated.Invoke(new InventoryEventArgs(tileAt.Inventory));
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

    private void ReadXmlFurnitures(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Furniture")) return;

        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            Furniture furniture = PlaceFurniture(reader.GetAttribute("Type"), tiles[x, y], false);
            furniture.ReadXml(reader);
        }
        while (reader.ReadToNextSibling("Furniture"));
    }

    private void ReadXmlCharacters(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Character")) return;

        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            Character character = CreateCharacter(tiles[x, y]);
            character.ReadXml(reader);
        }
        while (reader.ReadToNextSibling("Character"));
    }

    private void ReadXmlRooms(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Room")) return;

        do
        {
            Room room = new Room();
            Rooms.Add(room);
            room.ReadXml(reader);
        }
        while (reader.ReadToNextSibling("Room"));
    }
}
