using UnityEngine;
using UnityEngine.UI;

public class FurnitureMenuManager : MonoBehaviour
{
    [SerializeField]
    private GameObject buildFurnitureButtonPrefab;
    private ConstructionController constructionController;

    private void Start()
    {
        if (buildFurnitureButtonPrefab == null) return;

        constructionController = FindObjectOfType<ConstructionController>();

        foreach (string furnitureType in PrototypeManager.Furnitures.Keys)
        {
            string furnitureName = PrototypeManager.Furnitures[furnitureType].Name;
            GameObject buttonGameObject = Instantiate(buildFurnitureButtonPrefab);
            buttonGameObject.transform.SetParent(transform);

            buttonGameObject.name = "Button - Build " + furnitureName;
            buttonGameObject.GetComponentInChildren<Text>().text = "Build " + furnitureName;

            Button button = buttonGameObject.GetComponent<Button>();

            string type = furnitureType;
            button.onClick.AddListener(delegate
            {
                constructionController.BuildFurniture(type);
            });
        } 
    }
}
