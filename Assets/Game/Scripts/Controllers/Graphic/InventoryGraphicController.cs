using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class InventoryGraphicController
{
    private readonly GameObject inventoryUIPrefab;
    private readonly GameObject inventoryParent;
    private readonly Dictionary<Inventory, GameObject> inventoryGameObjectMap;

    // Use this for initialization
    public InventoryGraphicController(GameObject inventoryUI)
    {
        inventoryUIPrefab = inventoryUI;
        inventoryParent = new GameObject("Inventory");
        inventoryGameObjectMap = new Dictionary<Inventory, GameObject>();

        World.Current.InventoryCreated += OnInventoryCreated;
        foreach (string type in World.Current.InventoryManager.Inventories.Keys)
        {
            foreach (Inventory inventory in World.Current.InventoryManager.Inventories[type])
            {
                OnInventoryCreated(this, new InventoryEventArgs(inventory));
            }
        }
    }

    public void OnInventoryCreated(object sender, InventoryEventArgs args)
    {
        // FIXME: Does not consider multi-tile objects nor rotated objects
        GameObject inventoryGameObject = new GameObject();
        inventoryGameObjectMap.Add(args.Inventory, inventoryGameObject);

        inventoryGameObject.name = args.Inventory.Type;
        inventoryGameObject.transform.position = new Vector3(args.Inventory.Tile.X, args.Inventory.Tile.Y, 0);
        inventoryGameObject.transform.SetParent(inventoryParent.transform, true);

        SpriteRenderer spriteRenderer = inventoryGameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteManager.Current.GetSprite("Inventory", args.Inventory.Type);
        spriteRenderer.sortingLayerName = "Inventory";

        if (args.Inventory.MaxStackSize > 1)
        {
            // This is a stackable object, so let's add a InventoryUI component
            GameObject uiGameObject = Object.Instantiate(inventoryUIPrefab);
            uiGameObject.transform.SetParent(inventoryGameObject.transform);
            uiGameObject.transform.localPosition = Vector3.zero;
            uiGameObject.GetComponentInChildren<Text>().text = args.Inventory.StackSize.ToString();
        }

        args.Inventory.InventoryChanged += OnInventoryChanged;
    }

    private void OnInventoryChanged(object sender, InventoryEventArgs args)
    {
        if (inventoryGameObjectMap.ContainsKey(args.Inventory) == false)
        {
            return;
        }

        GameObject inventoryGameObject = inventoryGameObjectMap[args.Inventory];
        if (args.Inventory.StackSize > 0)
        {
            Text text = inventoryGameObject.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = args.Inventory.StackSize.ToString();
            }
        }
        else
        {
            // This stack has gone to zero, so remove the sprite!
            Object.Destroy(inventoryGameObject);
            inventoryGameObjectMap.Remove(args.Inventory);
            args.Inventory.InventoryChanged -= OnInventoryChanged;
        }
    }
}
