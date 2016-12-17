using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryGraphicController : MonoBehaviour
{
    private static World world
    {
        get { return WorldController.Instance.World; }
    }

    [SerializeField]
    private GameObject inventoryUIPrefab;

    private Dictionary<Inventory, GameObject> inventoryGameObjectMap;
    private Dictionary<string, Sprite> inventorySprites;

    // Use this for initialization
    private void Start()
    {
        LoadSprites();
        inventoryGameObjectMap = new Dictionary<Inventory, GameObject>();
        world.InventoryCreated += OnInventoryCreated;

        foreach (string inventoryType in world.InventoryManager.Inventories.Keys)
        {
            foreach (Inventory inventory in world.InventoryManager.Inventories[inventoryType])
            {
                OnInventoryCreated(this, new InventoryCreatedEventArgs(inventory));
            }
        }
    }

    private void LoadSprites()
    {
        inventorySprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Inventory/");

        foreach (Sprite s in sprites)
        {
            inventorySprites[s.name] = s;
        }
    }

    public void OnInventoryCreated(object sender, InventoryCreatedEventArgs args)
    {
        GameObject inventoryGameObject = new GameObject();
        inventoryGameObjectMap.Add(args.Inventory, inventoryGameObject);
        inventoryGameObject.name = args.Inventory.Type;
        inventoryGameObject.transform.position = new Vector2(args.Inventory.Tile.X, args.Inventory.Tile.Y);
        inventoryGameObject.transform.SetParent(transform, true);

        SpriteRenderer spriteRenderer = inventoryGameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = inventorySprites[args.Inventory.Type];
        spriteRenderer.sortingLayerName = "Inventory";

        if (args.Inventory.MaxStackSize > 1)
        {
            GameObject uiGameObject = Instantiate(inventoryUIPrefab);
            uiGameObject.transform.SetParent(inventoryGameObject.transform);
            uiGameObject.transform.localPosition = Vector3.zero;
            uiGameObject.GetComponentInChildren<Text>().text = args.Inventory.StackSize.ToString();
        }

        // TODO: Inventory changed event
        //args.Inventory.InventoryChanged += OnCharacterChanged;
    }

    //private void OnCharacterChanged(object sender, CharacterChangedEventArgs args)
    //{
    //    if (inventoryGameObjectMap.ContainsKey(args.Character) == false)
    //    {
    //        Debug.LogError("CharacterGraphicController::OnCharacterChanged: Trying to change visuals for character not in our map.");
    //        return;
    //    }

    //    GameObject characterGameObject = inventoryGameObjectMap[args.Character];
    //    characterGameObject.transform.position = new Vector2(args.Character.X, args.Character.Y);
    //}
}
