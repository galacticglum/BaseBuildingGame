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
    public int Index { get { return World.Current.RoomManager.GetRoomIndex(this); } }
    public int Size { get { return tiles.Count; } }

    public float Pressure { get { return atmosphericGasses.Keys.Sum(n => atmosphericGasses[n]); } }
    public string[] GasNames { get { return atmosphericGasses.Keys.ToArray(); } }

    private readonly Dictionary<string, float> atmosphericGasses; 
    private readonly Dictionary<string, string> deltaGas;

    private List<Tile> tiles;
 
    public Room()
    {
        tiles = new List<Tile>();
        atmosphericGasses = new Dictionary<string, float>();
        deltaGas = new Dictionary<string, string>();
    }

    public void AssignTile(Tile tile)
    {
        if (tiles.Contains(tile))
        {
            return;
        }

        if (tile.Room != null)
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

    public bool IsOutsideRoom()
    {
        return this == World.Current.RoomManager.OutsideRoom;
    }

    public void ModifyGasValue(string name, float amount)
    {
        if (IsOutsideRoom())
        {
            return;
        }

        if (atmosphericGasses.ContainsKey(name))
        {
            atmosphericGasses[name] += amount;
            if (Mathf.Sign(amount) == 1)
            {
                deltaGas[name] = "+";
            }
            else
            {
                deltaGas[name] = "-";
            }
        }
        else
        {
            atmosphericGasses[name] = amount;
            deltaGas[name] = "=";
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

    public float GetGasPressure(string name)
    {
        if (atmosphericGasses.ContainsKey(name))
        {
            return atmosphericGasses[name] / Size;
        }

        return 0;
    }

    public float GetGasFraction(string name)
    {
        if (atmosphericGasses.ContainsKey(name) == false)
        {
            return 0;
        }

        float t = atmosphericGasses.Keys.Sum(n => atmosphericGasses[n]);

        return atmosphericGasses[name] / t;
    }

    public string GetModifiedGases(string name)
    {
        return deltaGas.ContainsKey(name) ? deltaGas[name] : "=";
    }

    public void EqualizeGas(Room room, float leakRate)
    {
        if (room == null)
        {
            return;
        }

        List<string> gasses = GasNames.ToList();
        gasses = gasses.Union(room.GasNames.ToList()).ToList();
        foreach (string gas in gasses)
        {
            float pressureDifference = GetGasPressure(gas) - room.GetGasPressure(gas);
            ModifyGasValue(gas, (-1) * pressureDifference * leakRate);
            room.ModifyGasValue(gas, pressureDifference * leakRate);
        }
    }

    public static void EqualizeGas(Tile tile, float leakRate)
    {
        List<Room> roomsDone = new List<Room>();
        foreach (Tile neighbour in tile.GetNeighbours())
        {
            // TODO: Verify that gas still leaks to the outside
            if (neighbour.Room == null)
            {
                continue;
            }

            if (roomsDone.Contains(neighbour.Room) != false) continue;
            foreach (Room r in roomsDone) 
            {
                neighbour.Room.EqualizeGas(r, leakRate);
            }

            roomsDone.Add(neighbour.Room);
        }
    }


    public static void CalculateRooms(Tile sourceTile, bool onlyIfOutside = false)
    {
        Room oldRoom = sourceTile.Room;
        if (oldRoom != null)
        {
            int oldRoomSize = oldRoom.Size;
            foreach (Tile neighbour in sourceTile.GetNeighbours())
            {
                if (neighbour.Room != null && (onlyIfOutside == false || neighbour.Room.IsOutsideRoom()))
                {
                    FloodFill(neighbour, oldRoom, oldRoomSize);
                }
            }

            sourceTile.Room = null;
            oldRoom.tiles.Remove(sourceTile);

            if (oldRoom.IsOutsideRoom()) return;
            World.Current.RoomManager.Delete(oldRoom);
        }
        else
        {
            FloodFill(sourceTile, null, 0);
        }
    }

    private static void FloodFill(Tile tile, Room oldRoom, int sizeOfOldRoom)
    {
        if (tile == null)
        {
            return;
        }

        if (tile.Room != oldRoom)
        {
            return;
        }

        if (tile.Furniture != null && tile.Furniture.RoomEnclosure)
        {
            return;
        }

        if (tile.Type == TileType.Empty)
        {
            return;
        }

        List<Room> oldRooms = new List<Room>();
        Room newRoom = new Room();
        Queue<Tile> tileQueue = new Queue<Tile>();
        tileQueue.Enqueue(tile);

        bool isConnectedToOutside = false;

        while (tileQueue.Count > 0)
        {
            Tile currentTile = tileQueue.Dequeue();

            if (currentTile.Room == newRoom) continue;
            if (currentTile.Room != null && oldRooms.Contains(currentTile.Room) == false)
            {
                oldRooms.Add(currentTile.Room);
                newRoom.MoveGas(currentTile.Room);
            }

            newRoom.AssignTile(currentTile);

            foreach (Tile neighbour in currentTile.GetNeighbours())
            {
                if (neighbour == null || neighbour.Type == TileType.Empty)
                {
                    isConnectedToOutside = true;
                }
                else
                {
                    if (
                        neighbour.Room != newRoom && (neighbour.Furniture == null || neighbour.Furniture.RoomEnclosure == false))
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
            newRoom.CloneGasData(oldRoom, sizeOfOldRoom);
        }

        World.Current.RoomManager.Add(newRoom);
    }

    private void MoveGas(Room room)
    {
        foreach (string gas in room.atmosphericGasses.Keys)
        {
            ModifyGasValue(gas, room.atmosphericGasses[gas]);
        }
    }

    private void CloneGasData(Room other, int roomArea)
    {
        foreach (string gas in other.atmosphericGasses.Keys)
        {
            atmosphericGasses[gas] = other.atmosphericGasses[gas] / roomArea * Size;
        }
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        // Write out gas info
        foreach (string gas in atmosphericGasses.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", gas);
            writer.WriteAttributeString("value", atmosphericGasses[gas].ToString());
            writer.WriteEndElement();
        }
    }

    public void ReadXml(XmlReader reader)
    {
        if (!reader.ReadToDescendant("Param")) return;
        do
        {
            string name = reader.GetAttribute("name");
            float value = float.Parse(reader.GetAttribute("value"));
            atmosphericGasses[name] = value;
        } 
        while (reader.ReadToNextSibling("Param"));
    }
}
