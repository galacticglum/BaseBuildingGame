using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public GameObject constructorMenu;
    public GameObject furnitureMenu;
    public GameObject floorMenu;
    
    public Button buttonConstructor;
    public Button buttonWorld;
    public Button buttonWork;
    public Button buttonOptions;
    public Button buttonSettings;
    public Button buttonQuests;

    private DialogBoxController dialogBoxController;

    // Use this for initialization.
    private void Start()
    {
        dialogBoxController = GameObject.Find("Dialog Boxes").GetComponent<DialogBoxController>();
        buttonConstructor.onClick.AddListener(OnButtonConstruction);
        buttonWorld.onClick.AddListener(OnButtonWorld);
        buttonWork.onClick.AddListener(OnButtonWork);
        buttonOptions.onClick.AddListener(OnButtonOptions);
        buttonSettings.onClick.AddListener(OnButtonSettings);

        buttonQuests = CreateButton("menu_quests");
        buttonQuests.onClick.AddListener(OnButtonQuests);

        DeactivateAll();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            DeactivateAll();
        }
    }

    private Button CreateButton(string text)
    {
        GameObject buttonQuestGameObject = (GameObject)Instantiate(Resources.Load("UI/MenuButton"), gameObject.transform);
        buttonQuestGameObject.name = "Button - " + text;
        Text buttonText = buttonQuestGameObject.transform.GetChild(0).GetComponent<Text>();
        buttonText.text = text;
        buttonText.GetComponent<TextLocalizer>().Text = buttonText;
        buttonText.GetComponent<TextLocalizer>().UpdateText();
    
        return buttonQuestGameObject.GetComponent<Button>();
    }

    // Deactivates All Menus.
    public void DeactivateAll()
    {
        constructorMenu.SetActive(false);
        DeactivateSubs();
    }

    // Deactivates any sub menu of the constrution options.
    public void DeactivateSubs()
    {
        furnitureMenu.SetActive(false);
        floorMenu.SetActive(false);
    }

    // Toggles whether menu is active.
    public void ToggleMenu(GameObject menu)
    {
        menu.SetActive(!menu.activeSelf);
    }

    public void OnButtonConstruction()
    {
        if (constructorMenu.activeSelf)
        {
            DeactivateAll();
        } 
        else 
        { 
            DeactivateAll();
            constructorMenu.SetActive(true);
        }
    }

    public void OnButtonWork()
    {
        DeactivateAll();
    }

    public void OnButtonWorld()
    {
        if (!WorldController.Instance.IsModal)
        {
            DeactivateAll();
        }
    }

    public void OnButtonQuests()
    {
        if (WorldController.Instance.IsModal) return;

        DeactivateAll();
        dialogBoxController.dialogBoxQuests.Show();
    }

    public void OnButtonOptions()
    {
        if (WorldController.Instance.IsModal) return;

        DeactivateAll();
        dialogBoxController.dialogBoxOptions.Show();
    }

    public void OnButtonSettings()
    {
        if (WorldController.Instance.IsModal) return;

        DeactivateAll();
        dialogBoxController.dialogBoxSettings.Show();
    }
}
