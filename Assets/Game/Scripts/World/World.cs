using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class World : IXmlSerializable
{
    public static World Current { get; private set; }

    // TODO: Save with the world data?
    public readonly string gameVersion = "Someone_will_come_up_with_a_proper_naming_scheme_later";

    public TileGraph TileGraph { get; set; }
    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public List<Character> Characters { get; private set; }
    public List<Furniture> Furnitures { get; private set; }
    public List<Room> Rooms { get; private set; }
    public InventoryManager InventoryManager { get; private set; }
    public PowerSystem PowerSystem { get; private set; }
    public Temperature Temperature { get; private set; }
    public Material Skybox { get; private set; } // TODO: Move me to somewhere more appropriate. World Controller??

    public Dictionary<string, Furniture> FurniturePrototypes { get; private set; }
    public Dictionary<string, Job> FurnitureJobPrototypes { get; private set; }
    public Dictionary<string, Need> NeedPrototypes { get; private set; }
    public Dictionary<string, InventoryPrototype> InventoryPrototypes { get; private set; }
    public Dictionary<string, TraderPrototype> TraderPrototypes { get; private set; }
    public List<Quest> Quests { get; private set; }

    public Room OutsideRoom { get { return Rooms != null ? Rooms[0] : null; } }
    public Tile CentreTile { get { return GetTileAt(Width / 2, Height / 2); } }

    public JobQueue JobQueue { get; private set; }
    public JobQueue JobWaitingQueue { get; private set; }

    public event FurnitureCreatedEventHandler FurnitureCreated;
    public void OnFurnitureCreated(FurnitureEventArgs args)
    {
        FurnitureCreatedEventHandler furnitureCreated = FurnitureCreated;
        if (furnitureCreated != null)
        {
            furnitureCreated(this, args);
        }
    }

    public event CharacterCreatedEventHandler CharacterCreated;
    public void OnCharacterCreated(CharacterEventArgs args)
    {
        CharacterCreatedEventHandler characterCreated = CharacterCreated;
        if (characterCreated != null)
        {
            characterCreated(this, args);
        }
    }


    public event InventoryCreatedEventHandler InventoryCreated;
    public void OnInventoryCreated(InventoryEventArgs args)
    {
        InventoryCreatedEventHandler inventoryCreated = InventoryCreated;
        if (inventoryCreated != null)
        {
            inventoryCreated(this, args);
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

    private Tile[,] tiles;

    public World() { }
    public World(int width, int height)
    {
        Initialize(width, height);

        WorldGenerator.Generate(this, UnityEngine.Random.Range(0, int.MaxValue));
        CreateCharacter(GetTileAt(Width / 2, Height / 2));
    }

    private void Initialize(int width, int height)
    {
        // TODO: Do we need to do any cleanup of the old world?
        Current = this;

        Width = width;
        Height = height;
        
        TileType.Load();
        tiles = new Tile[Width, Height];
        JobQueue = new JobQueue();
        JobWaitingQueue = new JobQueue();

        Rooms = new List<Room>
        {
            new Room()
        };

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                tiles[x, y] = new Tile(x, y);
                tiles[x, y].TileChanged += OnTileChangedEvent;
                tiles[x, y].Room = OutsideRoom; // Rooms 0 is always going to be outside, and that is our default room
            }
        }

        CreateFurniturePrototypes();
        CreateNeedPrototypes ();
        CreateInventoryPrototypes();
        CreateTraderPrototypes();
        CreateQuests();

        Characters = new List<Character>();
        Furnitures = new List<Furniture>();
        InventoryManager = new InventoryManager();
        PowerSystem = new PowerSystem();
        Temperature = new Temperature(Width, Height);
        LoadSkybox();
    }

    private void LoadSkybox(string name = null)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(Application.dataPath, "Game/Resources/Skyboxes"));
        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
        }

        FileInfo[] files = directoryInfo.GetFiles("*.mat", SearchOption.AllDirectories);
        if (files.Length > 0)
        {
            FileInfo file = null;
            if (!string.IsNullOrEmpty(name))
            {
                foreach (FileInfo fileInfo in files)
                {
                    if (!name.Equals(fileInfo.Name.Remove(fileInfo.Name.LastIndexOf(".", StringComparison.Ordinal)))) continue;
                    file = fileInfo;
                    break;
                }
            }

            // Maybe we passed in a name that doesn't exist? In that case, pick a random skybox.
            if (file == null)
            {
                file = files[(int)(UnityEngine.Random.value * files.Length)];
            }

            if (!string.IsNullOrEmpty(file.DirectoryName))
            {
                string resourcePath = Path.Combine(file.DirectoryName.Substring(file.DirectoryName.IndexOf("Skyboxes", StringComparison.Ordinal)), file.Name);
                if (resourcePath.Contains("."))
                {
                    resourcePath = resourcePath.Remove(resourcePath.LastIndexOf(".", StringComparison.Ordinal));
                }

                Skybox = Resources.Load<Material>(resourcePath);
            }
            RenderSettings.skybox = Skybox;
        }
        else
        {
            Debug.LogWarning("No skyboxes detected! Falling back to black.");
        }
    }

    public void Update(float deltaTime)
    {
        // ReSharper disable once ForCanBeConvertedToForeach;
        // we can't use a foreach loop since the collection is 
        // modified whilst iterating. 
        for (int i = 0; i < Characters.Count; i++)
        {
            Characters[i].Update(deltaTime);
        }

        foreach (Furniture furniture in Furnitures)
        {
            furniture.Update(deltaTime);
        }

        Temperature.Update();
    }

    public Character CreateCharacter(Tile tile)
    {
        Character character = new Character(tile);
        InitializeCharacter(character);
        return character;
    }

    public Character CreateCharacter(Tile tile, Color colour)
    {
        Character character = new Character(tile, colour);
        InitializeCharacter(character);
        return character;
    }

    private void InitializeCharacter(Character character)
    {
        // TODO: Make character names Xml
        string filePath = Path.Combine(Application.streamingAssetsPath, "Data");
        filePath = Path.Combine(filePath, "CharacterNames.txt");

        string[] names = File.ReadAllLines(filePath);
        character.Name = names[UnityEngine.Random.Range(0, names.Length - 1)];
        Characters.Add(character);

        OnCharacterCreated(new CharacterEventArgs(character));
    }

    public Tile GetTileAt(int x, int y)
    {
        if (x >= Width || x < 0 || y >= Height || y < 0)
        {
            return null;
        }

        return tiles[x, y];
    }

    public Tile GetCentreTileWithNoInventory(int maxOffset)
    {
        for (int offset = 0; offset <= maxOffset; offset++)
        {
            int offsetX;
            int offsetY;
            Tile tileAt;

            for (offsetX = -offset; offsetX <= offset; offsetX++)
            {
                offsetY = offset;
                tileAt = GetTileAt(Width / 2 + offsetX, Height / 2 + offsetY);
                if (tileAt.Inventory == null)
                {
                    return tileAt;
                }

                offsetY = -offset;
                tileAt = GetTileAt(Width / 2 + offsetX, Height / 2 + offsetY);
                if (tileAt.Inventory == null)
                {
                    return tileAt;
                }
            }

            for (offsetY = -offset; offsetY <= offset; offsetY++)
            {
                offsetX = offset;
                tileAt = GetTileAt(Width / 2 + offsetX, Height / 2 + offsetY);
                if (tileAt.Inventory == null)
                {
                    return tileAt;
                }

                offsetX = -offset;
                tileAt = GetTileAt(Width / 2 + offsetX, Height / 2 + offsetY);
                if (tileAt.Inventory == null)
                {
                    return tileAt;
                }
            }
        }

        return null;
    }

    public Furniture PlaceFurniture(string type, Tile tile, bool floodFill = true)
    {
        if (FurniturePrototypes.ContainsKey(type) == false)
        {
            Debug.LogError("furniturePrototypes doesn't contain a proto for key: " + type);
            return null;
        }

        Furniture furnitureInstance = Furniture.Place(FurniturePrototypes[type], tile);
        if (furnitureInstance == null)
        {
            return null;
        }

        furnitureInstance.FurnitureRemoved += OnFurnitureRemoved;
        Furnitures.Add(furnitureInstance);

        // Do we need to recalculate our rooms?
        if (floodFill && furnitureInstance.RoomEnclosure)
        {
            Room.CalculateRooms(furnitureInstance.Tile);
        }

        OnFurnitureCreated(new FurnitureEventArgs(furnitureInstance));

        if (furnitureInstance.MovementCost == 1) return furnitureInstance;
        if (TileGraph != null)
        {
            TileGraph.Regenerate(tile);
        }

        return furnitureInstance;
    }

    private void OnTileChangedEvent(object sender, TileEventArgs args)
    {
        OnTileChanged(args);
        if (TileGraph != null)
        {
            TileGraph.Regenerate(args.Tile);
        }
    }

    public bool IsFurniturePlacementValid(string type, Tile tile)
    {
        return FurniturePrototypes[type].IsValidPosition(tile);
    }

    public Furniture GetFurniturePrototype(string type)
    {
        return FurniturePrototypes.ContainsKey(type) ? FurniturePrototypes[type] : null;
    }

    public void AddRoom(Room room)
    {
        Rooms.Add(room);
    }

    public void DeleteRoom(Room room)
    {
        if (room == OutsideRoom)
        {
            Debug.LogError("Tried to delete the outside room.");
            return;
        }

        Rooms.Remove(room);
        room.ClearTiles();
    }

    public int GetRoomIndex(Room room)
    {
        return Rooms.IndexOf(room);
    }

    public Room GetRoom(int index)
    {
        if (index < 0 || index > Rooms.Count - 1)
            return null;
        return Rooms[index];
    }

    public int FurnituresWithTypeCount(string type)
    {
        int count = Furnitures.Count(furniture => furniture.Type == type);
        return count;
    }

    private void CreateFurniturePrototypes()
    {
        LuaUtilities.LoadScriptFromFile(Path.Combine(Path.Combine(Application.streamingAssetsPath, "LUA"), "Furniture.lua"));

        FurniturePrototypes = new Dictionary<string, Furniture>();
        FurnitureJobPrototypes = new Dictionary<string, Job>();

        string filePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Furniture.xml");
        LoadFurniturePrototypesFromFile(File.ReadAllText(filePath));

        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string furnitureLuaModFile = Path.Combine(mod.FullName, "Furniture.lua");
            if (File.Exists(furnitureLuaModFile))
            {
                LuaUtilities.LoadScriptFromFile(furnitureLuaModFile);
            }

            string furnitureXmlModFile = Path.Combine(mod.FullName, "Furniture.xml");
            if (!File.Exists(furnitureXmlModFile)) continue;

            string furnitureXmlModText = File.ReadAllText(furnitureXmlModFile);
            LoadFurniturePrototypesFromFile(furnitureXmlModText);
        }
    }

    private void LoadFurniturePrototypesFromFile(string furnitureXmlText) 
    {
        XmlTextReader reader = new XmlTextReader(new StringReader(furnitureXmlText));
        if (reader.ReadToDescendant("Furnitures"))
        {
            if (reader.ReadToDescendant("Furniture"))
            {
                do
                {
                    Furniture furniture = new Furniture();
                    try
                    {
                        furniture.ReadXmlPrototype(reader);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error reading furniture prototype for: " + furniture.Type + Environment.NewLine + "Exception: " + e.Message + Environment.NewLine + "StackTrace: " + e.StackTrace);
                    }

                    FurniturePrototypes[furniture.Type] = furniture;
                }
                while (reader.ReadToNextSibling("Furniture"));
            }
            else
            {
                Debug.LogError("The furniture prototype definition file doesn't have any 'Furniture' elements.");
            }
        }
        else
        {
            Debug.LogError("Did not find a 'Furnitures' element in the prototype definition file.");
        }
    }

    private static void LoadNeedLua(string filePath)
    {
        string myLuaCode = File.ReadAllText(filePath);
        NeedActions.AddScript(myLuaCode);
    }

    private void CreateNeedPrototypes()
    {
        NeedPrototypes = new Dictionary<string, Need>();

        string needXmlSource = File.ReadAllText(Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Need.xml"));
        LoadNeedPrototypesFromFile (needXmlSource);

        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string needLuaModFile = Path.Combine(mod.FullName, "Need.lua");
            if (File.Exists(needLuaModFile))
            {
                LoadNeedLua(needLuaModFile);
            }

            string needXmlModFile = Path.Combine(mod.FullName, "Need.xml");
            if (!File.Exists(needXmlModFile)) continue;

            string needXmlModText = File.ReadAllText(needXmlModFile);
            LoadNeedPrototypesFromFile(needXmlModText);
        }
    }

    private void LoadNeedPrototypesFromFile(string needXmlText)
    {
        XmlTextReader reader = new XmlTextReader(new StringReader(needXmlText));

        if (!reader.ReadToDescendant("Needs")) return;
        if (reader.ReadToDescendant("Need"))
        {
            do
            {
                Need need = new Need();
                try
                {
                    need.ReadXmlPrototype(reader);
                }
                catch
                {
                    Debug.LogError("Error reading need prototype for: " + need.Type);
                }
                NeedPrototypes[need.Type] = need;
            }
            while (reader.ReadToNextSibling("Need"));
        }
        else
        {
            Debug.LogError("The need prototype definition file doesn't have any 'Need' elements.");
        }
    }

    private void CreateInventoryPrototypes()
    {
        InventoryPrototypes = new Dictionary<string, InventoryPrototype>();

        string filePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Inventory.xml");
        LoadInventoryPrototypesFromFile(File.ReadAllText(filePath));

        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string inventoryXmlModFile = Path.Combine(mod.FullName, "Inventory.xml");
            if (!File.Exists(inventoryXmlModFile)) continue;

            string inventoryXmlModText = File.ReadAllText(inventoryXmlModFile);
            LoadInventoryPrototypesFromFile(inventoryXmlModText);
        }
    }

    private void CreateTraderPrototypes()
    {
        TraderPrototypes = new Dictionary<string, TraderPrototype>();

        string filePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Trader.xml");
        LoadTraderPrototypesFromFile(File.ReadAllText(filePath));

        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string traderXmlModFile = Path.Combine(mod.FullName, "Trader.xml");
            if (!File.Exists(traderXmlModFile)) continue;

            string traderXmlModText = File.ReadAllText(traderXmlModFile);
            LoadTraderPrototypesFromFile(traderXmlModText);
        }
    }

    private void LoadTraderPrototypesFromFile(string traderXmlText)
    {
        XmlTextReader reader = new XmlTextReader(new StringReader(traderXmlText));
        if (reader.ReadToDescendant("Traders"))
        {
            if (reader.ReadToDescendant("Trader"))
            {
                do
                {
                    TraderPrototype trader = new TraderPrototype();
                    try
                    {
                        trader.ReadXmlPrototype(reader);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error reading trader prototype for: " + trader.Type + Environment.NewLine + "Exception: " + e.Message + Environment.NewLine + "StackTrace: " + e.StackTrace);
                    }

                    TraderPrototypes[trader.Type] = trader;      
                }
                while (reader.ReadToNextSibling("Trader"));
            }
            else
            {
                Debug.LogError("The trader prototype definition file doesn't have any 'Trader' elements.");
            }
        }
        else
        {
            Debug.LogError("Did not find a 'Traders' element in the prototype definition file.");
        }
    }

    private void CreateQuests()
    {
        Quests = new List<Quest>();

        string filePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Quest.xml");
        LoadQuestsFromFile(File.ReadAllText(filePath));
        
        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string traderXmlModFile = Path.Combine(mod.FullName, "Quest.xml");
            if (!File.Exists(traderXmlModFile)) continue;

            string questXmlModText = File.ReadAllText(traderXmlModFile);
            LoadQuestsFromFile(questXmlModText);
        }
    }

    private void LoadQuestsFromFile(string questXmlText)
    {
        XmlTextReader reader = new XmlTextReader(new StringReader(questXmlText));
        if (reader.ReadToDescendant("Quests"))
        {
            if (reader.ReadToDescendant("Quest"))
            {
                do
                {
                    Quest quest = new Quest();
                    try
                    {
                        quest.ReadXmlPrototype(reader);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error reading quest for: " + quest.Name + Environment.NewLine + "Exception: " + e.Message + Environment.NewLine + "StackTrace: " + e.StackTrace);
                    }

                    Quests.Add(quest);
                }
                while (reader.ReadToNextSibling("Quest"));
            }
            else
            {
                Debug.LogError("The quest prototype definition file doesn't have any 'Quest' elements.");
            }
        }
        else
        {
            Debug.LogError("Did not find a 'Quests' element in the prototype definition file.");
        }
    }

    private void LoadInventoryPrototypesFromFile(string inventoryXmlText)
    {
        XmlTextReader reader = new XmlTextReader(new StringReader(inventoryXmlText));
        if (reader.ReadToDescendant("Inventories"))
        {
            if (reader.ReadToDescendant("Inventory"))
            {
                do
                {
                    InventoryPrototype inventoryPrototype = new InventoryPrototype();
                    try
                    {
                        inventoryPrototype.ReadXmlPrototype(reader);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error reading inventory prototype for: " + inventoryPrototype.Type + Environment.NewLine + "Exception: " + e.Message + Environment.NewLine + "StackTrace: " + e.StackTrace);
                    }

                    InventoryPrototypes[inventoryPrototype.Type] = inventoryPrototype;
                }
                while (reader.ReadToNextSibling("Inventory"));
            }
            else
            {
                Debug.LogError("The inventory prototype definition file doesn't have any 'Inventory' elements.");
            }
        }
        else
        {
            Debug.LogError("Did not find a 'Inventories' element in the prototype definition file.");
        }
    }

    public void OnFurnitureRemoved(object sender, FurnitureEventArgs args)
    {
        Furnitures.Remove(args.Furniture);
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

        writer.WriteStartElement("Rooms");
        foreach (Room room in Rooms)
        {
            if (OutsideRoom == room)
            {
                continue;
            }  

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

        writer.WriteStartElement("Inventories");
        foreach (string type in InventoryManager.Inventories.Keys)
        {
            foreach (Inventory inventory in InventoryManager.Inventories[type])
            {
                writer.WriteStartElement("Inventory");
                inventory.WriteXml(writer);
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
            writer.WriteStartElement("Character");
            character.WriteXml(writer);
            writer.WriteEndElement();

        }
        writer.WriteEndElement();
        writer.WriteElementString("Skybox", Skybox.name);
    }

    public void ReadXml(XmlReader reader)
    {
        Width = int.Parse(reader.GetAttribute("Width"));
        Height = int.Parse(reader.GetAttribute("Height"));

        Initialize(Width, Height);

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
                case "Inventories":
                    ReadXmlInventories(reader);
                    break;
                case "Furnitures":
                    ReadXmlFurnitures(reader);
                    break;
                case "Characters":
                    ReadXmlCharacters(reader);
                    break;
                case "Skybox":
                    LoadSkybox(reader.ReadElementString("Skybox"));
                    break;
            }
        }

        // DEBUGGING ONLY!  REMOVE ME LATER!
        // Create an Inventory Item
        Inventory inventory = new Inventory("Steel Plate", 50, 50);
        Tile tileAt = GetTileAt(Width / 2, Height / 2);
        InventoryManager.Place(tileAt, inventory);
        if (InventoryCreated != null)
        {
            OnInventoryCreated(new InventoryEventArgs(tileAt.Inventory));
        }

        inventory = new Inventory("Steel Plate", 50, 4);
        tileAt = GetTileAt(Width / 2 + 2, Height / 2);
        InventoryManager.Place(tileAt, inventory);
        if (InventoryCreated != null)
        {
            OnInventoryCreated(new InventoryEventArgs(tileAt.Inventory));
        }

        inventory = new Inventory("Copper Wire", 50, 3);
        tileAt = GetTileAt(Width / 2 + 1, Height / 2 + 2);
        InventoryManager.Place(tileAt, inventory);
        if (InventoryCreated != null)
        {
            OnInventoryCreated(new InventoryEventArgs(tileAt.Inventory));
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

    private void ReadXmlInventories(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Inventory")) return;
        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            Inventory inventory = new Inventory(reader.GetAttribute("objectType"), int.Parse(reader.GetAttribute("maxStackSize")), int.Parse(reader.GetAttribute("stackSize")));           
            InventoryManager.Place(tiles[x,y],inventory);

        }
        while (reader.ReadToNextSibling("Inventory"));
    }

    private void ReadXmlFurnitures(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Furniture")) return;
        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            Furniture furniture = PlaceFurniture(reader.GetAttribute("objectType"), tiles[x, y], false);
            furniture.ReadXml(reader);
        }
        while (reader.ReadToNextSibling("Furniture"));
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

    private void ReadXmlCharacters(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Character")) return;
        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            if(reader.GetAttribute("r") != null)
            {
                float r = float.Parse(reader.GetAttribute("r"));
                float b = float.Parse(reader.GetAttribute("b"));;
                float g = float.Parse(reader.GetAttribute("g"));;
                Color colour = new Color(r, g, b, 1.0f);
                Character character = CreateCharacter(tiles[x, y], colour);
                character.ReadXml(reader);
            }

            else
            {
                Character character = CreateCharacter(tiles[x, y]);
                character.ReadXml(reader);
            }
                
        }
        while (reader.ReadToNextSibling("Character"));
    }
}
