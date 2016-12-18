using UnityEngine;
using System.Collections;

public static class FurnitureBehaviours
{
    public static void UpdateDoor(Furniture furniture, float deltaTime)
    {
        if (furniture.GetParameter("isOpening") >= 1)
        {
            furniture.ModifyParameter("openness", deltaTime * 4);
            if (furniture.GetParameter("openness") >= 1)
            {
                furniture.SetParamater("isOpening", 0);
            }
        }
        else
        {
            furniture.ModifyParameter("openness", deltaTime * -4);
        }

        furniture.SetParamater("openness", Mathf.Clamp01(furniture.GetParameter("openness")));
        furniture.OnFurnitureChanged(new FurnitureChangedEventArgs(furniture));
    }

    public static TileEnterability DoorTryEnter(Furniture furniture)
    {
        furniture.SetParamater("isOpening", 1);
        return furniture.GetParameter("openness") >= 1 ? TileEnterability.Immediate : TileEnterability.Soon;
    }

    public static void BuildFurniture(object sender, JobCompleteEventArgs args)
    {
        WorldController.Instance.World.PlaceFurniture(args.Job.Type, args.Job.Tile);
        args.Job.Tile.PendingFurnitureJob = null;
    }
}