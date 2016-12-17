using UnityEngine;
using System.Collections.Generic;

public class Room
{
    public float AtmosO2 { get; set; }
    public float AtmosCO2 { get; set; }
    public float AtmosN { get; set; }

    private List<Tile> tiles;

    public Room()
    {
        tiles = new List<Tile>();
    }

    public void AddTile(Tile tile)
    {
        if (tiles.Contains(tile))
        {
            // This tile is already in this room
            return;
        }

        if (tile.Room != null)
        {
            // Belongs to some other room
            tile.Room.tiles.Remove(tile);
        }

        tile.Room = this;
        tiles.Add(tile);
    }

    public void ClearTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Room = tile.World.OutsideRoom;
        }
        tiles = new List<Tile>();
    }

    public static void CreateRooms(Furniture furniture)
    {
        World world = furniture.Tile.World;
        Room oldRoom = furniture.Tile.Room;

        // Try build new rooms for each of our NESW directions
        foreach (Tile neighbour in furniture.Tile.GetNeighbours())
        {
            FloodFill(neighbour, oldRoom);
        }

        furniture.Tile.Room = null;
        oldRoom.tiles.Remove(furniture.Tile);

        if (oldRoom == world.OutsideRoom) return;
        if (oldRoom.tiles.Count > 0)
        {
            Debug.LogError("Room::CreateRooms: Room 'oldRoom' still has tiles assigned to it!");
        }

        world.DeleteRoom(oldRoom);
    }

    protected static void FloodFill(Tile tile, Room oldRoom)
    {
        if (tile == null)
        {
            // We are trying to flood fill off a non-existent tile; possibly off the map? Just return without doing anything
            return;           
        }

        if (tile.Room != oldRoom)
        {
            // This tile was already assigned to another room, just return without doing anything
            return;
        }

        if (tile.Furniture != null && tile.Furniture.RoomEnclosure)
        {
            // This tile has a furniture in it so return without doing anything; we can't do a room here
            return;
        }

        if (tile.Type == TileType.Empty)
        {
            // This tile is an empty space which means it must remain part of the outside. 
            return;
        }

        Room newRoom = new Room();
        Queue<Tile> tileQueue = new Queue<Tile>();
        tileQueue.Enqueue(tile);

        while (tileQueue.Count > 0)
        {
            Tile t = tileQueue.Dequeue();
            if (t.Room != oldRoom) continue;

            newRoom.AddTile(t);

            Tile[] neighbours = t.GetNeighbours();
            foreach (Tile neighbour in neighbours)
            {
                if (neighbour == null || neighbour.Type == TileType.Empty)
                {
                    // We have "hit" open space (this could mean we are at the edge of the map or an empty tile). 
                    // The room we are creating is actually part of the "outside" so return
                    newRoom.ClearTiles();
                    return;
                }

                if (neighbour.Room == oldRoom && (neighbour.Furniture == null || neighbour.Furniture.RoomEnclosure == false))
                {
                    tileQueue.Enqueue(neighbour);
                }
            }
        }

        newRoom.AtmosO2 = oldRoom.AtmosO2;
        newRoom.AtmosCO2 = oldRoom.AtmosCO2;
        newRoom.AtmosN = oldRoom.AtmosN;
        tile.World.AddRoom(newRoom);
    }
}
