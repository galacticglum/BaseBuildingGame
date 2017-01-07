using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class InventoryGraphicController : MonoBehaviour
{
    private static World world
    {
        get { return WorldController.Instance.World; }
    }

    [SerializeField]
	private GameObject inventoryUIPrefab;

	private Dictionary<Inventory, GameObject> inventoryGameObjectMap; 

	// Use this for initialization
    private void Start ()
    {
		inventoryGameObjectMap = new Dictionary<Inventory, GameObject>();
		world.InventoryCreated += OnInventoryCreated;

		foreach(string type in world.InventoryManager.Inventories.Keys)
        {
			foreach(Inventory inventory in world.InventoryManager.Inventories[type])
            {
				world.OnInventoryCreated(new InventoryEventArgs(inventory));
			}
		}
	}

	public void OnInventoryCreated(object sender, InventoryEventArgs args)
    {
		GameObject inventoryGameObject = new GameObject();
		inventoryGameObjectMap.Add(args.Inventory, inventoryGameObject);
		inventoryGameObject.name = args.Inventory.Type;
		inventoryGameObject.transform.position = new Vector3(args.Inventory.Tile.X, args.Inventory.Tile.Y, 0);
		inventoryGameObject.transform.SetParent(transform, true);

		SpriteRenderer spriteRenderer = inventoryGameObject.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = SpriteManager.Current.GetSprite("Inventory", args.Inventory.Type);
		spriteRenderer.sortingLayerName = "Inventory";

		if(args.Inventory.MaxStackSize > 1)
        {
			GameObject uiGameObject = Instantiate(inventoryUIPrefab);
			uiGameObject.transform.SetParent( inventoryGameObject.transform );
			uiGameObject.transform.localPosition = Vector3.zero;
			uiGameObject.GetComponentInChildren<Text>().text = args.Inventory.StackSize.ToString();
		}

        args.Inventory.InventoryChanged += OnInventoryChanged;
	}

    private void OnInventoryChanged(object sender, InventoryEventArgs args)
    {
		if(inventoryGameObjectMap.ContainsKey(args.Inventory) == false)
        {
			Debug.LogError("InventoryGraphicController::OnInventoryChanged: Trying to change visuals for inventory not in our map.");
			return;
		}

		GameObject inventoryGameObject = inventoryGameObjectMap[args.Inventory];
		if(args.Inventory.StackSize > 0)
        {
			Text textObject = inventoryGameObject.GetComponentInChildren<Text>();
			if(textObject != null)
            {
				textObject.text = args.Inventory.StackSize.ToString();
			}
		}
		else
        {
			Destroy(inventoryGameObject);
			inventoryGameObjectMap.Remove(args.Inventory);
            args.Inventory.InventoryChanged -= OnInventoryChanged;
		}

	}
}
