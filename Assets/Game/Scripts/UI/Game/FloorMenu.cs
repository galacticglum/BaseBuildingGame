using UnityEngine;
using UnityEngine.UI;

public class FloorMenu : MonoBehaviour
{
    public GameObject buildFurnitureButtonPrefab;

    private string lastLanguage;
    private TileType[] tileTypes;

    private void Start()
    {

        tileTypes = TileType.GetTileTypes();
        ConstructionController constructionController = WorldController.Instance.ConstructionController;

        foreach (TileType type in tileTypes)
        {
            GameObject instance = Instantiate(buildFurnitureButtonPrefab);
            instance.transform.SetParent(transform);

            TileType tileType = type;
            instance.name = "Button - Build " + tileType.Type;

            if (type == TileType.Empty)
            {
                instance.name = "Button - Remove " + tileType.Type;
                instance.GetComponentInChildren<TextLocalizer>().DefaultText = "remove";
            }

            instance.transform.GetComponentInChildren<TextLocalizer>().FormatValues = new[] { LocalizationTable.GetLocalization(tileType.LocalizationCode) };

            Button button = instance.GetComponent<Button>();
            button.onClick.AddListener(() => constructionController.BuildTile(tileType));
        }

        lastLanguage = LocalizationTable.CurrentLanguage;
    }

    // Update is called once per frame.
    private void Update()
    {
        if (lastLanguage == LocalizationTable.CurrentLanguage) return;

        lastLanguage = LocalizationTable.CurrentLanguage;
        TextLocalizer[] localizers = GetComponentsInChildren<TextLocalizer>();
        for (int i = 0; i < localizers.Length; i++)
        {
            localizers[i].UpdateText(new[] {LocalizationTable.GetLocalization(tileTypes[i].LocalizationCode)});
        }
    }
}