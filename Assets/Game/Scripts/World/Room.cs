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

    private List<Tile> tiles;
 
    public Room()
    {
        tiles = new List<Tile>();
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

        World.Current.RoomManager.Add(newRoom);
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
    }

    public void ReadXml(XmlReader reader)
    {
    }
}
