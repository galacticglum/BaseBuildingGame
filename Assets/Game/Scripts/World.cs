using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class World : IXmlSerializable
{
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

    public event TileChangedEventHandler TileChanged;
    public void OnTileChanged(TileChangedEventArgs args)
    {
        if (TileChanged != null)
        {
            TileChanged(this, args);
        }
    }

    public event FurnitureCreatedEventHandler FurnitureCreated;
    public void OnFurnitureCreated(FurnitureCreatedEventArgs args)
    {
        if (FurnitureCreated != null)
        {
            FurnitureCreated(this, args);
        }
    }

    public event CharacterCreatedEventHandler CharacterCreated;
    public void OnCharacterCreated(CharacterCreatedEventArgs args)
    {
        if (CharacterCreated != null)
        {
            CharacterCreated(this, args);
        }
    }

    public event InventoryCreatedEventHandler InventoryCreated;
    public void OnInventoryCreated(InventoryCreatedEventArgs args)
    {
        if (InventoryCreated != null)
        {
            InventoryCreated(this, args);
        }
    }

    private Tile[,] tiles;

    public World() { }
    public World(int width, int height)
    {
		Initialize(width, height);
		CreateCharacter(GetTileAt(Width / 2, Height / 2));
	}

    private void Initialize(int width, int height)
    {
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
				tiles[x,y] = new Tile(this, x, y);
                tiles[x, y].TileChanged += OnTileChangedEvent;
				tiles[x,y].Room = OutsideRoom;
			}
		}

		CreateFurniturePrototypes();

        // DEBUGGING ONLY!  REMOVE ME LATER!
        // Create an Inventory Item
        Inventory inventory = new Inventory("Steel Plate", 50, 27);
        Tile tileAt = GetTileAt(Width / 2, Height / 2);
        InventoryManager.PlaceInventory(tileAt, inventory);
        OnInventoryCreated(new InventoryCreatedEventArgs(tileAt.Inventory));

        inventory = new Inventory("Steel Plate", 50, 43);
        tileAt = GetTileAt(Width / 2 + 2, Height / 2);
        InventoryManager.PlaceInventory(tileAt, inventory);
        OnInventoryCreated(new InventoryCreatedEventArgs(tileAt.Inventory));

        inventory = new Inventory("Steel Plate", 50, 50);
        tileAt = GetTileAt(Width / 2 + 1, Height / 2 + 2);
        InventoryManager.PlaceInventory(tileAt, inventory);
        OnInventoryCreated(new InventoryCreatedEventArgs(tileAt.Inventory));
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
        OnCharacterCreated(new CharacterCreatedEventArgs(characterInstance));

		return characterInstance;
	}

    private void CreateFurniturePrototypes()
    {
		FurniturePrototypes = new Dictionary<string, Furniture>();
		FurnitureJobPrototypes = new Dictionary<string, Job>();

		FurniturePrototypes.Add("Wall", 
			new Furniture(
				"Wall",
				0,	// Impassable
				1,  // Width
				1,  // Height
				true, // Links to neighbours and "sort of" becomes part of a large object
				true  // Enclose rooms
			)
		);

		FurnitureJobPrototypes.Add("Wall",
			new Job(null, 
				"Wall",
                FurnitureBehaviours.BuildFurniture, 1f, 
				new[] { new Inventory("Steel Plate", 5, 0) } 
			)
		);

		FurniturePrototypes.Add("Door", 
			new Furniture(
				"Door",
				1,	// Door pathfinding cost
				1,  // Width
				1,  // Height
				false, // Links to neighbours and "sort of" becomes part of a large object
				true  // Enclose rooms
			)
		);

		FurniturePrototypes["Door"].SetParameter("openness", 0);
		FurniturePrototypes["Door"].SetParameter("is_opening", 0);
		FurniturePrototypes["Door"].UpdateBehaviours += FurnitureBehaviours.UpdateDoor;
		FurniturePrototypes["Door"].TryEnter = FurnitureBehaviours.DoorTryEnter;


		FurniturePrototypes.Add("Stockpile", 
			new Furniture(
				"Stockpile",
				1,	// Impassable
				1,  // Width
				1,  // Height
				true, // Links to neighbours and "sort of" becomes part of a large object
				false  // Enclose rooms
			)
		);

		FurniturePrototypes["Stockpile"].UpdateBehaviours += FurnitureBehaviours.UpdateStockpile;
		//furniturePrototypes["Stockpile"].Tint = new Color32(186, 31, 31, 255);
		FurniturePrototypes["Stockpile"].Tint = new Color32(168, 130, 42, 255);
        FurnitureJobPrototypes.Add("Stockpile",
			new Job( 
				null, 
				"Stockpile",
                FurnitureBehaviours.BuildFurniture,
				-1,
				null
			)
		);

        FurniturePrototypes.Add("Oxygen Generator",
            new Furniture(
                "Oxygen Generator",
                10,  // Door pathfinding cost
                2,  // Width
                2,  // Height
                false, // Links to neighbours and "sort of" becomes part of a large object
                false  // Enclose rooms
            )
        );
    }

	public void SetupPathfindingExample()
    {
		int l = Width / 2 - 5;
		int b = Height / 2 - 5;

		for (int x = l-5; x < l + 15; x++)
        {
			for (int y = b-5; y < b + 15; y++)
            {
				tiles[x,y].Type = TileType.Floor;


			    if (x != l && x != l + 9 && y != b && y != b + 9) continue;

			    if(x != l + 9 && y != b + 4)
                {
			        PlaceFurniture("Wall", tiles[x,y]);
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

	public Furniture PlaceFurniture(string type, Tile tile)
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

		if(furnitureInstance.RoomEnclosure)
        {
			Room.CreateRooms(furnitureInstance);
		}

        if (FurnitureCreated == null) return furnitureInstance;
        OnFurnitureCreated(new FurnitureCreatedEventArgs(furnitureInstance));

        if(furnitureInstance.MovementCost != 1)
        {
            InvalidateTileGraph();	
        }

        return furnitureInstance;
	}

    private void OnTileChangedEvent(object sender, TileChangedEventArgs args)
    {
		OnTileChanged(new TileChangedEventArgs(args.Tile));
		InvalidateTileGraph();
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

    public XmlSchema GetSchema()
    {
		return null;
	}

	public void WriteXml(XmlWriter writer)
    {
        // Save info here
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

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
                case "Tiles":
                    {
                        ReadXmlTiles(reader);
                        break;
                    }
                case "Furnitures":
                    {
                        ReadXmlFurnitures(reader);
                        break;
                    }
                case "Characters":
                    {
                        ReadXmlCharacters(reader);
                        break;
                    }
            }
        }
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

            Furniture furniture = PlaceFurniture(reader.GetAttribute("Type"), tiles[x, y]);
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
}
