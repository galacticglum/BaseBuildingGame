using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

public class World : IXmlSerializable
{
    public TileGraph TileGraph { get; set; }
    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public event TileChangedEventHandler TileChanged;
    public void OnTileChanged(TileChangedEventArgs args)
    {
        TileChangedEventHandler tileChanged = TileChanged;
        if (tileChanged != null)
        {
            tileChanged(this, args);
        }
    }

    public event FurnitureCreatedEventHandler FurnitureCreated;
    public void OnFurnitureCreated(FurnitureCreatedEventArgs args)
    {
        FurnitureCreatedEventHandler furnitureCreated = FurnitureCreated;
        if (furnitureCreated != null)
        {
            furnitureCreated(this, args);
        }
    }

    public event CharacterCreatedEventHandler CharacterCreated;
    public void OnCharacterCreated(CharacterCreatedEventArgs args)
    {
        CharacterCreatedEventHandler characterCreated = CharacterCreated;
        if (characterCreated != null)
        {
            characterCreated(this, args);
        }
    }

    public event InventoryCreatedEventHandler InventoryCreated;
    public void OnInventoryCreated(InventoryCreatedEventArgs args)
    {
        InventoryCreatedEventHandler inventoryCreated = InventoryCreated;
        if (inventoryCreated != null)
        {
            inventoryCreated(this, args);
        }
    }

    public List<Furniture> Furnitures { get; protected set; }
    public List<Character> Characters { get; protected set; }
    public List<Room> Rooms { get; protected set; }

    public InventoryManager InventoryManager { get; protected set; }

    public Room OutsideRoom
    {
        get { return Rooms[0]; }
    }

    public JobQueue JobQueue;

    private Tile[,] tiles;

    private Dictionary<string, Furniture> furniturePrototypes;
    public Dictionary<string, Job> FurnitureJobPrototypes { get; set; }

    public World() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class.
    /// </summary>
    /// <param name="width">Width in tiles.</param>
    /// <param name="height">Height in tiles.</param>
    public World(int width, int height)
    {
        Initialize(width, height);
    }

    private void Initialize(int width, int height)
    {
        Width = width;
        Height = height;

        tiles = new Tile[Width, Height];
        JobQueue = new JobQueue();

        Characters = new List<Character>();
        Furnitures = new List<Furniture>();
        InventoryManager = new InventoryManager();
        Rooms = new List<Room>()
        {
            new Room() // The "outside" room
        };

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
                tiles[x, y].TileChanged += OnTileChangedEvent;
                tiles[x, y].Room = OutsideRoom; // value 'Rooms[0]' is always going to be the outside room!
            }
        }

        CreateCharacter(GetTileAt(Width / 2, Height / 2));
        CreateFurniturePrototypes();
    }

    public void Update(float deltaTime)
    {
        foreach (Character character in Characters)
        {
            character.Update(deltaTime);
        }

        foreach (Furniture furniture in Furnitures)
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
        Character character = new Character(tile);
        OnCharacterCreated(new CharacterCreatedEventArgs(character));
        Characters.Add(character);
        return character;
    }

    private void CreateFurniturePrototypes()
    {
        // TODO: Furniture defintions in a file (XML??)

        furniturePrototypes = new Dictionary<string, Furniture>
        {
            {
                "Wall", new Furniture(
                    "Wall",
                    0, // Impassable
                    1, // Width
                    1, // Height
                    true, // Links to neighbours and "sort of" becomes part of a large object
                    true // Enclose rooms
                )
            },
            {
                "Door", new Furniture(
                    "Door",
                    1, // Door pathfinding cost
                    1, // Width
                    1, // Height
                    false, // Links to neighbours and "sort of" becomes part of a large object
                    true // Enclose rooms
                )
            }
        };

        FurnitureJobPrototypes = new Dictionary<string, Job>
        {
            {
                "Wall",
                new Job(null, "Wall", FurnitureBehaviours.BuildFurniture, 1f, new[] { new Inventory("Steel Plate", 5, 0) })
            }
        };

        furniturePrototypes["Door"].SetParamater("openness", 0);
        furniturePrototypes["Door"].SetParamater("isOpening", 0);
        furniturePrototypes["Door"].UpdateBehaviours += FurnitureBehaviours.UpdateDoor;
        furniturePrototypes["Door"].TryEnter += FurnitureBehaviours.DoorTryEnter;
    }

    public void SetupPathfindingExample()
    {
        int l = Width / 2 - 5;
        int b = Height / 2 - 5;

        for (int x = l - 5 ; x < l + 15; x++)
        {
            for (int y = b - 5; y < b + 15; y++)
            {
                tiles[x, y].Type = TileType.Floor;

                if (x != l && x != (l + 9) && y != b && y != (b + 9)) continue;
                if (x != (l + 9) && y != (b + 4))
                {
                    PlaceFurniture("Wall", tiles[x, y]);
                }
            }
        }
    }

    /// <summary>
    /// Gets the tile data at x and y.
    /// </summary>
    /// <returns>The <see cref="Tile"/>.</returns>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public Tile GetTileAt(int x, int y)
    {
        if (x >= Width || x < 0 || y >= Height || y < 0)
        {
            return null;
        }
        return tiles[x, y];
    }

    public Furniture PlaceFurniture(string type, Tile t)
    {
        // TODO: This function assumes 1x1 tiles -- change this later!

        if (furniturePrototypes.ContainsKey(type) == false)
        {
            Debug.LogError("World::PlaceFurniture: Dictionary<string, Furniture> 'furniturePrototypes' does not contain a prototype for key: " + type);
            return null;
        }

        Furniture furniture = Furniture.Place(furniturePrototypes[type], t);
        if (furniture == null)
        {
            return null;
        }

        Furnitures.Add(furniture);
        if (furniture.RoomEnclosure)
        {
            Room.CreateRooms(furniture);
        }

        if (FurnitureCreated == null) return furniture;

        OnFurnitureCreated(new FurnitureCreatedEventArgs(furniture));
        if (furniture.MovementCost != 1)
        {
            InvalidateTileGraph();
        }

        return furniture;
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

    public bool IsFurniturePlacementValid(string furnitureType, Tile tile)
    {
        return furniturePrototypes[furnitureType].IsValidPosition(tile);
    }

    public Furniture GetFurniture(string objectType)
    {
        if (furniturePrototypes.ContainsKey(objectType)) return furniturePrototypes[objectType];
        Debug.LogError("World::GetFurniture: No furniture with type: " + objectType);
        return null;
    }

    // Saving and Loading
    public XmlSchema GetSchema()
    {
        return null; 
    }

    public void WriteXml(XmlWriter writer)
    {
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
        foreach (Character character in Characters)
        {
            writer.WriteStartElement("Characters");
            character.WriteXml(writer);
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

        // DEBUGGING ONLY! REMOVE ME!!!!
        Inventory inventory = new Inventory
        {
            StackSize = 10
        };

        Tile tile = GetTileAt(width / 2, height / 2);
        InventoryManager.PlaceInventory(tile, inventory);
        OnInventoryCreated(new InventoryCreatedEventArgs(tile.Inventory));
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
