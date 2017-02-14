using UnityEngine;
using UnityEngine.UI;

public class DialogBoxController : MonoBehaviour
{
    public MenuController mouseController;
    public DialogBoxLoadGame dialogBoxLoadGame;
    public DialogBoxSaveGame dialogBoxSaveGame;
    public DialogBoxOptions dialogBoxOptions;
    public DialogBoxSettings dialogBoxSettings;
    public DialogBoxTrade dialogBoxTrade;
    public DialogBoxAreYouSure dialogBoxAreYouSure;
    public DialogBoxQuests dialogBoxQuests;

    private void Awake()
    {
        GameObject Controllers = GameObject.Find("Dialog Boxes");
        mouseController = GameObject.Find("Dialog Boxes").GetComponent<MenuController>();

        GameObject instance = (GameObject)Instantiate(Resources.Load("UI/DB_SaveFile"), Controllers.transform.position, Controllers.transform.rotation,  Controllers.transform);
        instance.name = "Save File";
        dialogBoxSaveGame = instance.GetComponent<DialogBoxSaveGame>();

        instance = (GameObject)Instantiate(Resources.Load("UI/DB_LoadFile"), Controllers.transform.position, Controllers.transform.rotation,  Controllers.transform);
        instance.name = "Load File";
        dialogBoxLoadGame = instance.GetComponent<DialogBoxLoadGame>();

        instance = (GameObject)Instantiate(Resources.Load("UI/DB_Options"), Controllers.transform.position, Controllers.transform.rotation,  Controllers.transform);
        instance.name = "Options";
        dialogBoxOptions = instance.GetComponent<DialogBoxOptions>();

        instance = (GameObject)Instantiate(Resources.Load("UI/DB_Settings"), Controllers.transform.position, Controllers.transform.rotation,  Controllers.transform);
        instance.name = "Settings";
        dialogBoxSettings = instance.GetComponent<DialogBoxSettings>();

        instance = (GameObject)Instantiate(Resources.Load("UI/DB_Trade"), Controllers.transform.position, Controllers.transform.rotation,  Controllers.transform);
        instance.name = "Trade";
        dialogBoxTrade = instance.GetComponent<DialogBoxTrade>();

        instance = (GameObject)Instantiate(Resources.Load("UI/DB_AreYouSure"), Controllers.transform.position, Controllers.transform.rotation,  Controllers.transform);
        instance.name = "Are You Sure";
        dialogBoxAreYouSure = instance.GetComponent<DialogBoxAreYouSure>();

        instance = (GameObject)Instantiate(Resources.Load("UI/DB_Quests"), Controllers.transform.position, Controllers.transform.rotation, Controllers.transform);
        instance.name = "Quests";
        dialogBoxQuests = instance.GetComponent<DialogBoxQuests>();

        ConstructQuestList();
    }

    private void ConstructQuestList()
    {
        Transform layoutRoot = GameObject.Find("Dialog Boxes").transform.parent.GetComponent<Transform>();
        GameObject instance = (GameObject)Instantiate(Resources.Load("UI/QuestsMainScreenBox"), layoutRoot.transform);
        instance.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, -55, 0);

        Toggle pinButton = CreatePinQuestButton();

        pinButton.onValueChanged.AddListener(instance.SetActive);
    }

    private Toggle CreatePinQuestButton()
    {
        GameObject buttonObject = (GameObject)Instantiate(Resources.Load("UI/PinToggleButton"), gameObject.transform);
        buttonObject.name = "ToggleQuestPinButton"; 
        buttonObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, -30, 0);
        return buttonObject.GetComponent<Toggle>();
    }
}