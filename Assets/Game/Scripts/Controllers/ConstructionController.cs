using UnityEngine;

public class ConstructionController : MonoBehaviour
{
    private bool furnitureConstruction = false;
    private TileType constructionTileType = TileType.Floor;
    private string buildModeObjectType;
    
    public void BuildFloor()
    {
        furnitureConstruction = false;
        constructionTileType = TileType.Floor;
    }

    public void Bulldoze()
    {
        furnitureConstruction = false;
        constructionTileType = TileType.Empty;
    }

    public void BuildFurniture(string objectType)
    {
        furnitureConstruction = true;
        buildModeObjectType = objectType;
    }

    public void PathfindingTest()
    {
        WorldController.Instance.World.SetupPathfindingExample();
    }

    public void DoBuild(Tile tile)
    {
        if (furnitureConstruction)
        {
            // Can we build the furniture in the selected tile?
            string furnitureType = buildModeObjectType;

            if (!WorldController.Instance.World.IsFurniturePlacementValid(furnitureType, tile) || tile.PendingFurnitureJob != null) return;

            Job j = new Job(tile, furnitureType, (sender, args) =>
            {
                WorldController.Instance.World.PlaceFurniture(furnitureType, args.Job.Tile);
                tile.PendingFurnitureJob = null;
            });

            tile.PendingFurnitureJob = j;

            j.JobCancel += ((sender, args) =>
            {
                args.Job.Tile.PendingFurnitureJob = null;
            });

            WorldController.Instance.World.JobQueue.Enqueue(j);
        }
        else
        {
            tile.Type = constructionTileType;
        }
    }
}
