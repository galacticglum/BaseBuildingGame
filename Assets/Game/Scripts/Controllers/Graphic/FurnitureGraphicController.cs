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

        World.Current.cbFurnitureCreated += OnFurnitureCreated;
        foreach (Furniture furniture in World.Current.Furnitures)
        {
            OnFurnitureCreated(furniture);
        }            
    }

    public void OnFurnitureCreated(Furniture furniture)
    {
        GameObject furnitureGameObject = new GameObject();
        furnitureGameObjectMap.Add(furniture, furnitureGameObject);
        furnitureGameObject.name = furniture.Type + "_" + furniture.Tile.X + "_" + furniture.Tile.Y;
        furnitureGameObject.transform.position = new Vector3(furniture.Tile.X + ((furniture.Width - 1) / 2f), furniture.Tile.Y + ((furniture.Height - 1) / 2f), 0);
        furnitureGameObject.transform.SetParent(furnitureParent.transform, true);

        // FIXME: Don't hardcode orientation: make it a parameter of furniture instead.
        if (furniture.HasTypeTag("Door"))
        {
            Tile northTile = World.Current.GetTileAt(furniture.Tile.X, furniture.Tile.Y + 1);
            Tile southTile = World.Current.GetTileAt(furniture.Tile.X, furniture.Tile.Y - 1);

            if (northTile != null && southTile != null && northTile.Furniture != null && southTile.Furniture != null &&
                northTile.Furniture.HasTypeTag("Wall") && southTile.Furniture.HasTypeTag("Wall"))
            {
                furniture.VerticalDoor = true;
            }      
        }

        SpriteRenderer spriteRenderer = furnitureGameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSpriteForFurniture(furniture);
        spriteRenderer.sortingLayerName = "Furniture";
        spriteRenderer.color = furniture.Tint;

        if (furniture.PowerValue < 0)
        {
            GameObject powerGameObject = new GameObject();
            powerStatusGameObjectMap.Add(furniture, powerGameObject);
            powerGameObject.transform.parent = furnitureGameObject.transform;
            powerGameObject.transform.position = furnitureGameObject.transform.position;

            SpriteRenderer powerSpriteRenderer = powerGameObject.AddComponent<SpriteRenderer>();
            powerSpriteRenderer.sprite = GetPowerStatusSprite();
            powerSpriteRenderer.sortingLayerName = "Power";
            powerSpriteRenderer.color = PowerStatusColor();

            powerGameObject.SetActive(!(World.Current.PowerSystem.PowerLevel > 0));
        }

        furniture.cbOnChanged += OnFurnitureChanged;
        World.Current.PowerSystem.PowerLevelChanged += OnPowerStatusChange;
        furniture.cbOnRemoved += OnFurnitureRemoved;

    }

    private void OnFurnitureChanged(Furniture furniture)
    {
        if (furnitureGameObjectMap.ContainsKey(furniture) == false)
        {
            Debug.LogError("FurnitureGraphicController::FurnitureChanged: Trying to change visuals for furniture not in our map.");
            return;
        }

        GameObject furnitureGameObject = furnitureGameObjectMap[furniture];
        if (furniture.HasTypeTag("Door"))
        {
            Tile northTile = World.Current.GetTileAt(furniture.Tile.X, furniture.Tile.Y + 1);
            Tile southTile = World.Current.GetTileAt(furniture.Tile.X, furniture.Tile.Y - 1);
            Tile eastTile = World.Current.GetTileAt(furniture.Tile.X + 1, furniture.Tile.Y);
            Tile westTile = World.Current.GetTileAt(furniture.Tile.X - 1, furniture.Tile.Y);

            if (northTile != null && southTile != null && northTile.Furniture != null && southTile.Furniture != null &&
                northTile.Furniture.HasTypeTag("Wall") && southTile.Furniture.HasTypeTag("Wall"))
            {
                furniture.VerticalDoor = true;
            }
            else if (eastTile != null && westTile != null && eastTile.Furniture != null && westTile.Furniture != null &&
                eastTile.Furniture.HasTypeTag("Wall") && westTile.Furniture.HasTypeTag("Wall"))
            {
                furniture.VerticalDoor = false;
            }
        }

        furnitureGameObject.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furniture);
        furnitureGameObject.GetComponent<SpriteRenderer>().color = furniture.Tint;
    }

    private void OnPowerStatusChange(IPowerRelated powerRelated)
    {
        Furniture furniture = (Furniture)powerRelated;
        if (furniture == null) return;
        if (powerStatusGameObjectMap.ContainsKey(furniture) == false) return;

        GameObject powerStatusGameObject = powerStatusGameObjectMap[furniture];
        powerStatusGameObject.SetActive(!(World.Current.PowerSystem.PowerLevel > 0));
        powerStatusGameObject.GetComponent<SpriteRenderer>().color = PowerStatusColor();
    }

    private void OnFurnitureRemoved(Furniture furniture)
    {
        if (furnitureGameObjectMap.ContainsKey(furniture) == false)
        {
            Debug.LogError("FurnitureGraphicController::OnFurnitureRemoved: Trying to change visuals for furniture not in our map.");
            return;
        }

        GameObject furnitureGameObject = furnitureGameObjectMap[furniture];
        Object.Destroy(furnitureGameObject);

        furnitureGameObjectMap.Remove(furniture);

        if (powerStatusGameObjectMap.ContainsKey(furniture) == false) return;
        powerStatusGameObjectMap.Remove(furniture);
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