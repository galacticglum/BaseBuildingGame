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
                Tile tileAt = World.Current.GetTileAt(x, y);
                GameObject tileGameObject = new GameObject();

                tileGameObjectMap.Add(tileAt, tileGameObject);
                tileGameObject.name = "Tile_" + x + "_" + y;
                tileGameObject.transform.position = new Vector3(tileAt.X, tileAt.Y, 0);
                tileGameObject.transform.SetParent(tileParent.transform, true);

                SpriteRenderer spriteRenderer = tileGameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = SpriteManager.Current.GetSprite("Tile", "Empty");
                spriteRenderer.sortingLayerName = "Tiles";

                OnTileChanged(this, new TileEventArgs(tileAt));
            }
        }

        World.Current.TileChanged += OnTileChanged;
    }

    private void OnTileChanged(object sender, TileEventArgs args)
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

        if (args.Tile.Type == TileType.Empty)
        {
            tileGameObject.SetActive(false);
            return;
        }

        tileGameObject.SetActive(true);
        tileGameObject.GetComponent<SpriteRenderer>().sprite = SpriteManager.Current.GetSprite("Tile", args.Tile.Type.Name);
    }
}
