using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class Furniture : IXmlSerializable, IPrototypable, ISelectable, IContextActionProvider, IPowerRelated
{
    private string name;
    public string Name
    {
        get { return string.IsNullOrEmpty(name) ? Type : name; }
        set { name = value; }
    }

    public string Type { get; private set; }
    public Tile Tile { get; private set; }

    public Vector2 WorkPositionOffset { get; private set; }
    public Tile WorkTile { get { return World.Current.GetTileAt(Tile.X + (int)WorkPositionOffset.x, Tile.Y + (int)WorkPositionOffset.y); } }

    public Vector2 InventorySpawnPosition { get; private set; }
    public Tile InventorySpawnTile { get { return World.Current.GetTileAt(Tile.X + (int)InventorySpawnPosition.x, Tile.Y + (int)InventorySpawnPosition.y); } }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public float MovementCost { get; private set; }
    public bool RoomEnclosure { get; private set; }
    public bool LinksToNeighbour { get; private set; }

    public List<string> ReplaceableFurniture { get; private set; }
    public int JobCount { get { return jobs.Count; } }

    private float powerValue;
    public float PowerValue
    {
        get { return powerValue; }
        set
        {
            if (powerValue.AreEqual(value)) return;
            powerValue = value;
            OnPowerValueChanged(new PowerEventArgs(this));
        }
    }

    public bool IsConsumingPower { get { return PowerValue < 0.0f; } }

    public Color Tint { get; set; }
    public string DragMode { get; private set; }

    public ParameterContainer Parameters { get; private set; }
    public LuaEventManager EventManager { get; private set; }

    public event PowerChangedEventHandler PowerValueChanged;
    public void OnPowerValueChanged(PowerEventArgs args)
    {
        PowerChangedEventHandler powerChanged = PowerValueChanged;
        if (powerChanged != null)
        {
            powerChanged(this, args);
        }
    }

    public event FurnitureChangedEventHandler FurnitureChanged;
    public void OnFurnitureChanged(FurnitureEventArgs args)
    {
        FurnitureChangedEventHandler furnitureChanged = FurnitureChanged;
        if (furnitureChanged != null)
        {
            furnitureChanged(this, args);
        }
    }

    public event FurnitureRemovedEventHandler FurnitureRemoved;
    public void OnFurnitureRemoved(FurnitureEventArgs args)
    {
        FurnitureRemovedEventHandler furnitureRemoved = FurnitureRemoved;
        if (furnitureRemoved != null)
        {
            furnitureRemoved(this, args);
        }
    }

    public bool IsSelected { get; set; }
    public bool VerticalDoor = false;

    public string LocalizationCode { get; private set; }
    public string UnlocalizedDescription { get; private set; }

    private string description = "";
    private readonly HashSet<string> typeTags;
    private readonly List<Job> jobs;

    private string tryEnterFunction;
    private string getSpriteNameFunction;
    private readonly List<ContextMenuLuaAction> contextMenuLuaActions;

    public Furniture()
    {
        Height = 1;
        Width = 1;

        EventManager = new LuaEventManager("FurnitureUpdate", "FurnitureInstalled", "FurnitureUninstalled", "TemperatureUpdated");
        ReplaceableFurniture = new List<string>();
        Parameters = new ParameterContainer("furnParameters");
        Tint = Color.white;

        jobs = new List<Job>();
        typeTags = new HashSet<string>();
        contextMenuLuaActions = new List<ContextMenuLuaAction>();
    }

    protected Furniture(Furniture other)
    {
        Type = other.Type;
        Name = other.Name;
        MovementCost = other.MovementCost;
        RoomEnclosure = other.RoomEnclosure;
        Width = other.Width;
        Height = other.Height;
        Tint = other.Tint;
        LinksToNeighbour = other.LinksToNeighbour;

        WorkPositionOffset = other.WorkPositionOffset;
        InventorySpawnPosition = other.InventorySpawnPosition;

        Parameters = new ParameterContainer(other.Parameters);

        jobs = new List<Job>();
        typeTags = new HashSet<string>(other.typeTags);
        description = other.description;

        EventManager = new LuaEventManager("FurnitureUpdate", "FurnitureInstalled", "FurnitureUninstalled", "TemperatureUpdated");
        if (other.EventManager != null)
        {
            EventManager = other.EventManager.Clone();
        }

        if (other.contextMenuLuaActions != null)
        {
            contextMenuLuaActions = new List<ContextMenuLuaAction>(other.contextMenuLuaActions);
        }

        tryEnterFunction = other.tryEnterFunction;
        getSpriteNameFunction = other.getSpriteNameFunction;
        powerValue = other.powerValue;

        if (!powerValue.IsZero())
        {
            World.Current.PowerSystem.AddToPowerGrid(this);
        }

        LocalizationCode = other.LocalizationCode;
        UnlocalizedDescription = other.UnlocalizedDescription;
    }

    public virtual Furniture Clone()
    {
        return new Furniture(this);
    }

    public void Update(float deltaTime)
    {
        EventManager.Trigger("FurnitureUpdate", this, deltaTime);
    }

    public static Furniture Place(Furniture prototype, Tile tile)
    {
        if (prototype.IsValidPosition(tile) == false)
        {
            return null;
        }

        Furniture furnitureInstance = prototype.Clone();
        furnitureInstance.Tile = tile;

        if (tile.PlaceFurniture(furnitureInstance) == false)
        {
            return null;
        }

        if (furnitureInstance.LinksToNeighbour)
        {
            UpdateNeighbouringFurnitures(prototype, tile);
        }

        furnitureInstance.EventManager.Trigger("FurnitureInstalled", furnitureInstance);
        UpdateThermalDiffusitivity(furnitureInstance, tile);

        return furnitureInstance;
    }

       public void Deconstruct()
    {
        CancelJobs();
        EventManager.Trigger("FurnitureUninstalled", this);
        World.Current.Temperature.SetThermalDiffusivity(Tile.X, Tile.Y, Temperature.DefaultThermalDiffusivity);
        Tile.RemoveFurniture();

        OnFurnitureRemoved(new FurnitureEventArgs(this));

        if (RoomEnclosure)
        {
            Room.CalculateRooms(Tile);
        }

        if (World.Current.TileGraph != null)
        {
            World.Current.TileGraph.Regenerate(Tile);
        }

        if (LinksToNeighbour)
        {
            UpdateNeighbouringFurnitures(this, Tile);
        }
    }

    private static void UpdateNeighbouringFurnitures(Furniture furniture, Tile tile)
    {
        int x = tile.X;
        int y = tile.Y;

        for (int xOffset = x - 1; xOffset < x + furniture.Width + 1; xOffset++)
        {
            for (int yOffset = y - 1; yOffset < y + furniture.Height + 1; yOffset++)
            {
                Tile tileAt = World.Current.GetTileAt(xOffset, yOffset);
                if (tileAt != null && tileAt.Furniture != null)
                {
                    tileAt.Furniture.OnFurnitureChanged(new FurnitureEventArgs(tileAt.Furniture));
                }
            }
        }
    }

    private static void UpdateThermalDiffusitivity(Furniture furniture, Tile tile)
    {
        float thermalDiffusivity = Temperature.DefaultThermalDiffusivity;
        if (furniture.Parameters.ContainsKey("thermal_diffusivity"))
        {
            thermalDiffusivity = furniture.Parameters["thermal_diffusivity"].Float();
        }

        World.Current.Temperature.SetThermalDiffusivity(tile.X, tile.Y, thermalDiffusivity);
    }

    public bool IsValidPosition(Tile tile)
    {
        const int minEdgeDistance = 5;
        bool outOfBorder = tile.X < minEdgeDistance || tile.Y < minEdgeDistance || 
            World.Current.Width - tile.X <= minEdgeDistance || World.Current.Height - tile.Y <= minEdgeDistance;

        if (outOfBorder)
        {
            return false;
        }

        if (HasTypeTag("OutdoorOnly"))
        {
            if (tile.Room == null || !tile.Room.IsOutsideRoom())
            {
                return false;
            }
        }

        for (int xOffset = tile.X; xOffset < (tile.X + Width); xOffset++)
        {
            for (int yOffset = tile.Y; yOffset < (tile.Y + Height); yOffset++)
            {
                Tile tileAt = World.Current.GetTileAt(xOffset, yOffset);
                bool isReplaceable = false;

                if (tileAt.Furniture != null)
                {
                    foreach (string furnitureType in ReplaceableFurniture)
                    {
                        if (tileAt.Furniture.HasTypeTag(furnitureType))
                        {
                            isReplaceable = true;
                        }
                    }
                }

                if (tileAt.Type != TileType.Floor)
                {
                    return false;
                }

                if (tileAt.Furniture != null && isReplaceable == false)
                {
                    return false;
                }

                if (tileAt.Inventory != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void AddJob(Job job)
    {
        job.Furniture = this;
        jobs.Add(job);
        job.JobStopped += OnJobStopped;
        World.Current.JobQueue.Enqueue(job);
    }

    private void RemoveJob(Job job)
    {
        job.JobStopped -= OnJobStopped;
        jobs.Remove(job);
        job.Furniture = null;
    }

    private void ClearJobs()
    {
        Job[] jobsToClear = jobs.ToArray();
        foreach (Job job in jobsToClear)
        {
            RemoveJob(job);
        }
    }

    public void CancelJobs()
    {
        Job[] jobsToClear = jobs.ToArray();
        foreach (Job job in jobsToClear)
        {
            job.CancelJob();
        }
    }

    public bool HasPower()
    {
        return World.Current.PowerSystem.RequestPower(this) || World.Current.PowerSystem.AddToPowerGrid(this);
    }

    public string GetSpriteName()
    {
        return string.IsNullOrEmpty(getSpriteNameFunction) ? Type : Lua.Call(getSpriteNameFunction, this).String;
    }

    private void OnJobStopped(object sender, JobEventArgs args)
    {
        RemoveJob(args.Job);
    }

    public TileEnterability TryEnter()
    {
        if (string.IsNullOrEmpty(tryEnterFunction))
        {
            return TileEnterability.Immediate;
        }

        return (TileEnterability)Lua.Call(tryEnterFunction, this).Number;
    }

    public bool HasTypeTag(string typeTag)
    {
        return typeTags.Contains(typeTag);
    }

    public bool IsStockpile()
    {
        return HasTypeTag("Storage");
    }

    public Inventory[] GetStorageInventoryFilter()
    {
        if (IsStockpile() == false)
        {
            return null;
        }

        Dictionary<string, Inventory> inventories = new Dictionary<string, Inventory>();
        foreach (string objectType in PrototypeManager.Inventories.Keys)
        {
            inventories[objectType] = new Inventory(objectType, PrototypeManager.Inventories[objectType].MaxStackSize, 0);
        }

        Inventory[] inventoryFilter = new Inventory[inventories.Count];
        inventories.Values.CopyTo(inventoryFilter, 0);
        return inventoryFilter;
    }

    public static void Build(object sender, JobEventArgs args)
    {
        World.Current.FurnitureManager.Place(args.Job.Type, args.Job.Tile);
        args.Job.Tile.PendingBuildJob = null;
    }

    public IEnumerable<ContextMenuAction> GetContextMenuActions(ContextMenu contextMenu)
    {
        ContextMenuAction action = new ContextMenuAction
        {
            Text = "Deconstruct " + Name,
            RequiresCharacterSelection = false,
        };

        action.Action += (sender, args) => Deconstruct();

        yield return action;

        foreach (var contextMenuLuaAction in contextMenuLuaActions)
        {
            action = new ContextMenuAction
            {
                Text = contextMenuLuaAction.Text,
                RequiresCharacterSelection = contextMenuLuaAction.RequiresCharacterSelection,
            };

            ContextMenuLuaAction luaAction = contextMenuLuaAction;
            action.Action += (sender, args) => InvokeContextMenuLuaAction(luaAction.LuaFunction, args.Character);

            yield return action;
        }
    }

    private void InvokeContextMenuLuaAction(string luaFunction, Character character)
    {
        Lua.Call(luaFunction, this, character);
    }

    public string GetName()
    {
        return LocalizationCode;
    }

    public string GetDescription()
    {
        return UnlocalizedDescription;
    }

    public string GetHitPointString()
    {
        return "18/18"; // TODO: Add a hitpoint system to...well...everything
    }

    public string GetJobDescription()
    {
        return "";
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", Tile.X.ToString());
        writer.WriteAttributeString("Y", Tile.Y.ToString());
        writer.WriteAttributeString("objectType", Type);

        Parameters.WriteXml(writer);
    }

    public void ReadXml(XmlReader reader)
    {
        Parameters = ParameterContainer.ReadXml(reader);
    }

    public void ReadXmlPrototype(XmlReader parentReader)
    {
        Type = parentReader.GetAttribute("objectType");
        XmlReader reader = parentReader.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Name":
                    reader.Read();
                    Name = reader.ReadContentAsString();
                    break;
                case "TypeTag":
                    reader.Read();
                    typeTags.Add(reader.ReadContentAsString());
                    break;
                case "Description":
                    reader.Read();
                    description = reader.ReadContentAsString();
                    break;
                case "MovementCost":
                    reader.Read();
                    MovementCost = reader.ReadContentAsFloat();
                    break;
                case "Width":
                    reader.Read();
                    Width = reader.ReadContentAsInt();
                    break;
                case "Height":
                    reader.Read();
                    Height = reader.ReadContentAsInt();
                    break;
                case "LinksToNeighbours":
                    reader.Read();
                    LinksToNeighbour = reader.ReadContentAsBoolean();
                    break;
                case "EnclosesRooms":
                    reader.Read();
                    RoomEnclosure = reader.ReadContentAsBoolean();
                    break;
                case "CanReplaceFurniture":
                    string attribute = reader.GetAttribute("typeTag");
                    if (attribute != null)
                    {
                        ReplaceableFurniture.Add(attribute);
                    }

                    break;
                case "DragType":
                    reader.Read();
                    DragMode = reader.ReadContentAsString();
                    break;
                case "BuildingJob":
                    float jobTime = float.Parse(reader.GetAttribute("jobTime"));
                    JobPriority priority = JobPriority.High;
                    bool repeatingJob = false;
                    bool workAdjacent = false;

                    List<Inventory> inventories = new List<Inventory>();
                    XmlReader readerSubtree = reader.ReadSubtree();

                    while (readerSubtree.Read())
                    {
                        switch (readerSubtree.Name)
                        {
                            case "JobPriority":
                                readerSubtree.Read();
                                priority = (JobPriority)Enum.Parse(typeof(JobPriority), reader.ReadContentAsString());
                                break;
                            case "RepeatingJob":
                                readerSubtree.Read();
                                repeatingJob = reader.ReadContentAsBoolean();
                                break;
                            case "WorkAdjacent":
                                readerSubtree.Read();
                                workAdjacent = reader.ReadContentAsBoolean();
                                break;
                            case "Inventory":
                                inventories.Add(new Inventory(readerSubtree.GetAttribute("objectType"), Int32.Parse(readerSubtree.GetAttribute("amount")), 0));
                                break;
                        }
                    }

                    Job job = new Job(null, Type, Build, jobTime, inventories.ToArray(), priority, repeatingJob)
                    {
                        Description = "job_build_" + Type + "_desc",
                        WorkAdjacent = workAdjacent
                    };

                    World.Current.FurnitureJobPrototypes[Type] = job;
                    break;

                case "Action":
                    XmlReader subtree = reader.ReadSubtree();
                    subtree.Read();

                    string eventTag = reader.GetAttribute("event");
                    string functionName = reader.GetAttribute("functionName");
                    EventManager.AddHandler(eventTag, functionName);

                    subtree.Close();
                    break;
                case "ContextMenuAction":
                    contextMenuLuaActions.Add(new ContextMenuLuaAction
                    {
                        LuaFunction = reader.GetAttribute("FunctionName"),
                        Text = reader.GetAttribute("Text"),
                        RequiresCharacterSelection = bool.Parse(reader.GetAttribute("RequiereCharacterSelected"))
                    });
                    break;
                case "IsEnterable":
                    tryEnterFunction = reader.GetAttribute("FunctionName");
                    break;
                case "GetSpriteName":
                    getSpriteNameFunction = reader.GetAttribute("FunctionName");
                    break;

                case "JobSpotOffset":
                    WorkPositionOffset = new Vector2(Int32.Parse(reader.GetAttribute("X")), Int32.Parse(reader.GetAttribute("Y")));
                    break;
                case "JobSpawnSpotOffset":
                    InventorySpawnPosition = new Vector2(Int32.Parse(reader.GetAttribute("X")), Int32.Parse(reader.GetAttribute("Y")));
                    break;

                case "Power":
                    reader.Read();
                    powerValue = reader.ReadContentAsFloat();
                    break;

                case "Params":
                    Parameters = ParameterContainer.ReadXml(reader);
                    break;

                case "LocalizationCode":
                    reader.Read();
                    LocalizationCode = reader.ReadContentAsString();
                    break;

                case "UnlocalizedDescription":
                    reader.Read();
                    UnlocalizedDescription = reader.ReadContentAsString();
                    break;
            }
        }
    }
}
