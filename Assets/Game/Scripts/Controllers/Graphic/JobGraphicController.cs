using UnityEngine;
using System.Collections.Generic;

public class JobGraphicController
{
    private readonly FurnitureGraphicController furnitureGraphicController;
    private readonly Dictionary<Job, GameObject> jobGameObjectMap;
    private readonly GameObject jobParent;

    // Use this for initialization
    public JobGraphicController(FurnitureGraphicController furnitureGraphicController)
    {
        jobGameObjectMap = new Dictionary<Job, GameObject>();

        this.furnitureGraphicController = furnitureGraphicController;
        World.Current.JobQueue.JobCreated += OnJobCreated;
        jobParent = new GameObject("Jobs");
    }

    private void OnJobCreated(object sender, JobEventArgs args)
    {
        if (args.Job.Type == null && args.Job.TileType == null)
        {
            return;
        }

        if (jobGameObjectMap.ContainsKey(args.Job))
        {
            Debug.LogError("JobGraphicController::OnJobCreated: Called for a job GameObject that alreadys exists. Most likely a job being requeued, as opposed to created.");
            return;
        }

        GameObject jobGameObject = new GameObject();
        jobGameObjectMap.Add(args.Job, jobGameObject);
        jobGameObject.name = "JOB_" + args.Job.Type + "_" + args.Job.Tile.X + "_" + args.Job.Tile.Y;
        jobGameObject.transform.SetParent(jobParent.transform, true);

        SpriteRenderer spriteRenderer = jobGameObject.AddComponent<SpriteRenderer>();
        if (args.Job.TileType != null)
        {
            jobGameObject.transform.position = new Vector3(args.Job.Tile.X, args.Job.Tile.Y, 0);
            spriteRenderer.sprite = SpriteManager.Current.GetSprite("Tile", "Solid");
        }
        else
        {
            jobGameObject.transform.position = new Vector3(args.Job.Tile.X + ((args.Job.FurniturePrototype.Width - 1) / 2f), args.Job.Tile.Y + ((args.Job.FurniturePrototype.Height - 1) / 2f), 0);
            spriteRenderer.sprite = furnitureGraphicController.GetSpriteForFurniture (args.Job.Type);
        }
        spriteRenderer.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        spriteRenderer.sortingLayerName = "Jobs";

        // FIXME: This hardcoding is not ideal! 
        if (args.Job.Type == "Door")
        {
            Tile northTile = World.Current.GetTileAt(args.Job.Tile.X, args.Job.Tile.Y + 1);
            Tile southTile = World.Current.GetTileAt(args.Job.Tile.X, args.Job.Tile.Y - 1);

            if (northTile != null && southTile != null && northTile.Furniture != null && southTile.Furniture != null &&
            northTile.Furniture.Type.Contains("Wall") && southTile.Furniture.Type.Contains("Wall"))
            {
                jobGameObject.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        args.Job.JobCompleted += OnJobEnded;
        args.Job.JobStopped += OnJobEnded;
    }

    private void OnJobEnded(object sender, JobEventArgs args)
    {
        GameObject jobGameObject = jobGameObjectMap[args.Job];
        args.Job.JobCompleted -= OnJobEnded;
        args.Job.JobStopped -= OnJobEnded;
        Object.Destroy(jobGameObject);
    }
}
