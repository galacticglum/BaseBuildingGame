using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FurnitureMenuManager : MonoBehaviour
{
    public GameObject buildFurnitureButtonPrefab;
    private string lastLanguage;

    private void Start()
    {
        ConstructionController constructionController = WorldController.Instance.ConstructionController;
        foreach (string key in World.Current.FurniturePrototypes.Keys)
        {
            GameObject instance = Instantiate(buildFurnitureButtonPrefab);
            instance.transform.SetParent(transform);

            string id = key;
            instance.name = "Button - Build " + id;

            instance.transform.GetComponentInChildren<TextLocalizer>().FormatValues = new[] { LocalizationTable.GetLocalization(World.Current.FurniturePrototypes[key].LocalizationCode) };

            Button b = instance.GetComponent<Button>();

            b.onClick.AddListener(() => 
            {
                constructionController.BuildFurniture(id);
                gameObject.SetActive(false);
            });

            string furniture = key;
            LocalizationTable.CBLocalizationFilesChanged += () =>
            {
                instance.transform.GetComponentInChildren<TextLocalizer>().FormatValues = new[] { LocalizationTable.GetLocalization(World.Current.FurniturePrototypes[furniture].LocalizationCode) };
            };
        }

        lastLanguage = LocalizationTable.CurrentLanguage;
    }

    private void Update()
    {
        if (lastLanguage == LocalizationTable.CurrentLanguage) return;

        lastLanguage = LocalizationTable.CurrentLanguage;
        TextLocalizer[] localizers = GetComponentsInChildren<TextLocalizer>();
        for (int i = 0; i < localizers.Length; i++)
        {
            localizers[i].UpdateText(new[]
            {
                LocalizationTable.GetLocalization(World.Current.FurniturePrototypes.ElementAt(i).Value.GetName())
            });
        }
    }
}
