using UnityEngine;
using System.Collections.Generic;

public class Room
{
	public float AtmosO2 { get; set; }
    public float AtmosCo2 { get; set; }
    public float AtmosN { get; set; }

    private List<Tile> tiles;

	public Room()
    {
		tiles = new List<Tile>();
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
	        tile.Room = tile.World.OutsideRoom;	
	    }
	    tiles = new List<Tile>();
	}

	public static void CreateRooms(Furniture furniture)
    {
		World world = furniture.Tile.World;
		Room oldRoom = furniture.Tile.Room;

		foreach(Tile tile in furniture.Tile.GetNeighbours())
        {
			FloodFill( tile, oldRoom );
		}
			
		furniture.Tile.Room = null;
		oldRoom.tiles.Remove(furniture.Tile);

        if (oldRoom == world.OutsideRoom) return;
        if(oldRoom.tiles.Count > 0)
        {
            Debug.LogError("Room::CreateRooms: Room 'oldRoom' still has tiles assigned to it!");
        }
        world.DeleteRoom(oldRoom);
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

		while(tileQueue.Count > 0)
        {
			Tile currentTile = tileQueue.Dequeue();


            if (currentTile.Room != oldRoom) continue;
            newRoom.AssignTile(currentTile);

            Tile[] neighbours = currentTile.GetNeighbours();
            foreach(Tile neighbour in neighbours)
            {
                if(neighbour == null || neighbour.Type == TileType.Empty)
                {
                    newRoom.ClearTiles();
                    return;
                }

                if(neighbour.Room == oldRoom && (neighbour.Furniture == null || neighbour.Furniture.RoomEnclosure == false))
                {
                    tileQueue.Enqueue(neighbour);
                }
            }
        }

		newRoom.AtmosCo2 = oldRoom.AtmosCo2;
		newRoom.AtmosN = oldRoom.AtmosN;
		newRoom.AtmosO2 = oldRoom.AtmosO2;

		tile.World.AddRoom(newRoom);
	}
}
