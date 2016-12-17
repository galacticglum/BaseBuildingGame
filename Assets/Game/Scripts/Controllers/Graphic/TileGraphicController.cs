using UnityEngine;
using System.Collections.Generic;

public class TileGraphicController : MonoBehaviour
{
    [SerializeField]
    private Sprite floorSprite;  // FIXME!
    [SerializeField]
    private Sprite emptySprite;  // FIXME!

    private static World world
    {
        get { return WorldController.Instance.World; }
    }

    private Dictionary<Tile, GameObject> tileGameObjectMap;

    // Use this for initialization
    private void Start()
    {
        tileGameObjectMap = new Dictionary<Tile, GameObject>();

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile tile = world.GetTileAt(x, y);

                GameObject tileGameObject = new GameObject();
                tileGameObjectMap.Add(tile, tileGameObject);
                tileGameObject.name = "Tile_" + x + "_" + y;
                tileGameObject.transform.position = new Vector2(tile.X, tile.Y);
                tileGameObject.transform.SetParent(this.transform, true);

                SpriteRenderer spriteRenderer = tileGameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = emptySprite;
                spriteRenderer.sortingLayerName = "Tiles";

                OnTileChanged(this, new TileChangedEventArgs(tile));
            }
        }

        world.TileChanged += OnTileChanged;
    }

    private void OnTileChanged(object sender, TileChangedEventArgs args)
    {
        if (tileGameObjectMap.ContainsKey(args.Tile) == false)
        {
            Debug.LogError("TileGraphicController::OnTileChanged: tileGameObjectMap doesn't contain the tile -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }

        GameObject tileGameObject = tileGameObjectMap[args.Tile];

        if (tileGameObject == null)
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
