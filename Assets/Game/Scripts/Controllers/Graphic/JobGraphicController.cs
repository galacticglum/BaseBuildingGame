using UnityEngine;
using System.Collections.Generic;

public class JobGraphicController : MonoBehaviour
{
	private FurnitureGraphicController furnitureGraphicController;
	private Dictionary<Job, GameObject> jobGameObjectMap;

	// Use this for initialization
    private void Start ()
    {
		jobGameObjectMap = new Dictionary<Job, GameObject>();
		furnitureGraphicController = FindObjectOfType<FurnitureGraphicController>();

        WorldController.Instance.World.JobQueue.JobCreated += OnJobCreated;
    }

    private void OnJobCreated(object sender, JobCreatedEventArgs args) 
    {
		if(args.Job.Type == null)
        {
			return;
		}

		if(jobGameObjectMap.ContainsKey(args.Job))
        {
			Debug.LogError("JobGraphicController::OnJobCreated: Called for a job GameObject that alreadys exists. Most likely a job being requeued, as opposed to created.");
			return;
		}

		GameObject jobGameObject = new GameObject();
		jobGameObjectMap.Add(args.Job, jobGameObject);
		jobGameObject.name = "JOB_" + args.Job.Type + "_" + args.Job.Tile.X + "_" + args.Job.Tile.Y;
		jobGameObject.transform.position = new Vector3(args.Job.Tile.X, args.Job.Tile.Y, 0);
		jobGameObject.transform.SetParent(transform, true);

		SpriteRenderer spriteRenderer = jobGameObject.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = furnitureGraphicController.GetSpriteForFurniture(args.Job.Type);
		spriteRenderer.color = new Color( 0.5f, 1f, 0.5f, 0.25f);
		spriteRenderer.sortingLayerName = "Jobs";

		if(args.Job.Type == "Door")
        {
			Tile northTile = args.Job.Tile.World.GetTileAt(args.Job.Tile.X, args.Job.Tile.Y + 1 );
			Tile southTile = args.Job.Tile.World.GetTileAt(args.Job.Tile.X, args.Job.Tile.Y - 1 );

			if(northTile != null && southTile != null && northTile.Furniture != null && southTile.Furniture != null &&
				northTile.Furniture.Type=="Wall" && southTile.Furniture.Type=="Wall")
            {
				jobGameObject.transform.rotation = Quaternion.Euler( 0, 0, 90 );
			}
		}

		args.Job.JobComplete += OnJobEnded;
        args.Job.JobCancel += OnJobEnded;
	}

    private void OnJobEnded(object sender, JobEventArgs args)
    {
		GameObject jobGameObject = jobGameObjectMap[args.Job];

		args.Job.JobComplete -= OnJobEnded;
        args.Job.JobCancel -= OnJobEnded;

		Destroy(jobGameObject);
	}
}
