﻿using UnityEngine;
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
    private void Start ()
    {
		LoadSprites();
		furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();
		world.FurnitureCreated += OnFurnitureCreated;
        
		foreach(Furniture furniture in world.Furnitures)
        {
            OnFurnitureCreated(this, new FurnitureCreatedEventArgs(furniture));
        }
    }

    private void LoadSprites()
    {
		furnitureSprites = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Furniture/");

		foreach(Sprite sprite in sprites)
        {
			furnitureSprites[sprite.name] = sprite;
		}
	}

	public void OnFurnitureCreated(object sender, FurnitureCreatedEventArgs args)
    {
		GameObject furnitureGameObject = new GameObject();

        if (furnitureGameObjectMap.ContainsKey(args.Furniture))
        {
            Debug.LogError(
                "FurnitureGraphicController::OnFurnitureCreated: furnitureGameObjectMap already contains key: furniture (hashcode: " +
                args.Furniture.GetHashCode() + ") typeof: " + args.Furniture.Type + "!");
        }
        else
        {         
            furnitureGameObjectMap.Add(args.Furniture, furnitureGameObject);
        }

        furnitureGameObject.name = args.Furniture.Type + "_" + args.Furniture.Tile.X + "_" + args.Furniture.Tile.Y;
		furnitureGameObject.transform.position = new Vector3(args.Furniture.Tile.X + (args.Furniture.Width -1) / 2f, args.Furniture.Tile.Y + (args.Furniture.Height - 1) / 2f, 0);
		furnitureGameObject.transform.SetParent(transform, true);

		if(args.Furniture.Type == "Door")
        {
			Tile northTile = world.GetTileAt(args.Furniture.Tile.X, args.Furniture.Tile.Y + 1 );
			Tile southTile = world.GetTileAt(args.Furniture.Tile.X, args.Furniture.Tile.Y - 1 );

			if(northTile != null && southTile != null && northTile.Furniture != null && southTile.Furniture != null &&
				northTile.Furniture.Type=="Wall" && southTile.Furniture.Type=="Wall") {
				furnitureGameObject.transform.rotation = Quaternion.Euler(0, 0, 90);
			}
		}

		SpriteRenderer spriteRenderer = furnitureGameObject.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = GetSpriteForFurniture(args.Furniture);
		spriteRenderer.sortingLayerName = "Furniture";
		spriteRenderer.color = args.Furniture.Tint;
        args.Furniture.FurnitureChanged += OnFurnitureChanged;
	}

    private void OnFurnitureChanged(object sender, FurnitureChangedEventArgs args)
    {
		if(furnitureGameObjectMap.ContainsKey(args.Furniture) == false)
        {
			Debug.LogError("FurnitureGraphicController::FurnitureChanged: Trying to change visuals for furniture not in our map.");
			return;
		}

		GameObject furnitureGameObject = furnitureGameObjectMap[args.Furniture];
		furnitureGameObject.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(args.Furniture);
		furnitureGameObject.GetComponent<SpriteRenderer>().color = args.Furniture.Tint;
	}

	public Sprite GetSpriteForFurniture(Furniture furniture)
    {
		string spriteName = furniture.Type;

		if(furniture.LinksToNeighbour == false)
        {
            if (furniture.Type != "Door") return furnitureSprites[spriteName];
            if(furniture.GetParameter("openness") < 0.1f)
            {
                // Door is closed
                spriteName = "Door";
            }
            else if(furniture.GetParameter("openness") < 0.5f)
            {
                // Door is a bit open
                spriteName = "Door_openness_1";
            }
            else if(furniture.GetParameter("openness") < 0.9f)
            {
                // Door is a lot open
                spriteName = "Door_openness_2";
            }
            else
            {
                // Door is a fully open
                spriteName = "Door_openness_3";
            }

            return furnitureSprites[spriteName];
		}

		spriteName = furniture.Type + "_";

		int x = furniture.Tile.X;
		int y = furniture.Tile.Y;

        Tile tileAt = world.GetTileAt(x, y+1);
		if(tileAt != null && tileAt.Furniture != null && tileAt.Furniture.Type == furniture.Type)
        {
			spriteName += "N";
		}
		tileAt = world.GetTileAt(x+1, y);
		if(tileAt != null && tileAt.Furniture != null && tileAt.Furniture.Type == furniture.Type)
        {
			spriteName += "E";
		}
		tileAt = world.GetTileAt(x, y-1);
		if(tileAt != null && tileAt.Furniture != null && tileAt.Furniture.Type == furniture.Type)
        {
			spriteName += "S";
		}
		tileAt = world.GetTileAt(x-1, y);
		if(tileAt != null && tileAt.Furniture != null && tileAt.Furniture.Type == furniture.Type)
        {
			spriteName += "W";
		}

        if (furnitureSprites.ContainsKey(spriteName)) return furnitureSprites[spriteName];

        Debug.LogError("FurnitureGraphicController::GetSprite: No sprites with name: " + spriteName + ".");
        return null;
    }


	public Sprite GetSpriteForFurniture(string type)
    {
		if(furnitureSprites.ContainsKey(type))
        {
			return furnitureSprites[type];
		}

		if(furnitureSprites.ContainsKey(type+"_"))
        {
			return furnitureSprites[type+"_"];
		}

		Debug.LogError("FurnitureGraphicController::GetSprite: No sprites with name: " + type + ".");
		return null;
	}
}
