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

        World.Current.cbInventoryCreated += OnInventoryCreated;
        foreach (string type in World.Current.InventoryManager.Inventories.Keys)
        {
            foreach (Inventory inventory in World.Current.InventoryManager.Inventories[type])
            {
                OnInventoryCreated(inventory);
            }
        }
    }

    public void OnInventoryCreated(Inventory inv)
    {
        // FIXME: Does not consider multi-tile objects nor rotated objects
        GameObject inventoryGameObject = new GameObject();
        inventoryGameObjectMap.Add(inv, inventoryGameObject);

        inventoryGameObject.name = inv.Type;
        inventoryGameObject.transform.position = new Vector3(inv.Tile.X, inv.Tile.Y, 0);
        inventoryGameObject.transform.SetParent(inventoryParent.transform, true);

        SpriteRenderer spriteRenderer = inventoryGameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteManager.Current.GetSprite("Inventory", inv.Type);
        spriteRenderer.sortingLayerName = "Inventory";

        if (inv.MaxStackSize > 1)
        {
            // This is a stackable object, so let's add a InventoryUI component
            GameObject uiGameObject = Object.Instantiate(inventoryUIPrefab);
            uiGameObject.transform.SetParent(inventoryGameObject.transform);
            uiGameObject.transform.localPosition = Vector3.zero;
            uiGameObject.GetComponentInChildren<Text>().text = inv.StackSize.ToString();
        }

        inv.cbInventoryChanged += OnInventoryChanged;
    }

    private void OnInventoryChanged(Inventory inventory)
    {
        if (inventoryGameObjectMap.ContainsKey(inventory) == false)
        {
            return;
        }

        GameObject inventoryGameObject = inventoryGameObjectMap[inventory];
        if (inventory.StackSize > 0)
        {
            Text text = inventoryGameObject.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = inventory.StackSize.ToString();
            }
        }
        else
        {
            // This stack has gone to zero, so remove the sprite!
            Object.Destroy(inventoryGameObject);
            inventoryGameObjectMap.Remove(inventory);
            inventory.cbInventoryChanged -= OnInventoryChanged;
        }
    }
}
