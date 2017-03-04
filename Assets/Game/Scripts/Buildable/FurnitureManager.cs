using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

public class FurnitureManager : IEnumerable<Furniture>, IXmlSerializable
{
    private readonly List<Furniture> furnitures;

    public event FurnitureCreatedEventHandler FurnitureCreated;
    public void OnFurnitureCreated(FurnitureEventArgs args)
    {
        FurnitureCreatedEventHandler furnitureCreated = FurnitureCreated;
        if (furnitureCreated != null)
        {
            furnitureCreated(this, args);
        }
    }

    public FurnitureManager()
    {
        furnitures = new List<Furniture>();
    }

    public void Update(float deltaTime)
    {
        foreach (Furniture furniture in furnitures)
        {
            furniture.Update(deltaTime);
        }
    }

    public Furniture Place(string type, Tile tile, bool floodFill = true)
    {
        if (PrototypeManager.Furnitures.Contains(type) == false)
        {
            Debug.LogError("furniturePrototypes doesn't contain a proto for key: " + type);
            return null;
        }

        Furniture furnitureInstance = Furniture.Place(PrototypeManager.Furnitures[type], tile);
        if (furnitureInstance == null)
        {
            return null;
        }

        furnitureInstance.FurnitureRemoved += OnFurnitureRemoved;
        furnitures.Add(furnitureInstance);

        // Do we need to recalculate our rooms?
        if (floodFill && furnitureInstance.RoomEnclosure)
        {
            Room.CalculateRooms(furnitureInstance.Tile);
        }

        OnFurnitureCreated(new FurnitureEventArgs(furnitureInstance));

        if (furnitureInstance.MovementCost == 1) return furnitureInstance;
        if (World.Current.TileGraph != null)
        {
            World.Current.TileGraph.Regenerate(tile);
        }

        return furnitureInstance;
    }

    public bool IsPlacementValid(string type, Tile tile)
    {
        return PrototypeManager.Furnitures[type].IsValidPosition(tile);
    }

    public int FurnituresWithTypeCount(string type)
    {
        return furnitures.Count(furniture => furniture.Type == type);
    }

    private void OnFurnitureRemoved(object sender, FurnitureEventArgs args)
    {
        furnitures.Remove(args.Furniture);
    }

    public IEnumerable<Furniture> Find(Func<Furniture, bool> filter)
    {
        return furnitures.Where(filter);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return furnitures.GetEnumerator();
    }

    public IEnumerator<Furniture> GetEnumerator()
    {
        return ((IEnumerable<Furniture>) furnitures).GetEnumerator();
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("Furnitures");
        foreach (Furniture furniture in furnitures)
        {
            writer.WriteStartElement("Furniture");
            furniture.WriteXml(writer);
            writer.WriteEndElement();

        }
        writer.WriteEndElement();
    }

    public void ReadXml(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Furniture")) return;

        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            Furniture furniture = Place(reader.GetAttribute("objectType"), World.Current.GetTileAt(x, y), false);
            furniture.ReadXml(reader);
        }
        while (reader.ReadToNextSibling("Furniture"));
    }
}