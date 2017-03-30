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

    public RoomManager RoomManager { get; private set; }
    public CharacterManager CharacterManager { get; private set; }
    public InventoryManager InventoryManager { get; private set; }
    public FurnitureManager FurnitureManager { get; private set; }
    public QuestManager QuestManager { get; private set; }

    public PowerSystem PowerSystem { get; private set; }
    public Temperature Temperature { get; private set; }
    public Material Skybox { get; private set; } // TODO: Move me to somewhere more appropriate. World Controller??

    public Dictionary<string, Job> FurnitureJobPrototypes { get; private set; }

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
        QuestManager = new QuestManager();
        // FIXME: Temporary fix for quest loading.
        QuestManager.Initialize();

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

    public void FixedFrequencyUpdate(float deltaTime)
    {
        FurnitureManager.Update(deltaTime);
        QuestManager.Update(deltaTime);

        Temperature.Update();
    }

    public void EveryFrameUpdate(float deltaTime)
    {
        CharacterManager.Update(deltaTime);
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
        return PrototypeManager.Furnitures.Contains(type) ? PrototypeManager.Furnitures[type] : null;
    }

    private void CreateFurniturePrototypes()
    {
        Lua.Parse(Path.Combine(Path.Combine(Application.streamingAssetsPath, "LUA"), "Furniture.lua"));
        FurnitureJobPrototypes = new Dictionary<string, Job>();

        string filePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Furniture.xml");
        PrototypeManager.Furnitures.Load(File.ReadAllText(filePath));

        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string furnitureLuaModFile = Path.Combine(mod.FullName, "Furniture.lua");
            if (File.Exists(furnitureLuaModFile))
            {
                Lua.Parse(furnitureLuaModFile);
            }

            string furnitureXmlModFile = Path.Combine(mod.FullName, "Furniture.xml");
            if (!File.Exists(furnitureXmlModFile)) continue;

            string furnitureXmlModText = File.ReadAllText(furnitureXmlModFile);
            PrototypeManager.Furnitures.Load(furnitureXmlModText);
        }
    }

    private static void CreateNeedPrototypes()
    {
        string needXmlSource = File.ReadAllText(Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Need.xml"));
        PrototypeManager.Needs.Load(needXmlSource);

        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string needLuaModFile = Path.Combine(mod.FullName, "Need.lua");
            if (File.Exists(needLuaModFile))
            {
                Lua.Parse(needLuaModFile);
            }

            string needXmlModFile = Path.Combine(mod.FullName, "Need.xml");
            if (!File.Exists(needXmlModFile)) continue;

            string needXmlModText = File.ReadAllText(needXmlModFile);
            PrototypeManager.Needs.Load(needXmlModText);
        }
    }

    private static void CreateInventoryPrototypes()
    {
        string filePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Inventory.xml");
        PrototypeManager.Inventories.Load(File.ReadAllText(filePath));

        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string inventoryXmlModFile = Path.Combine(mod.FullName, "Inventory.xml");
            if (!File.Exists(inventoryXmlModFile)) continue;

            string inventoryXmlModText = File.ReadAllText(inventoryXmlModFile);
            PrototypeManager.Inventories.Load(inventoryXmlModText);
        }
    }

    private static void CreateTraderPrototypes()
    {
        string filePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Trader.xml");
        PrototypeManager.Traders.Load(File.ReadAllText(filePath));

        DirectoryInfo[] mods = WorldController.Instance.ModManager.ModDirectories;
        foreach (DirectoryInfo mod in mods)
        {
            string traderXmlModFile = Path.Combine(mod.FullName, "Trader.xml");
            if (!File.Exists(traderXmlModFile)) continue;

            string traderXmlModText = File.ReadAllText(traderXmlModFile);
            PrototypeManager.Inventories.Load(traderXmlModText);
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
