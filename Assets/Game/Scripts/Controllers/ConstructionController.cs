using UnityEngine;

public class ConstructionController : MonoBehaviour
{
    public string ConstructionObjectType { get; private set; }
    public ConstructionMode ConstructionMode { get; private set; }

	private TileType constructionTileType = TileType.Floor;

    private void Start()
    {
        ConstructionMode = ConstructionMode.Floor;
    }

	public void BuildFloor( )
    {
		ConstructionMode = ConstructionMode.Floor;
		constructionTileType = TileType.Floor;

        FindObjectOfType<MouseController>().InConstructionMode();
	}
	
	public void RemoveFloor( )
    {
		ConstructionMode = ConstructionMode.Floor;
		constructionTileType = TileType.Empty;

        FindObjectOfType<MouseController>().InConstructionMode();
    }

    public void BuildFurniture(string type)
    {
		ConstructionMode = ConstructionMode.Furniture;
		ConstructionObjectType = type;

        FindObjectOfType<MouseController>().InConstructionMode();
    }

    public void DeconstructFurniture()
    {
        ConstructionMode = ConstructionMode.Deconstruct;
        FindObjectOfType<MouseController>().InConstructionMode();
    }

    public void PathfindingTest()
    {
		WorldController.Instance.World.SetupPathfindingExample();
	}

	public void DoBuild(Tile tile)
    {
		if(ConstructionMode == ConstructionMode.Furniture)
        {
			string furnitureType = ConstructionObjectType;

            if (!WorldController.Instance.World.IsFurniturePlacementValid(furnitureType, tile) || tile.PendingFurnitureJob != null) return;

            Job job;
            if(WorldController.Instance.World.FurnitureJobPrototypes.ContainsKey(furnitureType))
            {
                job = WorldController.Instance.World.FurnitureJobPrototypes[furnitureType].Clone();
                job.Tile = tile;
            }
            else
            {
                Debug.LogError("There is no furniture job prototype for '" + furnitureType + "'");
                job = new Job(tile, furnitureType, FurnitureBehaviours.BuildFurniture, 0.1f, null);
            }

            tile.PendingFurnitureJob = job;
            job.FurniturePrototype = WorldController.Instance.World.FurniturePrototypes[furnitureType];
            job.JobStopped += (sender, args) =>
            {
                args.Job.Tile.PendingFurnitureJob = null;
            };

            WorldController.Instance.World.JobQueue.Enqueue(job);
        }
		else if (ConstructionMode == ConstructionMode.Floor)
		{
		    tile.Type = constructionTileType;
		}
		else if(ConstructionMode == ConstructionMode.Deconstruct && tile.Furniture != null)
		{
            tile.Furniture.Deconstruct();
		}
	}

    public bool IsFurnitureDraggable()
    {
        if (ConstructionMode == ConstructionMode.Floor || ConstructionMode == ConstructionMode.Deconstruct)
        {
            return true;
        }

        Furniture furniturePrototype = WorldController.Instance.World.FurniturePrototypes[ConstructionObjectType];
        return furniturePrototype.Width == 1 && furniturePrototype.Height == 1;
    }
}
