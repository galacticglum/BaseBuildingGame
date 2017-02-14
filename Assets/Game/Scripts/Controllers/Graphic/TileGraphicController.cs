using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class TileGraphicController
{
    private readonly Dictionary<Tile, GameObject> tileGameObjectMap;

    public TileGraphicController()
    {
        GameObject tileParent = new GameObject("Tiles");
        tileGameObjectMap = new Dictionary<Tile, GameObject>();

        for (int x = 0; x < World.Current.Width; x++)
        {
            for (int y = 0; y < World.Current.Height; y++)
            {
                // Get the tile data
                Tile tile_data = World.Current.GetTileAt(x, y);

                // This creates a new GameObject and adds it to our scene.
                GameObject tile_go = new GameObject();

                // Add our tile/GO pair to the dictionary.
                tileGameObjectMap.Add(tile_data, tile_go);

                tile_go.name = "Tile_" + x + "_" + y;
                tile_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
                tile_go.transform.SetParent(tileParent.transform, true);

                // Add a Sprite Renderer
                // Add a default sprite for empty tiles.
                SpriteRenderer sr = tile_go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteManager.Current.GetSprite("Tile", "Empty");
                sr.sortingLayerName = "Tiles";

                OnTileChanged(tile_data);
            }
        }

        World.Current.cbTileChanged += OnTileChanged;
    }

    private void OnTileChanged(Tile tile)
    {
        if (tileGameObjectMap.ContainsKey(tile) == false)
        {
            Debug.LogError("TileGraphicController::OnTileChanged: tileGameObjectMap doesn't contain the tile -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }

        GameObject tileGameObject = tileGameObjectMap[tile];
        if (tileGameObject == null)
        {
            Debug.LogError("TileGraphicController::OnTileChanged: tileGameObjectMap's returned GameObject is null -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }
        
        tileGameObject.GetComponent<SpriteRenderer>().sprite = SpriteManager.Current.GetSprite("Tile", tile.Type.Name);
    }
}
