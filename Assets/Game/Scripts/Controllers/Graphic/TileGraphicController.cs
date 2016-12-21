using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class TileGraphicController : MonoBehaviour
{
    private static World world
    {
        get { return WorldController.Instance.World; }
    }

    [SerializeField]
	private Sprite floorSprite;
    [SerializeField]
	private Sprite emptySprite;

    private Dictionary<Tile, GameObject> tileGameObjectMap;

	// Use this for initialization
    private void Start ()
    {
		tileGameObjectMap = new Dictionary<Tile, GameObject>();
        world.TileChanged += OnTileChanged;

        for (int x = 0; x < world.Width; x++)
        {
			for (int y = 0; y < world.Height; y++)
            {
				Tile tileAt = world.GetTileAt(x, y);		
				GameObject tileGameObject = new GameObject();

				tileGameObjectMap.Add( tileAt, tileGameObject );
				tileGameObject.name = "Tile_" + x + "_" + y;
				tileGameObject.transform.position = new Vector3( tileAt.X, tileAt.Y, 0);
				tileGameObject.transform.SetParent(transform, true);

				SpriteRenderer spriteRenderer = tileGameObject.AddComponent<SpriteRenderer>();
				spriteRenderer.sprite = emptySprite;
				spriteRenderer.sortingLayerName = "Tiles";

                OnTileChanged(this, new TileChangedEventArgs(tileAt));
            }
		}
	}

	// This function should be called automatically whenever a tile's data gets changed.
    private void OnTileChanged(object sender, TileChangedEventArgs args)
    {
		if(tileGameObjectMap.ContainsKey(args.Tile) == false)
        {
			Debug.LogError("TileGraphicController::OnTileChanged: tileGameObjectMap doesn't contain the tile -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
			return;
		}

		GameObject tileGameObject = tileGameObjectMap[args.Tile];

		if(tileGameObject == null)
        {
			Debug.LogError("TileGraphicController::OnTileChanged: tileGameObjectMap's returned GameObject is null -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
			return;
		}

		switch (args.Tile.Type)
		{
		    case TileType.Floor:
		        tileGameObject.GetComponent<SpriteRenderer>().sprite = floorSprite;
		        break;
		    case TileType.Empty:
		        tileGameObject.GetComponent<SpriteRenderer>().sprite = emptySprite;
		        break;
		    default:
		        Debug.LogError("TileGraphicController::OnTileChanged: Unrecognized tile type.");
		        break;
		}
	}
}
