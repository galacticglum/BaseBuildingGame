using System;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

public class Furniture : IXmlSerializable
{
    // Pseudo "scripting" system
    protected Dictionary<string, float> FurnitureParameters { get; set; }
    public Action<Furniture, float> UpdateBehaviours { get; set; }
    public Func<Furniture, TileEnterability> TryEnter { get; set; }

    public Tile Tile { get; protected set; }
    public string Type { get; protected set; }

    public float MovementCost { get; protected set; }
    public bool LinksToNeighbour { get; protected set; }
    public bool RoomEnclosure { get; protected set; }

    public event FurnitureChangedEventHandler FurnitureChanged;
    public void OnFurnitureChanged(FurnitureChangedEventArgs args)
    {
        FurnitureChangedEventHandler furnitureChanged = FurnitureChanged;
        if (furnitureChanged != null)
        {
            furnitureChanged(this, args);
        }
    }

    private readonly int width;
    private readonly int height;

    // TODO: Implement larger objects
    // TODO: Implement object rotation

    public Furniture()
    {
        FurnitureParameters = new Dictionary<string, float>();   
    }

    protected Furniture(Furniture furniture)
    {
        FurnitureParameters = new Dictionary<string, float>(furniture.FurnitureParameters);
        if (furniture.UpdateBehaviours != null)
        {
            UpdateBehaviours = (Action<Furniture, float>) furniture.UpdateBehaviours.Clone();
        }

        TryEnter = furniture.TryEnter;
        Type = furniture.Type;
        MovementCost = furniture.MovementCost;
        width = furniture.width;
        height = furniture.height;
        LinksToNeighbour = furniture.LinksToNeighbour;
        RoomEnclosure = furniture.RoomEnclosure;
    }

    public virtual Furniture Clone()
    {
        return new Furniture(this);
    }

    public Furniture(string type) : this(type, 1, 1, 1, false, false) { }
    public Furniture(string type, float movementCost, int width, int height, bool linksToNeighbour, bool roomEnclosure)
    {
        FurnitureParameters = new Dictionary<string, float>();

        this.width = width;
        this.height = height;
        Type = type;
        MovementCost = movementCost;
        LinksToNeighbour = linksToNeighbour;
        RoomEnclosure = roomEnclosure;
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
        if (furniture.IsValidPosition(tile) == false)
        {
            Debug.LogError("Furniture::Place: Position Validity Function returned 'false'.");
            return null;
        }

        Furniture instance = new Furniture(furniture) { Tile = tile };

        // FIXME: This assumes we are 1x1!
        if (tile.PlaceFurniture(instance) == false)
        {
            return null;
        }

        if (!instance.LinksToNeighbour) return instance;

        int x = tile.X;
        int y = tile.Y;

        Tile neighbourTile = tile.World.GetTileAt(x, y + 1);
        if (HasFurnitureOfSameType(instance, neighbourTile))
        {
            neighbourTile.Furniture.OnFurnitureChanged(new FurnitureChangedEventArgs(neighbourTile.Furniture));
        }

        neighbourTile = tile.World.GetTileAt(x + 1, y);
        if (HasFurnitureOfSameType(instance, neighbourTile))
        {
            neighbourTile.Furniture.OnFurnitureChanged(new FurnitureChangedEventArgs(neighbourTile.Furniture));
        }

        neighbourTile = tile.World.GetTileAt(x, y - 1);
        if (HasFurnitureOfSameType(instance, neighbourTile))
        {
            neighbourTile.Furniture.OnFurnitureChanged(new FurnitureChangedEventArgs(neighbourTile.Furniture));
        }

        neighbourTile = tile.World.GetTileAt(x - 1, y);
        if (HasFurnitureOfSameType(instance, neighbourTile))
        {
            neighbourTile.Furniture.OnFurnitureChanged(new FurnitureChangedEventArgs(neighbourTile.Furniture));
        }

        return instance;
    }

    private static bool HasFurnitureOfSameType(Furniture furniture, Tile tile)
    {
        return (tile != null && tile.Furniture != null && tile.Furniture.Type == furniture.Type && tile.Furniture.FurnitureChanged != null);
    }

    public bool IsValidPosition(Tile tile)
    {
        // Make sure tile is FLOOR
        if(tile.Type != TileType.Floor)
        {
            return false;
        }

        // Make sure tile doesn't already have furniture
        return tile.Furniture == null;
    }

    public float GetParameter(string name, float defaultValue = 0)
    {
        return FurnitureParameters.ContainsKey(name) == false ? defaultValue : FurnitureParameters[name];
    }

    public void SetParamater(string name, float value)
    {
        FurnitureParameters[name] = value;
    }

    public void ModifyParameter(string name, float value)
    {
        if (FurnitureParameters.ContainsKey(name) == false)
        {
            FurnitureParameters[name] = value;
            return;
        }
        FurnitureParameters[name] += value;
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
