using UnityEngine;
using UnityEngine.UI;

public class InventoryDebugController
{
    public string PendingBuildInventory { get; private set; }
    private GameObject spawnUI;

    public InventoryDebugController()
    {
        GenerateSpawnUI();
        GenerateInventoryButtons();
    }

    private void GenerateSpawnUI()
    {
        spawnUI = new GameObject
        {
            name = "Inventory Debug UI",
            layer = LayerMask.NameToLayer("UI")
        };

        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        spawnUI.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = spawnUI.AddComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0, 0.5f);
        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(0, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0,0);

        spawnUI.AddComponent<Image>();

        VerticalLayoutGroup verticalLayoutGroup = spawnUI.AddComponent<VerticalLayoutGroup>();
        verticalLayoutGroup.childForceExpandWidth = false;
        verticalLayoutGroup.childForceExpandHeight = false;
        verticalLayoutGroup.spacing = 0;

        ContentSizeFitter contentSizeFitter = spawnUI.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
    }

    private void GenerateInventoryButtons()
    {
        foreach(string iName in World.Current.InventoryPrototypes.Keys)
        {
            GameObject inventoryButton = new GameObject
            {
                name = "Button - " + iName,
                layer = LayerMask.NameToLayer("UI")
            };

            inventoryButton.transform.SetParent(spawnUI.transform);
            inventoryButton.AddComponent<Image>();

            Button button = inventoryButton.AddComponent<Button>();
            ColorBlock colorBlock = new ColorBlock
            {
                normalColor = Color.white,
                pressedColor = Color.blue
            };

            button.colors = colorBlock;
            string localName = iName;
            button.onClick.AddListener(() => { OnButtonClick(localName); } );
            CreateTextComponent(inventoryButton, iName);

            LayoutElement layoutElement = inventoryButton.AddComponent<LayoutElement>();
            layoutElement.minWidth = 120;
            layoutElement.minHeight = 20;
        }
    }

    private static void CreateTextComponent(GameObject gameObject, string name)
    {
        GameObject textGameObject = new GameObject
        {
            name = "Text",
            layer = LayerMask.NameToLayer("UI")
        };

        RectTransform rectTransform = textGameObject.AddComponent<RectTransform>();
        rectTransform.SetParent(gameObject.transform);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Text text = textGameObject.AddComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Arial",14);
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.black;
        text.text = name;
    }

    private void OnButtonClick(string name)
    {
        PendingBuildInventory = name;
        WorldController.Instance.MouseController.StartSpawnMode();
    }

    public void SpawnInventory(Tile tile)
    {
        Inventory inventoryChange = new Inventory(PendingBuildInventory, 1);
        if (tile.Furniture != null)
        {
            return; 
        }

        if (tile.Inventory == null || tile.Inventory.Type == PendingBuildInventory)
        {
            World.Current.InventoryManager.Place(tile, inventoryChange);
        }
    }
}