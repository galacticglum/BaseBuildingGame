using MoonSharp.Interpreter;
using UnityEngine;

public class ConstructionController
{
    public string TileType { get { return constructionTileType.ToString(); } }

    public ConstructionMode ConstructionMode { get; set; }
    public string ConstructionType { get; set; }
    public MouseController MouseController { get; set; }

    private TileType constructionTileType = global::TileType.Floor;

    public bool IsFurnitureDraggable()
    {
        if (ConstructionMode == ConstructionMode.Floor || ConstructionMode == ConstructionMode.Deconstruct)
        {
            return true;
        }

        Furniture furniture = PrototypeManager.Furnitures[ConstructionType];
        return furniture.Width == 1 && furniture.Height == 1;
    }

    public void BuildTile(TileType type)
    {
        ConstructionMode = ConstructionMode.Floor;
        constructionTileType = type;

        MouseController.StartConstruction();
    }
    
    public void BuildFurniture(string type)
    {
        ConstructionMode = ConstructionMode.Furniture;
        ConstructionType = type;

        MouseController.StartConstruction();
    }

    public void Deconstruct()
    {
        ConstructionMode = ConstructionMode.Deconstruct;
        MouseController.StartConstruction();
    }

    public void DoBuild(Tile tile)
    {
        switch (ConstructionMode)
        {
            case ConstructionMode.Furniture:
                string furnitureType = ConstructionType;

                if (WorldController.Instance.World.FurnitureManager.IsPlacementValid(furnitureType, tile) && 
                    IsBuildJobOverlap(tile, furnitureType) == false)
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
                        Debug.LogError("There is no furniture job prototype for '" + furnitureType + "'");
                        job = new Job(tile, furnitureType, Furniture.Build, 0.1f, null, JobPriority.High)
                        {
                            Description = "job_build_" + furnitureType + "_desc"
                        };
                    }

                    job.FurniturePrototype = PrototypeManager.Furnitures[furnitureType];
                    for (int xOffset = tile.X; xOffset < tile.X + PrototypeManager.Furnitures[furnitureType].Width; xOffset++)
                    {
                        for (int yOffset = tile.Y; yOffset < tile.Y + PrototypeManager.Furnitures[furnitureType].Height; yOffset++)
                        {
                            Tile tileAt = WorldController.Instance.World.GetTileAt(xOffset, yOffset);
                            tileAt.PendingBuildJob = job;

                            job.JobStopped += (sender, arg) =>
                            {
                                tileAt.PendingBuildJob = null;
                            };
                        }
                    }

                    if (WorldController.Instance.DevelopmentMode)
                    {
                        WorldController.Instance.World.FurnitureManager.Place(job.Type, job.Tile);
                    }
                    else
                    {
                        WorldController.Instance.World.JobQueue.Enqueue(job);
                    }
                }
                break;
            case ConstructionMode.Floor:
                TileType tileType = constructionTileType;
                if (tile.Type != tileType && 
                    tile.Furniture == null &&
                    tile.PendingBuildJob == null &&
                    CanBuild(tile, tileType))
                {
                    Job job = tileType.JobPrototype;  
                    job.Tile = tile;

                    tile.PendingBuildJob = job;
                    job.JobStopped += (sender, args) =>
                    {
                        args.Job.Tile.PendingBuildJob = null;
                    };

                    if (WorldController.Instance.DevelopmentMode)
                    {
                        job.Tile.Type = job.TileType;
                    }
                    else
                    {
                        WorldController.Instance.World.JobQueue.Enqueue(job);
                    }
                }
                break;
            case ConstructionMode.Deconstruct:
                if (tile.Furniture != null)
                {
                    if (tile.Furniture.HasTypeTag("Wall"))
                    {
                        Tile[] neighbors = tile.GetNeighbours(); 
                        int pressuredNeighbors = 0;
                        int vacuumNeighbors = 0;
                        foreach (Tile neighbor in neighbors)
                        {
                            if (neighbor == null || neighbor.Room == null) continue;

                            if (neighbor.Room == World.Current.RoomManager.OutsideRoom || neighbor.Room.Pressure.IsZero())
                            {
                                vacuumNeighbors++;
                            }
                            else
                            {
                                pressuredNeighbors++;
                            }
                        }

                        if (vacuumNeighbors > 0 && pressuredNeighbors > 0)
                        {
                            return;
                        }
                    }

                    tile.Furniture.Deconstruct();
                }
                else if (tile.PendingBuildJob != null)
                {
                    tile.PendingBuildJob.CancelJob();
                }
                break;
        }
    }

    public bool IsBuildJobOverlap(Tile tile, string furnitureType)
    {
        for (int xOffset = tile.X; xOffset < tile.X + PrototypeManager.Furnitures[furnitureType].Width; xOffset++)
        {
            for (int yOffset = tile.Y; yOffset < tile.Y + PrototypeManager.Furnitures[furnitureType].Height; yOffset++)
            {
                if (WorldController.Instance.World.GetTileAt(xOffset, yOffset).PendingBuildJob != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool CanBuild(Tile tile, TileType type)
    {
        DynValue value = Lua.Call(type.CanBuildHereLua, tile);
        if (value != null)
        {
            return value.Boolean;
        }

        Debug.Log("Found no lua function " + type.CanBuildHereLua);
        return false;
    }    
}
