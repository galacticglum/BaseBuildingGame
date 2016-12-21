using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Furniture : IXmlSerializable
{
	protected Dictionary<string, float> FurnitureParameters { get; set; }
	public Action<Furniture, float> UpdateBehaviours { get; set; }
    public Func<Furniture, TileEnterability> TryEnter { get; set; }

    public Tile Tile { get; protected set; }
    public string Type { get; protected set; }
    
    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public float MovementCost { get; protected set; }
    public bool LinksToNeighbour { get; protected set; }
    public bool RoomEnclosure { get; protected set; }

    public Color Tint { get; set; }

    public event FurnitureChangedEventHandler FurnitureChanged;
    public void OnFurnitureChanged(FurnitureChangedEventArgs args)
    {
        if (FurnitureChanged != null)
        {
            FurnitureChanged(this, args);
        }
    }

    private readonly List<Job> jobs;

	public Furniture()
    {
		FurnitureParameters = new Dictionary<string, float>();
		jobs = new List<Job>();
	}

    public Furniture(string type) : this(type, 1, 1, 1, false, false) { }
	public Furniture (string type, float movementCost, int width, int height, bool linksToNeighbour, bool roomEnclosure)
    {
		Type = type;
		MovementCost = movementCost;
		RoomEnclosure = roomEnclosure;
        LinksToNeighbour = linksToNeighbour;
        Tint = Color.white;

        FurnitureParameters = new Dictionary<string, float>();

        this.Width = width;
		this.Height = height;
	}

    protected Furniture(Furniture furniture)
    {
        Type = furniture.Type;
        MovementCost = furniture.MovementCost;
        RoomEnclosure = furniture.RoomEnclosure;
        Tint = Color.white;
        Width = furniture.Width;
        Height = furniture.Height;
        Tint = furniture.Tint;
        LinksToNeighbour = furniture.LinksToNeighbour;

        FurnitureParameters = new Dictionary<string, float>(furniture.FurnitureParameters);
        jobs = new List<Job>();

        if (furniture.UpdateBehaviours != null)
            UpdateBehaviours = (Action<Furniture, float>)furniture.UpdateBehaviours.Clone();

        TryEnter = furniture.TryEnter;
    }

    public virtual Furniture Clone()
    {
        return new Furniture(this);
    }

    public void Update(float deltaTime)
    {
        if (UpdateBehaviours != null)
        {
            UpdateBehaviours(this, deltaTime);
        }
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

        Tile tileAt = tile.World.GetTileAt(x, y+1);
        if(HasFurnitureOfSameType(furnitureInstance, tileAt))
        {
            tileAt.Furniture.OnFurnitureChanged(new FurnitureChangedEventArgs(tileAt.Furniture));
        }
        tileAt = tile.World.GetTileAt(x+1, y);
        if(HasFurnitureOfSameType(furnitureInstance, tileAt))
        {
            tileAt.Furniture.OnFurnitureChanged(new FurnitureChangedEventArgs(tileAt.Furniture));
        }
        tileAt = tile.World.GetTileAt(x, y-1);
        if(HasFurnitureOfSameType(furnitureInstance, tileAt))
        {
            tileAt.Furniture.OnFurnitureChanged(new FurnitureChangedEventArgs(tileAt.Furniture));
        }
        tileAt = tile.World.GetTileAt(x-1, y);
        if(HasFurnitureOfSameType(furnitureInstance, tileAt))
        {
            tileAt.Furniture.OnFurnitureChanged(new FurnitureChangedEventArgs(tileAt.Furniture));
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
                Tile tileAt = tile.World.GetTileAt(x, y);

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
		jobs.Add(job);
		Tile.World.JobQueue.Enqueue(job);
	}

	public void RemoveJob(Job job)
    {
		jobs.Remove(job);
		job.CancelJob();
		Tile.World.JobQueue.Remove(job);
	}

	public void ClearJobs()
    {
		foreach(Job j in jobs)
        {
			RemoveJob(j);
		}
	}

	public bool IsStockpile()
    {
		return Type == "Stockpile";
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
        if (!reader.ReadToDescendant("Paramater")) return;

        do
        {
            string name = reader.GetAttribute("Name");
            float value = float.Parse(reader.GetAttribute("Value"));
            FurnitureParameters[name] = value;
        }
        while (reader.ReadToNextSibling("Paramater"));
    }
}
