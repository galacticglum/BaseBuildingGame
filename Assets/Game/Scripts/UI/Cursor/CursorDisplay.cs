using UnityEngine;

public class CursorDisplay
{
    private readonly MouseController mouseController;
    private readonly ConstructionController constructionController;

    private int validPostionCount;
    private int invalidPositionCount;

    public CursorDisplay(MouseController mouseController, ConstructionController constructionController)
    {
        this.mouseController = mouseController;
        this.constructionController = constructionController;
    }

    public string MousePosition(Tile t)
    {
        if (t == null) return string.Empty;

        string x = t.X.ToString();
        string y = t.Y.ToString();
        return "X:" + x + " Y:" + y;
    }

    public void GetPlacementValidationCounts()
    {
        validPostionCount = invalidPositionCount = 0;

        foreach (GameObject dragGameObject in mouseController.DragObjects)
        {
            Tile tileUnderDrag = GetTileUnderDrag(dragGameObject.transform.position);
            if (WorldController.Instance.World.IsFurniturePlacementValid(constructionController.ConstructionType, tileUnderDrag) && tileUnderDrag.PendingBuildJob == null)
            {
                validPostionCount++;
            }
            else
            {
                invalidPositionCount++;
            }
        }
    }

    public string ValidBuildPositionCount()
    {
        return validPostionCount.ToString();
    }

    public string InvalidBuildPositionCount()
    {
        return invalidPositionCount.ToString();
    }

    public string GetCurrentBuildRequirements()
    {
        if (World.Current.FurnitureJobPrototypes == null) return "furnitureJobPrototypes is null";

        string result = string.Empty;
        foreach (string itemName in WorldController.Instance.World.FurnitureJobPrototypes[constructionController.ConstructionType].InventoryRequirements.Keys)
        {
            string requiredMaterialCount = (WorldController.Instance.World.FurnitureJobPrototypes[constructionController.ConstructionType].InventoryRequirements[itemName].MaxStackSize * validPostionCount).ToString();
            if (WorldController.Instance.World.FurnitureJobPrototypes[constructionController.ConstructionType].InventoryRequirements.Count > 1)
            {
                return result += requiredMaterialCount + " " + itemName + "\n";
            }
            else
            {
                return result += requiredMaterialCount + " " + itemName;
            }
        }

        return "furnitureJobPrototypes is null";
    }

    private static Tile GetTileUnderDrag(Vector3 gameObject_Position)
    {
        return WorldController.Instance.GetTileAtWorldCoordinate(gameObject_Position);
    }
}
