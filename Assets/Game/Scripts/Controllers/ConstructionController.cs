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
	    switch (ConstructionMode)
	    {
	        case ConstructionMode.Furniture:
	            string furnitureType = ConstructionObjectType;
	            if (WorldController.Instance.World.IsFurniturePlacementValid(furnitureType, tile) && IsBuildJobOverlap(furnitureType, tile) == false)
	            {

	                if (tile.Furniture != null)
	                {
	                    tile.Furniture.Deconstruct();
	                }

	                Job job;
	                if (WorldController.Instance.World.FurnitureJobPrototypes.ContainsKey(furnitureType))
	                {
	                    job = WorldController.Instance.World.FurnitureJobPrototypes[furnitureType].Clone();
	                    job.Tile = tile;
	                }
	                else
	                {
	                    job = new Job(tile, furnitureType, 0.1f, JobPriority.High, Furniture.BuildCallback, null, false,
	                        true);
	                }

                    job.FurniturePrototype = PrototypeManager.Furnitures[furnitureType];

	                for (int xOffset = tile.X; xOffset < (tile.X + PrototypeManager.Furnitures[furnitureType].Width); xOffset++)
	                {
	                    for (int yOffset = tile.Y; yOffset < (tile.Y + PrototypeManager.Furnitures[furnitureType].Height); yOffset++)
	                    {
	                        Tile tileAt = WorldController.Instance.World.GetTileAt(xOffset, yOffset);
	                        tileAt.PendingBuildJob = job;
                            job.JobStopped += (sender, args) =>
                            {
                                tileAt.PendingBuildJob = null;
                            };
                        }
	                }

	                WorldController.Instance.World.JobQueue.Enqueue(job);
	            }
	            break;

	        case ConstructionMode.Floor:
	            TileType tileType = constructionTileType;
	            if (tile.Type != tileType && tile.Furniture == null && tile.PendingBuildJob == null)
	            {
	                Job job = new Job(tile, tileType, 0.1f, JobPriority.High, Tile.OnJobCompleted, null);
	                tile.PendingBuildJob = job;
	                job.JobStopped += (sender, args) =>
	                {
	                    args.Job.Tile.PendingBuildJob = null;
	                };

                    WorldController.Instance.World.JobQueue.Enqueue(job);
	            }
	            break;
	        case ConstructionMode.Deconstruct:
	            if (tile.Furniture != null)
	            {
	                tile.Furniture.Deconstruct();
	            }
	            break;
	    }
	}

    public bool IsFurnitureDraggable()
    {
        if (ConstructionMode == ConstructionMode.Floor || ConstructionMode == ConstructionMode.Deconstruct)
        {
            return true;
        }

        Furniture furniturePrototype = PrototypeManager.Furnitures[ConstructionObjectType];
        return furniturePrototype.Width == 1 && furniturePrototype.Height == 1;
    }

    public bool IsBuildJobOverlap(string furnitureType, Tile tile)
    {
        for (int xOffset = tile.X; xOffset < (tile.X + PrototypeManager.Furnitures[furnitureType].Width); xOffset++)
        {
            for (int yOffset = tile.Y; yOffset < (tile.Y + PrototypeManager.Furnitures[furnitureType].Height); yOffset++)
            {
                if (WorldController.Instance.World.GetTileAt(xOffset, yOffset).PendingBuildJob != null)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
