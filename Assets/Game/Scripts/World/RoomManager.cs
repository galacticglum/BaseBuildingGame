using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class RoomManager : IEnumerable<Room>, IXmlSerializable
{
    public LuaEventManager EventManager { get; protected set; }
    public Room OutsideRoom { get { return rooms[0]; } }

    private readonly List<Room> rooms;

    public RoomManager()
    {
        rooms = new List<Room>
        {
            new Room()
        };
    }

    public void AddRoom(Room room)
    {
        rooms.Add(room);
    }

    public void DeleteRoom(Room room)
    {
        if (room == OutsideRoom)
        {
            Debug.LogError("World::DeleteRoom: tried to delete the 'outside' room!");
            return;
        }

        rooms.Remove(room);
        room.ClearTiles();
    }

    public Room GetRoom(int index)
    {
        if (index < 0 || index > rooms.Count - 1)
        {
            return null;
        }

        return rooms[index];
    }

    public int GetRoomIndex(Room room)
    {
        return rooms.IndexOf(room);
    }

    public IEnumerable<Room> Find(Func<Room, bool> filter)
    {
        return rooms.Where(filter);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return rooms.GetEnumerator();
    }

    public IEnumerator<Room> GetEnumerator()
    {
        return ((IEnumerable<Room>)rooms).GetEnumerator();
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("Rooms");
        foreach (Room room in rooms)
        {
            if (room == OutsideRoom) continue;

            writer.WriteStartElement("Room");
            room.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    public void ReadXml(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Room")) return;

        do
        {
            Room room = new Room();
            rooms.Add(room);
            room.ReadXml(reader);
        }
        while (reader.ReadToNextSibling("Room"));
    }
}
