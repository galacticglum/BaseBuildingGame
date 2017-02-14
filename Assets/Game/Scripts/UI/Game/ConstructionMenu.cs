using UnityEngine;
using UnityEngine.UI;

public class ConstructionMenu : MonoBehaviour
{
    public GameObject furnitureMenu;
    public GameObject floorMenu;
    public Button buttonFloors;
    public Button buttonFurniture;
    public Button buttonDeconstruction;

    private ConstructionController constructionController;

    private void Start()
    {
        constructionController = WorldController.Instance.ConstructionController;

        buttonDeconstruction.onClick.AddListener(OnClickDeconstruct);
        buttonFloors.onClick.AddListener(OnClickFloors);
        buttonFurniture.onClick.AddListener(OnClickFurniture);
    }

    public void OnClickDeconstruct()
    {
        DeactivateSubs();
        constructionController.Deconstruct();
    }

    public void OnClickFloors()
    {
        DeactivateSubs();
        ToggleMenu(floorMenu);
    }

    public void OnClickFurniture()
    {
        DeactivateSubs();
        ToggleMenu(furnitureMenu);
    }

    public void DeactivateSubs()
    {
        furnitureMenu.SetActive(false);
        floorMenu.SetActive(false);
    }

    public void ToggleMenu(GameObject menu)
    {
        menu.SetActive(!menu.activeSelf);
    }
}