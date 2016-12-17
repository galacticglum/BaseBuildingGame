using UnityEngine;
using System.Collections.Generic;

public class FurnitureGraphicController : MonoBehaviour
{
    private static World world
    {
        get { return WorldController.Instance.World; }
    }

    private Dictionary<Furniture, GameObject> furnitureGameObjectMap;
    private Dictionary<string, Sprite> furnitureSprites;

    // Use this for initialization
    private void Start()
    {
        LoadSprites();

        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();

        world.FurnitureCreated += OnFurnitureCreated;

        foreach (Furniture furniture in world.Furnitures)
        {
            OnFurnitureCreated(this, new FurnitureCreatedEventArgs(furniture));
        }
    }

    private void LoadSprites()
    {
        furnitureSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Furniture/");

        foreach (Sprite sprite in sprites)
        {
            furnitureSprites[sprite.name] = sprite;
        }
    }  

    public void OnFurnitureCreated(object sender, FurnitureCreatedEventArgs args)
    {
        GameObject furnitureObject = new GameObject();

        furnitureGameObjectMap.Add(args.Furniture, furnitureObject);

        furnitureObject.name = args.Furniture.Type + "_" + args.Furniture.Tile.X + "_" + args.Furniture.Tile.Y;
        furnitureObject.transform.position = new Vector2(args.Furniture.Tile.X, args.Furniture.Tile.Y);
        furnitureObject.transform.SetParent(transform, true);

        // FIXME: DON'T HARDCODE!!
        if (args.Furniture.Type == "Door")
        {
            Tile northTile = world.GetTileAt(args.Furniture.Tile.X, args.Furniture.Tile.Y + 1);
            Tile southTile = world.GetTileAt(args.Furniture.Tile.X, args.Furniture.Tile.Y - 1);

            if (northTile != null && southTile != null && northTile.Furniture != null && southTile.Furniture != null &&
                northTile.Furniture.Type == "Wall" && southTile.Furniture.Type == "Wall")
            {
                furnitureObject.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        SpriteRenderer sr = furnitureObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite(args.Furniture);
        sr.sortingLayerName = "Furniture";

        args.Furniture.FurnitureChanged += OnFurnitureChanged;
    }

    private void OnFurnitureChanged(object sender, FurnitureChangedEventArgs args)
    {
        if (furnitureGameObjectMap.ContainsKey(args.Furniture) == false)
        {
            Debug.LogError("FurnitureGraphicController::FurnitureChanged: Trying to change visuals for furniture not in our map.");
            return;
        }

        GameObject furnitureGameObject = furnitureGameObjectMap[args.Furniture];
        furnitureGameObject.GetComponent<SpriteRenderer>().sprite = GetSprite(args.Furniture);
    }

    public Sprite GetSprite(Furniture furniture)
    {
        string spriteName = furniture.Type;

        if (furniture.LinksToNeighbour == false)
        {
            // FIXME: Hardcoded values need to be generalized.
            if (furniture.Type != "Door") return furnitureSprites[spriteName];

            if (furniture.GetParameter("openness") < 0.1f)
            {
                // Door is closed
                spriteName = "Door";
            }
            else if (furniture.GetParameter("openness") < 0.5f)
            {
                // Door is open a bit
                spriteName = "Door_openness_1";
            }
            else if (furniture.GetParameter("openness") < 0.9f)
            {
                // Door is open a lot
                spriteName = "Door_openness_2";
            }
            else
            {
                spriteName = "Door_openness_3";
            }

            return furnitureSprites[spriteName];
        }

        spriteName = furniture.Type + "_";

        // Check for neighbours North, East, South, West
        int x = furniture.Tile.X;
        int y = furniture.Tile.Y;

        Tile tile = world.GetTileAt(x, y + 1);
        if (tile != null && tile.Furniture != null && tile.Furniture.Type == furniture.Type)
        {
            spriteName += "N";
        }
        tile = world.GetTileAt(x + 1, y);
        if (tile != null && tile.Furniture != null && tile.Furniture.Type == furniture.Type)
        {
            spriteName += "E";
        }
        tile = world.GetTileAt(x, y - 1);
        if (tile != null && tile.Furniture != null && tile.Furniture.Type == furniture.Type)
        {
            spriteName += "S";
        }
        tile = world.GetTileAt(x - 1, y);
        if (tile != null && tile.Furniture != null && tile.Furniture.Type == furniture.Type)
        {
            spriteName += "W";
        }

        if (furnitureSprites.ContainsKey(spriteName)) return furnitureSprites[spriteName];
        Debug.LogError("FurnitureGraphicController::GetSprite: No sprites with name: " + spriteName);
        return null;
    }

    public Sprite GetSprite(string type)
    {
        if (furnitureSprites.ContainsKey(type))
        {
            return furnitureSprites[type];
        }

        if (furnitureSprites.ContainsKey(type + "_"))
        {
            return furnitureSprites[type + "_"];
        }

        Debug.LogError("FurnitureGraphicController::GetSprite: No sprites with name: " + type);
        return null;
    }
}
