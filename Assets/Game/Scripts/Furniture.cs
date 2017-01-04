using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Furniture : IXmlSerializable
{
    protected Dictionary<string, float> FurnitureParameters { get; set; }
    //public event FurnitureUpdateEventHandler UpdateBehaviours;
    //public void OnFurnitureUpdate(FurnitureUpdateEventArgs args)
    //{
    //    FurnitureUpdateEventHandler updateBehaviours = UpdateBehaviours;
    //    if (updateBehaviours != null)
    //    {
    //        updateBehaviours(this, args);
    //    }
    //}

    //public Func<Furniture, TileEnterability> TryEnter { get; set; }

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

    private readonly List<Job> jobs;

    private readonly List<string> updateBehaviours;
    private string tryEnterFunction = string.Empty;

    public Furniture()
    {
        FurnitureParameters = new Dictionary<string, float>();
        updateBehaviours = new List<string>();

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
        Tint = furniture.Tint;
        LinksToNeighbour = furniture.LinksToNeighbour;
        WorkPositionOffset = furniture.WorkPositionOffset;

        FurnitureParameters = new Dictionary<string, float>(furniture.FurnitureParameters);
        jobs = new List<Job>();

        if (furniture.updateBehaviours != null)
        {
            updateBehaviours = furniture.updateBehaviours;
        }

        tryEnterFunction = furniture.tryEnterFunction;
    }

    public virtual Furniture Clone()
    {
        return new Furniture(this);
    }

    public void Update(float deltaTime)
    {
        FurnitureBehaviours.Execute(updateBehaviours.ToArray(), this, deltaTime);
        //OnFurnitureUpdate(new FurnitureUpdateEventArgs(this, deltaTime));
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

        int x = tile.X;
        int y = tile.Y;

        Tile tileAt = World.Current.GetTileAt(x, y+1);
        if(HasFurnitureOfSameType(furnitureInstance, tileAt))
        {
            tileAt.Furniture.OnFurnitureChanged(new FurnitureEventArgs(tileAt.Furniture));
        }
        tileAt = World.Current.GetTileAt(x+1, y);
        if(HasFurnitureOfSameType(furnitureInstance, tileAt))
        {
            tileAt.Furniture.OnFurnitureChanged(new FurnitureEventArgs(tileAt.Furniture));
        }
        tileAt = World.Current.GetTileAt(x, y-1);
        if(HasFurnitureOfSameType(furnitureInstance, tileAt))
        {
            tileAt.Furniture.OnFurnitureChanged(new FurnitureEventArgs(tileAt.Furniture));
        }
        tileAt = World.Current.GetTileAt(x-1, y);
        if(HasFurnitureOfSameType(furnitureInstance, tileAt))
        {
            tileAt.Furniture.OnFurnitureChanged(new FurnitureEventArgs(tileAt.Furniture));
        }

        return furnitureInstance;
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

                if (tileAt.Furniture != null)
                {
                    return false;
                }
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
        OnFurnitureRemoved(new FurnitureEventArgs(this));

        if (RoomEnclosure)
        {
            Room.CalculateRooms(Tile);
        }
        
        World.Current.InvalidateTileGraph();
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

        return (TileEnterability)FurnitureBehaviours.Execute(tryEnterFunction, this).UserData.Object;
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
                    List<Inventory> inventories = new List<Inventory>();
                    XmlReader inventoryReader = readerSubtree.ReadSubtree();

                    while (inventoryReader.Read())
                    {
                        if (inventoryReader.Name == "Inventory")
                        {
                            inventories.Add(new Inventory(inventoryReader.GetAttribute("Type"), int.Parse(inventoryReader.GetAttribute("Amount")), 0));
                        }
                    }

                    World.Current.FurnitureJobPrototypes[Type] = new Job(null, Type, FurnitureBehaviours.BuildFurniture, workTime, inventories.ToArray()); 
                    break;

                case "OnUpdate":
                    updateBehaviours.Add(readerSubtree.GetAttribute("FunctionName"));
                    break;

                case "TryEnter":
                    tryEnterFunction = readerSubtree.GetAttribute("FunctionName");
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
