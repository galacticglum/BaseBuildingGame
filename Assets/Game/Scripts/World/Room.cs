using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class Room : IXmlSerializable
{
    public int Index { get { return World.Current.RoomManager.GetRoomIndex(this); }}

    private readonly Dictionary<string, float> atmosphericGasses;
    private List<Tile> tiles;

    public Room()
    {
        tiles = new List<Tile>();
        atmosphericGasses = new Dictionary<string, float>();
    }

	public void AssignTile(Tile tile)
    {
		if(tiles.Contains(tile))
        {
			return;
		}

		if(tile.Room != null)
        {
			tile.Room.tiles.Remove(tile);
		}
			
		tile.Room = this;
		tiles.Add(tile);
	}

	public void ClearTiles()
	{
	    foreach (Tile tile in tiles)
	    {
	        tile.Room = World.Current.RoomManager.OutsideRoom;	
	    }
	    tiles = new List<Tile>();
	}

	public static void CalculateRooms(Tile tile, bool ifOutside = false)
    {
		Room oldRoom = tile.Room;

        if (oldRoom != null)
        {
            foreach (Tile neighbour in tile.GetNeighbours())
            {
                if (neighbour.Room != null && (ifOutside == false || neighbour.Room.IsOutsideRoom()))
                {
                    FloodFill(neighbour, oldRoom);
                }
            }

            tile.Room = null;
            oldRoom.tiles.Remove(tile);

            if (oldRoom.IsOutsideRoom()) return;
            if (oldRoom.tiles.Count > 0)
            {
                Debug.LogError("Room::CreateRooms: Room 'oldRoom' still has tiles assigned to it!");
            }

            World.Current.RoomManager.DeleteRoom(oldRoom);
        }
        else
        {
            FloodFill(tile, null);
        }
    }

	private static void FloodFill(Tile tile, Room oldRoom)
    {
		if(tile == null)
        {
			return;
		}

		if(tile.Room != oldRoom)
        {
			return;
		}

		if(tile.Furniture != null && tile.Furniture.RoomEnclosure)
        {
			return;
		}

		if(tile.Type == TileType.Empty)
        {
			return;
		}	

		Room newRoom = new Room();
		Queue<Tile> tileQueue = new Queue<Tile>();
		tileQueue.Enqueue(tile);

        bool isConnectedToOutside = false;

		while(tileQueue.Count > 0)
        {
			Tile currentTile = tileQueue.Dequeue();

            if (currentTile.Room != newRoom)
            {
                newRoom.AssignTile(currentTile);

                Tile[] neighbours = currentTile.GetNeighbours();
                foreach (Tile neighbour in neighbours)
                {
                    if (neighbour == null || neighbour.Type == TileType.Empty)
                    {
                        isConnectedToOutside = true;
                    }
                    else if (neighbour.Room != newRoom && (neighbour.Furniture == null || neighbour.Furniture.RoomEnclosure == false))
                    { 
                        tileQueue.Enqueue(neighbour);
                    }
                }
            }
        }

        if (isConnectedToOutside)
        {
            newRoom.ClearTiles();
            return;
        }

        if (oldRoom != null)
        {
            newRoom.CloneAtmosphericGasses(oldRoom);
        }
        else
        {
            // TODO: Distribute and merge gas.   
        }

        World.Current.RoomManager.AddRoom(newRoom);
	}

    public void ModifyGasValue(string name, float amount)
    {
        if (IsOutsideRoom()) return;

        if (atmosphericGasses.ContainsKey(name))
        {
            atmosphericGasses[name] += amount;
        }
        else
        {
            atmosphericGasses[name] = amount;
        }

        if (atmosphericGasses[name] < 0)
        {
            atmosphericGasses[name] = 0;
        }
    }

    public float GetGasValue(string name)
    {
        return atmosphericGasses.ContainsKey(name) ? atmosphericGasses[name] : 0;
    }

    public float GetGasPercentage(string name)
    {
        if (atmosphericGasses.ContainsKey(name) == false)
        {
            return 0;
        }

        float gasTotal = atmosphericGasses.Keys.Sum(gas => atmosphericGasses[gas]);
        return atmosphericGasses[name] / gasTotal;
    }

    public string[] GetAllGasses()
    {
        return atmosphericGasses.Keys.ToArray();
    }

    private void CloneAtmosphericGasses(Room room)
    {
        foreach (string gas in room.atmosphericGasses.Keys)
        {
            atmosphericGasses[gas] = room.atmosphericGasses[gas];
        }
    }

    public bool IsOutsideRoom()
    {
        return this == World.Current.RoomManager.OutsideRoom;
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        // Write atmospheric gas info
        foreach (string gas in atmosphericGasses.Keys)
        {
            writer.WriteStartElement("AtmosphericGas");
            writer.WriteAttributeString("Name", gas);
            writer.WriteAttributeString("Value", atmosphericGasses[gas].ToString());
            writer.WriteEndElement();
        }
    }

    public void ReadXml(XmlReader reader)
    {
        // Read atmospheric gas info
        if (!reader.ReadToDescendant("AtmosphericGas")) return;
        do
        {
            string gas = reader.GetAttribute("Name");
            float value = float.Parse(reader.GetAttribute("Value"));
            atmosphericGasses[gas] = value;
        }
        while (reader.ReadToNextSibling("AtmosphericGas"));
    }
}
