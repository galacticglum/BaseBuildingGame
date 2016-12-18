using UnityEngine;

public class ConstructionController : MonoBehaviour
{
    private bool furnitureConstruction;
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

            Job job;
            if (WorldController.Instance.World.FurnitureJobPrototypes.ContainsKey(furnitureType))
            {
                job = WorldController.Instance.World.FurnitureJobPrototypes[furnitureType].Clone();
                job.Tile = tile;
            }
            else
            {
                Debug.LogWarning("ConstructionController::DoBuild: There is no furniture job prototype for '" + furnitureType + "'.");
                job = new Job(tile, furnitureType, FurnitureBehaviours.BuildFurniture, 0.1f);
            }

            tile.PendingFurnitureJob = job;
            job.JobCancel += (sender, args) =>
            {
                args.Job.Tile.PendingFurnitureJob = null;
            };

            WorldController.Instance.World.JobQueue.Enqueue(job);
        }
        else
        {
            tile.Type = constructionTileType;
        }
    }
}
