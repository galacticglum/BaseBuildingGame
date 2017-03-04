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
    public int Width { get; private set; }
    public int Height { get; private set; }

    //public List<Room> Rooms { get; private set; }

    public RoomManager RoomManager { get; private set; }
    public CharacterManager CharacterManager { get; private set; }
    public InventoryManager InventoryManager { get; private set; }
    public FurnitureManager FurnitureManager { get; private set; }

    public PowerSystem PowerSystem { get; private set; }
    public Temperature Temperature { get; private set; }
    public Material Skybox { get; private set; } // TODO: Move me to somewhere more appropriate. World Controller??

    public Dictionary<string, Furniture> FurniturePrototypes { get; private set; }
    public Dictionary<string, Job> FurnitureJobPrototypes { get; private set; }
    public Dictionary<string, Need> NeedPrototypes { get; private set; }
    public Dictionary<string, InventoryPrototype> InventoryPrototypes { get; private set; }
    public Dictionary<string, TraderPrototype> TraderPrototypes { get; private set; }
    public List<Quest> Quests { get; private set; }

    public Tile CentreTile { get { return GetTileAt(Width / 2, Height / 2); } }

    public JobQueue JobQueue { get; private set; }
    public JobQueue JobWaitingQueue { get; private set; }

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
        CharacterManager.Create(GetTileAt(Width / 2, Height / 2));
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

        FurnitureManager = new FurnitureManager();
        RoomManager = new RoomManager();
        CharacterManager = new CharacterManager();
        InventoryManager = new InventoryManager();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                tiles[x, y] = new Tile(x, y);
                tiles[x, y].TileChanged += OnTileChangedEvent;
                tiles[x, y].Room = RoomManager.OutsideRoom; // Rooms 0 is always going to be outside, and that is our default room
            }
        }

        CreateFurniturePrototypes();
        CreateNeedPrototypes();
        CreateInventoryPrototypes();
        CreateTraderPrototypes();
        CreateQuests();

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
        FurnitureManager.Update(deltaTime);
        CharacterManager.Update(deltaTime);

        Temperature.Update();
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

    private void OnTileChangedEvent(object sender, TileEventArgs args)
    {
        OnTileChanged(args);
        if (TileGraph != null)
        {
            TileGraph.Regenerate(args.Tile);
        }
    }

    public Furniture GetFurniturePrototype(string type)
    {
        return FurniturePrototypes.ContainsKey(type) ? FurniturePrototypes[type] : null;
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
        LoadNeedPrototypesFromFile(needXmlSource);

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

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
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

        FurnitureManager.WriteXml(writer);
        CharacterManager.WriteXml(writer);

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
                    RoomManager.ReadXml(reader);
                    break;
                case "Tiles":
                    ReadXmlTiles(reader);
                    break;
                case "Inventories":
                    ReadXmlInventories(reader);
                    break;
                case "Furnitures":
                    FurnitureManager.ReadXml(reader);
                    break;
                case "Characters":
                    CharacterManager.ReadXml(reader);
                    break;
                case "Skybox":
                    LoadSkybox(reader.ReadElementString("Skybox"));
                    break;
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

    private void ReadXmlInventories(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Inventory")) return;
        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            Inventory inventory = new Inventory(reader.GetAttribute("objectType"), int.Parse(reader.GetAttribute("maxStackSize")), int.Parse(reader.GetAttribute("stackSize")));
            InventoryManager.Place(tiles[x, y], inventory);

        }
        while (reader.ReadToNextSibling("Inventory"));
    }
}
