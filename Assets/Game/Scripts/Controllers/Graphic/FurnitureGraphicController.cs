using UnityEngine;
using System.Collections.Generic;

public class FurnitureGraphicController
{
    private readonly Dictionary<Furniture, GameObject> furnitureGameObjectMap;
    private readonly Dictionary<Furniture, GameObject> powerStatusGameObjectMap;

    private readonly GameObject furnitureParent;

    public FurnitureGraphicController()
    {
        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();
        powerStatusGameObjectMap = new Dictionary<Furniture, GameObject>();
        furnitureParent = new GameObject("Furniture");

        World.Current.FurnitureManager.FurnitureCreated += OnFurnitureCreated;
        foreach (Furniture furniture in World.Current.FurnitureManager)
        {
            OnFurnitureCreated(this, new FurnitureEventArgs(furniture));
        }            
    }

    public void OnFurnitureCreated(object sender, FurnitureEventArgs args)
    {
        GameObject furnitureGameObject = new GameObject();
        furnitureGameObjectMap.Add(args.Furniture, furnitureGameObject);
        furnitureGameObject.name = args.Furniture.Type + "_" + args.Furniture.Tile.X + "_" + args.Furniture.Tile.Y;
        furnitureGameObject.transform.position = new Vector3(args.Furniture.Tile.X + ((args.Furniture.Width - 1) / 2f), args.Furniture.Tile.Y + ((args.Furniture.Height - 1) / 2f), 0);
        furnitureGameObject.transform.SetParent(furnitureParent.transform, true);

        // FIXME: Don't hardcode orientation: make it a parameter of furniture instead.
        if (args.Furniture.HasTypeTag("Door"))
        {
            Tile northTile = World.Current.GetTileAt(args.Furniture.Tile.X, args.Furniture.Tile.Y + 1);
            Tile southTile = World.Current.GetTileAt(args.Furniture.Tile.X, args.Furniture.Tile.Y - 1);

            if (northTile != null && southTile != null && northTile.Furniture != null && southTile.Furniture != null &&
                northTile.Furniture.HasTypeTag("Wall") && southTile.Furniture.HasTypeTag("Wall"))
            {
                args.Furniture.VerticalDoor = true;
            }      
        }

        SpriteRenderer spriteRenderer = furnitureGameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSpriteForFurniture(args.Furniture);
        spriteRenderer.sortingLayerName = "Furniture";
        spriteRenderer.color = args.Furniture.Tint;

        if (args.Furniture.PowerValue < 0)
        {
            GameObject powerGameObject = new GameObject();
            powerStatusGameObjectMap.Add(args.Furniture, powerGameObject);
            powerGameObject.transform.parent = furnitureGameObject.transform;
            powerGameObject.transform.position = furnitureGameObject.transform.position;

            SpriteRenderer powerSpriteRenderer = powerGameObject.AddComponent<SpriteRenderer>();
            powerSpriteRenderer.sprite = GetPowerStatusSprite();
            powerSpriteRenderer.sortingLayerName = "Power";
            powerSpriteRenderer.color = PowerStatusColor();

            powerGameObject.SetActive(!(World.Current.PowerSystem.PowerLevel > 0));
        }

        args.Furniture.FurnitureChanged += OnFurnitureChanged;
        World.Current.PowerSystem.PowerLevelChanged += OnPowerStatusChange;
        args.Furniture.FurnitureRemoved += OnFurnitureRemoved;

    }

    private void OnFurnitureChanged(object sender, FurnitureEventArgs args)
    {
        if (furnitureGameObjectMap.ContainsKey(args.Furniture) == false)
        {
            Debug.LogError("FurnitureGraphicController::FurnitureChanged: Trying to change visuals for furniture not in our map.");
            return;
        }

        GameObject furnitureGameObject = furnitureGameObjectMap[args.Furniture];
        if (args.Furniture.HasTypeTag("Door"))
        {
            Tile northTile = World.Current.GetTileAt(args.Furniture.Tile.X, args.Furniture.Tile.Y + 1);
            Tile southTile = World.Current.GetTileAt(args.Furniture.Tile.X, args.Furniture.Tile.Y - 1);
            Tile eastTile = World.Current.GetTileAt(args.Furniture.Tile.X + 1, args.Furniture.Tile.Y);
            Tile westTile = World.Current.GetTileAt(args.Furniture.Tile.X - 1, args.Furniture.Tile.Y);

            if (northTile != null && southTile != null && northTile.Furniture != null && southTile.Furniture != null &&
                northTile.Furniture.HasTypeTag("Wall") && southTile.Furniture.HasTypeTag("Wall"))
            {
                args.Furniture.VerticalDoor = true;
            }
            else if (eastTile != null && westTile != null && eastTile.Furniture != null && westTile.Furniture != null &&
                eastTile.Furniture.HasTypeTag("Wall") && westTile.Furniture.HasTypeTag("Wall"))
            {
                args.Furniture.VerticalDoor = false;
            }
        }

        furnitureGameObject.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(args.Furniture);
        furnitureGameObject.GetComponent<SpriteRenderer>().color = args.Furniture.Tint;
    }

    private void OnPowerStatusChange(object sender, PowerEventArgs args)
    {
        Furniture furniture = (Furniture)args.PowerRelated;
        if (furniture == null) return;
        if (powerStatusGameObjectMap.ContainsKey(furniture) == false) return;

        GameObject powerStatusGameObject = powerStatusGameObjectMap[furniture];
        powerStatusGameObject.SetActive(!(World.Current.PowerSystem.PowerLevel > 0));
        powerStatusGameObject.GetComponent<SpriteRenderer>().color = PowerStatusColor();
    }

    private void OnFurnitureRemoved(object sender, FurnitureEventArgs args)
    {
        if (furnitureGameObjectMap.ContainsKey(args.Furniture) == false)
        {
            Debug.LogError("FurnitureGraphicController::OnFurnitureRemoved: Trying to change visuals for furniture not in our map.");
            return;
        }

        GameObject furnitureGameObject = furnitureGameObjectMap[args.Furniture];
        Object.Destroy(furnitureGameObject);

        furnitureGameObjectMap.Remove(args.Furniture);

        if (powerStatusGameObjectMap.ContainsKey(args.Furniture) == false) return;
        powerStatusGameObjectMap.Remove(args.Furniture);
    }

    public Sprite GetSpriteForFurniture(string type)
    {
        return SpriteManager.Current.GetSprite("Furniture", type + (World.Current.FurniturePrototypes[type].LinksToNeighbour ? "_" : ""));
    }

    public Sprite GetSpriteForFurniture(Furniture furniture)
    {
        string spriteName = furniture.GetSpriteName();
        if (furniture.LinksToNeighbour == false)
        {
            return SpriteManager.Current.GetSprite("Furniture", spriteName);
        }

        spriteName += "_";
        int x = furniture.Tile.X;
        int y = furniture.Tile.Y;

        spriteName += GetSuffixForNeighbour(furniture, x, y + 1, "N");
        spriteName += GetSuffixForNeighbour(furniture, x + 1, y, "E");
        spriteName += GetSuffixForNeighbour(furniture, x, y - 1, "S");
        spriteName += GetSuffixForNeighbour(furniture, x - 1, y, "W");

        return SpriteManager.Current.GetSprite("Furniture", spriteName);
    }
    
    private static string GetSuffixForNeighbour(Furniture furniture, int x, int y, string suffix)
    {
         Tile tileAt = World.Current.GetTileAt(x, y);
         if (tileAt != null && tileAt.Furniture != null && tileAt.Furniture.Type == furniture.Type)
         {
             return suffix;
         }
         return "";
    }

    private static Sprite GetPowerStatusSprite()
    {
        return SpriteManager.Current.GetSprite("Power", "PowerIcon");
    }

    private static Color PowerStatusColor()
    {
        return World.Current.PowerSystem.PowerLevel > 0 ? Color.green : Color.red;
    }
}
