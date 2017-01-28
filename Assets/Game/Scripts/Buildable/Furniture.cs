using System;
using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Furniture : IPrototypable, IXmlSerializable, ISelectable
{
    public static JobCompletedEventHandler BuildCallback { get { return (sender, args) => { Lua.Call("BuildFurniture", sender, args); }; }}

    protected Dictionary<string, float> FurnitureParameters { get; set; }
    public event FurnitureUpdateEventHandler FurnitureUpdate;
    public void OnFurnitureUpdate(FurnitureUpdateEventArgs args)
    {
        FurnitureUpdateEventHandler updateBehaviours = FurnitureUpdate;
        if (updateBehaviours != null)
        {
            updateBehaviours(this, args);
        }

        EventManager.Trigger("FurnitureUpdate", this, args);
    }

    private string name = string.Empty;
    public string Name
    {
        get { return string.IsNullOrEmpty(name) ? Type : name; }
        set { name = value; }
    }

    public string Type { get; protected set; }
    public Tile Tile { get; protected set; }

    public Vector2 WorkPositionOffset { get; set; }
    public Tile WorkTile { get { return World.Current.GetTileAt(Tile.X + (int)WorkPositionOffset.x, Tile.Y + (int)WorkPositionOffset.y); } }

    public Vector2 InventorySpawnPositionOffset { get; set; }
    public Tile InventorySpawnTile { get { return World.Current.GetTileAt(Tile.X + (int)InventorySpawnPositionOffset.x, Tile.Y + (int)InventorySpawnPositionOffset.y); } }

    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public float MovementCost { get; protected set; }
    public bool LinksToNeighbour { get; protected set; }
    public bool RoomEnclosure { get; protected set; }

    public Color Tint { get; set; }
    public DragMode DragMode { get; protected set; }

    public LuaEventManager EventManager { get; set; }
    public event FurnitureChangedEventHandler FurnitureChanged;
    public void OnFurnitureChanged(FurnitureEventArgs args)
    {
        FurnitureChangedEventHandler furnitureChanged = FurnitureChanged;
        if (furnitureChanged != null)
        {
            furnitureChanged(this, args);
        }

        EventManager.Trigger("FurnitureChanged", this, args);
    }

    public event FurnitureRemovedEventHandler FurnitureRemoved;
    public void OnFurnitureRemoved(FurnitureEventArgs args)
    {
        FurnitureRemovedEventHandler furnitureRemoved = FurnitureRemoved;
        if (furnitureRemoved != null)
        {
            furnitureRemoved(this, args);
        }

        EventManager.Trigger("FurnitureRemoved", this, args);
    }

    private readonly List<Job> jobs;

    private string tryEnterFunction = string.Empty;

    public Furniture()
    {
        EventManager = new LuaEventManager("FurnitureUpdate", "FurnitureChanged", "FurnitureRemoved");
        FurnitureParameters = new Dictionary<string, float>();

        jobs = new List<Job>();
        Tint = Color.white;
    }

    protected Furniture(Furniture furniture)
    {
        Name = furniture.Name;
        Type = furniture.Type;
        MovementCost = furniture.MovementCost;
        RoomEnclosure = furniture.RoomEnclosure;
        Tint = furniture.Tint;
        Width = furniture.Width;
        Height = furniture.Height;
        LinksToNeighbour = furniture.LinksToNeighbour;
        WorkPositionOffset = furniture.WorkPositionOffset;

        EventManager = new LuaEventManager("FurnitureUpdate", "FurnitureChanged", "FurnitureRemoved");

        FurnitureParameters = new Dictionary<string, float>(furniture.FurnitureParameters);
        jobs = new List<Job>();

        FurnitureUpdate = furniture.FurnitureUpdate;

        tryEnterFunction = furniture.tryEnterFunction;
    }

    public virtual Furniture Clone()
    {
        return new Furniture(this);
    }

    public void Update(float deltaTime)
    {
        OnFurnitureUpdate(new FurnitureUpdateEventArgs(this, deltaTime));
    }

    public static Furniture Place(Furniture furniture, Tile tile)
    {
        if(furniture.IsValidPosition(tile) == false)
        {
			Debug.LogError("Furniture::Place: Position Validity Function returned 'false'.");
			return null;
		}

        Furniture furnitureInstance = new Furniture(furniture) { Tile = tile };
        if(tile.PlaceFurniture(furnitureInstance) == false)
        {
			return null;
		}

        if (!furnitureInstance.LinksToNeighbour) return furnitureInstance;

        UpdateNeighbouringFurnitures(furnitureInstance);

        return furnitureInstance;
	}

    private static void UpdateNeighbouringFurnitures(Furniture furniture)
    {
        foreach (Tile neighbourTile in furniture.Tile.GetNeighbours())
        {
            if (HasFurnitureOfSameType(furniture, neighbourTile))
            {
                neighbourTile.Furniture.OnFurnitureChanged(new FurnitureEventArgs(neighbourTile.Furniture));
            }
        }
    }

    private static bool HasFurnitureOfSameType(Furniture furniture, Tile tile)
    {
        return tile != null && tile.Furniture != null && tile.Furniture.Type == furniture.Type;
    }

    public bool IsValidPosition(Tile tile)
    {
        for (int x = tile.X; x < tile.X + Width; x++)
        {
            for (int y = tile.Y; y < tile.Y + Height; y++)
            {
                Tile tileAt = World.Current.GetTileAt(x, y);

                if (tileAt.Type != TileType.Floor)
                {
                    return false;
                }

                if (tileAt.Furniture != null || tileAt.Inventory != null)
                {
                    return false;
                }

                // TODO: Character move order when building
            }
        }

        return true;
    }

    public float GetParameter(string key, float value = 0)
	{
	    return FurnitureParameters.ContainsKey(key) == false ? value : FurnitureParameters[key];
	}

	public void SetParameter(string key, float value)
    {
		FurnitureParameters[key] = value;
	}

	public void ModifyParameter(string key, float value)
    {
		if(FurnitureParameters.ContainsKey(key) == false)
        {
			FurnitureParameters[key] = value;
		}

		FurnitureParameters[key] += value;
	}

	public int JobCount
    {
	    get { return jobs.Count; }
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
        World.Current.JobQueue.Remove(job);
		jobs.Remove(job);
        job.Furniture = null;
	}

	private void ClearJobs()
	{
	    Job[] jobsToClear = jobs.ToArray();
		foreach(Job job in jobsToClear)
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

	public bool IsStockpile()
    {
		return Type == "Stockpile";
	}

    public void Deconstruct()
    {
        Tile.RemoveFurniture();
        UpdateNeighbouringFurnitures(this);
        OnFurnitureRemoved(new FurnitureEventArgs(this));
        ClearJobs();

        if (RoomEnclosure)
        {
            Room.CalculateRooms(Tile);
        }

        if (World.Current.TileGraph != null)
        {
            World.Current.TileGraph.Regenerate(Tile);
        }
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

        return (TileEnterability)Lua.Call(tryEnterFunction, this).UserData.Object;
    }

    public string GetName()
    {
        return Name;
    }

    public string GetDescription()
    {
        return "A furniture description in a galaxy far, far away (jurrasic park?).";
    }

    public IEnumerable<string> GetAdditionalInfo()
    {
        return null;
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", Tile.X.ToString());
        writer.WriteAttributeString("Y", Tile.Y.ToString());
        writer.WriteAttributeString("Type", Type);

        foreach (string paramater in FurnitureParameters.Keys)
        {
            writer.WriteStartElement("Paramater");
            writer.WriteAttributeString("Name", paramater);
            writer.WriteAttributeString("Value", FurnitureParameters[paramater].ToString());
            writer.WriteEndElement();
        }
    }

    public void ReadXml(XmlReader reader)
    {
        ReadXmlParamaters(reader);
    }

    public void ReadXmlPrototype(XmlReader reader)
    {
        Type = reader.GetAttribute("Type");
        XmlReader readerSubtree = reader.ReadSubtree();

        while (readerSubtree.Read())
        {
            switch (readerSubtree.Name)
            {
                case "Name":
                    readerSubtree.Read();
                    Name = readerSubtree.ReadContentAsString();
                    break;

                case "MovementCost":
                    readerSubtree.Read();
                    MovementCost = readerSubtree.ReadContentAsFloat();
                    break;

                case "Width":
                    readerSubtree.Read();
                    Width = readerSubtree.ReadContentAsInt();
                    break;

                case "Height":
                    readerSubtree.Read();
                    Height = readerSubtree.ReadContentAsInt();
                    break;

                case "LinksToNeighbours":
                    readerSubtree.Read();
                    LinksToNeighbour = readerSubtree.ReadContentAsBoolean();
                    break;

                case "RoomEnclosure":
                    readerSubtree.Read();
                    RoomEnclosure = readerSubtree.ReadContentAsBoolean();
                    break;

                case "BuildJob":
                    float workTime = float.Parse(reader.GetAttribute("WorkTime"));
                    JobPriority priority = JobPriority.High;
                    bool repeatingJob = false;
                    bool workAdjacent = false;

                    List<Inventory> inventories = new List<Inventory>();
                    XmlReader subReader = readerSubtree.ReadSubtree();

                    while (subReader.Read())
                    {
                        switch (subReader.Name)
                        {
                            case "JobPriority":
                                subReader.Read();
                                priority = (JobPriority) Enum.Parse(typeof(JobPriority), reader.ReadContentAsString());
                                break;
                            case "RepeatingJob":
                                subReader.Read();
                                repeatingJob = reader.ReadContentAsBoolean();
                                break;
                            case "WorkAdjacent":
                                subReader.Read();
                                workAdjacent = reader.ReadContentAsBoolean();
                                break;
                            case "Inventory":
                                inventories.Add(new Inventory(subReader.GetAttribute("Type"), int.Parse(subReader.GetAttribute("Amount")), 0));
                                break;
                        }
                    }

                    Job job = new Job(null, Type, workTime, priority, BuildCallback, inventories.ToArray(), repeatingJob, workAdjacent);                   
                    World.Current.FurnitureJobPrototypes[Type] = job;
                    break;;

                case "OnUpdate":
                    Lua.Parse(readerSubtree.GetAttribute("FilePath"));

                    string functionName = readerSubtree.GetAttribute("FunctionName");
                    FurnitureUpdate += (sender, args) =>
                    {
                        Lua.Call(functionName, sender, args);
                    };

                    break;

                case "TryEnter":
                    tryEnterFunction = readerSubtree.GetAttribute("FunctionName");
                    break;

                case "Tint":
                    string alpha = readerSubtree.GetAttribute("A");
                    if (string.IsNullOrEmpty(alpha))
                    {
                        alpha = "255";
                    }

                    Tint = new Color32(byte.Parse(readerSubtree.GetAttribute("R")),
                                       byte.Parse(readerSubtree.GetAttribute("G")),
                                       byte.Parse(readerSubtree.GetAttribute("B")),
                                       byte.Parse(alpha));
                    break;
                case "DragMode":
                    readerSubtree.Read();
                    DragMode = (DragMode) Enum.Parse(typeof(DragMode), readerSubtree.ReadContentAsString());

                    break;
                case "WorkPositionOffset":
                    WorkPositionOffset = new Vector2(float.Parse(readerSubtree.GetAttribute("X")), 
                                                     float.Parse(readerSubtree.GetAttribute("Y")));
                    break;

                case "InventorySpawnPositionOffset":
                    InventorySpawnPositionOffset = new Vector2(float.Parse(readerSubtree.GetAttribute("X")),
                                                               float.Parse(readerSubtree.GetAttribute("Y")));
                    break;

                case "Paramaters":
                    ReadXmlParamaters(readerSubtree);
                    break;
            }
        }
    }

    public void ReadXmlParamaters(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Paramater")) return;
        do
        {
            string paramaterName = reader.GetAttribute("Name");
            float value = float.Parse(reader.GetAttribute("Value"));
            FurnitureParameters[paramaterName] = value;
        }
        while (reader.ReadToNextSibling("Paramater"));
    }
}
