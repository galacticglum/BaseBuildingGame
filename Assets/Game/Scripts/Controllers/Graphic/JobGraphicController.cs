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
        World.Current.JobQueue.cbJobCreated += OnJobCreated;
        jobParent = new GameObject("Jobs");
    }

    private void OnJobCreated(Job job)
    {
        if (job.Type == null && job.TileType == null)
        {
            return;
        }

        if (jobGameObjectMap.ContainsKey(job))
        {
            Debug.LogError("JobGraphicController::OnJobCreated: Called for a job GameObject that alreadys exists. Most likely a job being requeued, as opposed to created.");
            return;
        }

        GameObject jobGameObject = new GameObject();
        jobGameObjectMap.Add(job, jobGameObject);
        jobGameObject.name = "JOB_" + job.Type + "_" + job.Tile.X + "_" + job.Tile.Y;
        jobGameObject.transform.SetParent(jobParent.transform, true);

        SpriteRenderer spriteRenderer = jobGameObject.AddComponent<SpriteRenderer>();
        if (job.TileType != null)
        {
            jobGameObject.transform.position = new Vector3(job.Tile.X, job.Tile.Y, 0);
            spriteRenderer.sprite = SpriteManager.Current.GetSprite("Tile", "Solid");
        }
        else
        {
            jobGameObject.transform.position = new Vector3(job.Tile.X + ((job.FurniturePrototype.Width - 1) / 2f), job.Tile.Y + ((job.FurniturePrototype.Height - 1) / 2f), 0);
            spriteRenderer.sprite = furnitureGraphicController.GetSpriteForFurniture (job.Type);
        }
        spriteRenderer.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        spriteRenderer.sortingLayerName = "Jobs";

        // FIXME: This hardcoding is not ideal! 
        if (job.Type == "Door")
        {
            Tile northTile = World.Current.GetTileAt(job.Tile.X, job.Tile.Y + 1);
            Tile southTile = World.Current.GetTileAt(job.Tile.X, job.Tile.Y - 1);

            if (northTile != null && southTile != null && northTile.Furniture != null && southTile.Furniture != null &&
            northTile.Furniture.Type.Contains("Wall") && southTile.Furniture.Type.Contains("Wall"))
            {
                jobGameObject.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        job.cbJobCompleted += OnJobEnded;
        job.cbJobStopped += OnJobEnded;
    }

    private void OnJobEnded(Job job)
    {
        GameObject jobGameObject = jobGameObjectMap[job];
        job.cbJobCompleted -= OnJobEnded;
        job.cbJobStopped -= OnJobEnded;
        Object.Destroy(jobGameObject);
    }
}
